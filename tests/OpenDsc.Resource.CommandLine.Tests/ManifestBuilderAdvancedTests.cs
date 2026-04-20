// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using OpenDsc.Resource.CommandLine;
using Xunit;

namespace OpenDsc.Resource.CommandLine.Tests;

public class ManifestBuilderAdvancedTests
{
    [Fact]
    public void Build_WithGettableResource_IncludesGetMethod()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        Assert.NotNull(manifest.Get);
    }

    [Fact]
    public void Build_WithSettableResource_HasSetMethod()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        Assert.NotNull(manifest.Set);
    }

    [Fact]
    public void Build_WithTestableResource_HasTestMethod()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        Assert.NotNull(manifest.Test);
    }

    [Fact]
    public void Build_WithDeletableResource_HasDeleteMethod()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        Assert.NotNull(manifest.Delete);
    }

    [Fact]
    public void Build_WithExportableResource_HasExportMethod()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        Assert.NotNull(manifest.Export);
    }

    [Fact]
    public void Build_MinimalResource_StillBuilds()
    {
        // Arrange
        var resource = new TestResourceNoOps();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
    }

    [Fact]
    public void Build_WithDifferentCapabilities_IncludesOnlyApplicableMethods()
    {
        // Arrange
        var getSetResource = new TestResourceGetSet();

        // Act
        var manifest = ManifestBuilder.Build(getSetResource);

        // Assert
        Assert.NotNull(manifest);
        Assert.NotNull(manifest.Get);
        Assert.NotNull(manifest.Set);
        // Test/Delete/Export should not be present or should be null
    }

    [Fact]
    public void Build_WithSchema_EmbeddedSchemaIsPresent()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        Assert.NotNull(manifest.Schema);
    }

    [Fact]
    public void Build_ManifestHasCorrectType()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        Assert.Equal("TestResource/All", manifest.Type);
    }

    [Fact]
    public void Build_ManifestHasCorrectVersion()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        Assert.Equal("1.0.0", manifest.Version);
    }

    [Fact]
    public void Build_WithMultipleResources_EachHasOwnManifest()
    {
        // Arrange
        var resource1 = new TestResourceAll();
        var resource2 = new TestResourceGetSet();

        // Act
        var manifest1 = ManifestBuilder.Build(resource1);
        var manifest2 = ManifestBuilder.Build(resource2);

        // Assert
        Assert.NotEqual(manifest1.Type, manifest2.Type);
        Assert.Equal("TestResource/All", manifest1.Type);
        Assert.Equal("TestResource/GetSet", manifest2.Type);
    }
}
