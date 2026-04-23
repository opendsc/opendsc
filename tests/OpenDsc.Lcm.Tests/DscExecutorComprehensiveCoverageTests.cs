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
public class DscExecutorComprehensiveCoverageTests
{
    private readonly Mock<ILogger<DscExecutor>> _loggerMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();

    /// <summary>
    /// Minimal coverage tests for DscExecutor private methods.
    /// These tests use reflection to verify private methods exist and basic reflection calls work.
    /// </summary>

    [Fact]
    public void ParseDscResult_MethodExists()
    {
        var method = typeof(DscExecutor).GetMethod("ParseDscResult",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(string)],
            null);

        method.Should().NotBeNull("ParseDscResult private method should exist");
    }

    [Fact]
    public void BuildArguments_WithValidInputs_ReturnsProperFormat()
    {
        var method = typeof(DscExecutor).GetMethod("BuildArguments",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(string), typeof(string), typeof(LogLevel), typeof(string)],
            null);

        method.Should().NotBeNull();

        var args = method!.Invoke(null, ["test", "/path/to/config.yaml", LogLevel.Information, null]) as List<string>;

        args.Should().NotBeNull();
        args.Should().Contain("test");
        args.Should().Contain("/path/to/config.yaml");
    }

    [Fact]
    public void BuildArguments_WithParametersPath_IncludesParametersFlag()
    {
        var method = typeof(DscExecutor).GetMethod("BuildArguments",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(string), typeof(string), typeof(LogLevel), typeof(string)],
            null);

        method.Should().NotBeNull();

        var args = method!.Invoke(null, ["test", "/path/config.yaml", LogLevel.Debug, "/path/params.yaml"]) as List<string>;

        args.Should().NotBeNull();
        args!.Should().Contain("--parameters-file");
        args.Should().Contain("/path/params.yaml");
    }

    [Fact]
    public void BuildArguments_AllOperations_HaveCorrectCommand()
    {
        var method = typeof(DscExecutor).GetMethod("BuildArguments",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(string), typeof(string), typeof(LogLevel), typeof(string)],
            null);

        method.Should().NotBeNull();

        foreach (var operation in new[] { "test", "set" })
        {
            var args = method!.Invoke(null, [operation, "/config.yaml", LogLevel.Information, null]) as List<string>;

            args.Should().NotBeNull();
            // Arguments structure: --trace-level, trace-level-value, --trace-format, json, --progress-format, none, config, operation, --file, config-path, --output-format, json
            args.Should().Contain("--trace-level");
            args.Should().Contain("config");
            args.Should().Contain(operation);
            args.Should().Contain("--file");
            args.Should().Contain("/config.yaml");
            args.Should().Contain("--output-format");
            args.Should().Contain("json");
        }
    }

    [Fact]
    public void FindExecutableInPath_MethodExists()
    {
        var method = typeof(DscExecutor).GetMethod("FindExecutableInPath",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull("FindExecutableInPath private method should exist");
    }
}
