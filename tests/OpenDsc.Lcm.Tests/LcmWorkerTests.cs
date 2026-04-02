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
    public void LcmConfig_DefaultConfigurationPathIncludesLocalFolder()
    {
        var config = new LcmConfig();
        config.ConfigurationPath.Should().EndWith($"{Path.DirectorySeparatorChar}config{Path.DirectorySeparatorChar}local{Path.DirectorySeparatorChar}main.dsc.yaml");
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
            await Task.Delay(200, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

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
            await Task.Delay(200, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

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
            await Task.Delay(200, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

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
        await Task.Delay(100, TestContext.Current.CancellationToken);

        lcmConfig.ConfigurationMode = ConfigurationMode.Remediate;
        foreach (var callback in changeCallbacks)
        {
            callback(lcmConfig, "");
        }

        await Task.Delay(100, TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);

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
            await Task.Delay(1500, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

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
        await Task.Delay(300, TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);

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
            await Task.Delay(200, TestContext.Current.CancellationToken);

            lcmConfig.ConfigurationModeInterval = TimeSpan.FromMilliseconds(100);

            await Task.Delay(300, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

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
            var pullClient = new PullServerClient(httpClient, Mock.Of<IHttpClientFactory>(), optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

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
            await Task.Delay(1000, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

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
            var pullClient = new PullServerClient(httpClient, Mock.Of<IHttpClientFactory>(), optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

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
            await Task.Delay(1000, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

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
            var pullClient = new PullServerClient(httpClient, Mock.Of<IHttpClientFactory>(), optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

            var configMock = new Mock<IConfiguration>();
            configMock.SetupGet(c => c["Logging:LogLevel:OpenDsc.Lcm"]).Returns("Information");

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());

            var worker = new TestLcmWorker(configMock.Object, optionsMonitor, dscExecutorMock.Object,
                pullClient, certManagerMock.Object, Mock.Of<ILogger<LcmWorker>>(), extractDir);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await worker.StartAsync(cts.Token);
            await Task.Delay(700, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task ComputeLocalContentHash_NonExistentDirectory_ReturnsEmpty()
    {
        var worker = CreateTestWorker(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var result = await worker.CallComputeLocalContentHashAsync(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), TestContext.Current.CancellationToken);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ComputeLocalContentHash_EmptyDirectory_ReturnsEmpty()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"lcm-hash-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        try
        {
            var worker = CreateTestWorker(dir);
            var result = await worker.CallComputeLocalContentHashAsync(dir, TestContext.Current.CancellationToken);
            result.Should().BeEmpty();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task ComputeLocalContentHash_SingleFile_IsStable()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"lcm-hash-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "main.dsc.yaml"), "# config", TestContext.Current.CancellationToken);
            var worker = CreateTestWorker(dir);
            var hash1 = await worker.CallComputeLocalContentHashAsync(dir, TestContext.Current.CancellationToken);
            var hash2 = await worker.CallComputeLocalContentHashAsync(dir, TestContext.Current.CancellationToken);
            hash1.Should().NotBeEmpty();
            hash1.Should().Be(hash2);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task ComputeLocalContentHash_FileModified_ProducesDifferentHash()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"lcm-hash-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "main.dsc.yaml");
            await File.WriteAllTextAsync(file, "# original", TestContext.Current.CancellationToken);
            var worker = CreateTestWorker(dir);
            var hashBefore = await worker.CallComputeLocalContentHashAsync(dir, TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(file, "# modified", TestContext.Current.CancellationToken);
            var hashAfter = await worker.CallComputeLocalContentHashAsync(dir, TestContext.Current.CancellationToken);
            hashBefore.Should().NotBe(hashAfter);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task ComputeLocalContentHash_MultipleFilesInSubdirectories_IncludesAllFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"lcm-hash-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(Path.Combine(dir, "sub"));
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "main.dsc.yaml"), "# main", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "sub", "helper.dsc.yaml"), "# helper", TestContext.Current.CancellationToken);
            var worker = CreateTestWorker(dir);
            var hashBefore = await worker.CallComputeLocalContentHashAsync(dir, TestContext.Current.CancellationToken);

            await File.WriteAllTextAsync(Path.Combine(dir, "sub", "helper.dsc.yaml"), "# helper MODIFIED", TestContext.Current.CancellationToken);
            var hashAfter = await worker.CallComputeLocalContentHashAsync(dir, TestContext.Current.CancellationToken);
            hashBefore.Should().NotBe(hashAfter, "modifying a file in a subdirectory should change the overall hash");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task ComputeLocalContentHash_AddingFile_ProducesDifferentHash()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"lcm-hash-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "main.dsc.yaml"), "# main", TestContext.Current.CancellationToken);
            var worker = CreateTestWorker(dir);
            var hashBefore = await worker.CallComputeLocalContentHashAsync(dir, TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "extra.dsc.yaml"), "# extra", TestContext.Current.CancellationToken);
            var hashAfter = await worker.CallComputeLocalContentHashAsync(dir, TestContext.Current.CancellationToken);
            hashBefore.Should().NotBe(hashAfter, "adding a new file should change the overall hash");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task PullMode_ServerChecksumUnchanged_LocalHashMatches_SkipsDownload()
    {
        var extractDir = Path.Combine(Path.GetTempPath(), $"lcm-pull-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(extractDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(extractDir, "main.dsc.yaml"), "# config", TestContext.Current.CancellationToken);

            var helperWorker = CreateTestWorker(extractDir);
            var localHash = await helperWorker.CallComputeLocalContentHashAsync(extractDir, TestContext.Current.CancellationToken);

            var nodeId = Guid.NewGuid();
            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Monitor,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(200),
                ConfigurationSource = ConfigurationSource.Pull,
                PullServer = new PullServerSettings
                {
                    ServerUrl = "http://test-server",
                    NodeId = nodeId,
                    ConfigurationChecksum = "same-checksum",
                    ConfigurationEntryPoint = "main.dsc.yaml",
                    LocalContentHash = localHash,
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
                        Checksum = "same-checksum",
                        EntryPoint = "main.dsc.yaml"
                    })
                });

            var bundleCallCount = 0;
            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/bundle")),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((_, _) => bundleCallCount++)
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

            var httpClient = new HttpClient(httpHandlerMock.Object) { BaseAddress = new Uri("http://test-server") };
            var certManagerMock = new Mock<ICertificateManager>();
            var pullClient = new PullServerClient(httpClient, Mock.Of<IHttpClientFactory>(), optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Loose, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((new OpenDsc.Schema.DscResult { HadErrors = false }, 0));

            var worker = new TestLcmWorker(Mock.Of<IConfiguration>(c => c["Logging:LogLevel:OpenDsc.Lcm"] == "Information"),
                optionsMonitor, dscExecutorMock.Object, pullClient, certManagerMock.Object,
                Mock.Of<ILogger<LcmWorker>>(), extractDir);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await worker.StartAsync(cts.Token);
            await Task.Delay(500, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

            bundleCallCount.Should().Be(0, "bundle should not be downloaded when server checksum and local hash both match");
        }
        finally { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true); }
    }

    [Fact]
    public async Task PullMode_ServerChecksumUnchanged_NoLocalHash_TriggersDownload()
    {
        var extractDir = Path.Combine(Path.GetTempPath(), $"lcm-pull-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(extractDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(extractDir, "main.dsc.yaml"), "# config", TestContext.Current.CancellationToken);

            var nodeId = Guid.NewGuid();
            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Monitor,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(200),
                ConfigurationSource = ConfigurationSource.Pull,
                PullServer = new PullServerSettings
                {
                    ServerUrl = "http://test-server",
                    NodeId = nodeId,
                    ConfigurationChecksum = "same-checksum",
                    ConfigurationEntryPoint = "main.dsc.yaml",
                    LocalContentHash = null,
                    ReportCompliance = false,
                    CertificateSource = CertificateSource.Managed
                }
            };

            var optionsMonitor = CreateOptionsMonitor(lcmConfig);
            var httpHandlerMock = new Mock<HttpMessageHandler>();

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/checksum")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new ConfigurationChecksumResponse
                    {
                        Checksum = "same-checksum",
                        EntryPoint = "main.dsc.yaml"
                    })
                });

            var bundleCallCount = 0;
            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/bundle")),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((_, _) => bundleCallCount++)
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StreamContent(CreateZipBundle([("main.dsc.yaml", "# config")]))
                });

            var httpClient = new HttpClient(httpHandlerMock.Object) { BaseAddress = new Uri("http://test-server") };
            var certManagerMock = new Mock<ICertificateManager>();
            var pullClient = new PullServerClient(httpClient, Mock.Of<IHttpClientFactory>(), optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Loose, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((new OpenDsc.Schema.DscResult { HadErrors = false }, 0));

            var worker = new TestLcmWorker(Mock.Of<IConfiguration>(c => c["Logging:LogLevel:OpenDsc.Lcm"] == "Information"),
                optionsMonitor, dscExecutorMock.Object, pullClient, certManagerMock.Object,
                Mock.Of<ILogger<LcmWorker>>(), extractDir);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await worker.StartAsync(cts.Token);
            await Task.Delay(500, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

            bundleCallCount.Should().BeGreaterThan(0, "bundle must be downloaded when no local hash is stored");
        }
        finally { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true); }
    }

    [Fact]
    public async Task PullMode_ServerChecksumUnchanged_LocalHashMismatch_TriggersDownload()
    {
        var extractDir = Path.Combine(Path.GetTempPath(), $"lcm-pull-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(extractDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(extractDir, "main.dsc.yaml"), "# TAMPERED", TestContext.Current.CancellationToken);

            var nodeId = Guid.NewGuid();
            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Monitor,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(200),
                ConfigurationSource = ConfigurationSource.Pull,
                PullServer = new PullServerSettings
                {
                    ServerUrl = "http://test-server",
                    NodeId = nodeId,
                    ConfigurationChecksum = "same-checksum",
                    ConfigurationEntryPoint = "main.dsc.yaml",
                    LocalContentHash = "this-hash-does-not-match-current-files",
                    ReportCompliance = false,
                    CertificateSource = CertificateSource.Managed
                }
            };

            var optionsMonitor = CreateOptionsMonitor(lcmConfig);
            var httpHandlerMock = new Mock<HttpMessageHandler>();

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/checksum")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new ConfigurationChecksumResponse
                    {
                        Checksum = "same-checksum",
                        EntryPoint = "main.dsc.yaml"
                    })
                });

            var bundleCallCount = 0;
            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/bundle")),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((_, _) => bundleCallCount++)
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StreamContent(CreateZipBundle([("main.dsc.yaml", "# config")]))
                });

            var httpClient = new HttpClient(httpHandlerMock.Object) { BaseAddress = new Uri("http://test-server") };
            var certManagerMock = new Mock<ICertificateManager>();
            var pullClient = new PullServerClient(httpClient, Mock.Of<IHttpClientFactory>(), optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Loose, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((new OpenDsc.Schema.DscResult { HadErrors = false }, 0));

            var worker = new TestLcmWorker(Mock.Of<IConfiguration>(c => c["Logging:LogLevel:OpenDsc.Lcm"] == "Information"),
                optionsMonitor, dscExecutorMock.Object, pullClient, certManagerMock.Object,
                Mock.Of<ILogger<LcmWorker>>(), extractDir);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await worker.StartAsync(cts.Token);
            await Task.Delay(500, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

            bundleCallCount.Should().BeGreaterThan(0, "bundle must be re-downloaded when local hash does not match stored hash");
        }
        finally { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true); }
    }

    [Fact]
    public async Task PullMode_ServerChecksumChanged_TriggersDownload()
    {
        var extractDir = Path.Combine(Path.GetTempPath(), $"lcm-pull-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(extractDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(extractDir, "main.dsc.yaml"), "# old config", TestContext.Current.CancellationToken);

            var helperWorker = CreateTestWorker(extractDir);
            var localHash = await helperWorker.CallComputeLocalContentHashAsync(extractDir, TestContext.Current.CancellationToken);

            var nodeId = Guid.NewGuid();
            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Monitor,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(200),
                ConfigurationSource = ConfigurationSource.Pull,
                PullServer = new PullServerSettings
                {
                    ServerUrl = "http://test-server",
                    NodeId = nodeId,
                    ConfigurationChecksum = "old-checksum",
                    ConfigurationEntryPoint = "main.dsc.yaml",
                    LocalContentHash = localHash,
                    ReportCompliance = false,
                    CertificateSource = CertificateSource.Managed
                }
            };

            var optionsMonitor = CreateOptionsMonitor(lcmConfig);
            var httpHandlerMock = new Mock<HttpMessageHandler>();

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/checksum")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new ConfigurationChecksumResponse
                    {
                        Checksum = "NEW-checksum",
                        EntryPoint = "main.dsc.yaml"
                    })
                });

            var bundleCallCount = 0;
            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/bundle")),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((_, _) => bundleCallCount++)
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StreamContent(CreateZipBundle([("main.dsc.yaml", "# new config")]))
                });

            var httpClient = new HttpClient(httpHandlerMock.Object) { BaseAddress = new Uri("http://test-server") };
            var certManagerMock = new Mock<ICertificateManager>();
            var pullClient = new PullServerClient(httpClient, Mock.Of<IHttpClientFactory>(), optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Loose, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((new OpenDsc.Schema.DscResult { HadErrors = false }, 0));

            var worker = new TestLcmWorker(Mock.Of<IConfiguration>(c => c["Logging:LogLevel:OpenDsc.Lcm"] == "Information"),
                optionsMonitor, dscExecutorMock.Object, pullClient, certManagerMock.Object,
                Mock.Of<ILogger<LcmWorker>>(), extractDir);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await worker.StartAsync(cts.Token);
            await Task.Delay(500, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

            bundleCallCount.Should().BeGreaterThan(0, "bundle must be downloaded when server checksum has changed");
        }
        finally { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true); }
    }

    [Fact]
    public async Task PullMode_MultiFileBundle_CacheHit_SkipsDownload()
    {
        var extractDir = Path.Combine(Path.GetTempPath(), $"lcm-pull-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(Path.Combine(extractDir, "sub"));
        try
        {
            await File.WriteAllTextAsync(Path.Combine(extractDir, "main.dsc.yaml"), "# main", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(extractDir, "params.yaml"), "parameters: {}", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(extractDir, "sub", "helper.dsc.yaml"), "# helper", TestContext.Current.CancellationToken);

            var helperWorker = CreateTestWorker(extractDir);
            var localHash = await helperWorker.CallComputeLocalContentHashAsync(extractDir, TestContext.Current.CancellationToken);

            var nodeId = Guid.NewGuid();
            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Monitor,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(200),
                ConfigurationSource = ConfigurationSource.Pull,
                PullServer = new PullServerSettings
                {
                    ServerUrl = "http://test-server",
                    NodeId = nodeId,
                    ConfigurationChecksum = "composite-checksum",
                    ConfigurationEntryPoint = "main.dsc.yaml",
                    LocalContentHash = localHash,
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
                        Checksum = "composite-checksum",
                        EntryPoint = "main.dsc.yaml"
                    })
                });

            var bundleCallCount = 0;
            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/bundle")),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((_, _) => bundleCallCount++)
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

            var httpClient = new HttpClient(httpHandlerMock.Object) { BaseAddress = new Uri("http://test-server") };
            var certManagerMock = new Mock<ICertificateManager>();
            var pullClient = new PullServerClient(httpClient, Mock.Of<IHttpClientFactory>(), optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Loose, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((new OpenDsc.Schema.DscResult { HadErrors = false }, 0));

            var worker = new TestLcmWorker(Mock.Of<IConfiguration>(c => c["Logging:LogLevel:OpenDsc.Lcm"] == "Information"),
                optionsMonitor, dscExecutorMock.Object, pullClient, certManagerMock.Object,
                Mock.Of<ILogger<LcmWorker>>(), extractDir);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await worker.StartAsync(cts.Token);
            await Task.Delay(500, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

            bundleCallCount.Should().Be(0, "multi-file bundle should not be re-downloaded when all file checksums match");
        }
        finally { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true); }
    }

    [Fact]
    public async Task PullMode_MultiFileBundle_SubdirectoryFileModified_TriggersDownload()
    {
        var extractDir = Path.Combine(Path.GetTempPath(), $"lcm-pull-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(Path.Combine(extractDir, "sub"));
        try
        {
            await File.WriteAllTextAsync(Path.Combine(extractDir, "main.dsc.yaml"), "# main", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(extractDir, "sub", "helper.dsc.yaml"), "# original helper", TestContext.Current.CancellationToken);

            var helperWorker = CreateTestWorker(extractDir);
            var localHash = await helperWorker.CallComputeLocalContentHashAsync(extractDir, TestContext.Current.CancellationToken);

            await File.WriteAllTextAsync(Path.Combine(extractDir, "sub", "helper.dsc.yaml"), "# TAMPERED helper", TestContext.Current.CancellationToken);

            var nodeId = Guid.NewGuid();
            var lcmConfig = new LcmConfig
            {
                ConfigurationMode = ConfigurationMode.Monitor,
                ConfigurationModeInterval = TimeSpan.FromMilliseconds(200),
                ConfigurationSource = ConfigurationSource.Pull,
                PullServer = new PullServerSettings
                {
                    ServerUrl = "http://test-server",
                    NodeId = nodeId,
                    ConfigurationChecksum = "composite-checksum",
                    ConfigurationEntryPoint = "main.dsc.yaml",
                    LocalContentHash = localHash,
                    ReportCompliance = false,
                    CertificateSource = CertificateSource.Managed
                }
            };

            var optionsMonitor = CreateOptionsMonitor(lcmConfig);
            var httpHandlerMock = new Mock<HttpMessageHandler>();

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/checksum")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new ConfigurationChecksumResponse
                    {
                        Checksum = "composite-checksum",
                        EntryPoint = "main.dsc.yaml"
                    })
                });

            var bundleCallCount = 0;
            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/bundle")),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((_, _) => bundleCallCount++)
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StreamContent(CreateZipBundle([
                        ("main.dsc.yaml", "# main"),
                        ("sub/helper.dsc.yaml", "# original helper")
                    ]))
                });

            var httpClient = new HttpClient(httpHandlerMock.Object) { BaseAddress = new Uri("http://test-server") };
            var certManagerMock = new Mock<ICertificateManager>();
            var pullClient = new PullServerClient(httpClient, Mock.Of<IHttpClientFactory>(), optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Loose, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((new OpenDsc.Schema.DscResult { HadErrors = false }, 0));

            var worker = new TestLcmWorker(Mock.Of<IConfiguration>(c => c["Logging:LogLevel:OpenDsc.Lcm"] == "Information"),
                optionsMonitor, dscExecutorMock.Object, pullClient, certManagerMock.Object,
                Mock.Of<ILogger<LcmWorker>>(), extractDir);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await worker.StartAsync(cts.Token);
            await Task.Delay(500, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

            bundleCallCount.Should().BeGreaterThan(0, "modifying a file in a subdirectory should trigger re-download");
        }
        finally { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true); }
    }

    [Fact]
    public async Task PullMode_PersistsChecksumAndLocalHashAfterBundleExtraction()
    {
        var extractDir = Path.Combine(Path.GetTempPath(), $"lcm-pull-test-{Guid.NewGuid()}");
        var configFile = Path.Combine(Path.GetTempPath(), $"lcm-config-{Guid.NewGuid()}.json");
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
                    NodeId = nodeId,
                    ConfigurationChecksum = null,
                    ReportCompliance = false,
                    CertificateSource = CertificateSource.Managed
                }
            };

            var optionsMonitor = CreateOptionsMonitor(lcmConfig);
            var httpHandlerMock = new Mock<HttpMessageHandler>();

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

            httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("/configuration/checksum")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new ConfigurationChecksumResponse
                    {
                        Checksum = "server-checksum-abc",
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
                    Content = new StreamContent(CreateZipBundle([("main.dsc.yaml", "# config")]))
                });

            var httpClient = new HttpClient(httpHandlerMock.Object) { BaseAddress = new Uri("http://test-server") };
            var certManagerMock = new Mock<ICertificateManager>();
            var pullClient = new PullServerClient(httpClient, Mock.Of<IHttpClientFactory>(), optionsMonitor, certManagerMock.Object, Mock.Of<ILogger<PullServerClient>>());

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Loose, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            dscExecutorMock.Protected()
                .Setup<Task<(OpenDsc.Schema.DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<LcmConfig>(),
                    ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((new OpenDsc.Schema.DscResult { HadErrors = false }, 0));

            var worker = new TestLcmWorker(Mock.Of<IConfiguration>(c => c["Logging:LogLevel:OpenDsc.Lcm"] == "Information"),
                optionsMonitor, dscExecutorMock.Object, pullClient, certManagerMock.Object,
                Mock.Of<ILogger<LcmWorker>>(), extractDir, configFile);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await worker.StartAsync(cts.Token);
            await Task.Delay(500, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

            File.Exists(configFile).Should().BeTrue("config file should be written after extraction");
            var json = await File.ReadAllTextAsync(configFile, TestContext.Current.CancellationToken);
            json.Should().Contain("server-checksum-abc", "checksum should be persisted");
            json.Should().Contain("LocalContentHash", "local content hash should be persisted");
            json.Should().Contain("ConfigurationEntryPoint", "entry point should be persisted");
        }
        finally
        {
            if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true);
            if (File.Exists(configFile)) File.Delete(configFile);
        }
    }

    private static TestLcmWorker CreateTestWorker(string extractDir, string? configFile = null)
    {
        var lcmConfig = new LcmConfig();
        var optionsMonitor = CreateOptionsMonitor(lcmConfig);
        return new TestLcmWorker(
            Mock.Of<IConfiguration>(),
            optionsMonitor,
            new Mock<DscExecutor>(Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>()).Object,
            null!,
            Mock.Of<ICertificateManager>(),
            Mock.Of<ILogger<LcmWorker>>(),
            extractDir,
            configFile);
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
        string extractDir,
        string? configFilePath = null)
        : LcmWorker(configuration, lcmMonitor, dscExecutor, pullServerClient, certificateManager, logger)
    {
        protected override string GetPullExtractDirectory() => extractDir;
        protected override string GetLcmConfigPath() =>
            configFilePath ?? Path.Combine(Path.GetTempPath(), $"lcm-test-config-{Guid.NewGuid()}.json");

        public Task<string> CallComputeLocalContentHashAsync(string dir, CancellationToken ct) =>
            ComputeLocalContentHashAsync(dir, ct);
    }
}
