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
public class DscExecutorTests
{
    private readonly Mock<ILogger<DscExecutor>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger> _dscLoggerMock;
    private readonly DscExecutor _executor;

    public DscExecutorTests()
    {
        _loggerMock = new Mock<ILogger<DscExecutor>>();
        _dscLoggerMock = new Mock<ILogger>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger("DSC"))
            .Returns(_dscLoggerMock.Object);
        _executor = new DscExecutor(_loggerMock.Object, _loggerFactoryMock.Object);
    }

    [Theory]
    [InlineData(LogLevel.Trace, "trace")]
    [InlineData(LogLevel.Debug, "debug")]
    [InlineData(LogLevel.Information, "info")]
    [InlineData(LogLevel.Warning, "warn")]
    [InlineData(LogLevel.Error, "error")]
    [InlineData(LogLevel.Critical, "error")]
    public void MapLogLevelToTraceLevel_ReturnsCorrectMapping(LogLevel logLevel, string expected)
    {
        var traceLevelField = typeof(DscExecutor).GetMethod("MapLogLevelToTraceLevel",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = traceLevelField!.Invoke(null, [logLevel]);

        result.Should().Be(expected);
    }

    [Fact]
    public void BuildArguments_CreatesCorrectCommandLine()
    {
        var buildArgumentsMethod = typeof(DscExecutor).GetMethod("BuildArguments",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = buildArgumentsMethod!.Invoke(null, ["test", "/path/to/config.yaml", LogLevel.Information]) as List<string>;

        result.Should().NotBeNull();
        result.Should().Contain("--trace-level");
        result.Should().Contain("info");
        result.Should().Contain("--trace-format");
        result.Should().Contain("json");
        result.Should().Contain("--progress-format");
        result.Should().Contain("none");
        result.Should().Contain("config");
        result.Should().Contain("test");
        result.Should().Contain("--file");
        result.Should().Contain("/path/to/config.yaml");
        result.Should().Contain("--output-format");
        result.Should().Contain("json");
    }

    [Theory]
    [InlineData("test")]
    [InlineData("set")]
    public void BuildArguments_IncludesCorrectOperation(string operation)
    {
        var buildArgumentsMethod = typeof(DscExecutor).GetMethod("BuildArguments",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = buildArgumentsMethod!.Invoke(null, [operation, "/path/config.yaml", LogLevel.Debug]) as List<string>;

        result.Should().Contain(operation);
    }

    [Fact]
    public void ParseAndLogDscMessages_HandlesEmptyString()
    {
        var parseMethod = typeof(DscExecutor).GetMethod("ParseAndLogDscMessages",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var action = () => parseMethod!.Invoke(_executor, [""]);

        action.Should().NotThrow();
    }

    [Fact]
    public void FindExecutableInPath_ReturnsNullWhenPathEmpty()
    {
        var findMethod = typeof(DscExecutor).GetMethod("FindExecutableInPath",
            BindingFlags.NonPublic | BindingFlags.Static);

        var originalPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable("PATH", "");
            var result = findMethod!.Invoke(null, null);
            result.Should().BeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }
}
