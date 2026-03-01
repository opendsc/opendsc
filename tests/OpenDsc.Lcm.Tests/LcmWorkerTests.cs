// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;
using Moq.Protected;

using Xunit;

namespace OpenDsc.Lcm.Tests;

[Trait("Category", "Unit")]
public class LcmWorkerTests
{
    [Fact]
    public void ConfigurationModeInterval_DefaultValue()
    {
        var config = new LcmConfig();
        config.ConfigurationModeInterval.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void ConfigurationMode_DefaultValue()
    {
        var config = new LcmConfig();
        config.ConfigurationMode.Should().Be(ConfigurationMode.Monitor);
    }

    [Fact]
    public void LcmConfig_AcceptsCustomInterval()
    {
        var config = new LcmConfig
        {
            ConfigurationModeInterval = TimeSpan.FromMinutes(5)
        };

        config.ConfigurationModeInterval.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Theory]
    [InlineData(ConfigurationMode.Monitor)]
    [InlineData(ConfigurationMode.Remediate)]
    public void LcmConfig_AcceptsAllModes(ConfigurationMode mode)
    {
        var config = new LcmConfig
        {
            ConfigurationMode = mode
        };

        config.ConfigurationMode.Should().Be(mode);
    }

    [Fact]
    public void LcmConfig_AcceptsConfigurationPath()
    {
        var config = new LcmConfig
        {
            ConfigurationPath = "/test/config.yaml"
        };

        config.ConfigurationPath.Should().Be("/test/config.yaml");
    }

    [Fact]
    public void LcmConfig_AcceptsDscExecutablePath()
    {
        var config = new LcmConfig
        {
            DscExecutablePath = "/usr/bin/dsc"
        };

        config.DscExecutablePath.Should().Be("/usr/bin/dsc");
    }

    [Theory]
    [InlineData(ConfigurationSource.Local)]
    [InlineData(ConfigurationSource.Pull)]
    public void LcmConfig_AcceptsAllSources(ConfigurationSource source)
    {
        var config = new LcmConfig
        {
            ConfigurationSource = source
        };

        config.ConfigurationSource.Should().Be(source);
    }

    [Fact]
    public void LcmConfig_DefaultSourceIsLocal()
    {
        var config = new LcmConfig();
        config.ConfigurationSource.Should().Be(ConfigurationSource.Local);
    }

    [Fact]
    public void PullServerSettings_AcceptsServerUrl()
    {
        var settings = new PullServerSettings
        {
            ServerUrl = "https://server.example.com"
        };

        settings.ServerUrl.Should().Be("https://server.example.com");
    }

    [Fact]
    public void PullServerSettings_AcceptsRegistrationKey()
    {
        var settings = new PullServerSettings
        {
            RegistrationKey = "test-key-123"
        };

        settings.RegistrationKey.Should().Be("test-key-123");
    }

    [Fact]
    public void PullServerSettings_DefaultReportComplianceIsTrue()
    {
        var settings = new PullServerSettings();
        settings.ReportCompliance.Should().BeTrue();
    }

    [Fact]
    public void MinTimeSpanAttribute_WithValidValue_ReturnsTrue()
    {
        var attribute = new MinTimeSpanAttribute("00:01:00");

        var result = attribute.IsValid(TimeSpan.FromMinutes(5));

        result.Should().BeTrue();
    }

    [Fact]
    public void MinTimeSpanAttribute_WithTooSmallValue_ReturnsFalse()
    {
        var attribute = new MinTimeSpanAttribute("00:05:00");

        var result = attribute.IsValid(TimeSpan.FromSeconds(30));

        result.Should().BeFalse();
    }

    [Fact]
    public void MinTimeSpanAttribute_WithExactMinimum_ReturnsFalse()
    {
        var attribute = new MinTimeSpanAttribute("00:05:00");

        var result = attribute.IsValid(TimeSpan.FromMinutes(5));

        result.Should().BeFalse("because the value must be greater than the minimum, not equal to it");
    }

    [Fact]
    public void MinTimeSpanAttribute_WithNullValue_ReturnsTrue()
    {
        var attribute = new MinTimeSpanAttribute("00:01:00");

        var result = attribute.IsValid(null);

        result.Should().BeTrue("because null values return ValidationResult.Success");
    }

    [Fact]
    public void MinTimeSpanAttribute_WithNonTimeSpanValue_ReturnsTrue()
    {
        var attribute = new MinTimeSpanAttribute("00:01:00");

        var result = attribute.IsValid("not a timespan");

        result.Should().BeTrue("because non-TimeSpan values return ValidationResult.Success");
    }

    [Fact]
    public void MinTimeSpanAttribute_FormatErrorMessage_ReturnsCorrectMessage()
    {
        var attribute = new MinTimeSpanAttribute("00:05:00");

        var message = attribute.FormatErrorMessage("TestProperty");

        message.Should().Be("Interval must be greater than 00:05:00", "because the error message uses the fixed format string");
    }

    [Fact]
    public void PullServerSettings_WithAllValues_StoresCorrectly()
    {
        var settings = new PullServerSettings
        {
            ServerUrl = "https://test.com",
            RegistrationKey = "key123",
            NodeId = Guid.NewGuid(),
            ReportCompliance = false,
            ConfigurationChecksum = "abc123",
            CertificateSource = CertificateSource.Managed
        };

        settings.ServerUrl.Should().Be("https://test.com");
        settings.RegistrationKey.Should().Be("key123");
        settings.NodeId.Should().NotBeNull();
        settings.ReportCompliance.Should().BeFalse();
        settings.ConfigurationChecksum.Should().Be("abc123");
        settings.CertificateSource.Should().Be(CertificateSource.Managed);
    }

    [Fact]
    public void LcmConfig_WithPullServer_StoresCorrectly()
    {
        var config = new LcmConfig
        {
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = "https://dsc.example.com",
                RegistrationKey = "test-key"
            }
        };

        config.ConfigurationSource.Should().Be(ConfigurationSource.Pull);
        config.PullServer.Should().NotBeNull();
        config.PullServer!.ServerUrl.Should().Be("https://dsc.example.com");
        config.PullServer.RegistrationKey.Should().Be("test-key");
    }

    [Fact]
    public async Task LcmWorker_MonitorMode_ExecutesDscTest()
    {
        var tempConfigPath = Path.Combine(Path.GetTempPath(), $"test-config-{Guid.NewGuid()}.dsc.yaml");
        File.WriteAllText(tempConfigPath, "# Test configuration");

        try
        {
            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["Logging:LogLevel:OpenDsc.Lcm"]).Returns("Information");

            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Monitor,
                ConfigurationPath = tempConfigPath,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(100)
            };

            var optionsMonitor = CreateOptionsMonitor(lcmConfig);
            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            PullServerClient pullServerClient = null!;
            var certificateManagerMock = new Mock<CertificateManager>(optionsMonitor, new Mock<ILogger<CertificateManager>>().Object);
            var loggerMock = new Mock<ILogger<LcmWorker>>();

            var dscResult = new OpenDsc.Schema.DscResult { HadErrors = false };
            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((dscResult, 0));

            var worker = new LcmWorker(configMock.Object, optionsMonitor, dscExecutorMock.Object, pullServerClient, certificateManagerMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            await worker.StartAsync(cts.Token);
            await Task.Delay(200, CancellationToken.None);
            await worker.StopAsync(CancellationToken.None);

            dscExecutorMock.Protected()
                .Verify("ExecuteCommandAsync", Times.AtLeastOnce(),
                    ItExpr.Is<string>(s => s == "test"), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>());
        }
        finally
        {
            if (File.Exists(tempConfigPath)) File.Delete(tempConfigPath);
        }
    }

    [Fact]
    public async Task LcmWorker_RemediateMode_ExecutesTestAndSet()
    {
        var tempConfigPath = Path.Combine(Path.GetTempPath(), $"test-config-{Guid.NewGuid()}.dsc.yaml");
        File.WriteAllText(tempConfigPath, "# Test configuration");

        try
        {
            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["Logging:LogLevel:OpenDsc.Lcm"]).Returns("Information");

            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Remediate,
                ConfigurationPath = tempConfigPath,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(100)
            };

            var optionsMonitor = CreateOptionsMonitor(lcmConfig);
            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            PullServerClient pullServerClient = null!;
            var loggerMock = new Mock<ILogger<LcmWorker>>();

            var testResultJson = System.Text.Json.JsonDocument.Parse("{\"desiredState\":{},\"actualState\":{},\"inDesiredState\":false}");
            var testResult = new OpenDsc.Schema.DscResult
            {
                HadErrors = false,
                Results = new System.Collections.Generic.List<OpenDsc.Schema.DscResourceResult>
                {
                    new OpenDsc.Schema.DscResourceResult
                    {
                        Type = "Test/Resource",
                        Name = "TestInstance",
                        Result = testResultJson.RootElement.Clone()
                    }
                }
            };

            var setResult = new OpenDsc.Schema.DscResult { HadErrors = false };

            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((string op, string path, LcmConfig cfg, LogLevel level, CancellationToken ct) =>
                    op == "test" ? (testResult, 0) : (setResult, 0));

            var certificateManagerMock = new Mock<CertificateManager>(optionsMonitor, Mock.Of<ILogger<CertificateManager>>());
            var worker = new LcmWorker(configMock.Object, optionsMonitor, dscExecutorMock.Object, pullServerClient, certificateManagerMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            await worker.StartAsync(cts.Token);
            await Task.Delay(200, CancellationToken.None);
            await worker.StopAsync(CancellationToken.None);

            dscExecutorMock.Protected()
                .Verify("ExecuteCommandAsync", Times.AtLeastOnce(),
                    ItExpr.Is<string>(s => s == "test"), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>());
            dscExecutorMock.Protected()
                .Verify("ExecuteCommandAsync", Times.AtLeastOnce(),
                    ItExpr.Is<string>(s => s == "set"), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>());
        }
        finally
        {
            if (File.Exists(tempConfigPath)) File.Delete(tempConfigPath);
        }
    }

    [Fact]
    public async Task LcmWorker_RemediateMode_SkipsSetWhenInDesiredState()
    {
        var tempConfigPath = Path.Combine(Path.GetTempPath(), $"test-config-{Guid.NewGuid()}.dsc.yaml");
        File.WriteAllText(tempConfigPath, "# Test configuration");

        try
        {
            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["Logging:LogLevel:OpenDsc.Lcm"]).Returns("Information");

            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Remediate,
                ConfigurationPath = tempConfigPath,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(100)
            };

            var optionsMonitor = CreateOptionsMonitor(lcmConfig);
            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            PullServerClient pullServerClient = null!;
            var loggerMock = new Mock<ILogger<LcmWorker>>();

            var testResultJson = System.Text.Json.JsonDocument.Parse("{\"desiredState\":{},\"actualState\":{},\"inDesiredState\":true}");
            var testResult = new OpenDsc.Schema.DscResult
            {
                HadErrors = false,
                Results = new System.Collections.Generic.List<OpenDsc.Schema.DscResourceResult>
                {
                    new OpenDsc.Schema.DscResourceResult
                    {
                        Type = "Test/Resource",
                        Name = "TestInstance",
                        Result = testResultJson.RootElement.Clone()
                    }
                }
            };

            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.Is<string>(s => s == "test"), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((testResult, 0));

            var certificateManagerMock = new Mock<CertificateManager>(optionsMonitor, Mock.Of<ILogger<CertificateManager>>());
            var worker = new LcmWorker(configMock.Object, optionsMonitor, dscExecutorMock.Object, pullServerClient, certificateManagerMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            await worker.StartAsync(cts.Token);
            await Task.Delay(200, CancellationToken.None);
            await worker.StopAsync(CancellationToken.None);

            dscExecutorMock.Protected()
                .Verify("ExecuteCommandAsync", Times.AtLeastOnce(),
                    ItExpr.Is<string>(s => s == "test"), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>());
            dscExecutorMock.Protected()
                .Verify("ExecuteCommandAsync", Times.Never(),
                    ItExpr.Is<string>(s => s == "set"), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>());
        }
        finally
        {
            if (File.Exists(tempConfigPath)) File.Delete(tempConfigPath);
        }
    }

    [Fact]
    public async Task LcmWorker_ConfigurationChange_CancelsCurrentOperation()
    {
        var configMock = new Mock<IConfiguration>();
        configMock.SetupGet(c => c["Logging:LogLevel:OpenDsc.Lcm"]).Returns("Information");

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.dsc.yaml",
            ConfigurationModeInterval = TimeSpan.FromSeconds(10)
        };

        var optionsMonitorMock = new Mock<IOptionsMonitor<LcmConfig>>();
        optionsMonitorMock.Setup(o => o.CurrentValue).Returns(lcmConfig);

        var changeCallbacks = new List<Action<LcmConfig, string?>>();
        optionsMonitorMock.Setup(o => o.OnChange(It.IsAny<Action<LcmConfig, string?>>()))
            .Callback<Action<LcmConfig, string?>>(callback => changeCallbacks.Add(callback))
            .Returns(Mock.Of<IDisposable>());

        var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
        PullServerClient pullServerClient = null!;
        var loggerMock = new Mock<ILogger<LcmWorker>>();

        var dscResult = new OpenDsc.Schema.DscResult { HadErrors = false };
        dscExecutorMock.Protected()
            .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),
                ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((dscResult, 0));

        var certificateManagerMock = new Mock<CertificateManager>(optionsMonitorMock.Object, new Mock<ILogger<CertificateManager>>().Object);
        var worker = new LcmWorker(configMock.Object, optionsMonitorMock.Object, dscExecutorMock.Object, pullServerClient, certificateManagerMock.Object, loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await worker.StartAsync(cts.Token);
        await Task.Delay(100, CancellationToken.None);

        lcmConfig.ConfigurationMode = ConfigurationMode.Remediate;
        foreach (var callback in changeCallbacks)
        {
            callback(lcmConfig, "");
        }

        await Task.Delay(100, CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        changeCallbacks.Should().HaveCountGreaterThan(0, "configuration change callback should be registered");
    }

    [Fact]
    public async Task LcmWorker_HandlesExecutionErrors_ContinuesOperation()
    {
        var tempConfigPath = Path.Combine(Path.GetTempPath(), $"test-config-{Guid.NewGuid()}.dsc.yaml");
        File.WriteAllText(tempConfigPath, "# Test configuration");

        try
        {
            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["Logging:LogLevel:OpenDsc.Lcm"]).Returns("Information");

            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Monitor,
                ConfigurationPath = tempConfigPath,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(100)
            };

            var optionsMonitor = CreateOptionsMonitor(lcmConfig);
            var dscExecutorMock = new Mock<DscExecutor>(Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            PullServerClient pullServerClient = null!;
            var loggerMock = new Mock<ILogger<LcmWorker>>();

            var callCount = 0;
            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>())
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return Task.FromException<(OpenDsc.Schema.DscResult, int)>(new InvalidOperationException("Test error"));
                    }
                    return Task.FromResult<(OpenDsc.Schema.DscResult, int)>((new OpenDsc.Schema.DscResult { HadErrors = false }, 0));
                });

            var certificateManagerMock = new Mock<CertificateManager>(optionsMonitor, Mock.Of<ILogger<CertificateManager>>());
            var worker = new LcmWorker(configMock.Object, optionsMonitor, dscExecutorMock.Object, pullServerClient, certificateManagerMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await worker.StartAsync(cts.Token);
            await Task.Delay(1500, CancellationToken.None);
            await worker.StopAsync(CancellationToken.None);

            callCount.Should().BeGreaterThanOrEqualTo(2, "worker should retry after error");
        }
        finally
        {
            if (File.Exists(tempConfigPath)) File.Delete(tempConfigPath);
        }
    }

    [Fact]
    public async Task LcmWorker_MissingConfigurationFile_HandlesGracefully()
    {
        var configMock = new Mock<IConfiguration>();
        configMock.SetupGet(c => c["Logging:LogLevel:OpenDsc.Lcm"]).Returns("Information");

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = string.Empty,
            ConfigurationModeInterval = TimeSpan.FromMilliseconds(100)
        };

        var optionsMonitor = CreateOptionsMonitor(lcmConfig);
        var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
        PullServerClient pullServerClient = null!;
        var loggerMock = new Mock<ILogger<LcmWorker>>();

        var certificateManagerMock = new Mock<CertificateManager>(optionsMonitor, new Mock<ILogger<CertificateManager>>().Object);
        var worker = new LcmWorker(configMock.Object, optionsMonitor, dscExecutorMock.Object, pullServerClient, certificateManagerMock.Object, loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        await worker.StartAsync(cts.Token);
        await Task.Delay(300, CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        dscExecutorMock.Protected()
            .Verify("ExecuteCommandAsync", Times.Never(),
                ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task LcmWorker_IntervalChange_AdjustsTimingDynamically()
    {
        var tempConfigPath = Path.Combine(Path.GetTempPath(), $"test-config-{Guid.NewGuid()}.dsc.yaml");
        File.WriteAllText(tempConfigPath, "# Test configuration");

        try
        {
            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["Logging:LogLevel:OpenDsc.Lcm"]).Returns("Information");

            var initialConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Monitor,
                ConfigurationPath = tempConfigPath,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(500)
            };

            var optionsMonitorMock = new Mock<IOptionsMonitor<LcmConfig>>();
            var lcmConfig = initialConfig;
            optionsMonitorMock.Setup(o => o.CurrentValue).Returns(() => lcmConfig);
            optionsMonitorMock.Setup(o => o.OnChange(It.IsAny<Action<LcmConfig, string?>>()))
                .Returns(Mock.Of<IDisposable>());

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            PullServerClient pullServerClient = null!;
            var loggerMock = new Mock<ILogger<LcmWorker>>();

            var dscResult = new OpenDsc.Schema.DscResult { HadErrors = false };
            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((dscResult, 0));

            var certificateManagerMock = new Mock<CertificateManager>(optionsMonitorMock.Object, new Mock<ILogger<CertificateManager>>().Object);
            var worker = new LcmWorker(configMock.Object, optionsMonitorMock.Object, dscExecutorMock.Object, pullServerClient, certificateManagerMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            await worker.StartAsync(cts.Token);
            await Task.Delay(200, CancellationToken.None);

            lcmConfig.ConfigurationModeInterval = TimeSpan.FromMilliseconds(100);

            await Task.Delay(300, CancellationToken.None);
            await worker.StopAsync(CancellationToken.None);

            dscExecutorMock.Protected()
                .Verify("ExecuteCommandAsync", Times.AtLeastOnce(),
                    ItExpr.Is<string>(s => s == "test"), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<CancellationToken>());
        }
        finally
        {
            if (File.Exists(tempConfigPath)) File.Delete(tempConfigPath);
        }
    }

    private static IOptionsMonitor<LcmConfig> CreateOptionsMonitor(LcmConfig config)
    {
        var mock = new Mock<IOptionsMonitor<LcmConfig>>();
        mock.Setup(o => o.CurrentValue).Returns(config);
        mock.Setup(o => o.OnChange(It.IsAny<Action<LcmConfig, string?>>())).Returns(Mock.Of<IDisposable>());
        return mock.Object;
    }
}
