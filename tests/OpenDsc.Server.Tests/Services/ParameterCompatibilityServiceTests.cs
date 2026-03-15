// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

public class ParameterCompatibilityServiceTests
{
    private readonly ParameterCompatibilityService _service;
    private readonly ParameterSchemaBuilder _schemaBuilder;

    public ParameterCompatibilityServiceTests()
    {
        _service = new ParameterCompatibilityService(NullLogger<ParameterCompatibilityService>.Instance);
        _schemaBuilder = new ParameterSchemaBuilder();
    }

    [Fact]
    public void CompareSchemas_WithIdenticalSchemas_ShouldReturnNoChanges()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" }
        };
        var schema1 = _schemaBuilder.BuildJsonSchema(parametersBlock);
        var schema2 = _schemaBuilder.BuildJsonSchema(parametersBlock);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(schema1), JsonSerializer.Serialize(schema2), "1.0.0", "1.0.0");

        // Assert
        report.HasBreakingChanges.Should().BeFalse();
        report.BreakingChanges.Should().BeEmpty();
        report.NonBreakingChanges.Should().BeEmpty();
    }

    [Fact]
    public void CompareSchemas_WithAddedParameter_ShouldBeNonBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" },
            ["age"] = new ParameterDefinition { Type = "int", DefaultValue = 0 }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "1.1.0");

        // Assert
        report.HasBreakingChanges.Should().BeFalse();
        report.NonBreakingChanges.Should().ContainSingle();
        report.NonBreakingChanges[0].ParameterName.Should().Be("age");
        report.NonBreakingChanges[0].ChangeType.Should().Be("ParameterAdded");
    }

    [Fact]
    public void CompareSchemas_WithRemovedParameter_ShouldBeBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" },
            ["age"] = new ParameterDefinition { Type = "int" }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "2.0.0");

        // Assert
        report.HasBreakingChanges.Should().BeTrue();
        report.BreakingChanges.Should().ContainSingle();
        report.BreakingChanges[0].ParameterName.Should().Be("age");
        report.BreakingChanges[0].ChangeType.Should().Be("ParameterRemoved");
    }

    [Fact]
    public void CompareSchemas_WithTypeChange_ShouldBeBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["count"] = new ParameterDefinition { Type = "string" }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["count"] = new ParameterDefinition { Type = "int" }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "2.0.0");

        // Assert
        report.HasBreakingChanges.Should().BeTrue();
        report.BreakingChanges.Should().ContainSingle();
        report.BreakingChanges[0].ParameterName.Should().Be("count");
        report.BreakingChanges[0].ChangeType.Should().Be("TypeChanged");
        report.BreakingChanges[0].OldValue.Should().Be("string");
        report.BreakingChanges[0].NewValue.Should().Be("integer");
    }

    [Fact]
    public void CompareSchemas_WithRequiredToOptional_ShouldBeNonBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string", DefaultValue = "default" }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "1.1.0");

        // Assert
        report.HasBreakingChanges.Should().BeFalse();
        report.NonBreakingChanges.Should().NotBeEmpty();
        var change = report.NonBreakingChanges.FirstOrDefault(c => c.ParameterName == "name");
        change.Should().NotBeNull();
        change!.ChangeType.Should().Be("BecameOptional");
    }

    [Fact]
    public void CompareSchemas_WithOptionalToRequired_ShouldBeBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string", DefaultValue = "default" }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "2.0.0");

        // Assert
        report.HasBreakingChanges.Should().BeTrue();
        var change = report.BreakingChanges.FirstOrDefault(c => c.ParameterName == "name");
        change.Should().NotBeNull();
        change!.ChangeType.Should().Be("BecameRequired");
    }

    [Fact]
    public void CompareSchemas_WithEnumValuesAdded_ShouldBeNonBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["env"] = new ParameterDefinition
            {
                Type = "string",
                AllowedValues = new object[] { "dev", "test" }
            }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["env"] = new ParameterDefinition
            {
                Type = "string",
                AllowedValues = new object[] { "dev", "test", "prod" }
            }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "1.1.0");

        // Assert
        report.HasBreakingChanges.Should().BeFalse();
        report.NonBreakingChanges.Should().ContainSingle();
        report.NonBreakingChanges[0].ParameterName.Should().Be("env");
        report.NonBreakingChanges[0].ChangeType.Should().Be("AllowedValuesExpanded");
    }

    [Fact]
    public void CompareSchemas_WithEnumValuesRemoved_ShouldBeBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["env"] = new ParameterDefinition
            {
                Type = "string",
                AllowedValues = new object[] { "dev", "test", "prod" }
            }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["env"] = new ParameterDefinition
            {
                Type = "string",
                AllowedValues = new object[] { "dev", "test" }
            }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "2.0.0");

        // Assert
        report.HasBreakingChanges.Should().BeTrue();
        report.BreakingChanges.Should().ContainSingle();
        report.BreakingChanges[0].ParameterName.Should().Be("env");
        report.BreakingChanges[0].ChangeType.Should().Be("AllowedValuesReduced");
    }

    [Fact]
    public void CompareSchemas_WithMinimumIncreased_ShouldBeBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["port"] = new ParameterDefinition
            {
                Type = "int",
                MinValue = 1024
            }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["port"] = new ParameterDefinition
            {
                Type = "int",
                MinValue = 8080
            }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "2.0.0");

        // Assert
        report.HasBreakingChanges.Should().BeTrue();
        var change = report.BreakingChanges.FirstOrDefault(c => c.ParameterName == "port");
        change.Should().NotBeNull();
        change!.ChangeType.Should().Be("MinValueIncreased");
    }

    [Fact]
    public void CompareSchemas_WithMinimumDecreased_ShouldBeNonBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["port"] = new ParameterDefinition
            {
                Type = "int",
                MinValue = 8080
            }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["port"] = new ParameterDefinition
            {
                Type = "int",
                MinValue = 1024
            }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "1.1.0");

        // Assert
        report.HasBreakingChanges.Should().BeFalse();
        // Note: The implementation currently does not track constraint loosening (min decreased)
        // This is non-breaking but not reported as a change
        report.NonBreakingChanges.Should().BeEmpty();
        report.BreakingChanges.Should().BeEmpty();
    }

    [Fact]
    public void CompareSchemas_WithMaximumDecreased_ShouldBeBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["port"] = new ParameterDefinition
            {
                Type = "int",
                MaxValue = 65535
            }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["port"] = new ParameterDefinition
            {
                Type = "int",
                MaxValue = 9999
            }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "2.0.0");

        // Assert
        report.HasBreakingChanges.Should().BeTrue();
        var change = report.BreakingChanges.FirstOrDefault(c => c.ParameterName == "port");
        change.Should().NotBeNull();
        change!.ChangeType.Should().Be("MaxValueDecreased");
    }

    [Fact]
    public void CompareSchemas_WithMaximumIncreased_ShouldBeNonBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["port"] = new ParameterDefinition
            {
                Type = "int",
                MaxValue = 9999
            }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["port"] = new ParameterDefinition
            {
                Type = "int",
                MaxValue = 65535
            }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "1.1.0");

        // Assert
        report.HasBreakingChanges.Should().BeFalse();
        // Note: The implementation currently does not track constraint loosening (max increased)
        // This is non-breaking but not reported as a change
        report.NonBreakingChanges.Should().BeEmpty();
        report.BreakingChanges.Should().BeEmpty();
    }

    [Fact]
    public void CompareSchemas_WithMinLengthIncreased_ShouldBeBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["password"] = new ParameterDefinition
            {
                Type = "string",
                MinLength = 6
            }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["password"] = new ParameterDefinition
            {
                Type = "string",
                MinLength = 12
            }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "2.0.0");

        // Assert
        report.HasBreakingChanges.Should().BeTrue();
        var change = report.BreakingChanges.FirstOrDefault(c => c.ParameterName == "password");
        change.Should().NotBeNull();
        change!.ChangeType.Should().Be("MinLengthIncreased");
    }

    [Fact]
    public void CompareSchemas_WithMaxLengthDecreased_ShouldBeBreaking()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition
            {
                Type = "string",
                MaxLength = 100
            }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition
            {
                Type = "string",
                MaxLength = 50
            }
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "2.0.0");

        // Assert
        report.HasBreakingChanges.Should().BeTrue();
        var change = report.BreakingChanges.FirstOrDefault(c => c.ParameterName == "name");
        change.Should().NotBeNull();
        change!.ChangeType.Should().Be("MaxLengthDecreased");
    }

    [Fact]
    public void CompareSchemas_WithMultipleBreakingChanges_ShouldDetectAll()
    {
        // Arrange
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["param1"] = new ParameterDefinition { Type = "string" },
            ["param2"] = new ParameterDefinition { Type = "int" },
            ["param3"] = new ParameterDefinition { Type = "bool" }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["param1"] = new ParameterDefinition { Type = "int" },  // type change
            ["param3"] = new ParameterDefinition { Type = "bool" }
            // param2 removed
        };
        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "2.0.0");

        // Assert
        report.HasBreakingChanges.Should().BeTrue();
        report.BreakingChanges.Should().HaveCount(2);
        report.BreakingChanges.Should().Contain(c => c.ParameterName == "param1" && c.ChangeType == "TypeChanged");
        report.BreakingChanges.Should().Contain(c => c.ParameterName == "param2" && c.ChangeType == "ParameterRemoved");
    }

    [Fact]
    public void CompareSchemas_WithNestedSchemaStructure_ShouldExtractCorrectly()
    {
        // Arrange - test that the service correctly extracts from the nested "parameters" wrapper
        var oldParams = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" }
        };
        var newParams = new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" },
            ["age"] = new ParameterDefinition { Type = "int", DefaultValue = 0 }
        };

        var oldSchema = _schemaBuilder.BuildJsonSchema(oldParams);
        var newSchema = _schemaBuilder.BuildJsonSchema(newParams);

        // Act
        var report = _service.CompareSchemas(JsonSerializer.Serialize(oldSchema), JsonSerializer.Serialize(newSchema), "1.0.0", "1.1.0");

        // Assert - should correctly identify the added parameter
        report.HasBreakingChanges.Should().BeFalse();
        report.NonBreakingChanges.Should().ContainSingle();
        report.NonBreakingChanges[0].ParameterName.Should().Be("age");
    }
}
