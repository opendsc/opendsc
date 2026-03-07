// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using AwesomeAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;
using Moq.Protected;

using OpenDsc.Lcm.Contracts;

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
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((dscResult, 0));

            var worker = new LcmWorker(configMock.Object, optionsMonitor, dscExecutorMock.Object, pullServerClient, certificateManagerMock.Object, loggerMock.Object);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            await worker.StartAsync(cts.Token);
            await Task.Delay(200, CancellationToken.None);
            await worker.StopAsync(CancellationToken.None);

            dscExecutorMock.Protected()
                .Verify("ExecuteCommandAsync", Times.AtLeastOnce(),
                    ItExpr.Is<string>(s => s == "test"), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>());
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
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((string op, string path, LcmConfig cfg, LogLevel level, string? parametersPath, CancellationToken ct) =>
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
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>());
            dscExecutorMock.Protected()
                .Verify("ExecuteCommandAsync", Times.AtLeastOnce(),
                    ItExpr.Is<string>(s => s == "set"), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>());
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
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
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
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>());
            dscExecutorMock.Protected()
                .Verify("ExecuteCommandAsync", Times.Never(),
                    ItExpr.Is<string>(s => s == "set"), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>());
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
                ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
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
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
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
                ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>());
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
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
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
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>());
        }
        finally
        {
            if (File.Exists(tempConfigPath)) File.Delete(tempConfigPath);
        }
    }

    [Fact]
    public async Task PullMode_NewBundle_ClearsOrphanedFilesFromExtractDir()
    {
        var extractDir = Path.Combine(Path.GetTempPath(), $"lcm-pull-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(extractDir);
        var orphanedFile = Path.Combine(extractDir, "orphaned.dsc.yaml");
        File.WriteAllText(orphanedFile, "# orphaned config");

        try
        {
            var nodeId = Guid.NewGuid();
            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Monitor,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(200),
                ConfigurationSource = ConfigurationSource.Pull,
                PullServer = new PullServerSettings
                {
                    ServerUrl = "http://test-server",
                    RegistrationKey = "test-key",
                    NodeId = nodeId,
                    ConfigurationChecksum = "old-checksum",
                    ReportCompliance = false,
                    CertificateSource = CertificateSource.Managed
                }
            };

            var optionsMonitor = CreateOptionsMonitor(lcmConfig);
            var httpHandlerMock = new Mock<HttpMessageHandler>();

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/checksum")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new ConfigurationChecksumResponse
                    {
                        Checksum = "new-checksum",
                        EntryPoint = "main.dsc.yaml"
                    })
                });

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/bundle")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StreamContent(CreateZipBundle([("main.dsc.yaml", "# config")])),
                });

            var httpClient = new HttpClient(httpHandlerMock.Object)
            {
                BaseAddress = new Uri("http://test-server")
            };

            var certManagerMock = new Mock<ICertificateManager>();
            var pullClient = new PullServerClient(httpClient, optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["Logging:LogLevel:OpenDsc.Lcm"]).Returns("Information");

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Loose, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((new OpenDsc.Schema.DscResult { HadErrors = false }, 0));

            var worker = new TestLcmWorker(configMock.Object, optionsMonitor, dscExecutorMock.Object,
                pullClient, certManagerMock.Object, Mock.Of<ILogger<LcmWorker>>(), extractDir);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StartAsync(cts.Token);
            await Task.Delay(1000, CancellationToken.None);
            await worker.StopAsync(CancellationToken.None);

            File.Exists(orphanedFile).Should().BeFalse("orphaned files must be removed when a new bundle is extracted");
            File.Exists(Path.Combine(extractDir, "main.dsc.yaml")).Should().BeTrue("the new bundle entry point must be present");
        }
        finally
        {
            if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true);
        }
    }

    [Fact]
    public async Task PullMode_NewBundle_ExtractsAllBundleFiles()
    {
        var extractDir = Path.Combine(Path.GetTempPath(), $"lcm-pull-test-{Guid.NewGuid()}");

        try
        {
            var nodeId = Guid.NewGuid();
            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Monitor,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(200),
                ConfigurationSource = ConfigurationSource.Pull,
                PullServer = new PullServerSettings
                {
                    ServerUrl = "http://test-server",
                    RegistrationKey = "test-key",
                    NodeId = nodeId,
                    ConfigurationChecksum = "old-checksum",
                    ReportCompliance = false,
                    CertificateSource = CertificateSource.Managed
                }
            };

            var optionsMonitor = CreateOptionsMonitor(lcmConfig);
            var httpHandlerMock = new Mock<HttpMessageHandler>();

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/checksum")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new ConfigurationChecksumResponse
                    {
                        Checksum = "new-checksum",
                        EntryPoint = "main.dsc.yaml"
                    })
                });

            var bundleFiles = new[]
            {
                ("main.dsc.yaml", "# main config"),
                ("sub/helper.dsc.yaml", "# helper config")
            };

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/bundle")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StreamContent(CreateZipBundle(bundleFiles)),
                });

            var httpClient = new HttpClient(httpHandlerMock.Object)
            {
                BaseAddress = new Uri("http://test-server")
            };

            var certManagerMock = new Mock<ICertificateManager>();
            var pullClient = new PullServerClient(httpClient, optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["Logging:LogLevel:OpenDsc.Lcm"]).Returns("Information");

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Loose, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((new OpenDsc.Schema.DscResult { HadErrors = false }, 0));

            var worker = new TestLcmWorker(configMock.Object, optionsMonitor, dscExecutorMock.Object,
                pullClient, certManagerMock.Object, Mock.Of<ILogger<LcmWorker>>(), extractDir);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StartAsync(cts.Token);
            await Task.Delay(1000, CancellationToken.None);
            await worker.StopAsync(CancellationToken.None);

            File.Exists(Path.Combine(extractDir, "main.dsc.yaml")).Should().BeTrue();
            File.Exists(Path.Combine(extractDir, "sub", "helper.dsc.yaml")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true);
        }
    }

    [Fact]
    public async Task PullMode_BundleDownloadFails_StatusReturnsToIdle()
    {
        var extractDir = Path.Combine(Path.GetTempPath(), $"lcm-pull-test-{Guid.NewGuid()}");

        try
        {
            var nodeId = Guid.NewGuid();
            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Monitor,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(200),
                ConfigurationSource = ConfigurationSource.Pull,
                PullServer = new PullServerSettings
                {
                    ServerUrl = "http://test-server",
                    RegistrationKey = "test-key",
                    NodeId = nodeId,
                    ConfigurationChecksum = "old-checksum",
                    ReportCompliance = false,
                    CertificateSource = CertificateSource.Managed
                }
            };

            var optionsMonitor = CreateOptionsMonitor(lcmConfig);
            var httpHandlerMock = new Mock<HttpMessageHandler>();

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/checksum")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new ConfigurationChecksumResponse
                    {
                        Checksum = "new-checksum",
                        EntryPoint = "main.dsc.yaml"
                    })
                });

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/bundle")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

            var lcmStatusRequests = new List<LcmStatus>();
            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/lcm-status")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent })
                .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
                {
                    var body = await req.Content!.ReadAsStringAsync();
                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
                    var parsed = JsonSerializer.Deserialize<UpdateLcmStatusRequest>(body, jsonOptions);
                    if (parsed is not null)
                    {
                        lcmStatusRequests.Add(parsed.LcmStatus);
                    }
                });

            var httpClient = new HttpClient(httpHandlerMock.Object)
            {
                BaseAddress = new Uri("http://test-server")
            };

            var certManagerMock = new Mock<ICertificateManager>();
            var pullClient = new PullServerClient(httpClient, optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["Logging:LogLevel:OpenDsc.Lcm"]).Returns("Information");

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());

            var worker = new TestLcmWorker(configMock.Object, optionsMonitor, dscExecutorMock.Object,
                pullClient, certManagerMock.Object, Mock.Of<ILogger<LcmWorker>>(), extractDir);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await worker.StartAsync(cts.Token);
            await Task.Delay(700, CancellationToken.None);
            await worker.StopAsync(CancellationToken.None);

            lcmStatusRequests.Should().Contain(LcmStatus.Downloading, "status must be set to Downloading when a bundle download starts");
            lcmStatusRequests.Should().Contain(LcmStatus.Idle, "status must be reset to Idle after a failed bundle download");
            var lastStatus = lcmStatusRequests.Last();
            lastStatus.Should().Be(LcmStatus.Idle, "the final status before the next wait must be Idle, not Downloading");
        }
        finally
        {
            if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true);
        }
    }

    private static IOptionsMonitor<LcmConfig> CreateOptionsMonitor(LcmConfig config)
    {
        var mock = new Mock<IOptionsMonitor<LcmConfig>>();
        mock.Setup(o => o.CurrentValue).Returns(config);
        mock.Setup(o => o.OnChange(It.IsAny<Action<LcmConfig, string?>>())).Returns(Mock.Of<IDisposable>());
        return mock.Object;
    }

    private static MemoryStream CreateZipBundle(IEnumerable<(string Name, string Content)> files)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new System.IO.StreamWriter(entry.Open());
                writer.Write(content);
            }
        }
        stream.Position = 0;
        return stream;
    }

    private sealed class TestLcmWorker(
        IConfiguration configuration,
        IOptionsMonitor<LcmConfig> lcmMonitor,
        DscExecutor dscExecutor,
        PullServerClient pullServerClient,
        ICertificateManager certificateManager,
        ILogger<LcmWorker> logger,
        string extractDir)
        : LcmWorker(configuration, lcmMonitor, dscExecutor, pullServerClient, certificateManager, logger)
    {
        protected override string GetPullExtractDirectory() => extractDir;
    }
}
