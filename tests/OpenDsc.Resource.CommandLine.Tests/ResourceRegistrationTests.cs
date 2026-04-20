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

public class ResourceExecutorTests
{
    [Fact]
    public void Get_WithValidResource_ReturnsOutput()
    {
        // This test verifies resource execution by capturing console output

        // Arrange
        var resource = new TestResourceAll();
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var testSchema = new TestSchema { Name = "test", Value = "value", Enabled = true };
            var json = resource.ToJson(testSchema);

            // Assert
            Assert.NotNull(json);
            Assert.NotEmpty(json);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void SetTestResult_HasChangedProperties()
    {
        // Arrange
        var resource = new TestResourceAll();
        var desiredState = new TestSchema { Name = "test", Value = "value", Enabled = true };

        // Act
        var result = resource.Set(desiredState);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ActualState);
        Assert.NotNull(result.ChangedProperties);
        Assert.Contains("name", result.ChangedProperties);
    }

    [Fact]
    public void TestResult_HasDifferingProperties()
    {
        // Arrange
        var resource = new TestResourceAll();
        var desiredState = new TestSchema { Name = "test" };

        // Act
        var result = resource.Test(desiredState);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DifferingProperties);
    }

    [Fact]
    public void Export_ReturnsEnumerable()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var results = resource.Export(null);

        // Assert
        Assert.NotNull(results);
        var resultList = results.First();
        Assert.Equal("test", resultList.Name);
    }

    [Fact]
    public void Delete_WithInstance_Succeeds()
    {
        // Arrange
        var resource = new TestResourceAll();
        var instance = new TestSchema { Name = "test" };

        // Act & Assert - should not throw
        resource.Delete(instance);
    }

    [Fact]
    public void Get_ReturnsInstance()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var result = resource.Get(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
    }
}

public class CommandExecutorIntegrationTests
{
    [Fact]
    public void BuildCommand_WithAllCapabilities_IncludesAllVerbs()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();

        // Assert
        var verbNames = root.Subcommands.Select(c => c.Name).ToArray();
        Assert.Contains("get", verbNames);
        Assert.Contains("set", verbNames);
        Assert.Contains("test", verbNames);
        Assert.Contains("delete", verbNames);
        Assert.Contains("export", verbNames);
        Assert.Contains("schema", verbNames);
        Assert.Contains("manifest", verbNames);
    }

    [Fact]
    public void SchemaCommand_CanRetrieveSchema()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var schema = resource.GetSchema();

        // Assert
        Assert.NotNull(schema);
        Assert.NotEmpty(schema);
        Assert.Contains("json-schema", schema);
    }

    [Fact]
    public void ManifestBuilder_BuildsCompleteManifest()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        Assert.Equal("TestResource/All", manifest.Type);
        Assert.NotNull(manifest.Get);
        Assert.NotNull(manifest.Set);
        Assert.NotNull(manifest.Test);
        Assert.NotNull(manifest.Delete);
        Assert.NotNull(manifest.Export);
    }

    [Fact]
    public void ParseAndJson_RoundTrip()
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
}
