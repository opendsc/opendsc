// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using NuGet.Versioning;

using Xunit;

namespace OpenDsc.Resource.Tests;

/// <summary>
/// Tests for DscResourceAttribute additional variants and edge cases
/// </summary>
[Trait("Category", "Unit")]
public class DscResourceAttributeExtendedTests
{
    [Fact]
    public void Constructor_WithOwnerOnly_IsValid()
    {
        var attr = new DscResourceAttribute("Owner/Resource");

        attr.Type.Should().Be("Owner/Resource");
    }

    [Fact]
    public void Constructor_WithOwnerAndGroup_IsValid()
    {
        var attr = new DscResourceAttribute("Owner.Group/Resource");

        attr.Type.Should().Be("Owner.Group/Resource");
    }

    [Fact]
    public void Constructor_WithOwnerGroupAndArea_IsValid()
    {
        var attr = new DscResourceAttribute("Owner.Group.Area/Resource");

        attr.Type.Should().Be("Owner.Group.Area/Resource");
    }

    [Fact]
    public void Constructor_WithInvalidFormat_ThrowsArgumentException()
    {
        var action = () => new DscResourceAttribute("InvalidFormat");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithMissingSlash_ThrowsArgumentException()
    {
        var action = () => new DscResourceAttribute("OwnerResource");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithTwoArguments_SetsTypeAndVersion()
    {
        var attr = new DscResourceAttribute("Test/Resource", "1.0.0");

        attr.Type.Should().Be("Test/Resource");
        attr.Version.ToString().Should().StartWith("1.0.0");
    }

    [Fact]
    public void Constructor_WithTwoArguments_InvalidVersion_ThrowsArgumentException()
    {
        var action = () => new DscResourceAttribute("Test/Resource", "invalid");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Version_RetrievesFromAssemblyWhenNotExplicitlySet()
    {
        var attr = new DscResourceAttribute("Test/Resource");

        var version = attr.Version;

        version.Should().NotBeNull();
        version.Major.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Version_CanBeSetAfterConstruction()
    {
        var attr = new DscResourceAttribute("Test/Resource");
        var newVersion = new SemanticVersion(2, 0, 0);

        attr.Version = newVersion;

        attr.Version.Should().Be(newVersion);
    }

    [Fact]
    public void ManifestSchema_DefaultsToCorrectUri()
    {
        var attr = new DscResourceAttribute("Test/Resource");

        attr.ManifestSchema.Should().Be("https://aka.ms/dsc/schemas/v3/bundled/resource/manifest.json");
    }

    [Fact]
    public void ManifestSchema_CanBeChanged()
    {
        var attr = new DscResourceAttribute("Test/Resource")
        {
            ManifestSchema = "https://custom.schema/manifest.json"
        };

        attr.ManifestSchema.Should().Be("https://custom.schema/manifest.json");
    }

    [Fact]
    public void Constructor_WithComplexTypeFormat_IsValid()
    {
        var attr = new DscResourceAttribute("Microsoft.Windows.DSC/Service");

        attr.Type.Should().Be("Microsoft.Windows.DSC/Service");
    }
}

public class SetReturnAttributeTests
{
    [Fact]
    public void Constructor_WithNone_SetsSetReturn()
    {
        var attr = new SetReturnAttribute(SetReturn.None);

        attr.SetReturn.Should().Be(SetReturn.None);
    }

    [Fact]
    public void Constructor_WithState_SetsSetReturn()
    {
        var attr = new SetReturnAttribute(SetReturn.State);

        attr.SetReturn.Should().Be(SetReturn.State);
    }

    [Fact]
    public void Constructor_WithStateAndDiff_SetsSetReturn()
    {
        var attr = new SetReturnAttribute(SetReturn.StateAndDiff);

        attr.SetReturn.Should().Be(SetReturn.StateAndDiff);
    }
}

public class TestReturnAttributeTests
{
    [Fact]
    public void Constructor_WithState_SetsTestReturn()
    {
        var attr = new TestReturnAttribute(TestReturn.State);

        attr.TestReturn.Should().Be(TestReturn.State);
    }

    [Fact]
    public void Constructor_WithStateAndDiff_SetsTestReturn()
    {
        var attr = new TestReturnAttribute(TestReturn.StateAndDiff);

        attr.TestReturn.Should().Be(TestReturn.StateAndDiff);
    }
}

public class DscResourceConstructorVariantsTests
{
    [Fact]
    public void DscResourceWithJsonSerializerOptions_CanBeInstantiated()
    {
        var options = DscJsonSerializerSettings.Default;
        var resource = new TestResourceWithOptions(options);

        resource.Should().NotBeNull();
    }

    [Fact]
    public void DscResourceWithJsonSerializerOptions_CanGetSchema()
    {
        var options = DscJsonSerializerSettings.Default;
        var resource = new TestResourceWithOptions(options);

        var schema = resource.GetSchema();

        schema.Should().NotBeEmpty();
    }

    [Fact]
    public void DscResourceWithJsonSerializerOptions_CanParseJson()
    {
        var options = DscJsonSerializerSettings.Default;
        var resource = new TestResourceWithOptions(options);
        var json = """{"name":"test","value":42,"enabled":true}""";

        var instance = resource.Parse(json);

        instance.Should().NotBeNull();
        instance.Name.Should().Be("test");
    }

    [Fact]
    public void DscResourceWithJsonSerializerOptions_CanSerializeToJson()
    {
        var options = DscJsonSerializerSettings.Default;
        var resource = new TestResourceWithOptions(options);
        var instance = new TestSchema { Name = "test", Value = 42 };

        var json = resource.ToJson(instance);

        json.Should().NotBeEmpty();
        json.Should().Contain("test");
    }
}

/// <summary>
/// Test resource using JsonSerializerOptions constructor variant
/// </summary>
public class TestResourceWithOptions : DscResource<TestSchema>
{
    public TestResourceWithOptions(JsonSerializerOptions options) : base(options)
    {
    }
}

public class ManifestMethodArgsTests
{
    [Fact]
    public void Args_CanBeSetAndRetrieved()
    {
        var args = new object[] { "arg1", 42, true };
        var method = new ManifestMethod { Executable = "test.exe", Args = args };

        method.Args.Should().Equal(args);
    }

    [Fact]
    public void Args_CanContainMixedTypes()
    {
        var args = new object[] { "string", 123, 45.67, true, new { key = "value" } };
        var method = new ManifestMethod { Args = args };

        method.Args.Length.Should().Be(5);
    }

    [Fact]
    public void ManifestSetMethodArgs_InheritsFromBase()
    {
        var args = new object[] { "arg1" };
        var method = new ManifestSetMethod { Executable = "test.exe", Args = args };

        method.Args.Should().Equal(args);
    }

    [Fact]
    public void ManifestTestMethodArgs_InheritsFromBase()
    {
        var args = new object[] { "arg1" };
        var method = new ManifestTestMethod { Executable = "test.exe", Args = args };

        method.Args.Should().Equal(args);
    }

    [Fact]
    public void ManifestExportMethodArgs_CanBeSet()
    {
        var args = new object[] { "arg1" };
        var method = new ManifestExportMethod { Executable = "test.exe", Args = args };

        method.Args.Should().Equal(args);
    }
}
