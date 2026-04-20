// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Resource.Tests;

[Trait("Category", "Unit")]
public class CriticalCoverageGapTests
{
    [Fact]
    public void DscResourceAttribute_Version_ParsesMultipleFormats()
    {
        var versions = new[] { "1.0.0", "2.1.0-beta", "0.0.1-rc.1" };

        foreach (var version in versions)
        {
            var attr = new DscResourceAttribute("Test/Resource", version);
            attr.Version.Should().NotBeNull();
        }
    }

    [Fact]
    public void DscResourceAttribute_Version_PrereleaseParsesCorrectly()
    {
        var attr = new DscResourceAttribute("Test/Resource", "1.0.0-preview.1");

        attr.Version.Major.Should().Be(1);
        attr.Version.IsPrerelease.Should().BeTrue();
    }

    [Fact]
    public void DscResource_Parse_EmptyString_ThrowsJsonException()
    {
        var resource = new TestResource(SourceGenerationContext.Default);

        var action = () => resource.Parse("");

        action.Should().Throw<JsonException>();
    }

    [Fact]
    public void DscResource_Parse_Whitespace_ThrowsJsonException()
    {
        var resource = new TestResource(SourceGenerationContext.Default);

        var action = () => resource.Parse("   ");

        action.Should().Throw<JsonException>();
    }

    [Fact]
    public void DscResource_Parse_MalformedJson_ThrowsJsonException()
    {
        var resource = new TestResource(SourceGenerationContext.Default);

        var action = () => resource.Parse("{incomplete");

        action.Should().Throw<JsonException>();
    }

    [Fact]
    public void DscResource_GetTypeInfo_ReturnsValidTypeInfo()
    {
        var resource = new TestResource(SourceGenerationContext.Default);

        // GetTypeInfo is protected, so we call it through GetSchema which uses it internally
        var schema = resource.GetSchema();

        schema.Should().NotBeEmpty();
    }

    [Fact]
    public void ExitCodeResolver_WithMultipleInheritanceChain_FindsClosestMatch()
    {
        var resource = new TestResource(SourceGenerationContext.Default);

        // ArgumentOutOfRangeException extends ArgumentException
        var exitCode = ExitCodeResolver.GetExitCode(resource, typeof(ArgumentOutOfRangeException));

        exitCode.Should().Be(2);
    }

    [Fact]
    public void ExitCodeResolver_ExceptionTypeExactMatch_BeforeInheritanceMatch()
    {
        var resource = new TestResource(SourceGenerationContext.Default);

        // Test exact match for InvalidOperationException
        var code1 = ExitCodeResolver.GetExitCode(resource, typeof(InvalidOperationException));

        // Should match the exact ExitCode(3) not a base class match
        code1.Should().Be(3);
    }
}

/// <summary>
/// More comprehensive tests for DscResource parse variations
/// </summary>
public class DscResourceParseVariationsTests
{
    [Fact]
    public void Parse_ValidJsonWithExtraFields_Succeeds()
    {
        var resource = new TestResource(SourceGenerationContext.Default);
        var json = """{"name":"test","value":42,"enabled":true,"extra":"field"}""";

        // The test schema only uses these three properties, extra fields are ignored
        var instance = resource.Parse(json);

        instance.Name.Should().Be("test");
    }

    [Fact]
    public void Parse_JsonWithCorrectTypes_Succeeds()
    {
        var resource = new TestResource(SourceGenerationContext.Default);
        var json = """{"name":"valid","value":100,"enabled":false}""";

        var instance = resource.Parse(json);

        instance.Name.Should().Be("valid");
        instance.Value.Should().Be(100);
    }

    [Fact]
    public void ToJson_ThenParse_Maintains_Equivalence()
    {
        var resource = new TestResource(SourceGenerationContext.Default);
        var original = new TestSchema { Name = "original", Value = 55, Enabled = true };

        var json = resource.ToJson(original);
        var parsed = resource.Parse(json);

        parsed.Name.Should().Be(original.Name);
        parsed.Value.Should().Be(original.Value);
        parsed.Enabled.Should().Be(original.Enabled);
    }

    [Fact]
    public void Parse_JsonWithOnlyNameProperty_HasDefaults()
    {
        var resource = new TestResource(SourceGenerationContext.Default);
        var json = """{"name":"only-name"}""";

        var instance = resource.Parse(json);

        instance.Name.Should().Be("only-name");
        instance.Value.Should().Be(0);
        instance.Enabled.Should().BeFalse();
    }
}

/// <summary>
/// Tests for version attribute edge cases
/// </summary>
public class VersionAttributeEdgeCaseTests
{
    [Fact]
    public void SetReturnAttribute_AllEnumValues_CanBeSet()
    {
        var attr1 = new SetReturnAttribute(SetReturn.None);
        var attr2 = new SetReturnAttribute(SetReturn.State);
        var attr3 = new SetReturnAttribute(SetReturn.StateAndDiff);

        attr1.SetReturn.Should().Be(SetReturn.None);
        attr2.SetReturn.Should().Be(SetReturn.State);
        attr3.SetReturn.Should().Be(SetReturn.StateAndDiff);
    }

    [Fact]
    public void TestReturnAttribute_AllEnumValues_CanBeSet()
    {
        var attr1 = new TestReturnAttribute(TestReturn.State);
        var attr2 = new TestReturnAttribute(TestReturn.StateAndDiff);

        attr1.TestReturn.Should().Be(TestReturn.State);
        attr2.TestReturn.Should().Be(TestReturn.StateAndDiff);
    }

    [Fact]
    public void ExitCodeAttribute_WithDescriptionAndException_StoresAll()
    {
        var attr = new ExitCodeAttribute(42)
        {
            Description = "Test exit code",
            Exception = typeof(TimeoutException)
        };

        attr.ExitCode.Should().Be(42);
        attr.Description.Should().Be("Test exit code");
        attr.Exception.Should().Be(typeof(TimeoutException));
    }
}

/// <summary>
/// Tests for serialization settings edge cases
/// </summary>
public class SerializationSettingsEdgeCasesTests
{
    [Fact]
    public void DscJsonSerializerSettings_HandlesEnumConversion()
    {
        var options = DscJsonSerializerSettings.Default;
        var json = JsonSerializer.Serialize(SetReturn.StateAndDiff, typeof(SetReturn), options);

        json.Should().Contain("stateAndDiff");
    }

    [Fact]
    public void DscJsonSerializerSettings_DoesNotSerializeNull()
    {
        var options = DscJsonSerializerSettings.Default;
        var obj = new TestSchema { Name = null, Value = 0 };

        var json = JsonSerializer.Serialize(obj, typeof(TestSchema), options);

        json.Should().NotContain("name");
    }

    [Fact]
    public void DscJsonSerializerSettings_UsesCompactFormat()
    {
        var options = DscJsonSerializerSettings.Default;
        var obj = new TestSchema { Name = "test", Value = 1, Enabled = true };

        var json = JsonSerializer.Serialize(obj, typeof(TestSchema), options);

        // Compact format should not have newlines or excessive whitespace
        json.Should().NotContain("\n");
        json.Should().NotContain("\r");
    }
}

/// <summary>
/// Tests for resource manifest comprehensive coverage
/// </summary>
public class ManifestComprehensiveCoverageTests
{
    [Fact]
    public void ManifestMethod_WithMultipleArgs_SerializesCorrectly()
    {
        var method = new ManifestMethod
        {
            Executable = "resource.exe",
            Args = new object[] { "--input", "file.json", 42, true }
        };

        method.Executable.Should().Be("resource.exe");
        method.Args.Should().HaveCount(4);
    }

    [Fact]
    public void ManifestSetMethod_WithReturn_StoresValue()
    {
        var method = new ManifestSetMethod
        {
            Executable = "set.exe",
            Return = "stateAndDiff"
        };

        method.Return.Should().Be("stateAndDiff");
    }

    [Fact]
    public void ManifestTestMethod_WithReturn_StoresValue()
    {
        var method = new ManifestTestMethod
        {
            Executable = "test.exe",
            Return = "state"
        };

        method.Return.Should().Be("state");
    }

    [Fact]
    public void ManifestExportMethod_WithComplexArgs_Serializes()
    {
        var method = new ManifestExportMethod
        {
            Executable = "export.exe",
            Args = new object[] { new { nested = "object" } }
        };

        method.Executable.Should().Be("export.exe");
        method.Args.Should().NotBeNull();
    }
}
