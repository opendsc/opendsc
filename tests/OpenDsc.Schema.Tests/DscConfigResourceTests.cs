// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Nodes;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Schema.Tests;

[Trait("Category", "Unit")]
public class DscConfigResourceTests
{
    [Fact]
    public void DscConfigResource_DefaultValues_ShouldBeCorrect()
    {
        var resource = new DscConfigResource();

        resource.Type.Should().Be(string.Empty);
        resource.Name.Should().Be(string.Empty);
        resource.Properties.Should().BeEmpty();
        resource.DependsOn.Should().BeNull();
    }

    [Fact]
    public void DscConfigResource_WithType_ShouldStoreType()
    {
        var resource = new DscConfigResource { Type = "Microsoft/File" };

        resource.Type.Should().Be("Microsoft/File");
    }

    [Fact]
    public void DscConfigResource_WithName_ShouldStoreName()
    {
        var resource = new DscConfigResource { Name = "FileResource" };

        resource.Name.Should().Be("FileResource");
    }

    [Fact]
    public void DscConfigResource_WithProperties_ShouldStoreProperties()
    {
        var properties = new Dictionary<string, JsonNode?>
        {
            { "Path", JsonValue.Create("/etc/hosts") },
            { "Content", JsonValue.Create("sample content") }
        };

        var resource = new DscConfigResource { Properties = properties };

        resource.Properties.Should().HaveCount(2);
        resource.Properties["Path"]!.GetValue<string>().Should().Be("/etc/hosts");
        resource.Properties["Content"]!.GetValue<string>().Should().Be("sample content");
    }

    [Fact]
    public void DscConfigResource_WithNullPropertyValue_ShouldStoreNull()
    {
        var properties = new Dictionary<string, JsonNode?>
        {
            { "Optional", null }
        };

        var resource = new DscConfigResource { Properties = properties };

        resource.Properties.Should().HaveCount(1);
        resource.Properties["Optional"].Should().BeNull();
    }

    [Fact]
    public void DscConfigResource_WithDependsOn_ShouldStoreDependencies()
    {
        var dependencies = new[] { "[resourceId('Microsoft/Resource1', 'Res1')]", "[resourceId('Microsoft/Resource2', 'Res2')]" };
        var resource = new DscConfigResource { DependsOn = dependencies };

        resource.DependsOn.Should().HaveCount(2);
        resource.DependsOn![0].Should().Be("[resourceId('Microsoft/Resource1', 'Res1')]");
        resource.DependsOn[1].Should().Be("[resourceId('Microsoft/Resource2', 'Res2')]");
    }

    [Fact]
    public void DscConfigResource_WithEmptyDependsOn_ShouldStoreEmptyList()
    {
        var resource = new DscConfigResource { DependsOn = [] };

        resource.DependsOn.Should().BeEmpty();
    }

    [Fact]
    public void DscConfigResource_WithAllProperties_ShouldStoreAll()
    {
        var properties = new Dictionary<string, JsonNode?> { { "Path", JsonValue.Create("/test") } };
        var dependencies = new[] { "[resourceId('Microsoft/Dep', 'D1')]" };

        var resource = new DscConfigResource
        {
            Type = "Microsoft/File",
            Name = "TestFile",
            Properties = properties,
            DependsOn = dependencies
        };

        resource.Type.Should().Be("Microsoft/File");
        resource.Name.Should().Be("TestFile");
        resource.Properties.Should().HaveCount(1);
        resource.DependsOn.Should().HaveCount(1);
    }

}
