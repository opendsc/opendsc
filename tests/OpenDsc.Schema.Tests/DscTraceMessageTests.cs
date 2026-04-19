// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Schema.Tests;

public class DscTraceMessageTests
{
    [Fact]
    public void DscTraceMessage_DefaultValues_ShouldBeNull()
    {
        var message = new DscTraceMessage();

        message.Timestamp.Should().BeNull();
        message.Level.Should().BeNull();
        message.Fields.Should().BeNull();
    }

    [Fact]
    public void DscTraceMessage_WithTimestamp_ShouldStoreTimestamp()
    {
        const string timestamp = "2025-01-01T12:00:00Z";
        var message = new DscTraceMessage { Timestamp = timestamp };

        message.Timestamp.Should().Be(timestamp);
    }

    [Theory]
    [InlineData(DscTraceLevel.Error)]
    [InlineData(DscTraceLevel.Warn)]
    [InlineData(DscTraceLevel.Info)]
    [InlineData(DscTraceLevel.Debug)]
    [InlineData(DscTraceLevel.Trace)]
    public void DscTraceMessage_WithLevel_ShouldStoreLevel(DscTraceLevel level)
    {
        var message = new DscTraceMessage { Level = level };

        message.Level.Should().Be(level);
    }

    [Fact]
    public void DscTraceMessage_WithFields_ShouldStoreFields()
    {
        var fields = new DscTraceFields { Message = "Test message" };
        var message = new DscTraceMessage { Fields = fields };

        message.Fields.Should().NotBeNull();
        message.Fields!.Message.Should().Be("Test message");
    }

    [Fact]
    public void DscTraceMessage_WithAllProperties_ShouldStoreAll()
    {
        const string timestamp = "2025-01-01T12:00:00Z";
        var fields = new DscTraceFields { Message = "Detailed message" };
        var message = new DscTraceMessage
        {
            Timestamp = timestamp,
            Level = DscTraceLevel.Info,
            Fields = fields
        };

        message.Timestamp.Should().Be(timestamp);
        message.Level.Should().Be(DscTraceLevel.Info);
        message.Fields.Should().NotBeNull();
        message.Fields!.Message.Should().Be("Detailed message");
    }

    [Fact]
    public void DscTraceMessage_MultipleMessages_ShouldBeIndependent()
    {
        var msg1 = new DscTraceMessage { Timestamp = "2025-01-01T12:00:00Z", Level = DscTraceLevel.Error };
        var msg2 = new DscTraceMessage { Timestamp = "2025-01-01T13:00:00Z", Level = DscTraceLevel.Debug };

        msg1.Timestamp.Should().Be("2025-01-01T12:00:00Z");
        msg1.Level.Should().Be(DscTraceLevel.Error);
        msg2.Timestamp.Should().Be("2025-01-01T13:00:00Z");
        msg2.Level.Should().Be(DscTraceLevel.Debug);
    }
}

public class DscTraceFieldsTests
{
    [Fact]
    public void DscTraceFields_DefaultValues_ShouldBeNull()
    {
        var fields = new DscTraceFields();

        fields.Message.Should().BeNull();
    }

    [Fact]
    public void DscTraceFields_WithMessage_ShouldStoreMessage()
    {
        const string msg = "Test message content";
        var fields = new DscTraceFields { Message = msg };

        fields.Message.Should().Be(msg);
    }

    [Fact]
    public void DscTraceFields_WithEmptyMessage_ShouldStoreEmpty()
    {
        var fields = new DscTraceFields { Message = "" };

        fields.Message.Should().Be("");
    }

    [Fact]
    public void DscTraceFields_WithLongMessage_ShouldStoreEntireMessage()
    {
        var longMessage = string.Join(" ", Enumerable.Repeat("word", 1000));
        var fields = new DscTraceFields { Message = longMessage };

        fields.Message.Should().Be(longMessage);
        fields.Message!.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public void DscTraceFields_WithSpecialCharacters_ShouldStoreAsIs()
    {
        const string msgWithSpecialChars = "Error: [Resource/Type] failed with exit code 0x80004005";
        var fields = new DscTraceFields { Message = msgWithSpecialChars };

        fields.Message.Should().Be(msgWithSpecialChars);
    }

    [Fact]
    public void DscTraceFields_MultipleInstances_ShouldBeIndependent()
    {
        var fields1 = new DscTraceFields { Message = "Message 1" };
        var fields2 = new DscTraceFields { Message = "Message 2" };

        fields1.Message.Should().Be("Message 1");
        fields2.Message.Should().Be("Message 2");
    }
}
