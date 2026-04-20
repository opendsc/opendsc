// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.CommandLine;
using Xunit;

namespace OpenDsc.Resource.CommandLine.IntegrationTests;

/// <summary>
/// Integration tests for CommandBuilder and the System.CommandLine pipeline.
/// These tests verify that the command-line interface works end-to-end with the resource framework.
/// </summary>
[Trait("Category", "Integration")]
public class CommandLineIntegrationTests
{
    [Fact]
    public void RootCommand_HelpCommand_ParsesSuccessfully()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        var rootCommand = builder.Build();

        // Act
        var parseResult = rootCommand.Parse(["--help"]);

        // Assert
        Assert.NotNull(parseResult);
        Assert.NotNull(parseResult.CommandResult);
    }

    [Fact]
    public void GetCommand_HelpOption_ParsesSuccessfully()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        var rootCommand = builder.Build();

        // Act
        var parseResult = rootCommand.Parse(["get", "--help"]);

        // Assert
        Assert.NotNull(parseResult);
        Assert.NotNull(parseResult.CommandResult);
    }

    [Fact]
    public void SetCommand_HelpOption_ParsesSuccessfully()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        var rootCommand = builder.Build();

        // Act
        var parseResult = rootCommand.Parse(["set", "--help"]);

        // Assert
        Assert.NotNull(parseResult);
        Assert.NotNull(parseResult.CommandResult);
    }

    [Fact]
    public void SchemaCommand_AlwaysAvailable()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceNoOps, TestSchema>(new TestResourceNoOps());
        var rootCommand = builder.Build();

        // Act
        var schemaCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "schema");

        // Assert - schema command should always be present even for resources with no capabilities
        Assert.NotNull(schemaCommand);
    }

    [Fact]
    public void ManifestCommand_AlwaysAvailable()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceNoOps, TestSchema>(new TestResourceNoOps());
        var rootCommand = builder.Build();

        // Act
        var manifestCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "manifest");

        // Assert - manifest command should always be present even for resources with no capabilities
        Assert.NotNull(manifestCommand);
    }

    [Fact]
    public void MultipleResources_BuildsSuccessfully()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());

        // Act
        var rootCommand = builder.Build();

        // Assert - should build without errors with multiple resources
        Assert.NotNull(rootCommand);
        Assert.NotEmpty(rootCommand.Subcommands);
    }

    [Fact]
    public void CommandParser_WithValidArguments_ParsesSuccessfully()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        var rootCommand = builder.Build();

        // Act
        var parseResult = rootCommand.Parse(["get", "--help"]);

        // Assert
        Assert.NotNull(parseResult);
        Assert.NotNull(parseResult.CommandResult);
    }

    [Fact]
    public void AllVerbs_PresentForFullCapabilityResource()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        var rootCommand = builder.Build();

        // Act
        var verbNames = rootCommand.Subcommands.Select(c => c.Name).ToHashSet();

        // Assert - all verbs should be present for a resource with all capabilities
        Assert.Contains("get", verbNames);
        Assert.Contains("set", verbNames);
        Assert.Contains("test", verbNames);
        Assert.Contains("delete", verbNames);
        Assert.Contains("export", verbNames);
        Assert.Contains("schema", verbNames);
        Assert.Contains("manifest", verbNames);
    }

    [Fact]
    public void PartialCapabilityResource_BuildsSuccessfully()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());

        // Act
        var rootCommand = builder.Build();

        // Assert - should build with basic structure
        Assert.NotNull(rootCommand);
        var verbNames = rootCommand.Subcommands.Select(c => c.Name).ToHashSet();
        Assert.NotEmpty(verbNames);
        // At minimum, schema and manifest should be present
        Assert.Contains("schema", verbNames);
        Assert.Contains("manifest", verbNames);
    }
}
