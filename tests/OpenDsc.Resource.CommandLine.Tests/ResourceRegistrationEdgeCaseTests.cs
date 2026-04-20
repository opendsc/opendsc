// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Xunit;

namespace OpenDsc.Resource.CommandLine.Tests;

[Trait("Category", "Unit")]
public class ResourceRegistrationEdgeCaseTests
{
    [Fact]
    public void AddResource_MultipleResourcesOfDifferentTypes_SuccessfullyRegistered()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource1 = new TestResourceAll();
        var resource2 = new TestResourceGetSet();

        // Act
        builder.AddResource<TestResourceAll, TestSchema>(resource1);
        builder.AddResource<TestResourceGetSet, TestSchema>(resource2);
        var root = builder.Build();

        // Assert
        Assert.NotNull(root);
        Assert.Equal(7, root.Subcommands.Count);
    }

    [Fact]
    public void ResourceRegistry_TracksMultipleResources()
    {
        // Arrange
        var builder = new CommandBuilder();

        // Act
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());
        builder.AddResource<TestResourceNoOps, TestSchema>(new TestResourceNoOps());

        // Assert
        var root = builder.Build();
        Assert.NotNull(root);
    }

    [Fact]
    public void GetResource_DerivesCorrectCapabilities()
    {
        // Arrange
        var allCapabilities = new TestResourceAll();
        var getSetOnly = new TestResourceGetSet();

        // Act
        var allManifest = ManifestBuilder.Build(allCapabilities);
        var getSetManifest = ManifestBuilder.Build(getSetOnly);

        // Assert
        Assert.NotNull(allManifest.Get);
        Assert.NotNull(allManifest.Set);
        Assert.NotNull(allManifest.Test);
        Assert.NotNull(allManifest.Delete);
        Assert.NotNull(allManifest.Export);

        Assert.NotNull(getSetManifest.Get);
        Assert.NotNull(getSetManifest.Set);
    }

    [Fact]
    public void ResourceExecution_GetReturnsInstance()
    {
        // Arrange
        var resource = new TestResourceAll();
        var filter = new TestSchema { Name = "filter" };

        // Act
        var result = resource.Get(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
    }

    [Fact]
    public void ResourceExecution_SetReturnsResult()
    {
        // Arrange
        var resource = new TestResourceAll();
        var desired = new TestSchema { Name = "desired", Value = "value", Enabled = true };

        // Act
        var result = resource.Set(desired);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ActualState);
    }

    [Fact]
    public void ResourceExecution_TestReturnsResult()
    {
        // Arrange
        var resource = new TestResourceAll();
        var desired = new TestSchema { Name = "desired" };

        // Act
        var result = resource.Test(desired);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ActualState);
    }

    [Fact]
    public void ResourceExecution_DeleteCompletes()
    {
        // Arrange
        var resource = new TestResourceAll();
        var instance = new TestSchema { Name = "instance" };

        // Act & Assert
        resource.Delete(instance);  // Should not throw
    }

    [Fact]
    public void ResourceExecution_ExportReturnsEnumerable()
    {
        // Arrange
        var resource = new TestResourceAll();
        var filter = new TestSchema { Name = "filter" };

        // Act
        var results = resource.Export(filter);

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void Serialization_TestSchemaRoundTrip()
    {
        // Arrange
        var resource = new TestResourceAll();
        var original = new TestSchema { Name = "test", Value = "value", Enabled = true };

        // Act
        var json = resource.ToJson(original);
        var parsed = resource.Parse(json);

        // Assert
        Assert.NotNull(parsed);
        Assert.Equal(original.Name, parsed.Name);
        Assert.Equal(original.Value, parsed.Value);
        Assert.Equal(original.Enabled, parsed.Enabled);
    }

    [Fact]
    public void Schema_CanBeRetrieved()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var schema = resource.GetSchema();

        // Assert
        Assert.NotNull(schema);
        Assert.NotEmpty(schema);
        Assert.Contains("$schema", schema);
    }

    [Fact]
    public void Manifest_IncludesExitCodesFromAttributes()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        Assert.NotNull(manifest.ExitCodes);
    }

    [Fact]
    public void ResourceRegistry_CanHandleThreeResources()
    {
        // Arrange
        var builder = new CommandBuilder();

        // Act
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());
        builder.AddResource<TestResourceNoOps, TestSchema>(new TestResourceNoOps());
        var root = builder.Build();

        // Assert
        Assert.NotNull(root);
        Assert.Equal(7, root.Subcommands.Count);
    }
}
