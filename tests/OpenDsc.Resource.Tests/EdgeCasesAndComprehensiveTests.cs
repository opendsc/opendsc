// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Resource.Tests;

[Trait("Category", "Unit")]
public class JsonInputArgTests
{
    [Fact]
    public void Arg_CanBeSet()
    {
        var arg = new JsonInputArg { Arg = "--input" };

        arg.Arg.Should().Be("--input");
    }

    [Fact]
    public void Arg_DefaultsToEmpty()
    {
        var arg = new JsonInputArg();

        arg.Arg.Should().Be(string.Empty);
    }

    [Fact]
    public void Arg_CanBeSetToString()
    {
        var arg = new JsonInputArg { Arg = "json-arg" };

        arg.Arg.Should().Be("json-arg");
    }

    [Fact]
    public void Mandatory_CanBeSetToTrue()
    {
        var arg = new JsonInputArg { Mandatory = true };

        arg.Mandatory.Should().BeTrue();
    }

    [Fact]
    public void Mandatory_CanBeSetToFalse()
    {
        var arg = new JsonInputArg { Mandatory = false };

        arg.Mandatory.Should().BeFalse();
    }

    [Fact]
    public void Mandatory_DefaultsToNull()
    {
        var arg = new JsonInputArg();

        arg.Mandatory.Should().BeNull();
    }

    [Fact]
    public void CanSetBothProperties()
    {
        var arg = new JsonInputArg { Arg = "--config", Mandatory = true };

        arg.Arg.Should().Be("--config");
        arg.Mandatory.Should().BeTrue();
    }
}

/// <summary>
/// Tests for edge cases and error paths in DscResource
/// </summary>
public class DscResourceEdgeCasesTests
{
    [Fact]
    public void Parse_WithInvalidJsonStructure_ThrowsJsonException()
    {
        var resource = new TestResource(SourceGenerationContext.Default);
        var invalidJson = """{"name":123,"value":"not a number"}""";

        var action = () => resource.Parse(invalidJson);

        action.Should().Throw<JsonException>();
    }

    [Fact]
    public void Parse_WithEmptyJson_ThrowsException()
    {
        var resource = new TestResource(SourceGenerationContext.Default);

        var action = () => resource.Parse("");

        action.Should().Throw<Exception>();
    }

    [Fact]
    public void Parse_WithOnlyBraces_ReturnsInstanceWithDefaults()
    {
        var resource = new TestResource(SourceGenerationContext.Default);
        var json = """{}""";

        var instance = resource.Parse(json);

        instance.Should().NotBeNull();
    }

    [Fact]
    public void ToJson_WithAllNullValues_OmitsNullProperties()
    {
        var resource = new TestResource(SourceGenerationContext.Default);
        var instance = new TestSchema { Name = null, Value = 0, Enabled = false };

        var json = resource.ToJson(instance);

        // Name should not appear in output due to DefaultIgnoreCondition.WhenWritingNull
        json.Should().NotContain("name");
    }

    [Fact]
    public void GetSchema_CalledMultipleTimes_ReturnsConsistentResult()
    {
        var resource = new TestResource(SourceGenerationContext.Default);

        var schema1 = resource.GetSchema();
        var schema2 = resource.GetSchema();

        schema1.Should().Be(schema2);
    }

    [Fact]
    public void ToJson_WithZeroValue_IncludesZeroInJson()
    {
        var resource = new TestResource(SourceGenerationContext.Default);
        var instance = new TestSchema { Value = 0, Enabled = true };

        var json = resource.ToJson(instance);

        json.Should().Contain("\"value\":0");
    }

    [Fact]
    public void ToJson_WithFalseValue_IncludesFalseInJson()
    {
        var resource = new TestResource(SourceGenerationContext.Default);
        var instance = new TestSchema { Name = "test", Enabled = false };

        var json = resource.ToJson(instance);

        json.Should().Contain("\"enabled\":false");
    }
}

/// <summary>
/// Tests for edge cases in ExitCodeResolver
/// </summary>
public class ExitCodeResolverEdgeCasesTests
{
    private readonly TestResource _resource = new(SourceGenerationContext.Default);

    [Fact]
    public void GetExitCode_WithInheritedExceptionOfClosestMatch_ReturnsCorrectCode()
    {
        var exceptionType = typeof(ArgumentNullException);

        var exitCode = ExitCodeResolver.GetExitCode(_resource, exceptionType);

        exitCode.Should().Be(2);
    }

    [Fact]
    public void GetExitCode_MultipleCallsWithSameException_ReturnsSameCode()
    {
        var exceptionType = typeof(ArgumentException);

        var code1 = ExitCodeResolver.GetExitCode(_resource, exceptionType);
        var code2 = ExitCodeResolver.GetExitCode(_resource, exceptionType);

        code1.Should().Be(code2);
    }
}

/// <summary>
/// Tests for DscResourceManifest edge cases
/// </summary>
public class DscResourceManifestEdgeCasesTests
{
    [Fact]
    public void EmbeddedSchema_CanBeSet()
    {
        var jsonString = """{"type":"object"}""";
        var element = JsonDocument.Parse(jsonString).RootElement;
        var schema = new ManifestSchema { Embedded = element };
        var manifest = new DscResourceManifest { EmbeddedSchema = schema };

        manifest.EmbeddedSchema.Should().NotBeNull();
        manifest.EmbeddedSchema.Embedded.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void ExitCodes_CanBeEmptyDictionary()
    {
        var manifest = new DscResourceManifest { ExitCodes = new() };

        manifest.ExitCodes.Should().BeEmpty();
    }

    [Fact]
    public void Tags_CanBeEmpty()
    {
        var manifest = new DscResourceManifest { Tags = Array.Empty<string>() };

        manifest.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AllPropertiesCanBeSetSimultaneously()
    {
        var exitCodes = new Dictionary<string, string> { { "0", "Success" } };
        var tags = new[] { "Tag1" };

        var manifest = new DscResourceManifest
        {
            Type = "Test/Resource",
            Version = "1.0.0",
            Description = "Test",
            Tags = tags,
            ExitCodes = exitCodes
        };

        manifest.Type.Should().Be("Test/Resource");
        manifest.Version.Should().Be("1.0.0");
        manifest.Description.Should().Be("Test");
        manifest.Tags.Should().Equal(tags);
        manifest.ExitCodes.Should().Equal(exitCodes);
    }
}

/// <summary>
/// Tests for DscResourceAttribute format validation
/// </summary>
public class DscResourceAttributeValidationTests
{
    [Fact]
    public void Constructor_WithTrailingSlash_ThrowsArgumentException()
    {
        var action = () => new DscResourceAttribute("Owner/");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithLeadingSlash_ThrowsArgumentException()
    {
        var action = () => new DscResourceAttribute("/Resource");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithSpecialCharacters_ThrowsArgumentException()
    {
        var action = () => new DscResourceAttribute("Owner@/Resource");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithTooManyDots_ThrowsArgumentException()
    {
        var action = () => new DscResourceAttribute("Owner.Group.Area.Extra/Resource");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithVersionString_CanParseSemanticVersions()
    {
        var attr = new DscResourceAttribute("Test/Resource", "1.2.3-beta+build");

        attr.Version.Major.Should().Be(1);
        attr.Version.Minor.Should().Be(2);
        attr.Version.Patch.Should().Be(3);
    }

    [Fact]
    public void Constructor_WithValidComplexVersion_Succeeds()
    {
        var attr = new DscResourceAttribute("Test/Resource", "0.0.1");

        attr.Version.ToString().Should().StartWith("0.0.1");
    }
}
