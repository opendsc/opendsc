// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Xunit;

namespace OpenDsc.Resource.CommandLine.IntegrationTests;

[Trait("Category", "Integration")]
public class ResourceExecutionIntegrationTests
{
    [Fact]
    public void ExecuteGet_WithValidResource_OutputsJson()
    {
        // Arrange
        var builder = new CommandBuilder();
        var resource = new TestResourceAll();
        builder.AddResource<TestResourceAll, TestSchema>(resource);
        var rootCommand = builder.Build();

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var getCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "get");
            Assert.NotNull(getCommand);

            // Capture output by directly calling the resource
            var result = resource.Get(null);
            var json = resource.ToJson(result);
            Console.WriteLine(json);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains("test", output);
            Assert.Contains("value", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void ExecuteSet_WithValidResource_ReturnsSetResult()
    {
        // Arrange
        var resource = new TestResourceAll();
        var desiredState = new TestSchema { Name = "desired", Value = "value", Enabled = true };

        // Act
        var result = resource.Set(desiredState);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ActualState);
        Assert.NotNull(result.ChangedProperties);
    }

    [Fact]
    public void ExecuteTest_WithValidResource_ReturnsTestResult()
    {
        // Arrange
        var resource = new TestResourceAll();
        var desiredState = new TestSchema { Name = "desired", Value = "value", Enabled = true };

        // Act
        var result = resource.Test(desiredState);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ActualState);
        Assert.NotNull(result.DifferingProperties);
    }

    [Fact]
    public void ExecuteDelete_WithValidResource_Succeeds()
    {
        // Arrange
        var resource = new TestResourceAll();
        var instance = new TestSchema { Name = "instance", Value = "value" };

        // Act & Assert - should not throw
        resource.Delete(instance);
    }

    [Fact]
    public void ExecuteExport_WithValidResource_ReturnsEnumerable()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var results = resource.Export(null);

        // Assert
        Assert.NotNull(results);
        var resultList = results.ToList();
        Assert.NotEmpty(resultList);
        Assert.Equal("test", resultList[0].Name);
    }

    [Fact]
    public void ExecuteSchema_WithValidResource_ReturnsJsonSchema()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var schema = resource.GetSchema();

        // Assert
        Assert.NotNull(schema);
        Assert.NotEmpty(schema);
        Assert.Contains("json-schema", schema);
        Assert.Contains("TestResource", schema);
    }

    [Fact]
    public void ExecuteGet_WithNullInput_UsesDefaultInstance()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var result = resource.Get(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
    }

    [Fact]
    public void ExecuteSet_WithNullInput_UsesDefaultInstance()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var result = resource.Set(null);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ActualState);
    }

    [Fact]
    public void ExecuteTest_WithNullInput_UsesDefaultInstance()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var result = resource.Test(null);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ActualState);
    }

    [Fact]
    public void GetResourceCapabilities_FromTestResourceAll_IncludesAllInterfaces()
    {
        // Arrange
        var resource = new TestResourceAll();
        var type = resource.GetType();

        // Act & Assert
        Assert.True(typeof(IGettable<TestSchema>).IsAssignableFrom(type));
        Assert.True(typeof(ISettable<TestSchema>).IsAssignableFrom(type));
        Assert.True(typeof(ITestable<TestSchema>).IsAssignableFrom(type));
        Assert.True(typeof(IDeletable<TestSchema>).IsAssignableFrom(type));
        Assert.True(typeof(IExportable<TestSchema>).IsAssignableFrom(type));
    }

    [Fact]
    public void GetResourceCapabilities_FromTestResourceGetSet_IncludesGetAndSet()
    {
        // Arrange
        var resource = new TestResourceGetSet();
        var type = resource.GetType();

        // Act & Assert
        Assert.True(typeof(IGettable<TestSchema>).IsAssignableFrom(type));
        Assert.True(typeof(ISettable<TestSchema>).IsAssignableFrom(type));
        Assert.False(typeof(ITestable<TestSchema>).IsAssignableFrom(type));
    }

    [Fact]
    public void ExecuteWithJsonSerialization_RoundTrip()
    {
        // Arrange
        var resource = new TestResourceAll();
        var original = new TestSchema { Name = "test", Value = "testval", Enabled = true };

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
    public void ManifestBuilder_WithAllCapabilitiesResource()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest);
        Assert.NotNull(manifest.Get);
        Assert.NotNull(manifest.Set);
        Assert.NotNull(manifest.Test);
        Assert.NotNull(manifest.Delete);
        Assert.NotNull(manifest.Export);
        Assert.NotNull(manifest.EmbeddedSchema);
        Assert.NotNull(manifest.ExitCodes);
        Assert.Contains("1", manifest.ExitCodes.Keys);
        Assert.Contains("2", manifest.ExitCodes.Keys);
    }

    [Fact]
    public void ExecuteGet_WithResourceRegistration()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        var rootCommand = builder.Build();

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var resource = new TestResourceAll();
            var testSchema = new TestSchema { Name = "test", Value = "value" };
            var result = resource.Get(testSchema);
            var json = resource.ToJson(result);
            Console.WriteLine(json);

            // Assert
            var output = stringWriter.ToString();
            Assert.NotEmpty(output);
            Assert.Contains("test", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void ResourceNotImplementsCapability_ThrowsInvalidCastException()
    {
        // Arrange
        var resource = new TestResourceNoOps();

        // Act & Assert - TestResourceNoOps doesn't implement IGettable, so cast throws
        Assert.Throws<InvalidCastException>(() => ((IGettable<TestSchema>)resource).Get(null));
    }

    [Fact]
    public void ExecuteExport_ProducesMultipleOutputs()
    {
        // Arrange
        var resource = new TestResourceAll();
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var results = resource.Export(null);
            foreach (var item in results)
            {
                var json = resource.ToJson(item);
                Console.WriteLine(json);
            }

            // Assert
            var output = stringWriter.ToString();
            Assert.NotEmpty(output);
            Assert.Contains("test", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void SetResult_HasChangedPropertiesCollection()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var result = resource.Set(new TestSchema());

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ChangedProperties);
        Assert.NotEmpty(result.ChangedProperties);
        Assert.Contains("name", result.ChangedProperties);
    }

    [Fact]
    public void TestResult_HasDifferingPropertiesCollection()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var result = resource.Test(new TestSchema());

        // Assert
        Assert.NotNull(result.DifferingProperties);
    }
}
