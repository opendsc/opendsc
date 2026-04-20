// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.CommandLine;
using System.Text.Json;
using OpenDsc.Resource.CommandLine;
using Xunit;

namespace OpenDsc.Resource.CommandLine.Tests;

public class AdvancedCommandBuilderTests
{
    [Fact]
    public void GetCommand_WithSingleResource_ProducesFunctionality()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var getCommand = root.Subcommands.FirstOrDefault(c => c.Name == "get");

        // Assert
        Assert.NotNull(getCommand);
        // Get command should have options for input
        Assert.NotEmpty(getCommand.Options);
    }

    [Fact]
    public void SetCommand_WithSingleResource_ProducesFunctionality()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var setCommand = root.Subcommands.FirstOrDefault(c => c.Name == "set");

        // Assert
        Assert.NotNull(setCommand);
        Assert.NotEmpty(setCommand.Options);
    }

    [Fact]
    public void TestCommand_WithSingleResource_ProducesFunctionality()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var testCommand = root.Subcommands.FirstOrDefault(c => c.Name == "test");

        // Assert
        Assert.NotNull(testCommand);
        Assert.NotEmpty(testCommand.Options);
    }

    [Fact]
    public void DeleteCommand_WithSingleResource_ProducesFunctionality()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var deleteCommand = root.Subcommands.FirstOrDefault(c => c.Name == "delete");

        // Assert
        Assert.NotNull(deleteCommand);
        Assert.NotEmpty(deleteCommand.Options);
    }

    [Fact]
    public void ExportCommand_WithSingleResource_ProducesFunctionality()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var exportCommand = root.Subcommands.FirstOrDefault(c => c.Name == "export");

        // Assert
        Assert.NotNull(exportCommand);
        Assert.NotEmpty(exportCommand.Options);
    }

    [Fact]
    public void SchemaCommand_WithSingleResource_ProducesFunctionality()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var schemaCommand = root.Subcommands.FirstOrDefault(c => c.Name == "schema");

        // Assert
        Assert.NotNull(schemaCommand);
    }

    [Fact]
    public void ManifestCommand_WithSingleResource_ProducesFunctionality()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var manifestCommand = root.Subcommands.FirstOrDefault(c => c.Name == "manifest");

        // Assert
        Assert.NotNull(manifestCommand);
    }

    [Fact]
    public void MultipleAddResource_WithMultipleTypes_AllResisteredSuccessfully()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource1 = new TestResourceAll();
        var resource2 = new TestResourceGetSet();
        var resource3 = new TestResourceNoOps();

        // Act
        builder.AddResource<TestResourceAll, TestSchema>(resource1);
        builder.AddResource<TestResourceGetSet, TestSchema>(resource2);
        builder.AddResource<TestResourceNoOps, TestSchema>(resource3);
        var root = builder.Build();

        // Assert
        Assert.NotNull(root);
        Assert.Equal(7, root.Subcommands.Count);
    }

    [Fact]
    public void Build_WithThreeResources_AllCommandsPresent()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());
        builder.AddResource<TestResourceNoOps, TestSchema>(new TestResourceNoOps());

        // Act
        var root = builder.Build();
        var verbNames = root.Subcommands.Select(c => c.Name).ToArray();

        // Assert
        Assert.Contains("get", verbNames);
        Assert.Contains("set", verbNames);
        Assert.Contains("test", verbNames);
        Assert.Contains("delete", verbNames);
        Assert.Contains("export", verbNames);
        Assert.Contains("schema", verbNames);
        Assert.Contains("manifest", verbNames);
    }

    [Fact]
    public void BuildWithoutResources_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new CommandBuilder();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void SingleVsMultiResourcePaths()
    {
        // Arrange - Single resource
        var singleBuilder = new CommandBuilder();
        singleBuilder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var singleRoot = singleBuilder.Build();
        var singleGet = singleRoot.Subcommands.FirstOrDefault(c => c.Name == "get");

        // Assert - Verify structure is created
        Assert.NotNull(singleGet);
        Assert.NotEmpty(singleGet.Options);
    }

    [Fact]
    public void CommandBuilder_CanBeChainedWithMultipleAddResource()
    {
        // Arrange & Act
        var root = new CommandBuilder()
            .AddResource<TestResourceAll, TestSchema>(new TestResourceAll())
            .AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet())
            .Build();

        // Assert
        Assert.NotNull(root);
        Assert.Equal(7, root.Subcommands.Count);
    }
}
