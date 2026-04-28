// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

#pragma warning disable xUnit1051

using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

public class JsonYamlConverterTests
{
    private readonly JsonYamlConverter _converter = new();

    #region ConvertJsonToYaml Tests

    [Fact]
    public void ConvertJsonToYaml_WithEmptyJson_ReturnsEmpty()
    {
        var result = _converter.ConvertJsonToYaml("{}");

        result.Trim().Should().Be("{}");
    }

    [Fact]
    public void ConvertJsonToYaml_WithSimpleKeyValue_ConvertsSuccessfully()
    {
        var json = @"{ ""name"": ""test"", ""version"": ""1.0.0"" }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("name");
        result.Should().Contain("test");
        result.Should().Contain("version");
        result.Should().Contain("1.0.0");
    }

    [Fact]
    public void ConvertJsonToYaml_WithNestedObjects_PreservesStructure()
    {
        var json = @"{
            ""database"": {
                ""host"": ""localhost"",
                ""port"": 5432,
                ""credentials"": {
                    ""user"": ""admin"",
                    ""pass"": ""secret""
                }
            }
        }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("database");
        result.Should().Contain("host: localhost");
        result.Should().Contain("port: 5432");
        result.Should().Contain("credentials");
        result.Should().Contain("user: admin");
        result.Should().Contain("pass: secret");
    }

    [Fact]
    public void ConvertJsonToYaml_WithArrayOfStrings_ConvertsSuccessfully()
    {
        var json = @"{ ""servers"": [ ""srv1"", ""srv2"", ""srv3"" ] }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("servers");
        result.Should().Contain("srv1");
        result.Should().Contain("srv2");
        result.Should().Contain("srv3");
    }

    [Fact]
    public void ConvertJsonToYaml_WithArrayOfObjects_ConvertsSuccessfully()
    {
        var json = @"{
            ""items"": [
                { ""id"": 1, ""name"": ""item1"" },
                { ""id"": 2, ""name"": ""item2"" }
            ]
        }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("items");
        result.Should().Contain("id");
        result.Should().Contain("name");
        result.Should().Contain("item1");
        result.Should().Contain("item2");
    }

    [Fact]
    public void ConvertJsonToYaml_WithBooleanValues_ConvertsSuccessfully()
    {
        var json = @"{ ""enabled"": true, ""disabled"": false }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("enabled: true");
        result.Should().Contain("disabled: false");
    }

    [Fact]
    public void ConvertJsonToYaml_WithNullValues_HandlesNullsInYaml()
    {
        var json = @"{ ""name"": ""test"", ""description"": null }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("name: test");
        // YamlDotNet serializes null values, so description: may appear as empty
        result.Should().Contain("description");
    }

    [Fact]
    public void ConvertJsonToYaml_WithNumericTypes_PreservesNumbers()
    {
        var json = @"{ ""int"": 42, ""long"": 9999999999, ""double"": 3.14159, ""negative"": -100 }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("int: 42");
        result.Should().Contain("long: 9999999999");
        result.Should().Contain("double: 3.14159");
        result.Should().Contain("negative: -100");
    }

    [Fact]
    public void ConvertJsonToYaml_WithMixedCaseKeys_PreservesKeyNames()
    {
        var json = @"{ ""firstName"": ""John"", ""lastName"": ""Doe"", ""EmailAddress"": ""john@example.com"" }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("firstName");
        result.Should().Contain("lastName");
        // Keys are preserved as-is during conversion
        result.Should().Contain("EmailAddress");
    }

    [Fact]
    public void ConvertJsonToYaml_WithInvalidJson_ReturnsEmpty()
    {
        var invalidJson = "{ invalid json }";

        var result = _converter.ConvertJsonToYaml(invalidJson);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertJsonToYaml_WithMalformedJson_ReturnsEmpty()
    {
        var malformedJson = @"{ ""key"": ""unclosed string }";

        var result = _converter.ConvertJsonToYaml(malformedJson);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertJsonToYaml_WithEmptyString_ReturnsEmpty()
    {
        var result = _converter.ConvertJsonToYaml(string.Empty);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertJsonToYaml_WithWhitespaceOnly_ReturnsEmpty()
    {
        var result = _converter.ConvertJsonToYaml("   \n\t  ");

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertJsonToYaml_WithSpecialCharacters_HandlesCorrectly()
    {
        var json = @"{ ""path"": ""C:/Users/test"", ""quote"": ""He said hello"" }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("path");
        result.Should().Contain("quote");
    }

    [Fact]
    public void ConvertJsonToYaml_WithArrayOfMixedTypes_ConvertedToYaml()
    {
        var json = @"{ ""mixed"": [ 1, ""string"", true, null ] }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("mixed");
        result.Should().Contain("1");
        result.Should().Contain("string");
        result.Should().Contain("true");
    }

    [Fact]
    public void ConvertJsonToYaml_WithDeeplyNestedStructure_ConvertsSuccessfully()
    {
        var json = @"{
            ""level1"": {
                ""level2"": {
                    ""level3"": {
                        ""level4"": {
                            ""value"": ""deep""
                        }
                    }
                }
            }
        }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("level1");
        result.Should().Contain("level2");
        result.Should().Contain("level3");
        result.Should().Contain("level4");
        result.Should().Contain("value: deep");
    }

    #endregion

    #region ConvertYamlToJson Tests

    [Fact]
    public void ConvertYamlToJson_WithEmptyYaml_ReturnsEmptyJson()
    {
        var result = _converter.ConvertYamlToJson("{}");

        result.Trim().Should().Be("{}");
    }

    [Fact]
    public void ConvertYamlToJson_WithSimpleYaml_ConvertsSuccessfully()
    {
        var yaml = @"name: test
version: 1.0.0";

        var result = _converter.ConvertYamlToJson(yaml);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.GetProperty("name").GetString().Should().Be("test");
        root.GetProperty("version").GetString().Should().Be("1.0.0");
    }

    [Fact]
    public void ConvertYamlToJson_WithNestedYaml_PreservesStructure()
    {
        var yaml = @"database:
  host: localhost
  port: 5432
  credentials:
    user: admin
    pass: secret";

        var result = _converter.ConvertYamlToJson(yaml);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.GetProperty("database").GetProperty("host").GetString().Should().Be("localhost");
        root.GetProperty("database").GetProperty("port").GetString().Should().Be("5432");
        root.GetProperty("database").GetProperty("credentials").GetProperty("user").GetString().Should().Be("admin");
    }

    [Fact]
    public void ConvertYamlToJson_WithArrayYaml_ConvertsSuccessfully()
    {
        var yaml = @"servers:
  - srv1
  - srv2
  - srv3";

        var result = _converter.ConvertYamlToJson(yaml);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        var servers = root.GetProperty("servers");
        servers.GetArrayLength().Should().Be(3);
        servers[0].GetString().Should().Be("srv1");
    }

    [Fact]
    public void ConvertYamlToJson_WithBooleanYaml_ConvertsSuccessfully()
    {
        var yaml = @"enabled: true
disabled: false";

        var result = _converter.ConvertYamlToJson(yaml);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.GetProperty("enabled").GetString().Should().Be("true");
        root.GetProperty("disabled").GetString().Should().Be("false");
    }

    [Fact]
    public void ConvertYamlToJson_WithNumericYaml_ConvertsSuccessfully()
    {
        var yaml = @"int: 42
float: 3.14
negative: -100";

        var result = _converter.ConvertYamlToJson(yaml);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.GetProperty("int").GetString().Should().Be("42");
        root.GetProperty("float").GetString().Should().Be("3.14");
    }

    [Fact]
    public void ConvertYamlToJson_WithNullYaml_ConvertsSuccessfully()
    {
        var yaml = @"key: value
nullKey: null";

        var result = _converter.ConvertYamlToJson(yaml);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.GetProperty("key").GetString().Should().Be("value");
        root.GetProperty("nullKey").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void ConvertYamlToJson_WithInvalidYaml_ReturnsEmpty()
    {
        var invalidYaml = ": invalid yaml";

        var result = _converter.ConvertYamlToJson(invalidYaml);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertYamlToJson_WithEmptyString_ReturnsEmpty()
    {
        var result = _converter.ConvertYamlToJson(string.Empty);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertYamlToJson_WithWhitespaceOnly_ReturnsEmpty()
    {
        var result = _converter.ConvertYamlToJson("   \n\t  ");

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void ConvertYamlToJson_WithArrayOfObjects_ConvertsSuccessfully()
    {
        var yaml = @"items:
  - id: 1
    name: item1
  - id: 2
    name: item2";

        var result = _converter.ConvertYamlToJson(yaml);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        var items = root.GetProperty("items");
        items.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public void ConvertYamlToJson_WithDeeplyNestedStructure_ConvertsSuccessfully()
    {
        var yaml = @"level1:
  level2:
    level3:
      level4:
        value: deep";

        var result = _converter.ConvertYamlToJson(yaml);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.GetProperty("level1").GetProperty("level2").GetProperty("level3").GetProperty("level4").GetProperty("value").GetString().Should().Be("deep");
    }

    [Fact]
    public void ConvertYamlToJson_WithCamelCaseKeys_PreservesCasing()
    {
        var yaml = @"firstName: John
lastName: Doe";

        var result = _converter.ConvertYamlToJson(yaml);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.GetProperty("firstName").GetString().Should().Be("John");
        root.GetProperty("lastName").GetString().Should().Be("Doe");
    }

    [Fact]
    public void ConvertYamlToJson_OutputIsValidJson_CanBeParsed()
    {
        var yaml = @"config:
  debug: true
  timeout: 30";

        var result = _converter.ConvertYamlToJson(yaml);

        // Verify it's valid JSON by parsing it
        var action = () => JsonDocument.Parse(result);
        action.Should().NotThrow();
    }

    #endregion

    #region Round-Trip Conversion Tests

    [Fact]
    public void RoundTrip_JsonToYamlToJson_PreservesData()
    {
        var originalJson = @"{ ""name"": ""test"", ""enabled"": true }";

        var yaml = _converter.ConvertJsonToYaml(originalJson);
        var resultJson = _converter.ConvertYamlToJson(yaml);

        using var originalDoc = JsonDocument.Parse(originalJson);
        using var resultDoc = JsonDocument.Parse(resultJson);

        originalDoc.RootElement.GetProperty("name").GetString().Should().Be(resultDoc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void RoundTrip_ComplexStructure_PreservesAllData()
    {
        var originalJson = @"{
            ""app"": {
                ""name"": ""myapp"",
                ""servers"": [ ""srv1"", ""srv2"" ],
                ""config"": {
                    ""debug"": true,
                    ""timeout"": 30
                }
            }
        }";

        var yaml = _converter.ConvertJsonToYaml(originalJson);
        var resultJson = _converter.ConvertYamlToJson(yaml);

        using var originalDoc = JsonDocument.Parse(originalJson);
        using var resultDoc = JsonDocument.Parse(resultJson);

        var originalAppName = originalDoc.RootElement.GetProperty("app").GetProperty("name").GetString();
        var resultAppName = resultDoc.RootElement.GetProperty("app").GetProperty("name").GetString();

        originalAppName.Should().Be(resultAppName);
    }

    [Fact]
    public void RoundTrip_WithNullValues_NullsPreserved()
    {
        var originalJson = @"{ ""value1"": ""test"", ""value2"": null }";

        var yaml = _converter.ConvertJsonToYaml(originalJson);
        var resultJson = _converter.ConvertYamlToJson(yaml);

        using var resultDoc = JsonDocument.Parse(resultJson);
        var root = resultDoc.RootElement;

        root.GetProperty("value1").GetString().Should().Be("test");
        root.GetProperty("value2").ValueKind.Should().Be(JsonValueKind.Null);
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void RealWorld_ConfigurationFile_ConvertsCorrectly()
    {
        var appConfigJson = @"{
            ""appSettings"": {
                ""environment"": ""production"",
                ""logLevel"": ""info""
            },
            ""database"": {
                ""connectionString"": ""Server=prod.db;Port=5432;User=admin"",
                ""poolSize"": 20
            },
            ""features"": [ ""auth"", ""logging"", ""monitoring"" ]
        }";

        var yaml = _converter.ConvertJsonToYaml(appConfigJson);
        var roundTripJson = _converter.ConvertYamlToJson(yaml);

        // Verify YAML contains expected structure
        yaml.Should().Contain("appSettings");
        yaml.Should().Contain("database");
        yaml.Should().Contain("features");

        // Verify round-trip produces valid JSON
        using var doc = JsonDocument.Parse(roundTripJson);
        doc.RootElement.GetProperty("appSettings").GetProperty("environment").GetString().Should().Be("production");
    }

    [Fact]
    public void RealWorld_ParameterFile_ConvertsCorrectly()
    {
        var parameterYaml = @"
database:
  host: db.prod.local
  port: 5432
  credentials:
    username: dbadmin
    password: secretpass
app:
  debug: false
  maxConnections: 100
features:
  - featureA
  - featureB
  - featureC
";

        var json = _converter.ConvertYamlToJson(parameterYaml);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("database").GetProperty("host").GetString().Should().Be("db.prod.local");
        doc.RootElement.GetProperty("database").GetProperty("port").GetString().Should().Be("5432");
        doc.RootElement.GetProperty("app").GetProperty("debug").GetString().Should().Be("false");
    }

    [Fact]
    public void RealWorld_BlazorUIToggle_JsonYamlToggleWorks()
    {
        // Simulate user viewing JSON in editor, clicking YAML toggle
        var jsonFromEditor = @"{ ""environment"": ""dev"", ""port"": 8080 }";
        var yamlForDisplay = _converter.ConvertJsonToYaml(jsonFromEditor);

        // Simulate user editing YAML and clicking back to JSON
        var jsonAfterEdit = _converter.ConvertYamlToJson(yamlForDisplay);

        // Verify both are valid and contain expected data
        jsonAfterEdit.Should().NotBeEmpty();
        jsonAfterEdit.Should().Contain("environment");
        jsonAfterEdit.Should().Contain("port");
    }

    #endregion

    #region Error Handling and Edge Cases

    [Fact]
    public void ErrorHandling_JsonToYaml_VeryLargeNumber_ConvertsSuccessfully()
    {
        var json = @"{ ""bigNumber"": 18446744073709551615 }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("bigNumber");
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void ErrorHandling_JsonToYaml_VerySmallNumber_ConvertsSuccessfully()
    {
        var json = @"{ ""smallNumber"": 0.0000000001 }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().Contain("smallNumber");
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void ErrorHandling_YamlToJson_MultilineString_ConvertsSuccessfully()
    {
        var yaml = @"description: |
  This is a
  multi-line
  string";

        var result = _converter.ConvertYamlToJson(yaml);

        result.Should().NotBeEmpty();
        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("description").ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public void ErrorHandling_Unicode_ConvertsSuccessfully()
    {
        var json = @"{ ""greeting"": ""Hello 世界 🌍"", ""name"": ""Москва"" }";

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().NotBeEmpty();
        result.Should().Contain("greeting");
    }

    [Fact]
    public void ErrorHandling_VeryLargeStructure_ConvertsSuccessfully()
    {
        var jsonBuilder = new System.Text.StringBuilder();
        jsonBuilder.Append("{ \"items\": [");

        for (int i = 0; i < 100; i++)
        {
            if (i > 0) jsonBuilder.Append(',');
            jsonBuilder.Append($"{{ \"id\": {i}, \"name\": \"item{i}\" }}");
        }

        jsonBuilder.Append("] }");
        var json = jsonBuilder.ToString();

        var result = _converter.ConvertJsonToYaml(json);

        result.Should().NotBeEmpty();
        result.Should().Contain("items");
    }

    #endregion
}
