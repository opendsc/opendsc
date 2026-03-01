// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

public class ParameterValidatorTests
{
    private readonly ParameterValidator _validator;
    private readonly ParameterSchemaBuilder _schemaBuilder;

    public ParameterValidatorTests()
    {
        _validator = new ParameterValidator();
        _schemaBuilder = new ParameterSchemaBuilder();
    }

    [Fact]
    public void Validate_WithValidYaml_ShouldReturnSuccess()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition
            {
                Type = "string"
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
parameters:
  name: TestValue
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidJson_ShouldReturnSuccess()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition
            {
                Type = "string"
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var json = @"
{
  ""parameters"": {
    ""name"": ""TestValue""
  }
}
";

        // Act
        var result = _validator.Validate(schemaString, json);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingRequiredParameter_ShouldReturnError()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition
            {
                Type = "string"
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
parameters:
  otherField: value
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithWrongType_ShouldReturnError()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["count"] = new ParameterDefinition
            {
                Type = "int"
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
parameters:
  count: not_a_number
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidEnumValue_ShouldReturnError()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["env"] = new ParameterDefinition
            {
                Type = "string",
                AllowedValues = new object[] { "dev", "test", "prod" }
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
parameters:
  env: invalid
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithValueBelowMinimum_ShouldReturnError()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["port"] = new ParameterDefinition
            {
                Type = "int",
                MinValue = 1024
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
parameters:
  port: 80
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithValueAboveMaximum_ShouldReturnError()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["port"] = new ParameterDefinition
            {
                Type = "int",
                MaxValue = 65535
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
parameters:
  port: 99999
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithStringBelowMinLength_ShouldReturnError()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["password"] = new ParameterDefinition
            {
                Type = "string",
                MinLength = 8
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
parameters:
  password: short
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithStringAboveMaxLength_ShouldReturnError()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition
            {
                Type = "string",
                MaxLength = 10
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
parameters:
  name: VeryLongNameThatExceedsMaximum
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithAdditionalProperties_ShouldReturnError()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition
            {
                Type = "string"
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
parameters:
  name: TestValue
  unexpected: property
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithMissingParametersRoot_ShouldReturnError()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition
            {
                Type = "string"
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
name: TestValue
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidYaml_ShouldReturnParseError()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var invalidYaml = @"
parameters:
  invalid: [unclosed
";

        // Act
        var result = _validator.Validate(schemaString, invalidYaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidJson_ShouldReturnParseError()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var invalidJson = @"{ ""parameters"": { ""name"": }";

        // Act
        var result = _validator.Validate(schemaString, invalidJson);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithComplexNestedObject_ShouldValidateCorrectly()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["config"] = new ParameterDefinition
            {
                Type = "object"
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
parameters:
  config:
    server:
      host: localhost
      port: 8080
    database:
      name: testdb
      timeout: 30
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithArrayType_ShouldValidateCorrectly()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["items"] = new ParameterDefinition
            {
                Type = "array"
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
parameters:
  items:
    - item1
    - item2
    - item3
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithBooleanType_ShouldValidateCorrectly()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["enabled"] = new ParameterDefinition
            {
                Type = "bool"
            }
        };
        var schema = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schemaString = _schemaBuilder.SerializeSchema(schema);

        var yaml = @"
parameters:
  enabled: true
";

        // Act
        var result = _validator.Validate(schemaString, yaml);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
