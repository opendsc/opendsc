// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Nodes;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Schema.Tests;

[Trait("Category", "Unit")]
public class DscConfigDocumentTests
{
    [Fact]
    public void DscConfigDocument_DefaultValues_ShouldBeCorrect()
    {
        var document = new DscConfigDocument();

        document.Schema.Should().Be("https://aka.ms/dsc/schemas/v3/bundled/config/document.json");
        document.Resources.Should().BeEmpty();
    }

    [Fact]
    public void DscConfigDocument_WithCustomSchema_ShouldStoreSchema()
    {
        const string customSchema = "https://example.com/custom-schema.json";
        var document = new DscConfigDocument { Schema = customSchema };

        document.Schema.Should().Be(customSchema);
    }

    [Fact]
    public void DscConfigDocument_WithResources_ShouldStoreResources()
    {
        var resources = new List<DscConfigResource>
        {
            new()
            {
                Type = "Microsoft/File",
                Name = "FileResource",
                Properties = new Dictionary<string, JsonNode?>()
            }
        };

        var document = new DscConfigDocument { Resources = resources };

        document.Resources.Should().HaveCount(1);
        document.Resources[0].Type.Should().Be("Microsoft/File");
        document.Resources[0].Name.Should().Be("FileResource");
    }

    [Fact]
    public void DscConfigDocument_WithMultipleResources_ShouldStoreAll()
    {
        var resources = new List<DscConfigResource>
        {
            new() { Type = "Microsoft/Resource1", Name = "Resource1", Properties = new Dictionary<string, JsonNode?>() },
            new() { Type = "Microsoft/Resource2", Name = "Resource2", Properties = new Dictionary<string, JsonNode?>() },
            new() { Type = "Microsoft/Resource3", Name = "Resource3", Properties = new Dictionary<string, JsonNode?>() }
        };

        var document = new DscConfigDocument { Resources = resources };

        document.Resources.Should().HaveCount(3);
        document.Resources[0].Name.Should().Be("Resource1");
        document.Resources[1].Name.Should().Be("Resource2");
        document.Resources[2].Name.Should().Be("Resource3");
    }

    [Fact]
    public void DscConfigDocument_WithEmptyResourceList_ShouldBeValid()
    {
        var document = new DscConfigDocument { Resources = [] };

        document.Resources.Should().BeEmpty();
    }

    [Fact]
    public void DscConfigDocument_Resources_ShouldBeReadOnly()
    {
        var document = new DscConfigDocument();
        var resources = document.Resources;

        // Resources is IReadOnlyList, so it should not support Add
        resources.Should().BeAssignableTo<IReadOnlyList<DscConfigResource>>();
    }
}
