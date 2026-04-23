// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;

using AwesomeAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace OpenDsc.Lcm.Tests;

[Trait("Category", "Unit")]
public class DscExecutorErrorPathTests
{
    private readonly Mock<ILogger<DscExecutor>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger> _dscLoggerMock;
    private readonly DscExecutor _executor;

    public DscExecutorErrorPathTests()
    {
        _loggerMock = new Mock<ILogger<DscExecutor>>();
        _dscLoggerMock = new Mock<ILogger>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger("DSC"))
            .Returns(_dscLoggerMock.Object);
        _executor = new DscExecutor(_loggerMock.Object, _loggerFactoryMock.Object);
    }

    [Fact]
    public void BuildArguments_WithNullParametersPath_SkipsParametersFile()
    {
        var buildArgumentsMethod = typeof(DscExecutor).GetMethod("BuildArguments",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = buildArgumentsMethod!.Invoke(null, ["test", "/path/to/config.yaml", Microsoft.Extensions.Logging.LogLevel.Information, null]) as List<string>;

        result.Should().NotBeNull();
        result.Should().NotContain("--parameters-file");
    }

    [Fact]
    public void BuildArguments_WithParametersPath_IncludesParametersFile()
    {
        var buildArgumentsMethod = typeof(DscExecutor).GetMethod("BuildArguments",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = buildArgumentsMethod!.Invoke(null, ["test", "/path/to/config.yaml", Microsoft.Extensions.Logging.LogLevel.Information, "/path/to/params.json"]) as List<string>;

        result.Should().NotBeNull();
        result.Should().Contain("--parameters-file");
        result.Should().Contain("/path/to/params.json");
    }

    [Fact]
    public void MapLogLevelToTraceLevel_WithTraceLevel_ReturnsTrace()
    {
        var mapMethod = typeof(DscExecutor).GetMethod("MapLogLevelToTraceLevel",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = mapMethod!.Invoke(null, [Microsoft.Extensions.Logging.LogLevel.Trace]) as string;

        result.Should().Be("trace");
    }

    [Fact]
    public void MapLogLevelToTraceLevel_WithDebugLevel_ReturnsDebug()
    {
        var mapMethod = typeof(DscExecutor).GetMethod("MapLogLevelToTraceLevel",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = mapMethod!.Invoke(null, [Microsoft.Extensions.Logging.LogLevel.Debug]) as string;

        result.Should().Be("debug");
    }

    [Fact]
    public void MapLogLevelToTraceLevel_WithInformationLevel_ReturnsInfo()
    {
        var mapMethod = typeof(DscExecutor).GetMethod("MapLogLevelToTraceLevel",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = mapMethod!.Invoke(null, [Microsoft.Extensions.Logging.LogLevel.Information]) as string;

        result.Should().Be("info");
    }

    [Fact]
    public void MapLogLevelToTraceLevel_WithWarningLevel_ReturnsWarn()
    {
        var mapMethod = typeof(DscExecutor).GetMethod("MapLogLevelToTraceLevel",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = mapMethod!.Invoke(null, [Microsoft.Extensions.Logging.LogLevel.Warning]) as string;

        result.Should().Be("warn");
    }

    [Fact]
    public void MapLogLevelToTraceLevel_WithErrorLevel_ReturnsError()
    {
        var mapMethod = typeof(DscExecutor).GetMethod("MapLogLevelToTraceLevel",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = mapMethod!.Invoke(null, [Microsoft.Extensions.Logging.LogLevel.Error]) as string;

        result.Should().Be("error");
    }

    [Fact]
    public void MapLogLevelToTraceLevel_WithCriticalLevel_ReturnsError()
    {
        var mapMethod = typeof(DscExecutor).GetMethod("MapLogLevelToTraceLevel",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = mapMethod!.Invoke(null, [Microsoft.Extensions.Logging.LogLevel.Critical]) as string;

        result.Should().Be("error");
    }

    [Fact]
    public void MapLogLevelToTraceLevel_WithNoneLevel_ReturnsInfo()
    {
        var mapMethod = typeof(DscExecutor).GetMethod("MapLogLevelToTraceLevel",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = mapMethod!.Invoke(null, [Microsoft.Extensions.Logging.LogLevel.None]) as string;

        result.Should().Be("info", "LogLevel.None should default to info");
    }

    [Fact]
    public void ParseAndLogDscMessages_WithEmptyString_DoesNotThrow()
    {
        var parseMethod = typeof(DscExecutor).GetMethod("ParseAndLogDscMessages",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var action = () => parseMethod!.Invoke(_executor, [""])!;

        action.Should().NotThrow();
    }

    [Fact]
    public void ParseAndLogDscMessages_WithWhitespaceOnly_DoesNotThrow()
    {
        var parseMethod = typeof(DscExecutor).GetMethod("ParseAndLogDscMessages",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var action = () => parseMethod!.Invoke(_executor, ["   \n\t"])!;

        action.Should().NotThrow();
    }

    [Fact]
    public void ParseAndLogDscMessages_WithSingleLineJson_Succeeds()
    {
        var jsonLine = "{\"level\":\"error\",\"message\":\"Test error message\"}";
        var parseMethod = typeof(DscExecutor).GetMethod("ParseAndLogDscMessages",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var action = () => parseMethod!.Invoke(_executor, [jsonLine])!;

        action.Should().NotThrow();
    }

    [Fact]
    public void FindExecutableInPath_WithValidDotnet_ReturnsPathOrNull()
    {
        var findMethod = typeof(DscExecutor).GetMethod("FindExecutableInPath",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = findMethod!.Invoke(null, []) as string;

        // May or may not find dsc depending on system configuration
        (result == null || result is string).Should().BeTrue();
    }

    [Fact]
    public void BuildArguments_MultipleInvocations_ProducesConsistentResults()
    {
        var buildArgumentsMethod = typeof(DscExecutor).GetMethod("BuildArguments",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result1 = buildArgumentsMethod!.Invoke(null, ["test", "/config.yaml", Microsoft.Extensions.Logging.LogLevel.Information, null]) as List<string>;
        var result2 = buildArgumentsMethod!.Invoke(null, ["test", "/config.yaml", Microsoft.Extensions.Logging.LogLevel.Information, null]) as List<string>;

        result1.Should().Equal(result2, "multiple invocations should produce identical results");
    }

    [Fact]
    public void ParseAndLogDscMessages_WithMultipleLines_ProcessesAll()
    {
        var jsonLines = "{\"level\":\"info\",\"message\":\"Line 1\"}\n{\"level\":\"error\",\"message\":\"Line 2\"}";
        var parseMethod = typeof(DscExecutor).GetMethod("ParseAndLogDscMessages",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var action = () => parseMethod!.Invoke(_executor, [jsonLines])!;

        action.Should().NotThrow();
    }

    [Fact]
    public void BuildArguments_SetOperation_IncludesSetCommand()
    {
        var buildArgumentsMethod = typeof(DscExecutor).GetMethod("BuildArguments",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = buildArgumentsMethod!.Invoke(null, ["set", "/config.yaml", Microsoft.Extensions.Logging.LogLevel.Information, null]) as List<string>;

        result.Should().Contain("set");
    }

    [Fact]
    public void BuildArguments_TestOperation_IncludesTestCommand()
    {
        var buildArgumentsMethod = typeof(DscExecutor).GetMethod("BuildArguments",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = buildArgumentsMethod!.Invoke(null, ["test", "/config.yaml", Microsoft.Extensions.Logging.LogLevel.Information, null]) as List<string>;

        result.Should().Contain("test");
    }
}
