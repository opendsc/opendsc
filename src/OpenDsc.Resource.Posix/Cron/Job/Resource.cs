// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Posix.Cron.Job;

[DscResource("OpenDsc.Posix.Cron/Job", "0.1.0", Description = "Manage POSIX cron jobs", Tags = ["posix", "cron", "schedule", "job", "task"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(SecurityException), Description = "Access denied")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>, IExportable<Schema>
{
    private const string JobMarker = "# OpenDsc Job: ";

    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        return JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema instance)
    {
        var user = GetEffectiveUser(instance);
        var isSystemScope = instance.Scope == CronScope.System;

        if (isSystemScope && string.IsNullOrWhiteSpace(instance.FileName))
        {
            throw new ArgumentException("FileName is required when _scope is 'System'.");
        }

        var crontabContent = isSystemScope
            ? ReadSystemCrontab(instance.FileName!)
            : ReadUserCrontab(user);

        var job = FindJob(crontabContent, instance.Name);

        if (job == null)
        {
            return new Schema
            {
                Name = instance.Name,
                Scope = instance.Scope,
                User = instance.Scope == CronScope.User ? user : null,
                FileName = instance.Scope == CronScope.System ? instance.FileName : null,
                Exist = false
            };
        }

        return new Schema
        {
            Name = instance.Name,
            Scope = instance.Scope,
            User = instance.Scope == CronScope.User ? user : null,
            FileName = instance.Scope == CronScope.System ? instance.FileName : null,
            Schedule = job.Schedule,
            Command = job.Command,
            RunAsUser = job.RunAsUser,
            Comment = job.Comment
        };
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        if (string.IsNullOrWhiteSpace(instance.Schedule))
        {
            throw new ArgumentException("Schedule is required.");
        }

        if (string.IsNullOrWhiteSpace(instance.Command))
        {
            throw new ArgumentException("Command is required.");
        }

        var user = GetEffectiveUser(instance);
        var isSystemScope = instance.Scope == CronScope.System;

        if (isSystemScope && string.IsNullOrWhiteSpace(instance.FileName))
        {
            throw new ArgumentException("FileName is required when _scope is 'System'.");
        }

        if (isSystemScope && string.IsNullOrWhiteSpace(instance.RunAsUser))
        {
            instance.RunAsUser = "root";
        }

        var crontabContent = isSystemScope
            ? ReadSystemCrontab(instance.FileName!)
            : ReadUserCrontab(user);

        var updatedContent = UpsertJob(crontabContent, instance);

        if (isSystemScope)
        {
            WriteSystemCrontab(instance.FileName!, updatedContent);
        }
        else
        {
            WriteUserCrontab(user, updatedContent);
        }

        return null;
    }

    public void Delete(Schema instance)
    {
        var user = GetEffectiveUser(instance);
        var isSystemScope = instance.Scope == CronScope.System;

        if (isSystemScope && string.IsNullOrWhiteSpace(instance.FileName))
        {
            throw new ArgumentException("FileName is required when _scope is 'System'.");
        }

        var crontabContent = isSystemScope
            ? ReadSystemCrontab(instance.FileName!)
            : ReadUserCrontab(user);

        var updatedContent = RemoveJob(crontabContent, instance.Name);

        if (isSystemScope)
        {
            WriteSystemCrontab(instance.FileName!, updatedContent);
        }
        else
        {
            WriteUserCrontab(user, updatedContent);
        }
    }

    public IEnumerable<Schema> Export()
    {
        var currentUser = System.Environment.UserName;
        var userCrontab = ReadUserCrontab(currentUser);
        foreach (var job in ParseJobs(userCrontab, CronScope.User, currentUser, null))
        {
            yield return job;
        }

        if (Directory.Exists("/etc/cron.d"))
        {
            foreach (var file in Directory.GetFiles("/etc/cron.d"))
            {
                var fileName = Path.GetFileName(file);
                try
                {
                    var content = File.ReadAllText(file);
                    foreach (var job in ParseJobs(content, CronScope.System, null, fileName))
                    {
                        yield return job;
                    }
                }
                catch
                {
                }
            }
        }
    }

    private static string GetEffectiveUser(Schema instance)
    {
        if (instance.Scope == CronScope.System)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(instance.User)
            ? Environment.UserName
            : instance.User;
    }

    private static string ReadUserCrontab(string user)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "crontab",
            Arguments = $"-l -u {user}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start crontab process.");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0 && !error.Contains("no crontab for"))
        {
            throw new InvalidOperationException($"Failed to read crontab for user {user}: {error}");
        }

        return output;
    }

    private static void WriteUserCrontab(string user, string content)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "crontab",
            Arguments = $"-u {user} -",
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start crontab process.");
        }

        process.StandardInput.Write(content);
        process.StandardInput.Close();

        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to write crontab for user {user}: {error}");
        }
    }

    private static string ReadSystemCrontab(string fileName)
    {
        var path = Path.Combine("/etc/cron.d", fileName);
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        return File.ReadAllText(path);
    }

    private static void WriteSystemCrontab(string fileName, string content)
    {
        var path = Path.Combine("/etc/cron.d", fileName);
        File.WriteAllText(path, content);
    }

    private static CronJobInfo? FindJob(string crontabContent, string jobName)
    {
        var lines = crontabContent.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith(JobMarker + jobName, StringComparison.Ordinal))
            {
                string? userComment = null;
                if (i > 0 && lines[i - 1].StartsWith("#") && !lines[i - 1].StartsWith(JobMarker))
                {
                    userComment = lines[i - 1].TrimStart('#').Trim();
                }

                if (i + 1 < lines.Length)
                {
                    var jobLine = lines[i + 1];
                    var parsed = ParseJobLine(jobLine);
                    if (parsed != null)
                    {
                        parsed.Comment = userComment;
                        return parsed;
                    }
                }
            }
        }

        return null;
    }

    private static CronJobInfo? ParseJobLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
        {
            return null;
        }

        var parts = Regex.Split(line.Trim(), @"\s+", RegexOptions.None, TimeSpan.FromSeconds(1));

        if (line.StartsWith('@'))
        {
            if (parts.Length < 2)
            {
                return null;
            }

            var hasUser = parts.Length > 2 && !parts[1].Contains('/');
            return new CronJobInfo
            {
                Schedule = parts[0],
                RunAsUser = hasUser ? parts[1] : null,
                Command = hasUser ? string.Join(" ", parts.Skip(2)) : string.Join(" ", parts.Skip(1))
            };
        }

        if (parts.Length < 6)
        {
            return new CronJobInfo
            {
                Schedule = string.Join(" ", parts.Take(5)),
                Command = parts.Length > 5 ? string.Join(" ", parts.Skip(5)) : string.Empty
            };
        }

        var potentialUser = parts[5];
        var isUser = !potentialUser.Contains('/') && !potentialUser.Contains('.');

        if (isUser && parts.Length > 6)
        {
            return new CronJobInfo
            {
                Schedule = string.Join(" ", parts.Take(5)),
                RunAsUser = potentialUser,
                Command = string.Join(" ", parts.Skip(6))
            };
        }

        return new CronJobInfo
        {
            Schedule = string.Join(" ", parts.Take(5)),
            Command = string.Join(" ", parts.Skip(5))
        };
    }

    private static string UpsertJob(string crontabContent, Schema job)
    {
        var lines = new List<string>(crontabContent.Split('\n'));
        var marker = JobMarker + job.Name;
        int markerIndex = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith(marker, StringComparison.Ordinal))
            {
                markerIndex = i;
                break;
            }
        }

        var newJobLines = new List<string>();

        if (!string.IsNullOrWhiteSpace(job.Comment))
        {
            newJobLines.Add($"# {job.Comment}");
        }

        newJobLines.Add(marker);

        var jobLine = new StringBuilder();
        jobLine.Append(job.Schedule);

        if (job.Scope == CronScope.System && !string.IsNullOrWhiteSpace(job.RunAsUser))
        {
            jobLine.Append($" {job.RunAsUser}");
        }

        jobLine.Append($" {job.Command}");
        newJobLines.Add(jobLine.ToString());

        if (markerIndex != -1)
        {
            int removeStart = markerIndex;
            int removeCount = 1;

            if (markerIndex > 0 && lines[markerIndex - 1].StartsWith("#") && !lines[markerIndex - 1].StartsWith(JobMarker))
            {
                removeStart = markerIndex - 1;
                removeCount++;
            }

            if (markerIndex + 1 < lines.Count)
            {
                removeCount++;
            }

            lines.RemoveRange(removeStart, removeCount);
            lines.InsertRange(removeStart, newJobLines);
        }
        else
        {
            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
            {
                lines.Add(string.Empty);
            }

            lines.AddRange(newJobLines);
        }

        return string.Join("\n", lines);
    }

    private static string RemoveJob(string crontabContent, string jobName)
    {
        var lines = new List<string>(crontabContent.Split('\n'));
        var marker = JobMarker + jobName;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith(marker, StringComparison.Ordinal))
            {
                int removeStart = i;
                int removeCount = 1;

                if (i > 0 && lines[i - 1].StartsWith("#") && !lines[i - 1].StartsWith(JobMarker))
                {
                    removeStart = i - 1;
                    removeCount++;
                }

                if (i + 1 < lines.Count)
                {
                    removeCount++;
                }

                lines.RemoveRange(removeStart, removeCount);
                break;
            }
        }

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return string.Join("\n", lines);
    }

    private static IEnumerable<Schema> ParseJobs(string crontabContent, CronScope scope, string? user, string? fileName)
    {
        var lines = crontabContent.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith(JobMarker, StringComparison.Ordinal))
            {
                var jobName = line[JobMarker.Length..].Trim();

                string? userComment = null;
                if (i > 0 && lines[i - 1].StartsWith("#") && !lines[i - 1].StartsWith(JobMarker))
                {
                    userComment = lines[i - 1].TrimStart('#').Trim();
                }

                if (i + 1 < lines.Length)
                {
                    var jobLine = lines[i + 1];
                    var parsed = ParseJobLine(jobLine);
                    if (parsed != null)
                    {
                        yield return new Schema
                        {
                            Name = jobName,
                            Scope = scope,
                            User = scope == CronScope.User ? user : null,
                            FileName = scope == CronScope.System ? fileName : null,
                            Schedule = parsed.Schedule,
                            Command = parsed.Command,
                            RunAsUser = parsed.RunAsUser,
                            Comment = userComment
                        };
                    }
                }
            }
        }
    }

    private sealed class CronJobInfo
    {
        public string Schedule { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string? RunAsUser { get; set; }
        public string? Comment { get; set; }
    }
}
