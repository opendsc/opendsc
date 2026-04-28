// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

#pragma warning disable xUnit1051

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using Moq;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class RetentionBackgroundServiceTests
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<RetentionBackgroundService>> _mockLogger;

    public RetentionBackgroundServiceTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockLogger = new Mock<ILogger<RetentionBackgroundService>>();
    }

    private ServerDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ServerDbContext(options);
    }

    private void SetupServiceScope(
        ServerDbContext db,
        IVersionRetentionService retentionService)
    {
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockServiceProvider
            .Setup(p => p.GetService(typeof(ServerDbContext)))
            .Returns(db);

        mockServiceProvider
            .Setup(p => p.GetService(typeof(IVersionRetentionService)))
            .Returns(retentionService);

        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
        mockScope.Setup(s => s.Dispose());

        _mockScopeFactory
            .Setup(f => f.CreateScope())
            .Returns(mockScope.Object);
    }

    #region ExecuteAsync - Initialization Tests

    [Fact]
    public async Task ExecuteAsync_WithDefaultSettings_UsesDefaultInterval()
    {
        var db = CreateInMemoryDb();
        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomInterval_UsesCustomInterval()
    {
        var db = CreateInMemoryDb();
        var settings = new ServerSettings { RetentionScheduleInterval = TimeSpan.FromMilliseconds(50) };
        db.ServerSettings.Add(settings);
        await db.SaveChangesAsync();

        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        await service.StopAsync(CancellationToken.None);
    }

    #endregion

    #region ExecuteAsync - Retention Disabled Tests

    [Fact]
    public async Task ExecuteAsync_WhenRetentionDisabled_SkipsCleanup()
    {
        var db = CreateInMemoryDb();
        var settings = new ServerSettings
        {
            RetentionEnabled = false,
            RetentionScheduleInterval = TimeSpan.FromMilliseconds(50)
        };
        db.ServerSettings.Add(settings);
        await db.SaveChangesAsync();

        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        await service.StopAsync(CancellationToken.None);
        mockRetentionService.Verify(
            s => s.CleanupConfigurationVersionsAsync(It.IsAny<RetentionPolicy>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoSettingsFound_DoesNotThrow()
    {
        var db = CreateInMemoryDb();
        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(150);
        cts.Cancel();

        await service.StopAsync(CancellationToken.None);
    }

    #endregion

    #region ExecuteAsync - Retention Enabled Tests

    [Fact]
    public async Task ExecuteAsync_WhenRetentionEnabled_CallsAllCleanupMethods()
    {
        var db = CreateInMemoryDb();
        var settings = new ServerSettings
        {
            RetentionEnabled = true,
            RetentionScheduleInterval = TimeSpan.FromMilliseconds(50),
            RetentionKeepVersions = 5,
            RetentionKeepDays = 30,
            RetentionKeepReleaseVersions = true,
            RetentionReportKeepCount = 10,
            RetentionReportKeepDays = 7,
            RetentionStatusEventKeepCount = 100,
            RetentionStatusEventKeepDays = 14
        };
        db.ServerSettings.Add(settings);
        await db.SaveChangesAsync();

        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        await service.StopAsync(CancellationToken.None);

        mockRetentionService.Verify(
            s => s.CleanupConfigurationVersionsAsync(It.IsAny<RetentionPolicy>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        mockRetentionService.Verify(
            s => s.CleanupParameterVersionsAsync(It.IsAny<RetentionPolicy>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        mockRetentionService.Verify(
            s => s.CleanupCompositeConfigurationVersionsAsync(It.IsAny<RetentionPolicy>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        mockRetentionService.Verify(
            s => s.CleanupReportsAsync(It.IsAny<RecordRetentionPolicy>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        mockRetentionService.Verify(
            s => s.CleanupNodeStatusEventsAsync(It.IsAny<RecordRetentionPolicy>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCorrectRetentionPolicy()
    {
        var db = CreateInMemoryDb();
        var settings = new ServerSettings
        {
            RetentionEnabled = true,
            RetentionScheduleInterval = TimeSpan.FromMilliseconds(50),
            RetentionKeepVersions = 5,
            RetentionKeepDays = 30,
            RetentionKeepReleaseVersions = true
        };
        db.ServerSettings.Add(settings);
        await db.SaveChangesAsync();

        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        await service.StopAsync(CancellationToken.None);

        mockRetentionService.Verify(
            s => s.CleanupConfigurationVersionsAsync(
                It.Is<RetentionPolicy>(p =>
                    p.KeepVersions == 5 &&
                    p.KeepDays == 30 &&
                    p.KeepReleaseVersions &&
                    !p.DryRun &&
                    p.IsScheduled),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCorrectRecordRetentionPolicy()
    {
        var db = CreateInMemoryDb();
        var settings = new ServerSettings
        {
            RetentionEnabled = true,
            RetentionScheduleInterval = TimeSpan.FromMilliseconds(50),
            RetentionReportKeepCount = 10,
            RetentionReportKeepDays = 7
        };
        db.ServerSettings.Add(settings);
        await db.SaveChangesAsync();

        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        await service.StopAsync(CancellationToken.None);

        mockRetentionService.Verify(
            s => s.CleanupReportsAsync(
                It.Is<RecordRetentionPolicy>(p =>
                    p.KeepCount == 10 &&
                    p.KeepDays == 7 &&
                    !p.DryRun &&
                    p.IsScheduled),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region ExecuteAsync - Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_RespectsCancellationToken_DuringDelay()
    {
        var db = CreateInMemoryDb();
        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(200);

        cts.Cancel();
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_StopsCleanlyOnCancellation()
    {
        var db = CreateInMemoryDb();
        var settings = new ServerSettings
        {
            RetentionEnabled = true,
            RetentionScheduleInterval = TimeSpan.FromMilliseconds(50)
        };
        db.ServerSettings.Add(settings);
        await db.SaveChangesAsync();

        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(150);

        await service.StopAsync(CancellationToken.None);
    }

    #endregion

    #region ExecuteAsync - Exception Handling Tests

    [Fact]
    public async Task ExecuteAsync_CatchesExceptionInCleanup_ContinuesRunning()
    {
        var db = CreateInMemoryDb();
        var settings = new ServerSettings
        {
            RetentionEnabled = true,
            RetentionScheduleInterval = TimeSpan.FromMilliseconds(50)
        };
        db.ServerSettings.Add(settings);
        await db.SaveChangesAsync();

        var mockRetentionService = new Mock<IVersionRetentionService>();
        mockRetentionService
            .Setup(s => s.CleanupConfigurationVersionsAsync(It.IsAny<RetentionPolicy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCatchOperationCanceledException()
    {
        var db = CreateInMemoryDb();
        var settings = new ServerSettings
        {
            RetentionEnabled = true,
            RetentionScheduleInterval = TimeSpan.FromMilliseconds(50)
        };
        db.ServerSettings.Add(settings);
        await db.SaveChangesAsync();

        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(150);

        // Should complete without throwing
        await service.StopAsync(CancellationToken.None);
    }

    #endregion

    #region ExecuteAsync - Settings Validation Tests

    [Fact]
    public async Task ExecuteAsync_ChecksRetentionEnabledBeforeCleanup()
    {
        var db = CreateInMemoryDb();
        var settings = new ServerSettings
        {
            RetentionEnabled = false,
            RetentionScheduleInterval = TimeSpan.FromMilliseconds(50)
        };
        db.ServerSettings.Add(settings);
        await db.SaveChangesAsync();

        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        await service.StopAsync(CancellationToken.None);

        mockRetentionService.Verify(
            s => s.CleanupConfigurationVersionsAsync(It.IsAny<RetentionPolicy>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_RereadsSettingsAfterWait()
    {
        var db = CreateInMemoryDb();
        var settings = new ServerSettings
        {
            RetentionEnabled = false,
            RetentionScheduleInterval = TimeSpan.FromMilliseconds(50)
        };
        db.ServerSettings.Add(settings);
        await db.SaveChangesAsync();

        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);

        // Simulate enabling retention mid-run by updating settings
        settings.RetentionEnabled = true;
        await db.SaveChangesAsync();

        await Task.Delay(200);
        cts.Cancel();

        await service.StopAsync(CancellationToken.None);
    }

    #endregion

    #region ExecuteAsync - Scope Creation Tests

    [Fact]
    public async Task ExecuteAsync_CreatesNewScopeForEachIteration()
    {
        var db = CreateInMemoryDb();
        var settings = new ServerSettings
        {
            RetentionEnabled = false,
            RetentionScheduleInterval = TimeSpan.FromMilliseconds(50)
        };
        db.ServerSettings.Add(settings);
        await db.SaveChangesAsync();

        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        await service.StopAsync(CancellationToken.None);

        // Should be called at least twice (once for initial settings, once after delay)
        _mockScopeFactory.Verify(f => f.CreateScope(), Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_DisposesScopes()
    {
        var db = CreateInMemoryDb();
        var mockRetentionService = new Mock<IVersionRetentionService>();
        SetupServiceScope(db, mockRetentionService.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var service = new RetentionBackgroundService(_mockScopeFactory.Object, _mockLogger.Object);

        await service.StartAsync(cts.Token);
        await Task.Delay(150);
        cts.Cancel();

        await service.StopAsync(CancellationToken.None);
    }

    #endregion
}
