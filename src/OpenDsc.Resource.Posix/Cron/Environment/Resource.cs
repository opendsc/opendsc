// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Posix.Cron.Environment;

[DscResource("OpenDsc.Posix.Cron/Environment", "0.1.0", Description = "Manage environment variables in POSIX crontab files", Tags = ["posix", "cron", "environment"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(SecurityException), Description = "Access denied")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>, IExportable<Schema>
{
    private const string EnvStartMarker = "# OpenDsc Environment - Start";
    private const string EnvEndMarker = "# OpenDsc Environment - End";

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

        var variables = ExtractEnvironmentVariables(crontabContent);

        if (variables.Count == 0)
        {
            return new Schema
            {
                Scope = instance.Scope,
                User = instance.Scope == CronScope.User ? user : null,
                FileName = instance.Scope == CronScope.System ? instance.FileName : null,
                Exist = false
            };
        }

        return new Schema
        {
            Scope = instance.Scope,
            User = instance.Scope == CronScope.User ? user : null,
            FileName = instance.Scope == CronScope.System ? instance.FileName : null,
            Variables = variables
        };
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        if (instance.Variables == null || instance.Variables.Count == 0)
        {
            throw new ArgumentException("Variables dictionary cannot be empty when setting environment.");
        }

        var user = GetEffectiveUser(instance);
        var isSystemScope = instance.Scope == CronScope.System;

        if (isSystemScope && string.IsNullOrWhiteSpace(instance.FileName))
        {
            throw new ArgumentException("FileName is required when _scope is 'System'.");
        }

        var crontabContent = isSystemScope
            ? ReadSystemCrontab(instance.FileName!)
            : ReadUserCrontab(user);

        var updatedContent = UpsertEnvironmentVariables(crontabContent, instance.Variables);

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

        var updatedContent = RemoveEnvironmentVariables(crontabContent);

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
        var userVars = ExtractEnvironmentVariables(userCrontab);

        if (userVars.Count > 0)
        {
            yield return new Schema
            {
                Scope = CronScope.User,
                User = currentUser,
                Variables = userVars
            };
        }

        if (Directory.Exists("/etc/cron.d"))
        {
            foreach (var file in Directory.GetFiles("/etc/cron.d"))
            {
                var fileName = Path.GetFileName(file);
                try
                {
                    var content = File.ReadAllText(file);
                    var vars = ExtractEnvironmentVariables(content);

                    if (vars.Count > 0)
                    {
                        yield return new Schema
                        {
                            Scope = CronScope.System,
                            FileName = fileName,
                            Variables = vars
                        };
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
            ? System.Environment.UserName
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

    private static Dictionary<string, string> ExtractEnvironmentVariables(string crontabContent)
    {
        var variables = new Dictionary<string, string>();
        var lines = crontabContent.Split('\n');
        bool inManagedSection = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed == EnvStartMarker)
            {
                inManagedSection = true;
                continue;
            }

            if (trimmed == EnvEndMarker)
            {
                break;
            }

            if (inManagedSection && !string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith('#'))
            {
                var equalsIndex = trimmed.IndexOf('=');
                if (equalsIndex > 0)
                {
                    var key = trimmed[..equalsIndex].Trim();
                    var value = trimmed[(equalsIndex + 1)..].Trim();
                    variables[key] = value;
                }
            }
        }

        return variables;
    }

    private static string UpsertEnvironmentVariables(string crontabContent, Dictionary<string, string> variables)
    {
        var lines = new List<string>(crontabContent.Split('\n'));
        int startIndex = -1;
        int endIndex = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim() == EnvStartMarker)
            {
                startIndex = i;
            }
            else if (lines[i].Trim() == EnvEndMarker)
            {
                endIndex = i;
                break;
            }
        }

        var envLines = new List<string>
        {
            EnvStartMarker
        };

        foreach (var kvp in variables)
        {
            envLines.Add($"{kvp.Key}={kvp.Value}");
        }

        envLines.Add(EnvEndMarker);

        if (startIndex != -1 && endIndex != -1)
        {
            lines.RemoveRange(startIndex, endIndex - startIndex + 1);
            lines.InsertRange(startIndex, envLines);
        }
        else
        {
            var insertIndex = 0;
            while (insertIndex < lines.Count && (string.IsNullOrWhiteSpace(lines[insertIndex]) || lines[insertIndex].TrimStart().StartsWith('#')))
            {
                insertIndex++;
            }

            lines.InsertRange(insertIndex, envLines);

            if (insertIndex < lines.Count)
            {
                lines.Insert(insertIndex + envLines.Count, string.Empty);
            }
        }

        return string.Join("\n", lines);
    }

    private static string RemoveEnvironmentVariables(string crontabContent)
    {
        var lines = new List<string>(crontabContent.Split('\n'));
        int startIndex = -1;
        int endIndex = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim() == EnvStartMarker)
            {
                startIndex = i;
            }
            else if (lines[i].Trim() == EnvEndMarker)
            {
                endIndex = i;
                break;
            }
        }

        if (startIndex != -1 && endIndex != -1)
        {
            int removeCount = endIndex - startIndex + 1;

            if (startIndex + removeCount < lines.Count && string.IsNullOrWhiteSpace(lines[startIndex + removeCount]))
            {
                removeCount++;
            }

            lines.RemoveRange(startIndex, removeCount);
        }

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return string.Join("\n", lines);
    }
}
