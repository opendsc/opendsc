// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using OpenDsc.Resource;

namespace TestResource.NonAot;

[DscResource("OpenDsc.Test/NonAotFile", Description = "Non-AOT test resource for file existence.", Tags = ["test", "file", "non-aot"], SetReturn = SetReturn.StateAndDiff, TestReturn = TestReturn.StateAndDiff)]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(IOException), Description = "I/O error")]
[ExitCode(4, Exception = typeof(DirectoryNotFoundException), Description = "Directory not found")]
[ExitCode(5, Exception = typeof(UnauthorizedAccessException), Description = "Access denied")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context),
    IGettable<Schema>,
    ISettable<Schema>,
    IDeletable<Schema>,
    ITestable<Schema>,
    IExportable<Schema>
{

    public Schema Get(Schema instance)
    {
        // Support triggering specific exceptions for exit code testing
        if (instance.Path.Contains("trigger-json-exception", StringComparison.OrdinalIgnoreCase))
        {
            throw new JsonException("Simulated JSON parsing error");
        }
        if (instance.Path.Contains("trigger-generic-exception", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Simulated generic error");
        }
        if (instance.Path.Contains("trigger-io-exception", StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("Simulated I/O error");
        }
        if (instance.Path.Contains("trigger-directory-not-found", StringComparison.OrdinalIgnoreCase))
        {
            throw new DirectoryNotFoundException("Simulated directory not found");
        }
        if (instance.Path.Contains("trigger-unauthorized-access", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Simulated access denied");
        }

        var exists = File.Exists(instance.Path);
        return new Schema
        {
            Path = instance.Path,
            Exist = exists ? null : false
        };
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        var desiredExist = instance.Exist ?? true;
        var currentState = Get(instance);
        var currentExist = currentState.Exist ?? true;

        SetResult<Schema> result;
        if (desiredExist == currentExist)
        {
            result = new SetResult<Schema>(currentState) { ChangedProperties = [] };
        }
        else
        {
            var changedProperties = new HashSet<string>();

            if (desiredExist)
            {
                File.WriteAllText(instance.Path, string.Empty);
                changedProperties.Add("_exist");
            }
            else
            {
                if (File.Exists(instance.Path))
                {
                    File.Delete(instance.Path);
                    changedProperties.Add("_exist");
                }
            }

            var actualState = Get(instance);
            result = new SetResult<Schema>(actualState)
            {
                ChangedProperties = changedProperties
            };
        }

        return result;
    }

    public void Delete(Schema instance)
    {
        if (File.Exists(instance.Path))
        {
            File.Delete(instance.Path);
        }
    }

    public TestResult<Schema> Test(Schema instance)
    {
        var actual = Get(instance);

        var desiredExist = instance.Exist ?? true;
        var actualExist = actual.Exist ?? true;

        actual.InDesiredState = desiredExist == actualExist;

        var result = new TestResult<Schema>(actual)
        {
            DifferingProperties = []
        };

        if (desiredExist != actualExist)
        {
            result.DifferingProperties.Add("_exist");
        }

        return result;
    }

    public IEnumerable<Schema> Export()
    {
        var searchPath = Environment.GetEnvironmentVariable("TEST_EXPORT_DIR") ?? Directory.GetCurrentDirectory();
        var files = Directory.GetFiles(searchPath, "test-*.txt", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            yield return new Schema
            {
                Path = file,
                Exist = null
            };
        }
    }
}
