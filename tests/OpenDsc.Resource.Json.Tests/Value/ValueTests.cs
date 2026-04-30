// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using ValueResource = OpenDsc.Resource.Json.Value.Resource;
using ValueSchema = OpenDsc.Resource.Json.Value.Schema;

namespace OpenDsc.Resource.Json.Tests.Value;

[Trait("Category", "Integration")]
public sealed class ValueTests
{
    private readonly ValueResource _resource = new(OpenDsc.Resource.Json.SourceGenerationContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(ValueResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Json/Value");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentFile_ReturnsExistFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.json");

        var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.config.value" });

        result.Exist.Should().BeFalse();
        result.Path.Should().Be(tempFile);
        result.JsonPath.Should().Be("$.config.value");
    }

    [Fact]
    public void Get_NonExistentValue_ReturnsExistFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.config.missing" });

            result.Exist.Should().BeFalse();
            result.Path.Should().Be(tempFile);
            result.JsonPath.Should().Be("$.config.missing");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_ExistingValue_ReturnsValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": { \"name\": \"MyApp\" } }", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.config.name" });

            result.Exist.Should().BeNull();
            result.Value.Should().NotBeNull();
            result.Value?.GetString().Should().Be("MyApp");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_PrimitiveValue_CreatesValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.timeout", Value = JsonDocument.Parse("30").RootElement });

            var content = File.ReadAllText(tempFile);
            var parsed = JsonDocument.Parse(content);
            parsed.RootElement.GetProperty("config").GetProperty("timeout").GetInt32().Should().Be(30);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_NonExistentFile_ThrowsFileNotFoundException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.json");

        var act = () => _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.name", Value = JsonDocument.Parse("\"Test\"").RootElement });

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Delete_ExistingValue_RemovesValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": { \"timeout\": 30, \"name\": \"MyApp\" } }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Delete(new ValueSchema { Path = tempFile, JsonPath = "$.config.timeout", Exist = false });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("config").TryGetProperty("timeout", out _).Should().BeFalse();
            parsed.RootElement.GetProperty("config").GetProperty("name").GetString().Should().Be("MyApp");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Delete_NonExistentValue_DoesNotThrow()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            var act = () => _resource.Delete(new ValueSchema { Path = tempFile, JsonPath = "$.config.missing", Exist = false });
            act.Should().NotThrow();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_ExistingValue_UpdatesValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": { \"name\": \"original\" } }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.name", Value = JsonDocument.Parse("\"updated\"").RootElement });

            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.config.name" });
            result.Value?.GetString().Should().Be("updated");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_NestedPath_CreatesParentNodes()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.a.b.c", Value = JsonDocument.Parse("\"deep\"").RootElement });

            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.a.b.c" });
            result.Value?.GetString().Should().Be("deep");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_ObjectValue_CreatesObject()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.db", Value = JsonDocument.Parse("{ \"host\": \"localhost\", \"port\": 5432 }").RootElement });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("config").GetProperty("db").GetProperty("host").GetString().Should().Be("localhost");
            parsed.RootElement.GetProperty("config").GetProperty("db").GetProperty("port").GetInt32().Should().Be(5432);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_BooleanValue_CreatesTrue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.enabled", Value = JsonDocument.Parse("true").RootElement });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("config").GetProperty("enabled").GetBoolean().Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_NullValue_CreatesJsonNull()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.value", Value = JsonDocument.Parse("null").RootElement });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("config").GetProperty("value").ValueKind.Should().Be(JsonValueKind.Null);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Delete_NonExistentFile_DoesNotThrow()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.json");

        var act = () => _resource.Delete(new ValueSchema { Path = tempFile, JsonPath = "$.config.value", Exist = false });

        act.Should().NotThrow();
    }

    // Array indexing tests
    [Fact]
    public void Get_ArrayElement_ReturnsElement()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"items\": [\"first\", \"second\", \"third\"] }", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.items[1]" });

            result.Exist.Should().BeNull();
            result.Value?.GetString().Should().Be("second");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_ArrayElementOutOfRange_ReturnsExistFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"items\": [\"first\", \"second\"] }", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.items[10]" });

            result.Exist.Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_ArrayElement_SetsElement()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"items\": [\"first\", \"second\", \"third\"] }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.items[1]", Value = JsonDocument.Parse("\"updated\"").RootElement });

            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.items[1]" });
            result.Value?.GetString().Should().Be("updated");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_ArrayElement_ExtendsArray()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"items\": [\"first\"] }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.items[3]", Value = JsonDocument.Parse("\"fourth\"").RootElement });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            var array = parsed.RootElement.GetProperty("items");
            array.GetArrayLength().Should().Be(4);
            array[3].GetString().Should().Be("fourth");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Delete_ArrayElement_RemovesElement()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"items\": [\"first\", \"second\", \"third\"] }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Delete(new ValueSchema { Path = tempFile, JsonPath = "$.items[1]", Exist = false });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            var array = parsed.RootElement.GetProperty("items");
            array.GetArrayLength().Should().Be(2);
            array[0].GetString().Should().Be("first");
            array[1].GetString().Should().Be("third");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Bracket notation property tests
    [Fact]
    public void Get_BracketNotationProperty_ReturnsValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config-value\": \"test\" }", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$['config-value']" });

            result.Exist.Should().BeNull();
            result.Value?.GetString().Should().Be("test");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_BracketNotationProperty_SetsValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$['special-key']", Value = JsonDocument.Parse("\"value\"").RootElement });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("special-key").GetString().Should().Be("value");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Complex nested paths with arrays
    [Fact]
    public void Set_NestedArrayInPath_CreatesStructure()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.data[0].items[2]", Value = JsonDocument.Parse("\"nested\"").RootElement });

            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.data[0].items[2]" });
            result.Value?.GetString().Should().Be("nested");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_NestedArrayInPath_ReturnsValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        var json = """
        {
          "users": [
            { "name": "Alice", "roles": ["admin", "user"] },
            { "name": "Bob", "roles": ["user"] }
          ]
        }
        """;
        File.WriteAllText(tempFile, json, System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.users[0].roles[1]" });

            result.Value?.GetString().Should().Be("user");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Array element number tests
    [Fact]
    public void Get_ArrayOfNumbers_ReturnsNumber()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"values\": [10, 20, 30] }", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.values[2]" });

            result.Value?.GetInt32().Should().Be(30);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_ArrayOfObjects_ReturnsObject()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"items\": [{\"id\": 1, \"name\": \"item1\"}, {\"id\": 2, \"name\": \"item2\"}] }", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.items[1]" });

            var obj = result.Value?.Deserialize<System.Collections.Generic.Dictionary<string, JsonElement>>();
            obj?["id"].GetInt32().Should().Be(2);
            obj?["name"].GetString().Should().Be("item2");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Invalid JSON handling
    [Fact]
    public void Get_InvalidJson_ThrowsJsonException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ invalid json", System.Text.Encoding.UTF8);

        try
        {
            var act = () => _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.value" });

            act.Should().Throw<JsonException>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_InvalidJson_ThrowsJsonException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ invalid json", System.Text.Encoding.UTF8);

        try
        {
            var act = () => _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.value", Value = JsonDocument.Parse("\"test\"").RootElement });

            act.Should().Throw<JsonException>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // MinifiedJSON handling
    [Fact]
    public void Set_MinifiedJson_PreservesMinification()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{\"config\":{\"value\":1}}", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.value", Value = JsonDocument.Parse("2").RootElement });

            var content = File.ReadAllText(tempFile);
            // Should not have added indentation
            content.Should().NotContain("\n");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Set with no value (null test)
    [Fact]
    public void Set_NoValueProperty_CreatesNullValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            // When Value is null (no value provided), it should create a JSON null
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.value" });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("config").GetProperty("value").ValueKind.Should().Be(JsonValueKind.Null);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Array value in Set
    [Fact]
    public void Set_ArrayValue_CreatesArray()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.items", Value = JsonDocument.Parse("[1, 2, 3]").RootElement });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            var array = parsed.RootElement.GetProperty("config").GetProperty("items");
            array.ValueKind.Should().Be(JsonValueKind.Array);
            array.GetArrayLength().Should().Be(3);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Null instance handling for Get/Set/Delete
    [Fact]
    public void Get_NullInstance_ThrowsArgumentNullException()
    {
        var act = () => _resource.Get(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Set_NullInstance_ThrowsArgumentNullException()
    {
        var act = () => _resource.Set(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Delete_NullInstance_ThrowsArgumentNullException()
    {
        var act = () => _resource.Delete(null);

        act.Should().Throw<ArgumentNullException>();
    }

    // Complex object update
    [Fact]
    public void Set_ReplaceComplexObject_UpdatesObject()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": { \"old\": \"value\" } }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema
            {
                Path = tempFile,
                JsonPath = "$.config",
                Value = JsonDocument.Parse("{ \"new\": \"value\", \"nested\": { \"deep\": true } }").RootElement
            });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("config").GetProperty("new").GetString().Should().Be("value");
            parsed.RootElement.GetProperty("config").GetProperty("nested").GetProperty("deep").GetBoolean().Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_RootLevelArray_ReturnsArray()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "[1, 2, 3, 4, 5]", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$[2]" });

            result.Value?.GetInt32().Should().Be(3);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_RootLevelArray_UpdatesElement()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "[1, 2, 3]", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$[1]", Value = JsonDocument.Parse("99").RootElement });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement[1].GetInt32().Should().Be(99);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Delete_RootLevelArrayElement_RemovesElement()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "[\"a\", \"b\", \"c\"]", System.Text.Encoding.UTF8);

        try
        {
            _resource.Delete(new ValueSchema { Path = tempFile, JsonPath = "$[1]", Exist = false });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetArrayLength().Should().Be(2);
            parsed.RootElement[0].GetString().Should().Be("a");
            parsed.RootElement[1].GetString().Should().Be("c");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Double-quoted properties in bracket notation
    [Fact]
    public void Set_DoubleQuotedBracketProperty_SetsValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$[\"my-property\"]", Value = JsonDocument.Parse("\"test\"").RootElement });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("my-property").GetString().Should().Be("test");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_DoubleQuotedBracketProperty_GetsValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"my-property\": \"value\" }", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$[\"my-property\"]" });

            result.Value?.GetString().Should().Be("value");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Deeply nested path creation
    [Fact]
    public void Set_DeeplyNestedPath_CreatesAllLevels()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema
            {
                Path = tempFile,
                JsonPath = "$.a.b.c.d.e.f.g.h.i.j",
                Value = JsonDocument.Parse("\"deep\"").RootElement
            });

            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.a.b.c.d.e.f.g.h.i.j" });
            result.Value?.GetString().Should().Be("deep");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Error cases for JSONPath parsing
    [Fact]
    public void ParseJsonPath_InvalidSyntax_ThrowsException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            // JSONPath starting with invalid character (not $)
            var act = () => _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "invalid" });

            act.Should().Throw<Exception>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseJsonPath_UnclosedBracket_ThrowsException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            var act = () => _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.config[0" });

            act.Should().Throw<Exception>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseJsonPath_InvalidArrayIndex_ThrowsException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            // Using invalid property name in bracket that looks like array but isn't quoted
            var act = () => _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.config[notanumber]" });

            act.Should().Throw<Exception>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Complex nested array operations
    [Fact]
    public void Set_ReplaceArrayWithObject_ChangesType()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"data\": [1, 2, 3] }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema
            {
                Path = tempFile,
                JsonPath = "$.data",
                Value = JsonDocument.Parse("{ \"a\": 1 }").RootElement
            });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            var data = parsed.RootElement.GetProperty("data");
            data.ValueKind.Should().Be(JsonValueKind.Object);
            data.GetProperty("a").GetInt32().Should().Be(1);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_ArrayElement_WithComplexPathAfter_ReturnsElement()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        var json = """
        {
          "matrix": [
            { "items": [10, 20, 30] },
            { "items": [40, 50, 60] }
          ]
        }
        """;
        File.WriteAllText(tempFile, json, System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.matrix[1].items[2]" });

            result.Value?.GetInt32().Should().Be(60);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Delete_ArrayFromNestedStructure_RemovesArray()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"outer\": { \"inner\": [1, 2, 3] } }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Delete(new ValueSchema { Path = tempFile, JsonPath = "$.outer.inner", Exist = false });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("outer").TryGetProperty("inner", out _).Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Indentation preservation
    [Fact]
    public void Set_IndentedJson_PreservesIndentation()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        var indentedJson = """
{
  "config": {
    "value": 1
  }
}
""";
        File.WriteAllText(tempFile, indentedJson, System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.value", Value = JsonDocument.Parse("2").RootElement });

            var content = File.ReadAllText(tempFile);
            // Should have added indentation (newlines)
            content.Should().Contain("\n");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Test with existing value being replaced by null
    [Fact]
    public void Set_ReplaceValueWithNull_CreatesJsonNull()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": { \"value\": \"something\" } }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.value", Value = JsonDocument.Parse("null").RootElement });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("config").GetProperty("value").ValueKind.Should().Be(JsonValueKind.Null);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Test with object as root
    [Fact]
    public void Set_NestedPathCreateFromEmpty_CreatesStructure()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema
            {
                Path = tempFile,
                JsonPath = "$.config.db.host",
                Value = JsonDocument.Parse("\"localhost\"").RootElement
            });

            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.config.db.host" });
            result.Value?.GetString().Should().Be("localhost");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_Root_ReturnsRoot()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"key\": \"value\" }", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$" });

            result.Value?.ValueKind.Should().Be(JsonValueKind.Object);
            result.Value?.GetProperty("key").GetString().Should().Be("value");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Array with special characters in properties
    [Fact]
    public void Set_PropertyWithSpecialCharacters_SetsValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema
            {
                Path = tempFile,
                JsonPath = "$['property-with-dashes']",
                Value = JsonDocument.Parse("\"test\"").RootElement
            });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("property-with-dashes").GetString().Should().Be("test");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Test with array extending with nulls
    [Fact]
    public void Set_ArrayExtendWithNulls_FillsWithJsonNulls()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"items\": [] }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.items[5]", Value = JsonDocument.Parse("\"sixth\"").RootElement });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            var array = parsed.RootElement.GetProperty("items");
            array.GetArrayLength().Should().Be(6);
            array[5].GetString().Should().Be("sixth");
            // Check that intermediate positions are null
            array[0].ValueKind.Should().Be(JsonValueKind.Null);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Number types
    [Fact]
    public void Set_DoubleValue_CreatesDouble()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.pi", Value = JsonDocument.Parse("3.14159").RootElement });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            var value = parsed.RootElement.GetProperty("config").GetProperty("pi").GetDouble();
            value.Should().BeGreaterThan(3.0).And.BeLessThan(4.0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_BooleanFalse_CreatesFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.enabled", Value = JsonDocument.Parse("false").RootElement });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("config").GetProperty("enabled").GetBoolean().Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Delete on array element inside nested path
    [Fact]
    public void Delete_NestedArrayElement_RemovesCorrectElement()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"data\": { \"items\": [\"a\", \"b\", \"c\"] } }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Delete(new ValueSchema { Path = tempFile, JsonPath = "$.data.items[1]", Exist = false });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            var array = parsed.RootElement.GetProperty("data").GetProperty("items");
            array.GetArrayLength().Should().Be(2);
            array[0].GetString().Should().Be("a");
            array[1].GetString().Should().Be("c");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Ensure ValueSchema exposes the expected properties
    [Fact]
    public void Schema_HasExpectedProperties()
    {
        var schemaType = typeof(ValueSchema);

        var properties = schemaType.GetProperties();
        properties.Should().Contain(p => p.Name == "Path");
        properties.Should().Contain(p => p.Name == "JsonPath");
        properties.Should().Contain(p => p.Name == "Value");
        properties.Should().Contain(p => p.Name == "Exist");
    }

    // Ensure the Resource is properly registered with metadata
    [Fact]
    public void Resource_HasExitCodeAttributes()
    {
        var resourceType = typeof(ValueResource);
        var exitCodeAttrs = resourceType.GetCustomAttributes<OpenDsc.Resource.ExitCodeAttribute>();

        exitCodeAttrs.Should().NotBeEmpty();
        exitCodeAttrs.Should().Contain(attr => attr.ExitCode == 0);
        exitCodeAttrs.Should().Contain(attr => attr.ExitCode == 1);
    }

    // Test edge case: Get and then Delete
    [Fact]
    public void Get_Then_Delete_Sequence()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": { \"setting\": \"value\" } }", System.Text.Encoding.UTF8);

        try
        {
            // First, get the value
            var getResult = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.config.setting" });
            getResult.Value?.GetString().Should().Be("value");

            // Then delete it
            _resource.Delete(new ValueSchema { Path = tempFile, JsonPath = "$.config.setting", Exist = false });

            // Verify it's gone
            var getAfterDelete = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.config.setting" });
            getAfterDelete.Exist.Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Test: Set with Exist check
    [Fact]
    public void Set_WithExistTrue_SetsValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{}", System.Text.Encoding.UTF8);

        try
        {
            var newValue = JsonSerializer.SerializeToElement("updated");
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.test", Value = newValue, Exist = true });

            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.test" });
            result.Value?.GetString().Should().Be("updated");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Test: Delete with Exist check
    [Fact]
    public void Delete_WithExistFalse_DeletesValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"a\": 1, \"b\": 2 }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Delete(new ValueSchema { Path = tempFile, JsonPath = "$.a", Exist = false });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.TryGetProperty("a", out _).Should().BeFalse();
            parsed.RootElement.GetProperty("b").GetInt32().Should().Be(2);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Test: Multiple bracket notation properties in same path
    [Fact]
    public void Get_MultipleBracketNotationProperties_ReturnsValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"prop-1\": { \"sub-key\": \"result\" } }", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$['prop-1']['sub-key']" });
            result.Value?.GetString().Should().Be("result");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Test: Array with only null elements
    [Fact]
    public void Set_ArrayWithNullElements_PadsWithNulls()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"data\": [] }", System.Text.Encoding.UTF8);

        try
        {
            var newValue = JsonSerializer.SerializeToElement("value");
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.data[5]", Value = newValue });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            var array = parsed.RootElement.GetProperty("data");
            array.GetArrayLength().Should().Be(6);
            array[5].GetString().Should().Be("value");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Test: Create nested structure multiple levels deep
    [Fact]
    public void Set_DeepNestedPath_CreatesAllLevels()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{}", System.Text.Encoding.UTF8);

        try
        {
            var newValue = JsonSerializer.SerializeToElement("deep");
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.a.b.c.d", Value = newValue });

            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.a.b.c.d" });
            result.Value?.GetString().Should().Be("deep");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Test: Verify branch coverage for GetSchema null check
    [Fact]
    public void GetSchema_ReturnsJsonSchema()
    {
        var schema = _resource.GetSchema();
        schema.Should().NotBeNullOrEmpty();
        schema.Should().Contain("\"$schema\":");
    }

    // Test: JSONPath that doesn't match any elements (e.g., accessing property on array)
    [Fact]
    public void Get_NonExistentPathInArray_ReturnsExistFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        // Create an array at root
        File.WriteAllText(tempFile, "[1, 2, 3]", System.Text.Encoding.UTF8);

        try
        {
            // Try to access a property that doesn't exist on the array root
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.nonexistent" });
            result.Exist.Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // Test: Set on empty JSON and verify structure preservation
    [Fact]
    public void Set_EmptyJson_CreatesRootProperty()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{}", System.Text.Encoding.UTF8);

        try
        {
            var newValue = JsonSerializer.SerializeToElement(42);
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.value", Value = newValue });

            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.value" });
            result.Value?.GetInt32().Should().Be(42);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
