// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Resource.Tests;

[Trait("Category", "Unit")]
public class LoggerTests
{
    [Fact]
    public void WriteInfo_WithValidMessage_WritesToStderr()
    {
        var originalError = Console.Error;
        var writer = new StringWriter();

        try
        {
            Console.SetError(writer);
            Logger.WriteInfo("Test info message");

            var output = writer.ToString();
            output.Should().NotBeEmpty();

            var doc = JsonDocument.Parse(output.Trim());
            doc.RootElement.TryGetProperty("info", out var message).Should().BeTrue();
            message.GetString().Should().Be("Test info message");
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void WriteWarning_WithValidMessage_WritesToStderr()
    {
        var originalError = Console.Error;
        var writer = new StringWriter();

        try
        {
            Console.SetError(writer);
            Logger.WriteWarning("Test warning message");

            var output = writer.ToString();
            output.Should().NotBeEmpty();

            var doc = JsonDocument.Parse(output.Trim());
            doc.RootElement.TryGetProperty("warn", out var message).Should().BeTrue();
            message.GetString().Should().Be("Test warning message");
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void WriteError_WithValidMessage_WritesToStderr()
    {
        var originalError = Console.Error;
        var writer = new StringWriter();

        try
        {
            Console.SetError(writer);
            Logger.WriteError("Test error message");

            var output = writer.ToString();
            output.Should().NotBeEmpty();

            var doc = JsonDocument.Parse(output.Trim());
            doc.RootElement.TryGetProperty("error", out var message).Should().BeTrue();
            message.GetString().Should().Be("Test error message");
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void WriteTrace_WithValidMessage_WritesToStderr()
    {
        var originalError = Console.Error;
        var writer = new StringWriter();

        try
        {
            Console.SetError(writer);
            Logger.WriteTrace("Test trace message");

            var output = writer.ToString();
            output.Should().NotBeEmpty();

            var doc = JsonDocument.Parse(output.Trim());
            doc.RootElement.TryGetProperty("trace", out var message).Should().BeTrue();
            message.GetString().Should().Be("Test trace message");
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void WriteInfo_WithEmptyString_StillWritesToStderr()
    {
        var originalError = Console.Error;
        var writer = new StringWriter();

        try
        {
            Console.SetError(writer);
            Logger.WriteInfo("");

            var output = writer.ToString();
            output.Should().NotBeEmpty();
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    [Fact]
    public void WriteInfo_WithUnicodeCharacters_PreservesCharacters()
    {
        var originalError = Console.Error;
        var writer = new StringWriter();

        try
        {
            Console.SetError(writer);
            Logger.WriteInfo("Test with unicode: 🎉 Hello");

            var output = writer.ToString();
            var doc = JsonDocument.Parse(output.Trim());
            doc.RootElement.TryGetProperty("info", out var message).Should().BeTrue();
            message.GetString().Should().Contain("🎉");
        }
        finally
        {
            Console.SetError(originalError);
        }
    }
}
