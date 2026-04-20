// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

namespace OpenDsc.Resource.Tests;

[Trait("Category", "Unit")]

public class LoggingTypesTests
{
    [Fact]
    public void Info_HasMessageProperty()
    {
        var info = new Info { Message = "Test info" };

        info.Message.Should().Be("Test info");
    }

    [Fact]
    public void Info_DefaultMessageIsEmpty()
    {
        var info = new Info();

        info.Message.Should().BeEmpty();
    }

    [Fact]
    public void Info_CanBeSerializedToJson()
    {
        var info = new Info { Message = "Test" };

        var json = JsonSerializer.Serialize(info);

        json.Should().Contain("info");
        json.Should().Contain("Test");
    }

    [Fact]
    public void Warning_HasMessageProperty()
    {
        var warning = new Warning { Message = "Test warning" };

        warning.Message.Should().Be("Test warning");
    }

    [Fact]
    public void Warning_DefaultMessageIsEmpty()
    {
        var warning = new Warning();

        warning.Message.Should().BeEmpty();
    }

    [Fact]
    public void Warning_CanBeSerializedToJson()
    {
        var warning = new Warning { Message = "Test" };

        var json = JsonSerializer.Serialize(warning);

        json.Should().Contain("warn");
        json.Should().Contain("Test");
    }

    [Fact]
    public void Error_HasMessageProperty()
    {
        var error = new Error { Message = "Test error" };

        error.Message.Should().Be("Test error");
    }

    [Fact]
    public void Error_DefaultMessageIsEmpty()
    {
        var error = new Error();

        error.Message.Should().BeEmpty();
    }

    [Fact]
    public void Error_CanBeSerializedToJson()
    {
        var error = new Error { Message = "Test" };

        var json = JsonSerializer.Serialize(error);

        json.Should().Contain("error");
        json.Should().Contain("Test");
    }

    [Fact]
    public void Trace_HasMessageProperty()
    {
        var trace = new Trace { Message = "Test trace" };

        trace.Message.Should().Be("Test trace");
    }

    [Fact]
    public void Trace_DefaultMessageIsEmpty()
    {
        var trace = new Trace();

        trace.Message.Should().BeEmpty();
    }

    [Fact]
    public void Trace_CanBeSerializedToJson()
    {
        var trace = new Trace { Message = "Test" };

        var json = JsonSerializer.Serialize(trace);

        json.Should().Contain("trace");
        json.Should().Contain("Test");
    }

    [Fact]
    public void DifferentTypes_HaveDifferentPropertyNames()
    {
        var infoJson = JsonSerializer.Serialize(new Info { Message = "M" });
        var warningJson = JsonSerializer.Serialize(new Warning { Message = "M" });
        var errorJson = JsonSerializer.Serialize(new Error { Message = "M" });
        var traceJson = JsonSerializer.Serialize(new Trace { Message = "M" });

        infoJson.Should().Contain("\"info\"");
        warningJson.Should().Contain("\"warn\"");
        errorJson.Should().Contain("\"error\"");
        traceJson.Should().Contain("\"trace\"");
    }
}
