// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests;

public sealed class JsonYamlConverterTests
{
    private readonly JsonYamlConverter _converter = new();

    [Fact]
    public void ConvertJsonToYaml_WithValidJson_ReturnsYaml()
    {
        var json = """
        {
            "name": "test",
            "value": 123,
            "enabled": true
        }
        """;

        var yaml = _converter.ConvertJsonToYaml(json);

        yaml.Should().NotBeNullOrWhiteSpace();
        yaml.Should().Contain("name: test");
        yaml.Should().Contain("value: 123");
        yaml.Should().Contain("enabled: true");
    }

    [Fact]
    public void ConvertJsonToYaml_WithNestedJson_ReturnsNestedYaml()
    {
        var json = """
        {
            "outer": {
                "inner": "value"
            }
        }
        """;

        var yaml = _converter.ConvertJsonToYaml(json);

        yaml.Should().NotBeNullOrWhiteSpace();
        yaml.Should().Contain("outer:");
        yaml.Should().Contain("inner: value");
    }

    [Fact]
    public void ConvertJsonToYaml_WithArray_ReturnsYamlArray()
    {
        var json = """
        {
            "items": ["one", "two", "three"]
        }
        """;

        var yaml = _converter.ConvertJsonToYaml(json);

        yaml.Should().NotBeNullOrWhiteSpace();
        yaml.Should().Contain("items:");
        yaml.Should().Contain("- one");
        yaml.Should().Contain("- two");
        yaml.Should().Contain("- three");
    }

    [Fact]
    public void ConvertJsonToYaml_WithInvalidJson_ReturnsEmpty()
    {
        var json = "{ invalid json }";

        var yaml = _converter.ConvertJsonToYaml(json);

        yaml.Should().BeEmpty();
    }

    [Fact]
    public void ConvertJsonToYaml_WithEmptyJson_ReturnsEmpty()
    {
        var json = "";

        var yaml = _converter.ConvertJsonToYaml(json);

        yaml.Should().BeEmpty();
    }

    [Fact]
    public void ConvertYamlToJson_WithValidYaml_ReturnsJson()
    {
        var yaml = """
        name: test
        value: 123
        enabled: true
        """;

        var json = _converter.ConvertYamlToJson(yaml);

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"name\": \"test\"");
        json.Should().Contain("\"value\":");
        json.Should().Contain("\"enabled\":");
    }

    [Fact]
    public void ConvertYamlToJson_WithNestedYaml_ReturnsNestedJson()
    {
        var yaml = """
        outer:
          inner: value
        """;

        var json = _converter.ConvertYamlToJson(yaml);

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"outer\"");
        json.Should().Contain("\"inner\": \"value\"");
    }

    [Fact]
    public void ConvertYamlToJson_WithYamlArray_ReturnsJsonArray()
    {
        var yaml = """
        items:
          - one
          - two
          - three
        """;

        var json = _converter.ConvertYamlToJson(yaml);

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"items\"");
        json.Should().Contain("\"one\"");
        json.Should().Contain("\"two\"");
        json.Should().Contain("\"three\"");
    }

    [Fact]
    public void ConvertYamlToJson_WithInvalidYaml_ReturnsEmpty()
    {
        var yaml = "invalid: yaml: structure: :::";

        var json = _converter.ConvertYamlToJson(yaml);

        json.Should().BeEmpty();
    }

    [Fact]
    public void ConvertYamlToJson_WithEmptyYaml_ReturnsEmpty()
    {
        var yaml = "";

        var json = _converter.ConvertYamlToJson(yaml);

        json.Should().BeEmpty();
    }

    [Fact]
    public void ConvertJsonToYaml_ThenBackToJson_PreservesData()
    {
        var originalJson = """
        {
            "name": "test",
            "value": 123,
            "enabled": true
        }
        """;

        var yaml = _converter.ConvertJsonToYaml(originalJson);
        var jsonAgain = _converter.ConvertYamlToJson(yaml);

        jsonAgain.Should().Contain("\"name\": \"test\"");
        jsonAgain.Should().Contain("\"value\":");
        jsonAgain.Should().Contain("\"enabled\":");
    }
}
