// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Resource.Tests;

[Trait("Category", "Unit")]
public class DscResourceTests
{
    private readonly TestResource _resource = new(SourceGenerationContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJsonString()
    {
        var schema = _resource.GetSchema();

        schema.Should().NotBeEmpty();
        var doc = JsonDocument.Parse(schema);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void GetSchema_ContainsPropertiesObject()
    {
        var schema = _resource.GetSchema();
        var doc = JsonDocument.Parse(schema);

        doc.RootElement.TryGetProperty("properties", out var properties).Should().BeTrue();
        properties.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void GetSchema_ContainsSchemaProperties()
    {
        var schema = _resource.GetSchema();
        var doc = JsonDocument.Parse(schema);

        var properties = doc.RootElement.GetProperty("properties");
        properties.TryGetProperty("name", out _).Should().BeTrue();
        properties.TryGetProperty("value", out _).Should().BeTrue();
        properties.TryGetProperty("enabled", out _).Should().BeTrue();
    }

    [Fact]
    public void Parse_ValidJson_ReturnsDeserializedInstance()
    {
        var json = """{"name":"test","value":42,"enabled":true}""";

        var instance = _resource.Parse(json);

        instance.Should().NotBeNull();
        instance.Name.Should().Be("test");
        instance.Value.Should().Be(42);
        instance.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Parse_ValidJsonWithNullName_ReturnsInstanceWithNullName()
    {
        var json = """{"name":null,"value":10,"enabled":false}""";

        var instance = _resource.Parse(json);

        instance.Should().NotBeNull();
        instance.Name.Should().BeNull();
        instance.Value.Should().Be(10);
        instance.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsJsonException()
    {
        var json = "not valid json";

        var action = () => _resource.Parse(json);

        action.Should().Throw<JsonException>();
    }

    [Fact]
    public void ToJson_ValidInstance_ReturnsJsonString()
    {
        var instance = new TestSchema { Name = "test", Value = 42, Enabled = true };

        var json = _resource.ToJson(instance);

        json.Should().NotBeEmpty();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void ToJson_InstanceWithNullName_ReturnsJsonWithoutName()
    {
        var instance = new TestSchema { Value = 10, Enabled = false };

        var json = _resource.ToJson(instance);

        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("name", out _).Should().BeFalse();
    }

    [Fact]
    public void ToJson_InstanceAndParse_RoundTrip()
    {
        var original = new TestSchema { Name = "original", Value = 99, Enabled = true };

        var json = _resource.ToJson(original);
        var parsed = _resource.Parse(json);

        parsed.Name.Should().Be(original.Name);
        parsed.Value.Should().Be(original.Value);
        parsed.Enabled.Should().Be(original.Enabled);
    }

    [Fact]
    public void Get_WithInstance_ReturnsInstance()
    {
        var instance = new TestSchema { Name = "test", Value = 42 };

        var result = _resource.Get(instance);

        result.Should().Be(instance);
    }

    [Fact]
    public void Get_WithNull_ReturnsEmptyTestSchema()
    {
        var result = _resource.Get(null);

        result.Should().NotBeNull();
    }

    [Fact]
    public void Set_WithValidInstance_ReturnsSetResultWithChangedProperties()
    {
        var instance = new TestSchema { Name = "test", Value = 42 };

        var result = _resource.Set(instance);

        result.Should().NotBeNull();
        result!.ActualState.Should().Be(instance);
        result.ChangedProperties.Should().Contain("Value");
        result.ChangedProperties.Should().Contain("Enabled");
    }

    [Fact]
    public void Set_WithNull_ReturnsNull()
    {
        var result = _resource.Set(null);

        result.Should().BeNull();
    }

    [Fact]
    public void Test_WithValidInstance_ReturnsTestResultWithDifferingProperties()
    {
        var instance = new TestSchema { Name = "test", Value = 42 };

        var result = _resource.Test(instance);

        result.Should().NotBeNull();
        result.ActualState.Should().Be(instance);
        result.DifferingProperties.Should().Contain("Value");
    }

    [Fact]
    public void Test_WithNull_ReturnsTestResultWithEmptySchema()
    {
        var result = _resource.Test(null);

        result.Should().NotBeNull();
    }

    [Fact]
    public void Delete_WithInstance_CompletesWithoutException()
    {
        var instance = new TestSchema { Name = "test" };

        var action = () => _resource.Delete(instance);

        action.Should().NotThrow();
    }
}
