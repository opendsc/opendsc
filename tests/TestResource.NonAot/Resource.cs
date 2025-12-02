// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenDsc.Resource;

namespace TestResource.NonAot
{
    [DscResource("OpenDsc.Test/NonAotFile", Description = "Non-AOT test resource for file existence.", Tags = ["test", "file", "non-aot"], SetReturn = SetReturn.StateAndDiff, TestReturn = TestReturn.StateAndDiff)]
    [ExitCode(0, Description = "Success")]
    [ExitCode(1, Description = "Invalid parameter")]
    [ExitCode(2, Exception = typeof(Exception), Description = "Generic error")]
    [ExitCode(3, Exception = typeof(JsonException), Description = "Invalid JSON")]
    public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context),
        IGettable<Schema>,
        ISettable<Schema>,
        IDeletable<Schema>,
        ITestable<Schema>,
        IExportable<Schema>
    {

    public Schema Get(Schema instance)
    {
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
            result = new SetResult<Schema>(currentState) { ChangedProperties = new HashSet<string>() };
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

        var result = new TestResult<Schema>(actual);

        result.DifferingProperties = new HashSet<string>();

        if (desiredExist != actualExist)
        {
            result.DifferingProperties.Add("_exist");
        }

        return result;
    }

    public IEnumerable<Schema> Export()
    {
        var tempPath = Path.GetTempPath();
        var files = Directory.GetFiles(tempPath, "test-*.txt", SearchOption.AllDirectories);

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
}
