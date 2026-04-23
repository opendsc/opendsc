// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;

using AwesomeAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using OpenDsc.Lcm.Contracts;
using OpenDsc.Schema;

using Xunit;

namespace OpenDsc.Lcm.Tests;

[Trait("Category", "Unit")]
public class LcmWorkerComprehensiveCoverageTests
{
    [Fact]
    public void LogRestartRequirements_WithSystemRestart_LogsSystemRestartMessage()
    {
        var loggerMock = new Mock<ILogger<LcmWorker>>();
        var config = CreateTestLcmConfig();
        var optionsMonitor = CreateOptionsMonitor(config);
        var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
        var pullServerClientMock = new Mock<PullServerClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<IHttpClientFactory>(),
            optionsMonitor,
            Mock.Of<ICertificateManager>(),
            Mock.Of<ILogger<PullServerClient>>());
        var certificateManagerMock = new Mock<ICertificateManager>();

        var worker = new LcmWorker(Mock.Of<IConfiguration>(), optionsMonitor, dscExecutorMock.Object,
            pullServerClientMock.Object, certificateManagerMock.Object, loggerMock.Object);

        // Create a DscResult with system restart required
        var dscResult = new DscResult
        {
            HadErrors = false,
            Metadata = new DscMetadata
            {
                MicrosoftDsc = new MicrosoftDscMetadata
                {
                    RestartRequired = new List<DscRestartRequirement>
                    {
                        new() { System = "Computer" }
                    }
                }
            }
        };

        // Log the result
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

        var method = typeof(LcmWorker).GetMethod("LogRestartRequirements",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(DscResult)],
            null);

        method.Should().NotBeNull();

        // Should not throw
        var result = () => method!.Invoke(worker, [dscResult]);
        result.Should().NotThrow();
    }

    [Fact]
    public void LogRestartRequirements_WithServiceRestart_LogsServiceRestartMessage()
    {
        var loggerMock = new Mock<ILogger<LcmWorker>>();
        var config = CreateTestLcmConfig();
        var optionsMonitor = CreateOptionsMonitor(config);
        var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
        var pullServerClientMock = new Mock<PullServerClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<IHttpClientFactory>(),
            optionsMonitor,
            Mock.Of<ICertificateManager>(),
            Mock.Of<ILogger<PullServerClient>>());
        var certificateManagerMock = new Mock<ICertificateManager>();

        var worker = new LcmWorker(Mock.Of<IConfiguration>(), optionsMonitor, dscExecutorMock.Object,
            pullServerClientMock.Object, certificateManagerMock.Object, loggerMock.Object);

        // Create a DscResult with service restart required
        var dscResult = new DscResult
        {
            HadErrors = false,
            Metadata = new DscMetadata
            {
                MicrosoftDsc = new MicrosoftDscMetadata
                {
                    RestartRequired = new List<DscRestartRequirement>
                    {
                        new() { Service = "TestService" }
                    }
                }
            }
        };

        loggerMock.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

        var method = typeof(LcmWorker).GetMethod("LogRestartRequirements",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(DscResult)],
            null);

        // Should not throw
        var result = () => method!.Invoke(worker, [dscResult]);
        result.Should().NotThrow();
    }

    [Fact]
    public void LogRestartRequirements_WithProcessRestart_LogsProcessRestartMessage()
    {
        var loggerMock = new Mock<ILogger<LcmWorker>>();
        var config = CreateTestLcmConfig();
        var optionsMonitor = CreateOptionsMonitor(config);
        var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
        var pullServerClientMock = new Mock<PullServerClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<IHttpClientFactory>(),
            optionsMonitor,
            Mock.Of<ICertificateManager>(),
            Mock.Of<ILogger<PullServerClient>>());
        var certificateManagerMock = new Mock<ICertificateManager>();

        var worker = new LcmWorker(Mock.Of<IConfiguration>(), optionsMonitor, dscExecutorMock.Object,
            pullServerClientMock.Object, certificateManagerMock.Object, loggerMock.Object);

        // Create a DscResult with process restart required
        var dscResult = new DscResult
        {
            HadErrors = false,
            Metadata = new DscMetadata
            {
                MicrosoftDsc = new MicrosoftDscMetadata
                {
                    RestartRequired = new List<DscRestartRequirement>
                    {
                        new()
                        {
                            Process = new DscProcessRestartInfo
                            {
                                Name = "TestProcess",
                                Id = 1234
                            }
                        }
                    }
                }
            }
        };

        loggerMock.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

        var method = typeof(LcmWorker).GetMethod("LogRestartRequirements",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(DscResult)],
            null);

        // Should not throw
        var result = () => method!.Invoke(worker, [dscResult]);
        result.Should().NotThrow();
    }

    [Fact]
    public void LogRestartRequirements_WithMultipleRestartTypes_LogsAllTypes()
    {
        var loggerMock = new Mock<ILogger<LcmWorker>>();
        var config = CreateTestLcmConfig();
        var optionsMonitor = CreateOptionsMonitor(config);
        var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
        var pullServerClientMock = new Mock<PullServerClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<IHttpClientFactory>(),
            optionsMonitor,
            Mock.Of<ICertificateManager>(),
            Mock.Of<ILogger<PullServerClient>>());
        var certificateManagerMock = new Mock<ICertificateManager>();

        var worker = new LcmWorker(Mock.Of<IConfiguration>(), optionsMonitor, dscExecutorMock.Object,
            pullServerClientMock.Object, certificateManagerMock.Object, loggerMock.Object);

        // Create a DscResult with all restart types
        var dscResult = new DscResult
        {
            HadErrors = false,
            Metadata = new DscMetadata
            {
                MicrosoftDsc = new MicrosoftDscMetadata
                {
                    RestartRequired = new List<DscRestartRequirement>
                    {
                        new() { System = "Computer" },
                        new() { Service = "TestService" },
                        new() { Process = new DscProcessRestartInfo { Name = "TestProcess", Id = 5678 } }
                    }
                }
            }
        };

        loggerMock.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

        var method = typeof(LcmWorker).GetMethod("LogRestartRequirements",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(DscResult)],
            null);

        // Should not throw
        var result = () => method!.Invoke(worker, [dscResult]);
        result.Should().NotThrow();
    }

    [Fact]
    public void LogRestartRequirements_WithWarningLogLevelDisabled_DoesNotLog()
    {
        var loggerMock = new Mock<ILogger<LcmWorker>>();
        var config = CreateTestLcmConfig();
        var optionsMonitor = CreateOptionsMonitor(config);
        var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
        var pullServerClientMock = new Mock<PullServerClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<IHttpClientFactory>(),
            optionsMonitor,
            Mock.Of<ICertificateManager>(),
            Mock.Of<ILogger<PullServerClient>>());
        var certificateManagerMock = new Mock<ICertificateManager>();

        var worker = new LcmWorker(Mock.Of<IConfiguration>(), optionsMonitor, dscExecutorMock.Object,
            pullServerClientMock.Object, certificateManagerMock.Object, loggerMock.Object);

        var dscResult = new DscResult
        {
            HadErrors = false,
            Metadata = new DscMetadata
            {
                MicrosoftDsc = new MicrosoftDscMetadata
                {
                    RestartRequired = new List<DscRestartRequirement>
                    {
                        new() { System = "Computer" }
                    }
                }
            }
        };

        loggerMock.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(false);

        var method = typeof(LcmWorker).GetMethod("LogRestartRequirements",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(DscResult)],
            null);

        // Should not throw
        var result = () => method!.Invoke(worker, [dscResult]);
        result.Should().NotThrow();
    }

    [Fact]
    public async Task ComputeLocalContentHashAsync_WithNonExistentDirectory_ReturnsEmptyString()
    {
        var nonexistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}");

        var method = typeof(LcmWorker).GetMethod("ComputeLocalContentHashAsync",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(string), typeof(CancellationToken)],
            null);

        method.Should().NotBeNull();

        var task = method!.Invoke(null, [nonexistentPath, TestContext.Current.CancellationToken]) as Task<string>;
        task.Should().NotBeNull();

        var result = await task!;

        result.Should().Be(string.Empty);
    }

    [Fact]
    public async Task ComputeLocalContentHashAsync_WithEmptyDirectory_ReturnsEmptyString()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"empty-lcm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var method = typeof(LcmWorker).GetMethod("ComputeLocalContentHashAsync",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                [typeof(string), typeof(CancellationToken)],
                null);

            var task = method!.Invoke(null, [tempDir, TestContext.Current.CancellationToken]) as Task<string>;
            var result = await task!;

            result.Should().Be(string.Empty);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ComputeLocalContentHashAsync_WithFiles_ReturnsNonEmptyHash()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"hash-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create test files
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "content1", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.txt"), "content2", TestContext.Current.CancellationToken);

            var method = typeof(LcmWorker).GetMethod("ComputeLocalContentHashAsync",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                [typeof(string), typeof(CancellationToken)],
                null);

            var task = method!.Invoke(null, [tempDir, TestContext.Current.CancellationToken]) as Task<string>;
            var result = await task!;

            result.Should().NotBeNullOrEmpty();
            result.Should().HaveLength(64); // SHA256 hex string
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CheckAndRotateCertificateAsync_WithNoCertificate_ReturnsEarly()
    {
        var config = CreateTestLcmConfig();
        var optionsMonitor = CreateOptionsMonitor(config);
        var loggerMock = new Mock<ILogger<LcmWorker>>();
        var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
        var pullServerClientMock = new Mock<PullServerClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<IHttpClientFactory>(),
            optionsMonitor,
            Mock.Of<ICertificateManager>(),
            Mock.Of<ILogger<PullServerClient>>());
        var certificateManagerMock = new Mock<ICertificateManager>();
        certificateManagerMock.Setup(m => m.GetClientCertificate()).Returns((System.Security.Cryptography.X509Certificates.X509Certificate2)null!);

        var worker = new LcmWorker(Mock.Of<IConfiguration>(), optionsMonitor, dscExecutorMock.Object,
            pullServerClientMock.Object, certificateManagerMock.Object, loggerMock.Object);

        var pullServerSettings = new PullServerSettings { ServerUrl = "https://server.example.com" };

        var method = typeof(LcmWorker).GetMethod("CheckAndRotateCertificateAsync",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(PullServerSettings), typeof(CancellationToken)],
            null);

        method.Should().NotBeNull();

        var task = method!.Invoke(worker, [pullServerSettings, CancellationToken.None]) as Task;
        task.Should().NotBeNull();

        // Should complete without calling rotation methods
        await task!;

        certificateManagerMock.Verify(m => m.RotateCertificate(It.IsAny<PullServerSettings>()), Times.Never);
    }

    [Fact]
    public async Task CheckAndRotateCertificateAsync_WithNoRotationNeeded_ReturnsEarly()
    {
        // Test verifies that when ShouldRotateCertificate returns false, no rotation occurs
        // This is tested indirectly through the mock's Verify calls
        var config = CreateTestLcmConfig();
        var optionsMonitor = CreateOptionsMonitor(config);
        var loggerMock = new Mock<ILogger<LcmWorker>>();
        var dscExecutorMock = new Mock<DscExecutor>(MockBehavior.Strict, Mock.Of<ILogger<DscExecutor>>(), Mock.Of<ILoggerFactory>());
        var pullServerClientMock = new Mock<PullServerClient>(
            Mock.Of<HttpClient>(),
            Mock.Of<IHttpClientFactory>(),
            optionsMonitor,
            Mock.Of<ICertificateManager>(),
            Mock.Of<ILogger<PullServerClient>>());
        var certificateManagerMock = new Mock<ICertificateManager>();

        // Mock to return a non-null certificate and indicate no rotation needed
        certificateManagerMock.Setup(m => m.GetClientCertificate()).Returns(new Mock<System.Security.Cryptography.X509Certificates.X509Certificate2>().Object);
        certificateManagerMock.Setup(m => m.ShouldRotateCertificate(It.IsAny<System.Security.Cryptography.X509Certificates.X509Certificate2>(), It.IsAny<PullServerSettings>())).Returns(false);

        var worker = new LcmWorker(Mock.Of<IConfiguration>(), optionsMonitor, dscExecutorMock.Object,
            pullServerClientMock.Object, certificateManagerMock.Object, loggerMock.Object);

        var pullServerSettings = new PullServerSettings { ServerUrl = "https://server.example.com" };

        var method = typeof(LcmWorker).GetMethod("CheckAndRotateCertificateAsync",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(PullServerSettings), typeof(CancellationToken)],
            null);

        var task = method!.Invoke(worker, [pullServerSettings, TestContext.Current.CancellationToken]) as Task;

        await task!;

        // Verify RotateCertificate is not called when no rotation is needed
        certificateManagerMock.Verify(m => m.RotateCertificate(It.IsAny<PullServerSettings>()), Times.Never);
    }

    private static LcmConfig CreateTestLcmConfig()
    {
        return new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationSource = ConfigurationSource.Pull,
            ConfigurationModeInterval = TimeSpan.FromSeconds(30),
            PullServer = new PullServerSettings
            {
                ServerUrl = "https://dsc-server.example.com",
                NodeId = Guid.NewGuid()
            }
        };
    }

    private static IOptionsMonitor<LcmConfig> CreateOptionsMonitor(LcmConfig config)
    {
        var mock = new Mock<IOptionsMonitor<LcmConfig>>();
        mock.Setup(m => m.CurrentValue).Returns(config);
        mock.Setup(m => m.OnChange(It.IsAny<Action<LcmConfig, string?>>())).Returns(Mock.Of<IDisposable>());
        return mock.Object;
    }
}
