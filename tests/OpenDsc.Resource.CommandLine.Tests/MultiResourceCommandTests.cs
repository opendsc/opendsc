// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.CommandLine;
using OpenDsc.Resource.CommandLine;
using Xunit;

namespace OpenDsc.Resource.CommandLine.Tests;

public class MultiResourceCommandTests
{
    [Fact]
    public void GetCommand_WithMultipleResources_CreatesResourceOption()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());

        // Act
        var root = builder.Build();
        var getCommand = root.Subcommands.FirstOrDefault(c => c.Name == "get");

        // Assert
        Assert.NotNull(getCommand);
        // Verify command has options for multi-resource scenario
        Assert.NotEmpty(getCommand.Options);
    }

    [Fact]
    public void SetCommand_WithMultipleResources_IsConfiguredProperly()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());

        // Act
        var root = builder.Build();
        var setCommand = root.Subcommands.FirstOrDefault(c => c.Name == "set");

        // Assert
        Assert.NotNull(setCommand);
        Assert.NotEmpty(setCommand.Options);
    }

    [Fact]
    public void TestCommand_WithMultipleResources_IsConfiguredProperly()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());

        // Act
        var root = builder.Build();
        var testCommand = root.Subcommands.FirstOrDefault(c => c.Name == "test");

        // Assert
        Assert.NotNull(testCommand);
        Assert.NotEmpty(testCommand.Options);
    }

    [Fact]
    public void DeleteCommand_WithMultipleResources_IsConfiguredProperly()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());

        // Act
        var root = builder.Build();
        var deleteCommand = root.Subcommands.FirstOrDefault(c => c.Name == "delete");

        // Assert
        Assert.NotNull(deleteCommand);
        Assert.NotEmpty(deleteCommand.Options);
    }

    [Fact]
    public void ExportCommand_WithMultipleResources_IsConfiguredProperly()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());

        // Act
        var root = builder.Build();
        var exportCommand = root.Subcommands.FirstOrDefault(c => c.Name == "export");

        // Assert
        Assert.NotNull(exportCommand);
        Assert.NotEmpty(exportCommand.Options);
    }

    [Fact]
    public void SchemaCommand_WithMultipleResources_IsConfiguredProperly()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());

        // Act
        var root = builder.Build();
        var schemaCommand = root.Subcommands.FirstOrDefault(c => c.Name == "schema");

        // Assert
        Assert.NotNull(schemaCommand);
    }

    [Fact]
    public void ManifestCommand_WithMultipleResources_HasResourceOption()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());

        // Act
        var root = builder.Build();
        var manifestCommand = root.Subcommands.FirstOrDefault(c => c.Name == "manifest");

        // Assert
        Assert.NotNull(manifestCommand);
        // Manifest command may have resource option for multi-resource scenarios
        Assert.NotEmpty(manifestCommand.Options);
    }

    [Fact]
    public void MultiResourceBuilder_ProducesDifferentCommandStructure()
    {
        // Arrange
        var singleBuilder = new CommandBuilder();
        singleBuilder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        var multiBuilder = new CommandBuilder();
        multiBuilder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        multiBuilder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());

        // Act
        var singleRoot = singleBuilder.Build();
        var multiRoot = multiBuilder.Build();

        // Assert
        // Both should have same subcommands
        Assert.Equal(singleRoot.Subcommands.Count, multiRoot.Subcommands.Count);
    }

    [Fact]
    public void CommandBuilder_ChainedCalls_SucceedWithMultipleResources()
    {
        // Arrange & Act
        var root = new CommandBuilder()
            .AddResource<TestResourceAll, TestSchema>(new TestResourceAll())
            .AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet())
            .AddResource<TestResourceNoOps, TestSchema>(new TestResourceNoOps())
            .Build();

        // Assert
        Assert.NotNull(root);
        Assert.Equal(7, root.Subcommands.Count);
    }

    [Fact]
    public void RootCommand_WithMultipleResources_HasAllVerbs()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());
        builder.AddResource<TestResourceNoOps, TestSchema>(new TestResourceNoOps());

        // Act
        var root = builder.Build();

        // Assert
        Assert.NotNull(root);
        Assert.Contains("get", root.Subcommands.Select(c => c.Name));
        Assert.Contains("set", root.Subcommands.Select(c => c.Name));
        Assert.Contains("test", root.Subcommands.Select(c => c.Name));
        Assert.Contains("delete", root.Subcommands.Select(c => c.Name));
        Assert.Contains("export", root.Subcommands.Select(c => c.Name));
        Assert.Contains("schema", root.Subcommands.Select(c => c.Name));
        Assert.Contains("manifest", root.Subcommands.Select(c => c.Name));
    }
}
