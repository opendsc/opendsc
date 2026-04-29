// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

public class ParameterSchemaBuilderTests
{
    private readonly ParameterSchemaBuilder _builder;

    public ParameterSchemaBuilderTests()
    {
        _builder = new ParameterSchemaBuilder();
    }

    [Fact]
    public void BuildJsonSchema_ShouldWrapInRootParametersObject()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["stringParam"] = new ParameterDefinition
            {
                Type = "string"
            }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));
        var root = schemaJson.RootElement;

        // Assert
        root.TryGetProperty("properties", out var properties).Should().BeTrue();
        properties.TryGetProperty("parameters", out var parametersProperty).Should().BeTrue();
        parametersProperty.TryGetProperty("type", out var parametersType).Should().BeTrue();
        parametersType.GetString().Should().Be("object");
        parametersProperty.TryGetProperty("properties", out var innerProperties).Should().BeTrue();
        innerProperties.TryGetProperty("stringParam", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("string")]
    [InlineData("secureString")]
    public void BuildJsonSchema_ShouldMapStringTypes(string dscType)
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["testParam"] = new ParameterDefinition
            {
                Type = dscType
            }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var paramSchema = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties")
            .GetProperty("testParam");

        paramSchema.GetProperty("type").GetString().Should().Be("string");
    }

    [Fact]
    public void BuildJsonSchema_ShouldMapIntType()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["intParam"] = new ParameterDefinition
            {
                Type = "int"
            }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var paramSchema = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties")
            .GetProperty("intParam");

        paramSchema.GetProperty("type").GetString().Should().Be("integer");
    }

    [Fact]
    public void BuildJsonSchema_ShouldMapBoolType()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["boolParam"] = new ParameterDefinition
            {
                Type = "bool"
            }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var paramSchema = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties")
            .GetProperty("boolParam");

        paramSchema.GetProperty("type").GetString().Should().Be("boolean");
    }

    [Theory]
    [InlineData("object")]
    [InlineData("secureObject")]
    public void BuildJsonSchema_ShouldMapObjectTypes(string dscType)
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["objParam"] = new ParameterDefinition
            {
                Type = dscType
            }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var paramSchema = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties")
            .GetProperty("objParam");

        paramSchema.GetProperty("type").GetString().Should().Be("object");
    }

    [Fact]
    public void BuildJsonSchema_ShouldMapArrayType()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["arrayParam"] = new ParameterDefinition
            {
                Type = "array"
            }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var paramSchema = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties")
            .GetProperty("arrayParam");

        paramSchema.GetProperty("type").GetString().Should().Be("array");
    }

    [Fact]
    public void BuildJsonSchema_ShouldMapAllowedValuesToEnum()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["enumParam"] = new ParameterDefinition
            {
                Type = "string",
                AllowedValues = new object[] { "value1", "value2", "value3" }
            }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var paramSchema = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties")
            .GetProperty("enumParam");

        paramSchema.TryGetProperty("enum", out var enumValues).Should().BeTrue();
        var enumArray = enumValues.EnumerateArray().Select(e => e.GetString()).ToArray();
        enumArray.Should().BeEquivalentTo(new[] { "value1", "value2", "value3" });
    }

    [Fact]
    public void BuildJsonSchema_ShouldMapMinValueAndMaxValue()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["rangeParam"] = new ParameterDefinition
            {
                Type = "int",
                MinValue = 1,
                MaxValue = 100
            }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var paramSchema = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties")
            .GetProperty("rangeParam");

        paramSchema.GetProperty("minimum").GetInt32().Should().Be(1);
        paramSchema.GetProperty("maximum").GetInt32().Should().Be(100);
    }

    [Fact]
    public void BuildJsonSchema_ShouldMapMinLengthAndMaxLength()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["lengthParam"] = new ParameterDefinition
            {
                Type = "string",
                MinLength = 5,
                MaxLength = 50
            }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var paramSchema = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties")
            .GetProperty("lengthParam");

        paramSchema.GetProperty("minLength").GetInt32().Should().Be(5);
        paramSchema.GetProperty("maxLength").GetInt32().Should().Be(50);
    }

    [Fact]
    public void BuildJsonSchema_WithDefaultValue_ShouldNotMarkAsRequired()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["optionalParam"] = new ParameterDefinition
            {
                Type = "string",
                DefaultValue = "default"
            }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var parametersProperty = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters");

        // Parameter with defaultValue should not be in required array
        parametersProperty.TryGetProperty("required", out var required).Should().BeFalse();
    }

    [Fact]
    public void BuildJsonSchema_WithoutDefaultValue_ShouldMarkAsRequired()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["requiredParam"] = new ParameterDefinition
            {
                Type = "string"
            }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var parametersProperty = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters");

        parametersProperty.TryGetProperty("required", out var required).Should().BeTrue();
        var requiredArray = required.EnumerateArray().Select(e => e.GetString()).ToArray();
        requiredArray.Should().Contain("requiredParam");
    }

    [Fact]
    public void BuildJsonSchema_WithMetadata_ShouldIncludeDescriptionAndTitle()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["documentedParam"] = new ParameterDefinition
            {
                Type = "string",
                Description = "Test description"
            }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var paramSchema = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties")
            .GetProperty("documentedParam");

        paramSchema.TryGetProperty("description", out var description).Should().BeTrue();
        description.GetString().Should().Be("Test description");
    }

    [Fact]
    public void BuildJsonSchema_WithMultipleParameters_ShouldIncludeAll()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["param1"] = new ParameterDefinition { Type = "string" },
            ["param2"] = new ParameterDefinition { Type = "int" },
            ["param3"] = new ParameterDefinition { Type = "bool" }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var properties = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties");

        properties.TryGetProperty("param1", out _).Should().BeTrue();
        properties.TryGetProperty("param2", out _).Should().BeTrue();
        properties.TryGetProperty("param3", out _).Should().BeTrue();
    }

    [Fact]
    public void BuildJsonSchema_WithEmptyParametersBlock_ShouldReturnSchemaWithEmptyProperties()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>();

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var properties = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties");

        properties.EnumerateObject().Should().BeEmpty();
    }

    [Fact]
    public void BuildJsonSchema_ShouldSetAdditionalPropertiesFalse()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["param1"] = new ParameterDefinition { Type = "string" }
        };

        // Act
        var schema = _builder.BuildJsonSchema(parametersBlock);
        var schemaJson = JsonDocument.Parse(JsonSerializer.Serialize(schema));

        // Assert
        var parametersProperty = schemaJson.RootElement
            .GetProperty("properties")
            .GetProperty("parameters");

        parametersProperty.TryGetProperty("additionalProperties", out var additionalProps).Should().BeTrue();
        additionalProps.GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void SerializeSchema_ShouldProduceValidJsonString()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["testParam"] = new ParameterDefinition { Type = "string" }
        };

        var schema = _builder.BuildJsonSchema(parametersBlock);

        // Act
        var serialized = _builder.SerializeSchema(schema);

        // Assert
        serialized.Should().NotBeNullOrEmpty();
        var parsed = JsonDocument.Parse(serialized);
        parsed.Should().NotBeNull();
        parsed.RootElement.TryGetProperty("properties", out _).Should().BeTrue();
    }

    [Fact]
    public void SerializeSchema_WithComplexSchema_ProducesValidJson()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["requiredParam"] = new ParameterDefinition { Type = "string" },
            ["optionalParam"] = new ParameterDefinition { Type = "int", DefaultValue = 42 },
            ["enumParam"] = new ParameterDefinition { Type = "string", AllowedValues = new object[] { "a", "b", "c" } }
        };

        var schema = _builder.BuildJsonSchema(parametersBlock);

        // Act
        var serialized = _builder.SerializeSchema(schema);

        // Assert
        serialized.Should().NotBeNullOrEmpty();
        var parsed = JsonDocument.Parse(serialized);
        var properties = parsed.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties");

        properties.TryGetProperty("requiredParam", out _).Should().BeTrue();
        properties.TryGetProperty("optionalParam", out _).Should().BeTrue();
        properties.TryGetProperty("enumParam", out _).Should().BeTrue();
    }
}
