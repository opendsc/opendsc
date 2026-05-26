// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class ConfigurationServiceConversionTests
{
    #region ParseIntValue Tests

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, 0)]
    [InlineData(10, 10)]
    [InlineData(-5, -5)]
    public void ParseIntValue_WithIntInput_ReturnsExpected(int? input, int? expected)
    {
        ConfigurationService.ParseIntValue(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("10", 10)]
    [InlineData("-5", -5)]
    [InlineData("99", 99)]
    public void ParseIntValue_WithStringInput_ReturnsExpected(string input, int expected)
    {
        ConfigurationService.ParseIntValue(input).Should().Be(expected);
    }

    [Fact]
    public void ParseIntValue_WithLongInput_ReturnsCastToInt()
    {
        ConfigurationService.ParseIntValue((long)42).Should().Be(42);
    }

    [Fact]
    public void ParseIntValue_WithInvalidString_ReturnsNull()
    {
        ConfigurationService.ParseIntValue("notanumber").Should().BeNull();
    }

    #endregion

    #region ConvertToParameterDefinitions Tests

    [Fact]
    public void ConvertToParameterDefinitions_WithStringIntValues_ParsesMinValueAndMaxValue()
    {
        // YamlDotNet returns all scalars as System.String — this is the real scenario
        var paramObj = new Dictionary<object, object>
        {
            ["type"] = "int",
            ["description"] = "The integer value",
            ["minValue"] = "0",
            ["maxValue"] = "10"
        };

        var parametersBlock = new Dictionary<string, object>
        {
            ["myIntTest"] = paramObj
        };

        var result = ConfigurationService.ConvertToParameterDefinitions(parametersBlock);

        result.Should().ContainKey("myIntTest");
        var def = result["myIntTest"];
        def.MinValue.Should().Be(0);
        def.MaxValue.Should().Be(10);
    }

    [Fact]
    public void ConvertToParameterDefinitions_WithStringLengthValues_ParsesMinLengthAndMaxLength()
    {
        var paramObj = new Dictionary<object, object>
        {
            ["type"] = "string",
            ["minLength"] = "2",
            ["maxLength"] = "50"
        };

        var parametersBlock = new Dictionary<string, object>
        {
            ["myStringTest"] = paramObj
        };

        var result = ConfigurationService.ConvertToParameterDefinitions(parametersBlock);

        result.Should().ContainKey("myStringTest");
        var def = result["myStringTest"];
        def.MinLength.Should().Be(2);
        def.MaxLength.Should().Be(50);
    }

    [Fact]
    public void ConvertToParameterDefinitions_WithAllowedValues_PopulatesAllowedValues()
    {
        var paramObj = new Dictionary<object, object>
        {
            ["type"] = "string",
            ["allowedValues"] = new List<object> { "Option1", "Option2", "Option3" }
        };

        var parametersBlock = new Dictionary<string, object>
        {
            ["myEnumTest"] = paramObj
        };

        var result = ConfigurationService.ConvertToParameterDefinitions(parametersBlock);

        result["myEnumTest"].AllowedValues.Should().BeEquivalentTo(["Option1", "Option2", "Option3"]);
    }

    [Fact]
    public void ConvertToParameterDefinitions_WithNoConstraints_LeavesConstraintsNull()
    {
        var paramObj = new Dictionary<object, object>
        {
            ["type"] = "string",
            ["description"] = "Simple param"
        };

        var parametersBlock = new Dictionary<string, object>
        {
            ["mySimpleTest"] = paramObj
        };

        var result = ConfigurationService.ConvertToParameterDefinitions(parametersBlock);

        var def = result["mySimpleTest"];
        def.MinValue.Should().BeNull();
        def.MaxValue.Should().BeNull();
        def.MinLength.Should().BeNull();
        def.MaxLength.Should().BeNull();
    }

    #endregion
}
