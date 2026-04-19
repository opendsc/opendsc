// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Schema.Tests;

[Trait("Category", "Unit")]
public class DscMessageTests
{
    [Fact]
    public void DscMessage_DefaultValues_ShouldBeCorrect()
    {
        var message = new DscMessage();

        message.Message.Should().NotBeNull();
        message.Level.Should().Be(default(DscMessageLevel));
    }

    [Theory]
    [InlineData(DscMessageLevel.Error, "Error message")]
    [InlineData(DscMessageLevel.Warning, "Warning message")]
    [InlineData(DscMessageLevel.Information, "Info message")]
    public void DscMessage_WithLevelAndMessage_ShouldStoreValues(DscMessageLevel level, string msg)
    {
        var message = new DscMessage { Level = level, Message = msg };

        message.Level.Should().Be(level);
        message.Message.Should().Be(msg);
    }

    [Theory]
    [InlineData("Microsoft/Windows")]
    [InlineData("Microsoft.Custom/File")]
    [InlineData("Microsoft.Custom.Sub/Resource")]
    public void DscMessage_WithValidType_ShouldStoreType(string type)
    {
        var message = new DscMessage { Type = type };

        message.Type.Should().Be(type);
    }

    [Theory]
    [InlineData("")]
    public void DscMessage_WithEmptyType_ShouldThrowArgumentException(string type)
    {
        var message = new DscMessage();

        var act = () => message.Type = type;

        act.Should().Throw<ArgumentException>()
            .WithMessage("Type cannot be null or empty.");
    }

    [Fact]
    public void DscMessage_WithNullType_ShouldThrowArgumentException()
    {
        var message = new DscMessage();

        var act = () => message.Type = null!;

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
    public void DscMessage_WithInvalidType_ShouldThrowArgumentException(string type)
    {
        var message = new DscMessage();

        var act = () => message.Type = type;

        act.Should().Throw<ArgumentException>()
            .WithMessage("Type does not match format: <owner>[.<group>][.<area>]/<name>");
    }

    [Fact]
    public void DscMessage_WithAllProperties_ShouldStoreAllValues()
    {
        var message = new DscMessage
        {
            Name = "TestMessage",
            Type = "Microsoft/Windows",
            Message = "Test message content",
            Level = DscMessageLevel.Error
        };

        message.Name.Should().Be("TestMessage");
        message.Type.Should().Be("Microsoft/Windows");
        message.Message.Should().Be("Test message content");
        message.Level.Should().Be(DscMessageLevel.Error);
    }
}
