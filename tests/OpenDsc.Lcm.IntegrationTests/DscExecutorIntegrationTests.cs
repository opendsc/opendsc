// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Lcm.IntegrationTests;

[Trait("Category", "Integration")]
public class DscExecutorIntegrationTests
{
    private DscExecutor CreateExecutor()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var executorLogger = loggerFactory.CreateLogger<DscExecutor>();

        return new DscExecutor(executorLogger, loggerFactory);
    }

    private LcmConfig CreateConfig(string? dscPath = null)
    {
        return new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationPath = "test.yaml",
            DscExecutablePath = dscPath
        };
    }

    [Fact]
    public async Task ExecuteTestAsync_WithValidConfiguration_ReturnsResult()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var executor = CreateExecutor();
            var config = CreateConfig();

            var (result, exitCode) = await executor.ExecuteTestAsync(tempConfigPath, config, LogLevel.Information);

            result.Should().NotBeNull();
            exitCode.Should().Be(0);
            result.HadErrors.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteSetAsync_WithTestOperation_ReturnsResult()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var executor = CreateExecutor();
            var config = CreateConfig();

            // Use test operation which always returns JSON
            var (result, exitCode) = await executor.ExecuteTestAsync(tempConfigPath, config, LogLevel.Information);

            result.Should().NotBeNull();
            exitCode.Should().Be(0);
            result.HadErrors.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteTestAsync_WithDebugLogLevel_ExecutesSuccessfully()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var executor = CreateExecutor();
            var config = CreateConfig();

            var (result, exitCode) = await executor.ExecuteTestAsync(tempConfigPath, config, LogLevel.Debug);

            result.Should().NotBeNull();
            exitCode.Should().Be(0);
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteTestAsync_WithTraceLogLevel_ExecutesSuccessfully()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var executor = CreateExecutor();
            var config = CreateConfig();

            var (result, exitCode) = await executor.ExecuteTestAsync(tempConfigPath, config, LogLevel.Trace);

            result.Should().NotBeNull();
            exitCode.Should().Be(0);
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteTestAsync_WithWarningLogLevel_ExecutesSuccessfully()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var executor = CreateExecutor();
            var config = CreateConfig();

            var (result, exitCode) = await executor.ExecuteTestAsync(tempConfigPath, config, LogLevel.Warning);

            result.Should().NotBeNull();
            exitCode.Should().Be(0);
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteTestAsync_WithErrorLogLevel_ExecutesSuccessfully()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var executor = CreateExecutor();
            var config = CreateConfig();

            var (result, exitCode) = await executor.ExecuteTestAsync(tempConfigPath, config, LogLevel.Error);

            result.Should().NotBeNull();
            exitCode.Should().Be(0);
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteTestAsync_WithCriticalLogLevel_ExecutesSuccessfully()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var executor = CreateExecutor();
            var config = CreateConfig();

            var (result, exitCode) = await executor.ExecuteTestAsync(tempConfigPath, config, LogLevel.Critical);

            result.Should().NotBeNull();
            exitCode.Should().Be(0);
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteTestAsync_WithNonExistentResource_ThrowsException()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources:
  - type: NonExistent/Resource
    properties:
      test: value
");

            var executor = CreateExecutor();
            var config = CreateConfig();

            var act = async () => await executor.ExecuteTestAsync(tempConfigPath, config, LogLevel.Information);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*DSC command returned no output*");
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteSetAsync_WithNonExistentResource_ThrowsException()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources:
  - type: NonExistent/Resource
    properties:
      test: value
");

            var executor = CreateExecutor();
            var config = CreateConfig();

            var act = async () => await executor.ExecuteSetAsync(tempConfigPath, config, LogLevel.Information);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*DSC command returned no output*");
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteTestAsync_WithMultipleResources_ProcessesAll()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var executor = CreateExecutor();
            var config = CreateConfig();

            var (result, exitCode) = await executor.ExecuteTestAsync(tempConfigPath, config, LogLevel.Information);

            result.Should().NotBeNull();
            exitCode.Should().Be(0);
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteTestAsync_WithEmptyConfiguration_ReturnsEmptyResult()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var executor = CreateExecutor();
            var config = CreateConfig();

            var (result, exitCode) = await executor.ExecuteTestAsync(tempConfigPath, config, LogLevel.Information);

            result.Should().NotBeNull();
            exitCode.Should().Be(0);
            result.Results.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteTestAsync_WithCancellation_StopsExecution()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var executor = CreateExecutor();
            var config = CreateConfig();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () => await executor.ExecuteTestAsync(tempConfigPath, config, LogLevel.Information, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteSetAsync_WithCancellation_StopsExecution()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var executor = CreateExecutor();
            var config = CreateConfig();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () => await executor.ExecuteSetAsync(tempConfigPath, config, LogLevel.Information, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }
}
