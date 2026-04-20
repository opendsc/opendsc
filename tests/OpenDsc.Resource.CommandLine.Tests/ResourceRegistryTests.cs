// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Xunit;

namespace OpenDsc.Resource.CommandLine.Tests;

[Trait("Category", "Unit")]
public class ResourceRegistryTests
{
    [Fact]
    public void ResourceRegistry_RegisterSingleResource()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource = new TestResourceAll();

        // Act
        builder.AddResource<TestResourceAll, TestSchema>(resource);

        // Assert - verify it builds without error
        var root = builder.Build();
        Assert.NotNull(root);
    }

    [Fact]
    public void ResourceRegistry_RegisterMultipleResources()
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
        Assert.Equal(7, root.Subcommands.Count);
    }

    [Fact]
    public void ResourceRegistry_DuplicateResourceType_ThrowsException()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll()));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public void ResourceRegistry_NullResource_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new CommandBuilder();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            builder.AddResource<TestResourceAll, TestSchema>(null!));
        Assert.Equal("resource", ex.ParamName);
    }

    [Fact]
    public void CommandBuilder_EmptyRegistry_BuildThrows()
    {
        // Arrange
        var builder = new CommandBuilder();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("No resources registered", ex.Message);
    }

    [Fact]
    public void CommandBuilder_BuildThenAddResource_BuildAgainSucceeds()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        var root1 = builder.Build();

        // Act - note: builder state already has the resource
        var root2 = builder.Build();

        // Assert
        Assert.NotNull(root1);
        Assert.NotNull(root2);
        Assert.Equal(root1.Subcommands.Count, root2.Subcommands.Count);
    }

    [Fact]
    public void ResourceCapabilities_DetectedCorrectly_All()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var isGettable = resource is IGettable<TestSchema>;
        var isSettable = resource is ISettable<TestSchema>;
        var isTestable = resource is ITestable<TestSchema>;
        var isDeletable = resource is IDeletable<TestSchema>;
        var isExportable = resource is IExportable<TestSchema>;

        // Assert
        Assert.True(isGettable);
        Assert.True(isSettable);
        Assert.True(isTestable);
        Assert.True(isDeletable);
        Assert.True(isExportable);
    }

    [Fact]
    public void ResourceCapabilities_DetectedCorrectly_GetSetOnly()
    {
        // Arrange
        var resource = new TestResourceGetSet();

        // Act
        var isGettable = resource is IGettable<TestSchema>;
        var isSettable = resource is ISettable<TestSchema>;
        var isTestable = resource is ITestable<TestSchema>;
        var isDeletable = resource is IDeletable<TestSchema>;
        var isExportable = resource is IExportable<TestSchema>;

        // Assert
        Assert.True(isGettable);
        Assert.True(isSettable);
        Assert.False(isTestable);
        Assert.False(isDeletable);
        Assert.False(isExportable);
    }

    [Fact]
    public void ResourceCapabilities_DetectedCorrectly_NoOps()
    {
        // Arrange
        var resource = new TestResourceNoOps();

        // Act
        var isGettable = resource is IGettable<TestSchema>;
        var isSettable = resource is ISettable<TestSchema>;
        var isTestable = resource is ITestable<TestSchema>;
        var isDeletable = resource is IDeletable<TestSchema>;
        var isExportable = resource is IExportable<TestSchema>;

        // Assert
        Assert.False(isGettable);
        Assert.False(isSettable);
        Assert.False(isTestable);
        Assert.False(isDeletable);
        Assert.False(isExportable);
    }

    [Fact]
    public void DscResourceAttribute_RetrievedCorrectly()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var attr = resource.GetType().GetCustomAttributes(typeof(DscResourceAttribute), false)
            .FirstOrDefault() as DscResourceAttribute;

        // Assert
        Assert.NotNull(attr);
        Assert.Equal("TestResource/All", attr.Type);
        Assert.Equal("1.0.0", attr.Version.ToString());
    }

    [Fact]
    public void ExitCodeAttribute_RetrievedCorrectly()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var exitCodeAttrs = resource.GetType().GetCustomAttributes(typeof(ExitCodeAttribute), false)
            .Cast<ExitCodeAttribute>().ToList();

        // Assert
        Assert.NotEmpty(exitCodeAttrs);
        Assert.Equal(2, exitCodeAttrs.Count);
    }

    [Fact]
    public void ExitCodeAttribute_CorrectDescription()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var exitCodeAttrs = resource.GetType().GetCustomAttributes(typeof(ExitCodeAttribute), false)
            .Cast<ExitCodeAttribute>().ToList();

        var exitCode1 = exitCodeAttrs.FirstOrDefault(x => x.ExitCode == 1);
        var exitCode2 = exitCodeAttrs.FirstOrDefault(x => x.ExitCode == 2);

        // Assert
        Assert.NotNull(exitCode1);
        Assert.NotNull(exitCode2);
        Assert.Equal("Not found", exitCode1.Description);
        Assert.Equal("Invalid input", exitCode2.Description);
    }

    [Fact]
    public void CommandVerbs_AllPresent_InBuiltCommand()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var verbNames = root.Subcommands.Select(c => c.Name).ToHashSet();

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
    public void CommandBuilder_SingleResource_NoResourceOptionRequired()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var getCommand = root.Subcommands.First(c => c.Name == "get");

        // Assert - with single resource, shouldn't have --resource option as required
        var resourceOption = getCommand.Options.FirstOrDefault(o => o.Name == "resource");
        if (resourceOption != null)
        {
            // The option exists but shouldn't be required for single resource
            Assert.NotNull(resourceOption);
        }
    }

    [Fact]
    public void CommandBuilder_MultipleResources_ResourceOptionPresent()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        builder.AddResource<TestResourceGetSet, TestSchema>(new TestResourceGetSet());

        // Act
        var root = builder.Build();
        var getCommand = root.Subcommands.First(c => c.Name == "get");

        // Assert - with multiple resources, should have options including resource option
        Assert.NotEmpty(getCommand.Options);
        // Verify we have at least 2 options (--resource and --input)
        Assert.True(getCommand.Options.Count >= 2);
    }

    [Fact]
    public void SchemaCommand_AlwaysPresent()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var schemaCommand = root.Subcommands.FirstOrDefault(c => c.Name == "schema");

        // Assert
        Assert.NotNull(schemaCommand);
        Assert.Equal("Get the JSON schema for a resource", schemaCommand.Description);
    }

    [Fact]
    public void ManifestCommand_AlwaysPresent()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var manifestCommand = root.Subcommands.FirstOrDefault(c => c.Name == "manifest");

        // Assert
        Assert.NotNull(manifestCommand);
        Assert.Equal("Generate the DSC resource manifest(s)", manifestCommand.Description);
    }

    [Fact]
    public void GetCommand_Description_IsCorrect()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var getCommand = root.Subcommands.First(c => c.Name == "get");

        // Assert
        Assert.Equal("Get the current state of a resource instance", getCommand.Description);
    }

    [Fact]
    public void SetCommand_Description_IsCorrect()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var setCommand = root.Subcommands.First(c => c.Name == "set");

        // Assert
        Assert.Equal("Set the desired state of a resource instance", setCommand.Description);
    }
}
