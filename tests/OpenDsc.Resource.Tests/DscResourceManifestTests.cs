// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Resource.Tests;

[Trait("Category", "Unit")]
public class DscResourceManifestTests
{
    [Fact]
    public void Constructor_DefaultsSchemaToStandardUri()
    {
        var manifest = new DscResourceManifest();

        manifest.Schema.Should().Be("https://aka.ms/dsc/schemas/v3/bundled/resource/manifest.json");
    }

    [Fact]
    public void Constructor_DefaultsTypeToEmpty()
    {
        var manifest = new DscResourceManifest();

        manifest.Type.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_DefaultsVersionToEmpty()
    {
        var manifest = new DscResourceManifest();

        manifest.Version.Should().BeEmpty();
    }

    [Fact]
    public void Type_CanBeSet()
    {
        var manifest = new DscResourceManifest { Type = "OpenDsc.Test/TestResource" };

        manifest.Type.Should().Be("OpenDsc.Test/TestResource");
    }

    [Fact]
    public void Version_CanBeSet()
    {
        var manifest = new DscResourceManifest { Version = "1.0.0" };

        manifest.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void Description_DefaultsToNull()
    {
        var manifest = new DscResourceManifest();

        manifest.Description.Should().BeNull();
    }

    [Fact]
    public void Description_CanBeSet()
    {
        var manifest = new DscResourceManifest { Description = "Test description" };

        manifest.Description.Should().Be("Test description");
    }

    [Fact]
    public void Tags_DefaultsToNull()
    {
        var manifest = new DscResourceManifest();

        manifest.Tags.Should().BeNull();
    }

    [Fact]
    public void Tags_CanBeSet()
    {
        var tags = new[] { "tag1", "tag2" };
        var manifest = new DscResourceManifest { Tags = tags };

        manifest.Tags.Should().Equal(tags);
    }

    [Fact]
    public void ExitCodes_DefaultsToNull()
    {
        var manifest = new DscResourceManifest();

        manifest.ExitCodes.Should().BeNull();
    }

    [Fact]
    public void ExitCodes_CanBeSet()
    {
        var exitCodes = new Dictionary<string, string> { { "0", "Success" }, { "1", "Error" } };
        var manifest = new DscResourceManifest { ExitCodes = exitCodes };

        manifest.ExitCodes.Should().Equal(exitCodes);
    }

    [Fact]
    public void Get_DefaultsToNull()
    {
        var manifest = new DscResourceManifest();

        manifest.Get.Should().BeNull();
    }

    [Fact]
    public void Get_CanBeSet()
    {
        var method = new ManifestMethod { Executable = "test.exe" };
        var manifest = new DscResourceManifest { Get = method };

        manifest.Get.Should().Be(method);
    }

    [Fact]
    public void Set_DefaultsToNull()
    {
        var manifest = new DscResourceManifest();

        manifest.Set.Should().BeNull();
    }

    [Fact]
    public void Test_DefaultsToNull()
    {
        var manifest = new DscResourceManifest();

        manifest.Test.Should().BeNull();
    }

    [Fact]
    public void Delete_DefaultsToNull()
    {
        var manifest = new DscResourceManifest();

        manifest.Delete.Should().BeNull();
    }

    [Fact]
    public void Export_DefaultsToNull()
    {
        var manifest = new DscResourceManifest();

        manifest.Export.Should().BeNull();
    }
}

public class ManifestMethodTests
{
    [Fact]
    public void Executable_DefaultsToEmpty()
    {
        var method = new ManifestMethod();

        method.Executable.Should().BeEmpty();
    }

    [Fact]
    public void Executable_CanBeSet()
    {
        var method = new ManifestMethod { Executable = "test.exe" };

        method.Executable.Should().Be("test.exe");
    }
}

public class ManifestSchemaTests
{
    [Fact]
    public void Embedded_CanBeSetToJsonElement()
    {
        var jsonString = """{"type":"object","properties":{"name":{"type":"string"}}}""";
        var element = JsonDocument.Parse(jsonString).RootElement;

        var schema = new ManifestSchema { Embedded = element };

        schema.Embedded.ValueKind.Should().Be(JsonValueKind.Object);
    }
}

public class ManifestSetMethodTests
{
    [Fact]
    public void Return_DefaultsToNull()
    {
        var method = new ManifestSetMethod();

        method.Return.Should().BeNull();
    }

    [Fact]
    public void WhatIfReturn_DefaultsToNull()
    {
        var method = new ManifestSetMethod();

        method.WhatIfReturn.Should().BeNull();
    }

    [Fact]
    public void WhatIfReturn_CanBeSetToState()
    {
        var method = new ManifestSetMethod { WhatIfReturn = "state" };

        method.WhatIfReturn.Should().Be("state");
    }

    [Fact]
    public void WhatIfReturn_SerializesAsWhatIfReturns()
    {
        var method = new ManifestSetMethod { Executable = "test.exe", WhatIfReturn = "state" };

        var json = JsonSerializer.Serialize(method);

        json.Should().Contain("\"whatIfReturns\":\"state\"");
    }

    [Fact]
    public void Return_CanBeSetToState()
    {
        var method = new ManifestSetMethod { Return = "state" };

        method.Return.Should().Be("state");
    }

    [Fact]
    public void Return_CanBeSetToStateAndDiff()
    {
        var method = new ManifestSetMethod { Return = "stateAndDiff" };

        method.Return.Should().Be("stateAndDiff");
    }

    [Fact]
    public void Inherits_ExecutableProperty()
    {
        var method = new ManifestSetMethod { Executable = "test.exe" };

        method.Executable.Should().Be("test.exe");
    }
}

public class ManifestTestMethodTests
{
    [Fact]
    public void Return_DefaultsToNull()
    {
        var method = new ManifestTestMethod();

        method.Return.Should().BeNull();
    }

    [Fact]
    public void Return_CanBeSetToState()
    {
        var method = new ManifestTestMethod { Return = "state" };

        method.Return.Should().Be("state");
    }

    [Fact]
    public void Return_CanBeSetToStateAndDiff()
    {
        var method = new ManifestTestMethod { Return = "stateAndDiff" };

        method.Return.Should().Be("stateAndDiff");
    }

    [Fact]
    public void Inherits_ExecutableProperty()
    {
        var method = new ManifestTestMethod { Executable = "test.exe" };

        method.Executable.Should().Be("test.exe");
    }
}

public class WhatIfArgTests
{
    [Fact]
    public void Arg_DefaultsToEmpty()
    {
        var arg = new WhatIfArg();

        arg.Arg.Should().BeEmpty();
    }

    [Fact]
    public void Arg_CanBeSet()
    {
        var arg = new WhatIfArg { Arg = "--what-if" };

        arg.Arg.Should().Be("--what-if");
    }

    [Fact]
    public void Arg_SerializesAsWhatIfArg()
    {
        var arg = new WhatIfArg { Arg = "--what-if" };

        var json = JsonSerializer.Serialize(arg);

        json.Should().Be("""{"whatIfArg":"--what-if"}""");
    }
}

public class DeleteResultTests
{
    [Fact]
    public void Metadata_DefaultsToNull()
    {
        var result = new DeleteResult();

        result.Metadata.Should().BeNull();
    }

    [Fact]
    public void Metadata_SerializesWithUnderscorePrefix()
    {
        var result = new DeleteResult
        {
            Metadata = new DeleteWhatIfMetadata { WhatIf = ["Would delete item"] }
        };

        var json = JsonSerializer.Serialize(result);

        json.Should().Be("""{"_metadata":{"whatIf":["Would delete item"]}}""");
    }

    [Fact]
    public void RoundTrips_ThroughJson()
    {
        var json = """{"_metadata":{"whatIf":["message"]}}""";

        var result = JsonSerializer.Deserialize<DeleteResult>(json);

        result.Should().NotBeNull();
        result!.Metadata.Should().NotBeNull();
        result.Metadata!.WhatIf.Should().Equal("message");
    }
}

public class ManifestExportMethodTests
{
    [Fact]
    public void Executable_DefaultsToEmpty()
    {
        var method = new ManifestExportMethod();

        method.Executable.Should().BeEmpty();
    }

    [Fact]
    public void Executable_CanBeSet()
    {
        var method = new ManifestExportMethod { Executable = "test.exe" };

        method.Executable.Should().Be("test.exe");
    }

    [Fact]
    public void Args_DefaultsToNull()
    {
        var method = new ManifestExportMethod();

        method.Args.Should().BeNull();
    }

    [Fact]
    public void Args_CanBeSet()
    {
        var args = new object[] { "arg1", "arg2" };
        var method = new ManifestExportMethod { Args = args };

        method.Args.Should().Equal(args);
    }
}
