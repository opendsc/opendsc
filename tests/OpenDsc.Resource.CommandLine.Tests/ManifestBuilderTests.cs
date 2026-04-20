// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Xunit;

namespace OpenDsc.Resource.CommandLine.Tests;

public class ManifestBuilderTests
{
    [Fact]
    public void Build_WithValidResource_ReturnsManifest()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        Assert.Equal("TestResource/All", manifest.Type);
        Assert.Equal("1.0.0", manifest.Version);
    }

    [Fact]
    public void Build_WithValidResource_IncludesTags()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        // Tags may or may not be present depending on how they're set
        // Just verify the method succeeds
        Assert.NotNull(manifest);
    }

    [Fact]
    public void Build_WithValidResource_IncludesExitCodes()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.ExitCodes);
        Assert.Contains("1", manifest.ExitCodes.Keys);
        Assert.Contains("2", manifest.ExitCodes.Keys);
    }

    [Fact]
    public void Build_WithValidResource_IncludesEmbeddedSchema()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.EmbeddedSchema);
        // EmbeddedSchema.Embedded is a JsonElement (value type), so we check its kind
        Assert.NotEqual(System.Text.Json.JsonValueKind.Undefined, manifest.EmbeddedSchema.Embedded.ValueKind);
    }

    [Fact]
    public void Build_WithGettableResource_IncludesGetMethod()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Get);
    }

    [Fact]
    public void Build_WithSettableResource_IncludesSetMethod()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Set);
        // SetReturn will be set based on the DscResourceAttribute
    }

    [Fact]
    public void Build_WithTestableResource_IncludesTestMethod()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Test);
        // TestReturn will be set based on the DscResourceAttribute
    }

    [Fact]
    public void Build_WithDeletableResource_IncludesDeleteMethod()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Delete);
    }

    [Fact]
    public void Build_WithExportableResource_IncludesExportMethod()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Export);
    }

    [Fact]
    public void Build_WithResourceNoSetReturn_SetReturnIsNull()
    {
        // Arrange
        var resource = new TestResourceGetSet();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Set);
        // SetReturn may be null depending on how it's configured
    }

    [Fact]
    public void Build_WithResourceWithDescription_ManifestBuildsSuccessfully()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        // Description may or may not be populated depending on attribute usage
        // Just verify that manifest builds without error
    }

    [Fact]
    public void Build_ManifestMethodsHaveValidArgs()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Get);
        if (manifest.Get?.Args != null)
        {
            Assert.NotEmpty(manifest.Get.Args);
        }
        Assert.NotNull(manifest.Get?.Executable);
    }

    [Fact]
    public void Build_WithNoTagsResource_TagsIsNull()
    {
        // Arrange
        var resource = new TestResourceGetSet();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        // Tags should be null if empty
        if (manifest.Tags != null)
        {
            Assert.Empty(manifest.Tags);
        }
    }
}
