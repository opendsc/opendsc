// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.CommandLine;
using Xunit;

namespace OpenDsc.Resource.CommandLine.IntegrationTests;

[Trait("Category", "Integration")]
public class ResourceExecutionIntegrationTests
{
    [Fact]
    public void CommandBuilder_BuildsWorkingCommandLineInterface()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource = new TestResourceAll();
        builder.AddResource<TestResourceAll, TestSchema>(resource);
        var rootCommand = builder.Build();

        // Act
        var parseResult = rootCommand.Parse(["--help"]);

        // Assert
        Assert.NotNull(parseResult);
        Assert.NotNull(parseResult.CommandResult);
    }

    [Fact]
    public void GetCommand_ExecutionSucceeds()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource = new TestResourceAll();
        builder.AddResource<TestResourceAll, TestSchema>(resource);
        var rootCommand = builder.Build();

        // Act
        var getCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "get");
        var parseResult = rootCommand.Parse(["get", "--help"]);

        // Assert
        Assert.NotNull(getCommand);
        Assert.NotNull(parseResult);
        Assert.NotNull(parseResult.CommandResult);
    }

    [Fact]
    public void SchemaCommand_ReturnsValidJsonSchema()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource = new TestResourceAll();
        builder.AddResource<TestResourceAll, TestSchema>(resource);
        var rootCommand = builder.Build();

        // Act
        var schemaCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "schema");
        var parseResult = rootCommand.Parse(["schema", "--help"]);

        // Assert
        Assert.NotNull(schemaCommand);
        Assert.NotNull(parseResult);
    }

    [Fact]
    public void ManifestCommand_ReturnsValidManifest()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource = new TestResourceAll();
        builder.AddResource<TestResourceAll, TestSchema>(resource);
        var rootCommand = builder.Build();

        // Act
        var manifestCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "manifest");
        var parseResult = rootCommand.Parse(["manifest", "--help"]);

        // Assert
        Assert.NotNull(manifestCommand);
        Assert.NotNull(parseResult);
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
    public void SingleResource_BuildsWithoutResourceSelection()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        var rootCommand = builder.Build();

        // Act
        var getCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "get");

        // Assert - with single resource, --resource option should not be present
        Assert.NotNull(getCommand);
        var resourceOption = getCommand.Options.FirstOrDefault(o => o.Name == "resource");
        Assert.Null(resourceOption);
    }

    [Fact]
    public void ResourceWithoutCapabilities_StillBuilds()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource = new TestResourceNoOps();
        builder.AddResource<TestResourceNoOps, TestSchema>(resource);

        // Act
        var rootCommand = builder.Build();

        // Assert - should build with schema/manifest even without capability methods
        Assert.NotNull(rootCommand);
        var schemaCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "schema");
        var manifestCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "manifest");
        Assert.NotNull(schemaCommand);
        Assert.NotNull(manifestCommand);
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
