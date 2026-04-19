// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Schema.Tests;

[Trait("Category", "Unit")]
public class DscResourceResultTests
{
    [Fact]
    public void DscResourceResult_DefaultValues_ShouldBeCorrect()
    {
        var result = new DscResourceResult();

        result.Name.Should().Be(string.Empty);
        result.Type.Should().Be(string.Empty);
        result.Result.ValueKind.Should().Be(JsonValueKind.Undefined);
    }

    [Fact]
    public void DscResourceResult_WithProperties_ShouldStoreValues()
    {
        using var doc = JsonDocument.Parse("{\"test\": \"value\"}");
        var jsonResult = doc.RootElement.Clone();
        var result = new DscResourceResult
        {
            Name = "TestInstance",
            Type = "Test/Resource",
            Result = jsonResult
        };

        result.Name.Should().Be("TestInstance");
        result.Type.Should().Be("Test/Resource");
        result.Result.Should().NotBeNull();
        result.Result!.GetProperty("test").GetString().Should().Be("value");
    }

    [Theory]
    [InlineData("Microsoft/Windows")]
    [InlineData("Microsoft.Custom/File")]
    [InlineData("Microsoft.Custom.Sub/Resource")]
    public void DscResourceResult_WithValidType_ShouldStoreType(string type)
    {
        var result = new DscResourceResult { Type = type };

        result.Type.Should().Be(type);
    }

    [Theory]
    [InlineData("")]
    public void DscResourceResult_WithEmptyType_ShouldThrowArgumentException(string type)
    {
        var result = new DscResourceResult();

        var act = () => result.Type = type;

        act.Should().Throw<ArgumentException>()
            .WithMessage("Type cannot be null or empty.");
    }

    [Fact]
    public void DscResourceResult_WithNullType_ShouldThrowArgumentException()
    {
        var result = new DscResourceResult();

        var act = () => result.Type = null!;

        act.Should().Throw<ArgumentException>()
            .WithMessage("Type cannot be null or empty.");
    }

    [Theory]
    [InlineData("InvalidType")]
    [InlineData("Owner/")]
    [InlineData("/Name")]
    [InlineData("Owner/Sub/Name")]
    [InlineData("Owner.Sub.Area.Extra/Name")]
    [InlineData("Owner-Custom/Name")]
    [InlineData("Owner#Special/Name")]
    public void DscResourceResult_WithInvalidType_ShouldThrowArgumentException(string type)
    {
        var result = new DscResourceResult();

        var act = () => result.Type = type;

        act.Should().Throw<ArgumentException>()
            .WithMessage("Type does not match format: <owner>[.<group>][.<area>]/<name>");
    }

    [Fact]
    public void DscResourceResult_MultipleTypeAssignments_ShouldUpdateCorrectly()
    {
        var result = new DscResourceResult { Type = "First/Resource" };
        result.Type.Should().Be("First/Resource");

        result.Type = "Second/Resource";
        result.Type.Should().Be("Second/Resource");
    }
}
