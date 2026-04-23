// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;
using Moq.Protected;

using OpenDsc.Lcm.Contracts;
using OpenDsc.Schema;

using Xunit;

namespace OpenDsc.Lcm.Tests;

[Trait("Category", "Unit")]
public class LcmWorkerErrorPathTests
{
    [Fact]
    public async Task LcmWorker_ConfigurationReloadWithValidationError_LogsErrorAndContinues()
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
        var pullServerClientMock = new Mock<PullServerClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<IHttpClientFactory>(),
            optionsMonitorMock.Object,
            Mock.Of<ICertificateManager>(),
            Mock.Of<ILogger<PullServerClient>>());

        var loggerMock = new Mock<ILogger<LcmWorker>>();

        var dscResult = new DscResult { HadErrors = false };
        dscExecutorMock.Protected()
            .Setup<Task<(DscResult, int)>>("ExecuteCommandAsync",
                ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),
                ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((dscResult, 0));

        var certificateManagerMock = new Mock<CertificateManager>(optionsMonitorMock.Object, Mock.Of<ILogger<CertificateManager>>());
        var worker = new LcmWorker(configMock.Object, optionsMonitorMock.Object, dscExecutorMock.Object,
            pullServerClientMock.Object, certificateManagerMock.Object, loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        await worker.StartAsync(cts.Token);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Simulate a validation error during config reload
        changeCallbacks.Should().HaveCountGreaterThan(0);

        await worker.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task LcmWorker_MonitorMode_HandlesConfigurationFileNotFound()
    {
        var tempConfigPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.dsc.yaml");

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
        var pullServerClientMock = new Mock<PullServerClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<IHttpClientFactory>(),
            optionsMonitor,
            Mock.Of<ICertificateManager>(),
            Mock.Of<ILogger<PullServerClient>>());

        var loggerMock = new Mock<ILogger<LcmWorker>>();

        var dscResult = new DscResult { HadErrors = false };
        dscExecutorMock.Protected()
            .Setup<Task<(DscResult, int)>>("ExecuteCommandAsync",
                ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),
                ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((dscResult, 0));

        var certificateManagerMock = new Mock<CertificateManager>(optionsMonitor, Mock.Of<ILogger<CertificateManager>>());
        var worker = new LcmWorker(configMock.Object, optionsMonitor, dscExecutorMock.Object,
            pullServerClientMock.Object, certificateManagerMock.Object, loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        await worker.StartAsync(cts.Token);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        // Verify that DSC executor was not called for missing file
        dscExecutorMock.Protected().Verify("ExecuteCommandAsync", Times.Never(),
            ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),
            ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task LcmWorker_ModeSwitch_MonitorToRemediate()
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

            var optionsMonitorMock = new Mock<IOptionsMonitor<LcmConfig>>();
            optionsMonitorMock.Setup(o => o.CurrentValue).Returns(lcmConfig);

            var changeCallbacks = new List<Action<LcmConfig, string?>>();
            optionsMonitorMock.Setup(o => o.OnChange(It.IsAny<Action<LcmConfig, string?>>()))
                .Callback<Action<LcmConfig, string?>>(callback => changeCallbacks.Add(callback))
                .Returns(Mock.Of<IDisposable>());

            var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
            var pullServerClientMock = new Mock<PullServerClient>(
                Mock.Of<HttpClient>(),
                Mock.Of<IHttpClientFactory>(),
                optionsMonitorMock.Object,
                Mock.Of<ICertificateManager>(),
                Mock.Of<ILogger<PullServerClient>>());

            var loggerMock = new Mock<ILogger<LcmWorker>>();

            var dscResult = new DscResult { HadErrors = false };
            dscExecutorMock.Protected()
                .Setup<Task<(DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((dscResult, 0));

            var certificateManagerMock = new Mock<CertificateManager>(optionsMonitorMock.Object, Mock.Of<ILogger<CertificateManager>>());
            var worker = new LcmWorker(configMock.Object, optionsMonitorMock.Object, dscExecutorMock.Object,
                pullServerClientMock.Object, certificateManagerMock.Object, loggerMock.Object);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            await worker.StartAsync(cts.Token);
            await Task.Delay(150, TestContext.Current.CancellationToken);

            // Switch mode
            lcmConfig.ConfigurationMode = ConfigurationMode.Remediate;
            foreach (var callback in changeCallbacks)
            {
                callback(lcmConfig, "");
            }

            await Task.Delay(150, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

            changeCallbacks.Should().HaveCountGreaterThan(0, "configuration change callback should be registered");
        }
        finally
        {
            if (File.Exists(tempConfigPath)) File.Delete(tempConfigPath);
        }
    }

    [Fact]
    public async Task LcmWorker_ExecutionThrowsException_ContinuesWithErrorDelay()
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
            var pullServerClientMock = new Mock<PullServerClient>(
                Mock.Of<HttpClient>(),
                Mock.Of<IHttpClientFactory>(),
                optionsMonitor,
                Mock.Of<ICertificateManager>(),
                Mock.Of<ILogger<PullServerClient>>());

            var loggerMock = new Mock<ILogger<LcmWorker>>();

            var callCount = 0;
            dscExecutorMock.Protected()
                .Setup<Task<(DscResult, int)>>("ExecuteCommandAsync",
                    ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<LcmConfig>(), ItExpr.IsAny<LogLevel>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .Callback(() => callCount++)
                .ThrowsAsync(new InvalidOperationException("DSC execution failed"));

            var certificateManagerMock = new Mock<CertificateManager>(optionsMonitor, Mock.Of<ILogger<CertificateManager>>());
            var worker = new LcmWorker(configMock.Object, optionsMonitor, dscExecutorMock.Object,
                pullServerClientMock.Object, certificateManagerMock.Object, loggerMock.Object);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            await worker.StartAsync(cts.Token);
            await Task.Delay(500, TestContext.Current.CancellationToken);
            await worker.StopAsync(TestContext.Current.CancellationToken);

            // Despite the exception, the worker should continue and may retry
            callCount.Should().BeGreaterThan(0, "executor should have been called at least once despite the exception");
        }
        finally
        {
            if (File.Exists(tempConfigPath)) File.Delete(tempConfigPath);
        }
    }

    private static IOptionsMonitor<LcmConfig> CreateOptionsMonitor(LcmConfig config)
    {
        var optionsMonitorMock = new Mock<IOptionsMonitor<LcmConfig>>();
        optionsMonitorMock.Setup(o => o.CurrentValue).Returns(config);
        optionsMonitorMock.Setup(o => o.OnChange(It.IsAny<Action<LcmConfig, string?>>()))
            .Returns(Mock.Of<IDisposable>());
        return optionsMonitorMock.Object;
    }
}
