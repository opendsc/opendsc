// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

/// <summary>
/// Unit tests for <see cref="JsonSchemaFormBuilder"/> and its output models.
/// Also exercises the round-trip from <see cref="ParameterSchemaBuilder"/> through
/// <see cref="JsonSchemaFormBuilder"/> to verify the two services interoperate correctly.
/// </summary>
[Trait("Category", "Unit")]
public class JsonSchemaFormBuilderTests
{
    private readonly JsonSchemaFormBuilder _formBuilder = new();
    private readonly ParameterSchemaBuilder _schemaBuilder = new();

    // ──────────────────────────────────────────────────────────────
    // Helper: build a serialized JSON Schema from a parameter dict
    // ──────────────────────────────────────────────────────────────

    private string BuildSchema(Dictionary<string, ParameterDefinition> parameters)
    {
        var schema = _schemaBuilder.BuildJsonSchema(parameters);
        return _schemaBuilder.SerializeSchema(schema);
    }

    // ──────────────────────────────────────────────────────────────
    // Null / empty / malformed input
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildParameterForm_NullOrWhitespaceSchema_ReturnsEmptyDefinition()
    {
        _formBuilder.BuildParameterForm(null!).Fields.Should().BeEmpty();
        _formBuilder.BuildParameterForm("").Fields.Should().BeEmpty();
        _formBuilder.BuildParameterForm("   ").Fields.Should().BeEmpty();
    }

    [Fact]
    public void BuildParameterForm_InvalidJson_ReturnsEmptyDefinition()
    {
        var result = _formBuilder.BuildParameterForm("{ not valid json [[[");
        result.Fields.Should().BeEmpty();
    }

    [Fact]
    public void BuildParameterForm_JsonWithoutParametersEnvelope_ReturnsEmptyDefinition()
    {
        // JSON Schema without the expected { properties: { parameters: { properties: ... } } } envelope
        var result = _formBuilder.BuildParameterForm("{\"type\":\"object\"}");
        result.Fields.Should().BeEmpty();
    }

    [Fact]
    public void BuildParameterForm_EmptyParametersBlock_ReturnsEmptyDefinition()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>());
        var result = _formBuilder.BuildParameterForm(schemaJson);
        result.Fields.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // SchemaFormDefinition computed properties
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void SchemaFormDefinition_EmptyFields_HasNoSupportedAndNoUnsupported()
    {
        var def = new SchemaFormDefinition { Fields = [] };
        def.HasSupportedFields.Should().BeFalse();
        def.HasUnsupportedFields.Should().BeFalse();
    }

    [Fact]
    public void SchemaFormDefinition_AllSupportedFields_HasSupportedNotUnsupported()
    {
        var def = new SchemaFormDefinition
        {
            Fields =
            [
                new SchemaFormField { Name = "a", Kind = SchemaFieldKind.String, IsRequired = true },
                new SchemaFormField { Name = "b", Kind = SchemaFieldKind.Integer, IsRequired = false }
            ]
        };
        def.HasSupportedFields.Should().BeTrue();
        def.HasUnsupportedFields.Should().BeFalse();
    }

    [Fact]
    public void SchemaFormDefinition_MixedFields_HasBothSupportedAndUnsupported()
    {
        var def = new SchemaFormDefinition
        {
            Fields =
            [
                new SchemaFormField { Name = "a", Kind = SchemaFieldKind.String, IsRequired = true },
                new SchemaFormField { Name = "b", Kind = SchemaFieldKind.Unsupported, IsRequired = false }
            ]
        };
        def.HasSupportedFields.Should().BeTrue();
        def.HasUnsupportedFields.Should().BeTrue();
    }

    [Fact]
    public void SchemaFormDefinition_AllUnsupportedFields_HasNoSupportedAndHasUnsupported()
    {
        var def = new SchemaFormDefinition
        {
            Fields =
            [
                new SchemaFormField { Name = "a", Kind = SchemaFieldKind.Unsupported, IsRequired = true }
            ]
        };
        def.HasSupportedFields.Should().BeFalse();
        def.HasUnsupportedFields.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    // DSC type → SchemaFieldKind mapping
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("string", SchemaFieldKind.String)]
    [InlineData("securestring", SchemaFieldKind.String)]   // secure string rendered as password text field
    [InlineData("secureString", SchemaFieldKind.String)]   // backward-compat casing
    [InlineData("int", SchemaFieldKind.Integer)]
    [InlineData("bool", SchemaFieldKind.Boolean)]
    [InlineData("object", SchemaFieldKind.Unsupported)]
    [InlineData("secureobject", SchemaFieldKind.Unsupported)]
    [InlineData("secureObject", SchemaFieldKind.Unsupported)]
    [InlineData("array", SchemaFieldKind.Unsupported)]
    public void BuildParameterForm_DscType_MapsToCorrectKind(string dscType, SchemaFieldKind expectedKind)
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["param"] = new ParameterDefinition { Type = dscType, DefaultValue = GetDefaultForType(dscType) }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        result.Fields.Should().HaveCount(1);
        result.Fields[0].Kind.Should().Be(expectedKind);
    }

    // ──────────────────────────────────────────────────────────────
    // Required vs optional
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildParameterForm_ParameterWithoutDefault_IsRequired()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["required"] = new ParameterDefinition { Type = "string" }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        result.Fields.Single().IsRequired.Should().BeTrue();
    }

    [Fact]
    public void BuildParameterForm_ParameterWithDefault_IsNotRequired()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["optional"] = new ParameterDefinition { Type = "string", DefaultValue = "hello" }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        result.Fields.Single().IsRequired.Should().BeFalse();
    }

    [Fact]
    public void BuildParameterForm_MixedRequiredAndOptional_BothPresent()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["req"] = new ParameterDefinition { Type = "string" },
            ["opt"] = new ParameterDefinition { Type = "int", DefaultValue = 0 }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        result.Fields.Should().HaveCount(2);
        result.Fields.First(f => f.Name == "req").IsRequired.Should().BeTrue();
        result.Fields.First(f => f.Name == "opt").IsRequired.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // Description
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildParameterForm_ParameterWithDescription_DescriptionPreserved()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["param"] = new ParameterDefinition { Type = "string", Description = "My description" }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        result.Fields.Single().Description.Should().Be("My description");
    }

    [Fact]
    public void BuildParameterForm_ParameterWithoutDescription_DescriptionIsNull()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["param"] = new ParameterDefinition { Type = "int" }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        result.Fields.Single().Description.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────
    // Integer min/max (DSC minValue/maxValue)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildParameterForm_IntWithMinAndMaxValue_MinMaxPopulated()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["port"] = new ParameterDefinition { Type = "int", MinValue = 0, MaxValue = 10 }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        var field = result.Fields.Single();
        field.Kind.Should().Be(SchemaFieldKind.Integer);
        field.Minimum.Should().Be(0m);
        field.Maximum.Should().Be(10m);
    }

    [Fact]
    public void BuildParameterForm_IntWithMinValueOnly_MaxIsNull()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["param"] = new ParameterDefinition { Type = "int", MinValue = 1 }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        var field = result.Fields.Single();
        field.Minimum.Should().Be(1m);
        field.Maximum.Should().BeNull();
    }

    [Fact]
    public void BuildParameterForm_IntWithMaxValueOnly_MinIsNull()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["param"] = new ParameterDefinition { Type = "int", MaxValue = 100 }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        var field = result.Fields.Single();
        field.Minimum.Should().BeNull();
        field.Maximum.Should().Be(100m);
    }

    [Fact]
    public void BuildParameterForm_IntWithoutMinOrMax_BothNull()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["param"] = new ParameterDefinition { Type = "int" }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        var field = result.Fields.Single();
        field.Minimum.Should().BeNull();
        field.Maximum.Should().BeNull();
    }

    [Theory]
    [InlineData(-100, 100)]
    [InlineData(0, 65535)]
    [InlineData(-2147483648, 2147483647)]
    public void BuildParameterForm_IntRange_RoundTripsCorrectly(int min, int max)
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["param"] = new ParameterDefinition { Type = "int", MinValue = min, MaxValue = max }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        var field = result.Fields.Single();
        field.Minimum.Should().Be((decimal)min);
        field.Maximum.Should().Be((decimal)max);
    }

    // ──────────────────────────────────────────────────────────────
    // allowedValues → Enum kind
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildParameterForm_StringWithAllowedValues_KindIsEnum()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["env"] = new ParameterDefinition
            {
                Type = "string",
                AllowedValues = new object[] { "dev", "test", "prod" }
            }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        var field = result.Fields.Single();
        field.Kind.Should().Be(SchemaFieldKind.Enum);
    }

    [Fact]
    public void BuildParameterForm_StringWithAllowedValues_EnumValuesPopulated()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["env"] = new ParameterDefinition
            {
                Type = "string",
                AllowedValues = new object[] { "dev", "test", "prod" }
            }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        var values = result.Fields.Single().EnumValues.Select(v => v?.ToJsonString()).ToList();
        values.Should().BeEquivalentTo(new[] { "\"dev\"", "\"test\"", "\"prod\"" });
    }

    [Fact]
    public void BuildParameterForm_IntWithAllowedValues_KindIsEnum()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["level"] = new ParameterDefinition
            {
                Type = "int",
                AllowedValues = new object[] { 1, 2, 3 }
            }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        result.Fields.Single().Kind.Should().Be(SchemaFieldKind.Enum);
    }

    // ──────────────────────────────────────────────────────────────
    // Multiple parameters
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildParameterForm_MultipleParameters_AllFieldsPresent()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" },
            ["count"] = new ParameterDefinition { Type = "int", DefaultValue = 1 },
            ["enabled"] = new ParameterDefinition { Type = "bool", DefaultValue = true },
            ["config"] = new ParameterDefinition { Type = "object", DefaultValue = new Dictionary<string, object>() }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        result.Fields.Should().HaveCount(4);
        result.Fields.Select(f => f.Name).Should().BeEquivalentTo(
            new[] { "name", "count", "enabled", "config" });
    }

    [Fact]
    public void BuildParameterForm_MixedSupportedAndUnsupported_HasBothFlags()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["name"] = new ParameterDefinition { Type = "string" },
            ["settings"] = new ParameterDefinition { Type = "object", DefaultValue = new Dictionary<string, object>() }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        result.HasSupportedFields.Should().BeTrue();
        result.HasUnsupportedFields.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    // Field names and field structure
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void BuildParameterForm_FieldName_MatchesParameterName()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["mySpecialParameter"] = new ParameterDefinition { Type = "string" }
        });

        var result = _formBuilder.BuildParameterForm(schemaJson);
        result.Fields.Single().Name.Should().Be("mySpecialParameter");
    }

    // ──────────────────────────────────────────────────────────────
    // Full round-trip: ParameterSchemaBuilder → JsonSchemaFormBuilder
    // Validates the "view schema" scenario: the schema stored and returned
    // is the correct JSON Schema derived from the DSC parameter spec.
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_IntRangeParameter_FormEnforcesRange()
    {
        // Represent a DSC config with a single int parameter constrained 0–10
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["level"] = new ParameterDefinition
            {
                Type = "int",
                MinValue = 0,
                MaxValue = 10,
                Description = "Level between 0 and 10"
            }
        });

        var storedDoc = JsonDocument.Parse(schemaJson);
        var root = storedDoc.RootElement;

        // Verify the generated schema contains minimum/maximum for the form UI
        var paramSchema = root
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties")
            .GetProperty("level");

        paramSchema.GetProperty("type").GetString().Should().Be("integer");
        paramSchema.GetProperty("minimum").GetInt32().Should().Be(0);
        paramSchema.GetProperty("maximum").GetInt32().Should().Be(10);

        // Verify the form builder extracts them for the MudNumericField Min/Max
        var form = _formBuilder.BuildParameterForm(schemaJson);
        var field = form.Fields.Single(f => f.Name == "level");
        field.Kind.Should().Be(SchemaFieldKind.Integer);
        field.Minimum.Should().Be(0m);
        field.Maximum.Should().Be(10m);
        field.Description.Should().Be("Level between 0 and 10");
        field.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_SecureStringParameter_IsStringKind()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["secret"] = new ParameterDefinition { Type = "securestring" }
        });

        // Verify JSON schema has no format annotation
        var doc = JsonDocument.Parse(schemaJson);
        var paramSchema = doc.RootElement
            .GetProperty("properties")
            .GetProperty("parameters")
            .GetProperty("properties")
            .GetProperty("secret");
        paramSchema.TryGetProperty("format", out _).Should().BeFalse();
        paramSchema.GetProperty("type").GetString().Should().Be("string");

        // Verify form builder maps it to a plain String field
        var form = _formBuilder.BuildParameterForm(schemaJson);
        var field = form.Fields.Single();
        field.Kind.Should().Be(SchemaFieldKind.String);
        field.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_AllowedValuesParameter_KindIsEnumWithCorrectValues()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["env"] = new ParameterDefinition
            {
                Type = "string",
                AllowedValues = new object[] { "dev", "staging", "prod" },
                DefaultValue = "dev"
            }
        });

        var form = _formBuilder.BuildParameterForm(schemaJson);
        var field = form.Fields.Single();
        field.Kind.Should().Be(SchemaFieldKind.Enum);
        field.IsRequired.Should().BeFalse(); // has defaultValue
        field.EnumValues.Should().HaveCount(3);
        field.EnumValues.Select(v => v?.ToJsonString())
            .Should().BeEquivalentTo(new[] { "\"dev\"", "\"staging\"", "\"prod\"" });
    }

    [Fact]
    public void RoundTrip_BoolParameter_KindIsBoolean()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["enabled"] = new ParameterDefinition { Type = "bool" }
        });

        var form = _formBuilder.BuildParameterForm(schemaJson);
        var field = form.Fields.Single();
        field.Kind.Should().Be(SchemaFieldKind.Boolean);
        field.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_ObjectParameter_IsUnsupported()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["cfg"] = new ParameterDefinition { Type = "object", DefaultValue = new Dictionary<string, object>() }
        });

        var form = _formBuilder.BuildParameterForm(schemaJson);
        var field = form.Fields.Single();
        field.Kind.Should().Be(SchemaFieldKind.Unsupported);
        form.HasUnsupportedFields.Should().BeTrue();
        form.HasSupportedFields.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_ArrayParameter_IsUnsupported()
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["tags"] = new ParameterDefinition { Type = "array", DefaultValue = Array.Empty<string>() }
        });

        var form = _formBuilder.BuildParameterForm(schemaJson);
        form.Fields.Single().Kind.Should().Be(SchemaFieldKind.Unsupported);
    }

    // ──────────────────────────────────────────────────────────────
    // Validation helpers used by ParameterFormEditor (range errors)
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 10, -1)]    // below minimum
    [InlineData(0, 10, 11)]    // above maximum
    [InlineData(0, 10, -100)]
    [InlineData(0, 10, 100)]
    public void IntField_ValueOutsideRange_SchemaReflectsConstraints(int min, int max, int badValue)
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["n"] = new ParameterDefinition { Type = "int", MinValue = min, MaxValue = max }
        });

        var form = _formBuilder.BuildParameterForm(schemaJson);
        var field = form.Fields.Single();

        // The field exposes min/max for MudNumericField Min/Max and for ValidateInteger
        field.Minimum.Should().Be((decimal)min);
        field.Maximum.Should().Be((decimal)max);

        // Verify that the value IS outside the allowed range
        var isOutOfRange = (decimal)badValue < field.Minimum!.Value || (decimal)badValue > field.Maximum!.Value;
        isOutOfRange.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 10, 0)]     // at minimum boundary
    [InlineData(0, 10, 10)]    // at maximum boundary
    [InlineData(0, 10, 5)]     // in range
    public void IntField_ValueInsideRange_InRange(int min, int max, int goodValue)
    {
        var schemaJson = BuildSchema(new Dictionary<string, ParameterDefinition>
        {
            ["n"] = new ParameterDefinition { Type = "int", MinValue = min, MaxValue = max }
        });

        var form = _formBuilder.BuildParameterForm(schemaJson);
        var field = form.Fields.Single();

        var isInRange = (decimal)goodValue >= field.Minimum!.Value && (decimal)goodValue <= field.Maximum!.Value;
        isInRange.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    // Private helper
    // ──────────────────────────────────────────────────────────────

    private static object? GetDefaultForType(string dscType) => dscType.ToLowerInvariant() switch
    {
        "int" => 0,
        "bool" => false,
        "object" or "secureobject" => new Dictionary<string, object>(),
        "array" => Array.Empty<string>(),
        _ => null
    };
}
