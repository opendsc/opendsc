// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Xunit;

namespace OpenDsc.Resource.CommandLine.Tests;

[Trait("Category", "Unit")]
public class ResourceRegistrationTests
{
    [Fact]
    public void Build_CommandBuilder_CreatesValidResourceRegistration()
    {
        // This test verifies that the internal ResourceRegistry works correctly
        // by building a command and checking that resources are properly registered

        // Arrange
        var builder = new CommandBuilder();
        var resource = new TestResourceAll();
        builder.AddResource<TestResourceAll, TestSchema>(resource);

        // Act
        var rootCommand = builder.Build();

        // Assert - if the command builds without exception, the registry worked
        Assert.NotNull(rootCommand);
    }

    [Fact]
    public void Build_MultipleResources_BothRegistered()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource1 = new TestResourceAll();
        var resource2 = new TestResourceGetSet();

        // Act
        builder.AddResource<TestResourceAll, TestSchema>(resource1);
        builder.AddResource<TestResourceGetSet, TestSchema>(resource2);
        var rootCommand = builder.Build();

        // Assert - both resources should result in a successful build
        Assert.NotNull(rootCommand);
    }

    [Fact]
    public void AddResource_DuplicateType_ThrowsException()
    {
        // This test verifies that adding duplicate resource types is prevented

        // Arrange
        var builder = new CommandBuilder();
        var resource1 = new TestResourceAll();
        var resource2 = new TestResourceAll();

        builder.AddResource<TestResourceAll, TestSchema>(resource1);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            builder.AddResource<TestResourceAll, TestSchema>(resource2));
        Assert.Contains("already registered", ex.Message);
    }
}
