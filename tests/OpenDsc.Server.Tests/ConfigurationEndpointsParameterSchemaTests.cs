// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using OpenDsc.Server.Endpoints;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests;

[Trait("Category", "Unit")]
public class ConfigurationEndpointsParameterSchemaTests
{
    [Fact]
    public void ExtractParametersFromYaml_WithValidParameters_ReturnsParametersDictionary()
    {
        // Arrange
        var yamlContent = @"
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources: []
parameters:
  appName:
    type: string
    description: Name of the application
  environment:
    type: string
    allowedValues: [dev, test, prod]
";

        // Act
        var result = TestableConfigurationEndpoints.ExtractParametersFromYaml(yamlContent);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("appName");
        result.Should().ContainKey("environment");
    }

    [Fact]
    public void ExtractParametersFromYaml_WithNoParameters_ReturnsNull()
    {
        // Arrange
        var yamlContent = @"
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources: []
";

        // Act
        var result = TestableConfigurationEndpoints.ExtractParametersFromYaml(yamlContent);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractParametersFromYaml_WithInvalidYaml_ReturnsNull()
    {
        // Arrange
        var yamlContent = "{ invalid yaml content [[[";

        // Act
        var result = TestableConfigurationEndpoints.ExtractParametersFromYaml(yamlContent);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ConvertToParameterDefinitions_WithValidParameters_ReturnsDefinitions()
    {
        // Arrange
        var parametersBlock = new Dictionary<string, object>
        {
            ["appName"] = new Dictionary<object, object>
            {
                ["type"] = "string",
                ["description"] = "Name of the application"
            },
            ["port"] = new Dictionary<object, object>
            {
                ["type"] = "integer",
                ["minValue"] = 1,
                ["maxValue"] = 65535
            }
        };

        // Act
        var result = TestableConfigurationEndpoints.ConvertToParameterDefinitions(parametersBlock);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("appName");
        result["appName"].Type.Should().Be("string");
        result["appName"].Description.Should().Be("Name of the application");

        result.Should().ContainKey("port");
        result["port"].Type.Should().Be("integer");
        result["port"].MinValue.Should().Be(1);
        result["port"].MaxValue.Should().Be(65535);
    }
}

// Testable wrapper to expose private methods for unit testing
public static class TestableConfigurationEndpoints
{
    public static Dictionary<string, object>? ExtractParametersFromYaml(string yamlContent)
    {
        // Use reflection to call the private method
        var method = typeof(ConfigurationService).GetMethod(
            "ExtractParametersFromYaml",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException("Could not find ExtractParametersFromYaml method");
        }

        return method.Invoke(null, new object[] { yamlContent }) as Dictionary<string, object>;
    }

    public static Dictionary<string, ParameterDefinition> ConvertToParameterDefinitions(Dictionary<string, object> parametersBlock)
    {
        // Use reflection to call the private method
        var method = typeof(ConfigurationService).GetMethod(
            "ConvertToParameterDefinitions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException("Could not find ConvertToParameterDefinitions method");
        }

        return (Dictionary<string, ParameterDefinition>)method.Invoke(null, new object[] { parametersBlock })!;
    }
}
