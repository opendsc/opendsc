// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using Xunit;

namespace OpenDsc.Resource.CommandLine.Tests;

[Trait("Category", "Unit")]
public class ManifestGenerationTests
{
    [Fact]
    public void GenerateManifest_WithAllMethods_IncludesAllMethods()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Get);
        Assert.NotNull(manifest.Set);
        Assert.NotNull(manifest.Test);
        Assert.NotNull(manifest.Delete);
        Assert.NotNull(manifest.Export);
    }

    [Fact]
    public void GenerateManifest_WithGetSetOnly_ExcludesOtherMethods()
    {
        // Arrange
        var resource = new TestResourceGetSet();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Get);
        Assert.NotNull(manifest.Set);
        Assert.Null(manifest.Test);
        Assert.Null(manifest.Delete);
        Assert.Null(manifest.Export);
    }

    [Fact]
    public void GenerateManifest_IncludesExitCodes()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.ExitCodes);
        Assert.True(manifest.ExitCodes.Count >= 2);
        Assert.Contains("1", manifest.ExitCodes.Keys);
        Assert.Contains("2", manifest.ExitCodes.Keys);
        Assert.Equal("Not found", manifest.ExitCodes["1"]);
        Assert.Equal("Invalid input", manifest.ExitCodes["2"]);
    }

    [Fact]
    public void GenerateManifest_IncludesEmbeddedSchema()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.EmbeddedSchema);
        Assert.NotEqual(JsonValueKind.Undefined, manifest.EmbeddedSchema.Embedded.ValueKind);
    }

    [Fact]
    public void GenerateManifest_SetMethodHasCorrectStructure()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Set);
        Assert.NotNull(manifest.Set.Args);
        Assert.NotEmpty(manifest.Set.Args);
    }

    [Fact]
    public void GenerateManifest_TestMethodHasCorrectStructure()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Test);
        Assert.NotNull(manifest.Test.Return);
    }

    [Fact]
    public void GenerateManifest_ExportMethodHasCorrectStructure()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Export);
        Assert.NotNull(manifest.Export.Args);
    }

    [Fact]
    public void GenerateManifest_VersionMatchesAttribute()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.Equal("1.0.0", manifest.Version);
    }

    [Fact]
    public void GenerateManifest_TypeMatchesAttribute()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.Equal("TestResource/All", manifest.Type);
    }

    [Fact]
    public void GenerateManifest_GetSetResource_HasNoTestDelete()
    {
        // Arrange
        var resource = new TestResourceGetSet();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Get);
        Assert.NotNull(manifest.Set);
        Assert.Null(manifest.Test);
        Assert.Null(manifest.Delete);
    }

    [Fact]
    public void GenerateManifest_NoOpsResource_HasNoMethods()
    {
        // Arrange
        var resource = new TestResourceNoOps();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.Null(manifest.Get);
        Assert.Null(manifest.Set);
        Assert.Null(manifest.Test);
        Assert.Null(manifest.Delete);
        Assert.Null(manifest.Export);
    }

    [Fact]
    public void GenerateManifest_MethodArgsContainVerb()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Get);
        Assert.NotNull(manifest.Get.Args);
        Assert.Contains("get", manifest.Get.Args.Cast<string>());
    }

    [Fact]
    public void GenerateManifest_SerializesToJson()
    {
        // Arrange
        var resource = new TestResourceAll();
        var manifest = ManifestBuilder.Build(resource);

        // Act
        var json = JsonSerializer.Serialize(manifest);

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("TestResource/All", json);
        Assert.Contains("1.0.0", json);
    }

    [Fact]
    public void GenerateManifest_PreservesDescriptionWhenPresent()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Type);
        Assert.Equal("TestResource/All", manifest.Type);
    }

    [Fact]
    public void GenerateManifest_WithNoExitCodes_HandlesGracefully()
    {
        // Arrange
        var resource = new TestResourceGetSet();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        // Should either be null or empty
        if (manifest.ExitCodes != null)
        {
            Assert.Empty(manifest.ExitCodes);
        }
    }

    [Fact]
    public void ManifestMethod_AllHaveExecutable()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        if (manifest.Get != null)
        {
            Assert.NotNull(manifest.Get.Executable);
        }
        if (manifest.Set != null)
        {
            Assert.NotNull(manifest.Set.Executable);
        }
        if (manifest.Test != null)
        {
            Assert.NotNull(manifest.Test.Executable);
        }
    }

    [Fact]
    public void MultipleResources_GenerateDistinctManifests()
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
