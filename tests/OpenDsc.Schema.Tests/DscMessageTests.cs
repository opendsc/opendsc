// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Schema.Tests;

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
}
