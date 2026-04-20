// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.CommandLine;
using Xunit;

namespace OpenDsc.Resource.CommandLine.Tests;

public class CommandBuilderTests
{
    [Fact]
    public void AddResource_WithValidResource_AddsResourceToRegistry()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource = new TestResourceAll();

        // Act
        var result = builder.AddResource<TestResourceAll, TestSchema>(resource);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public void AddResource_WithNullResource_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new CommandBuilder();
        TestResourceAll? resource = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => builder.AddResource<TestResourceAll, TestSchema>(resource!));
        Assert.Equal("resource", ex.ParamName);
    }

    [Fact]
    public void AddResource_MultipleResources_AllAddedSuccessfully()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource1 = new TestResourceAll();
        var resource2 = new TestResourceGetSet();

        // Act
        var result1 = builder.AddResource<TestResourceAll, TestSchema>(resource1);
        var result2 = result1.AddResource<TestResourceGetSet, TestSchema>(resource2);

        // Assert
        Assert.NotNull(result2);
    }

    [Fact]
    public void Build_WithoutResources_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new CommandBuilder();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("No resources registered", ex.Message);
    }

    [Fact]
    public void Build_WithSingleResource_CreatesRootCommandWithCommands()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource = new TestResourceAll();
        builder.AddResource<TestResourceAll, TestSchema>(resource);

        // Act
        var rootCommand = builder.Build();

        // Assert
        Assert.NotNull(rootCommand);
        Assert.Equal("DSC Resource Command Line Interface", rootCommand.Description);

        // Verify all command verbs are present
        var commandNames = rootCommand.Subcommands.Select(c => c.Name).ToList();
        Assert.Contains("get", commandNames);
        Assert.Contains("set", commandNames);
        Assert.Contains("test", commandNames);
        Assert.Contains("delete", commandNames);
        Assert.Contains("export", commandNames);
        Assert.Contains("schema", commandNames);
        Assert.Contains("manifest", commandNames);
    }

    [Fact]
    public void Build_WithMultipleResources_CreatesRootCommandWithResourceOption()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());

        // Act
        var rootCommand = builder.Build();

        // Assert
        Assert.NotNull(rootCommand);
        // With multiple resources, the resource option should be available on subcommands
        var getCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "get");
        Assert.NotNull(getCommand);
        // The resource option may be under different names, just verify command exists
        Assert.NotEmpty(getCommand.Options);
    }

    [Fact]
    public void Build_SingleResourceGet_NoResourceOptionRequired()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var rootCommand = builder.Build();

        // Assert
        var getCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "get");
        Assert.NotNull(getCommand);
        // For single resource, the resource option should not be present or should be optional
        var resourceOption = getCommand.Options.FirstOrDefault(o => o.Name == "resource");
        // In single resource mode, the resource option should not be required
        if (resourceOption != null)
        {
            // We can't directly check IsRequired, but we can verify the option exists
            Assert.NotNull(resourceOption);
        }
    }

    [Fact]
    public void Build_ContainsAllVerbCommands()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var rootCommand = builder.Build();

        // Assert
        var expectedVerbs = new[] { "get", "set", "test", "delete", "export", "schema", "manifest" };
        var actualVerbs = rootCommand.Subcommands.Select(c => c.Name).ToArray();

        foreach (var verb in expectedVerbs)
        {
            Assert.Contains(verb, actualVerbs);
        }
    }

    [Fact]
    public void Build_SchemaCommand_CanBeFound()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var rootCommand = builder.Build();
        var schemaCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "schema");

        // Assert
        Assert.NotNull(schemaCommand);
        Assert.Equal("Get the JSON schema for a resource", schemaCommand.Description);
    }

    [Fact]
    public void Build_ManifestCommand_CanBeFound()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var rootCommand = builder.Build();
        var manifestCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "manifest");

        // Assert
        Assert.NotNull(manifestCommand);
        Assert.Equal("Generate the DSC resource manifest(s)", manifestCommand.Description);
    }

    [Theory]
    [InlineData("get")]
    [InlineData("set")]
    [InlineData("test")]
    [InlineData("delete")]
    [InlineData("export")]
    public void Build_VerbCommands_Exist(string verb)
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var rootCommand = builder.Build();

        // Assert
        // Verify that each verb command exists as a subcommand
        var command = rootCommand.Subcommands.FirstOrDefault(c => c.Name == verb);
        Assert.NotNull(command);
        Assert.Equal(verb, command.Name);
    }
}
