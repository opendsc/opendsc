// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Resource.Tests;

[Trait("Category", "Unit")]
public class FinalBranchCoverageTests
{
    [Fact]
    public void ExitCodeResolver_ResourceWithoutAttributes_ThrowsInvalidOperation()
    {
        var resourceWithoutAttrs = new ResourceWithoutExitCodes(SourceGenerationContext.Default);

        var action = () => ExitCodeResolver.GetExitCode(resourceWithoutAttrs, typeof(Exception));

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DscResourceAttribute_InvalidVersionString_ThrowsArgumentException()
    {
        var action = () => new DscResourceAttribute("Test/Resource", "not-a-version");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DscResourceAttribute_InvalidVersionFormat_ThrowsArgumentException()
    {
        var versions = new[] { "1.2.3.4.5", "abc", "1.x.3", "", "v1.0.0" };

        foreach (var version in versions)
        {
            var action = () => new DscResourceAttribute("Test/Resource", version);
            action.Should().Throw<ArgumentException>();
        }
    }

    [Fact]
    public void DscResource_ParseFromValidJsonContext_RoundTrips()
    {
        var resource = new TestResource(SourceGenerationContext.Default);
        var original = new TestSchema { Name = "complex", Value = 999, Enabled = false };

        var json = resource.ToJson(original);
        var reparsed = resource.Parse(json);

        reparsed.Name.Should().Be(original.Name);
        reparsed.Value.Should().Be(original.Value);
        reparsed.Enabled.Should().Be(original.Enabled);
    }

    [Fact]
    public void ExitCodeResolver_GetExitCode_WithNullExceptionAttribute_SkipsNull()
    {
        var resource = new TestResource(SourceGenerationContext.Default);

        // Even though TestResource has ExitCode(1) without exception type,
        // it should find the matching specific codes first
        var code = ExitCodeResolver.GetExitCode(resource, typeof(ArgumentException));

        code.Should().Be(2);
    }
}

/// <summary>
/// Test resource without ExitCode attributes to test error paths
/// </summary>
public class ResourceWithoutExitCodes : DscResource<TestSchema>
{
    public ResourceWithoutExitCodes(JsonSerializerContext context) : base(context)
    {
    }
}

/// <summary>
/// Tests for comprehensive method signatures and overloads
/// </summary>
public class MethodSignatureComprehensiveTests
{
    [Fact]
    public void TestResource_ImplementsAllRequiredInterfaces()
    {
        var resource = new TestResource(SourceGenerationContext.Default);

        resource.Should().BeAssignableTo<IDscResource<TestSchema>>();
        resource.Should().BeAssignableTo<IGettable<TestSchema>>();
        resource.Should().BeAssignableTo<ISettable<TestSchema>>();
        resource.Should().BeAssignableTo<ITestable<TestSchema>>();
        resource.Should().BeAssignableTo<IDeletable<TestSchema>>();
    }

    [Fact]
    public void DscResource_GetSchema_FormatIsValid()
    {
        var resource = new TestResource(SourceGenerationContext.Default);

        var schema = resource.GetSchema();
        var doc = JsonDocument.Parse(schema);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        doc.RootElement.TryGetProperty("properties", out _).Should().BeTrue();
    }

    [Fact]
    public void ManifestMethod_ToString_Works()
    {
        var method = new ManifestMethod { Executable = "test.exe" };

        var str = method.ToString();

        str.Should().NotBeNull();
    }

    [Fact]
    public void TestSchema_DefaultConstructor_Works()
    {
        var schema = new TestSchema();

        schema.Should().NotBeNull();
        schema.Name.Should().BeNull();
        schema.Value.Should().Be(0);
        schema.Enabled.Should().BeFalse();
    }
}

/// <summary>
/// Tests for attribute inheritance and multiple applications
/// </summary>
public class AttributeInheritanceTests
{
    [Fact]
    public void Class_WithMultipleExitCodeAttributes_HasAll()
    {
        var attrs = typeof(TestResource).GetCustomAttributes<ExitCodeAttribute>();

        attrs.Should().HaveCount(3);
    }

    [Fact]
    public void Class_WithDscResourceAttribute_CanRetrieveAttribute()
    {
        var attr = typeof(TestResource).GetCustomAttribute<DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr.Type.Should().Be("OpenDsc.Test/TestResource");
    }

    [Fact]
    public void SetReturn_EnumAllValuesCoverage()
    {
        var values = (SetReturn[])typeof(SetReturn).GetEnumValues();

        values.Should().Contain(SetReturn.None);
        values.Should().Contain(SetReturn.State);
        values.Should().Contain(SetReturn.StateAndDiff);
    }

    [Fact]
    public void TestReturn_EnumAllValuesCoverage()
    {
        var values = (TestReturn[])typeof(TestReturn).GetEnumValues();

        values.Should().Contain(TestReturn.State);
        values.Should().Contain(TestReturn.StateAndDiff);
    }
}

/// <summary>
/// Tests for JSON schema export options edge cases
/// </summary>
public class JsonSchemaExportEdgeCasesTests
{
    [Fact]
    public void DscJsonSchemaExporterOptions_Default_ImmutableAcrossCalls()
    {
        var opts1 = DscJsonSchemaExporterOptions.Default;
        var opts2 = DscJsonSchemaExporterOptions.Default;

        opts1.TreatNullObliviousAsNonNullable.Should().Be(opts2.TreatNullObliviousAsNonNullable);
    }

    [Fact]
    public void DscJsonSerializerSettings_Default_CreatesValidOptions()
    {
        var opts = DscJsonSerializerSettings.Default;

        opts.Should().NotBeNull();
        opts.PropertyNamingPolicy.Should().NotBeNull();
    }
}

/// <summary>
/// Tests for specific type format validation
/// </summary>
public class TypeFormatValidationTests
{
    [Fact]
    public void DscResourceAttribute_SingleCharacterParts_Valid()
    {
        var attr = new DscResourceAttribute("A/B");

        attr.Type.Should().Be("A/B");
    }

    [Fact]
    public void DscResourceAttribute_Numeric_ValidInType()
    {
        var attr = new DscResourceAttribute("Test123/Resource456");

        attr.Type.Should().Be("Test123/Resource456");
    }

    [Fact]
    public void DscResourceAttribute_Underscore_ValidInType()
    {
        var attr = new DscResourceAttribute("Test_Owner/Test_Resource");

        attr.Type.Should().Be("Test_Owner/Test_Resource");
    }

    [Theory]
    [InlineData("Owner/Name")]
    [InlineData("Owner.Group/Name")]
    [InlineData("Owner.Group.Area/Name")]
    public void DscResourceAttribute_ValidFormats_AllSucceed(string type)
    {
        var attr = new DscResourceAttribute(type);

        attr.Type.Should().Be(type);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Owner/Group/Name")]
    [InlineData("/Name")]
    [InlineData("Owner.Group.Area.Extra/Name")]
    [InlineData("Owner-Group/Name")]
    public void DscResourceAttribute_InvalidFormats_AllFail(string type)
    {
        var action = () => new DscResourceAttribute(type);

        action.Should().Throw<ArgumentException>();
    }
}
