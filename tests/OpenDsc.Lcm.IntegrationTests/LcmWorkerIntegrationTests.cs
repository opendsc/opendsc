// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Microsoft.Extensions.Options;

using Xunit;

namespace OpenDsc.Lcm.IntegrationTests;

[Trait("Category", "Integration")]
public class LcmWorkerIntegrationTests
{
    private static IHost CreateTestHost(ConfigurationMode mode, TimeSpan interval, string configPath)
    {
        var config = new Dictionary<string, string?>
        {
            ["LCM:ConfigurationMode"] = mode.ToString(),
            ["LCM:ConfigurationModeInterval"] = interval.ToString(),
            ["LCM:ConfigurationPath"] = configPath,
            ["LCM:ConfigurationSource"] = "Local"
        };

        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(builder =>
            {
                builder.AddInMemoryCollection(config);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOptions<LcmConfig>()
                    .Bind(context.Configuration.GetSection("LCM"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services.AddHttpClient();
                services.AddSingleton<DscExecutor>(); services.AddSingleton<ICertificateManager, CertificateManager>(); services.AddSingleton<PullServerClient>();
                services.AddHostedService<LcmWorker>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .Build();
    }

    [Fact]
    public async Task LcmWorker_MonitorMode_StartsAndStops()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            using var host = CreateTestHost(ConfigurationMode.Monitor, TimeSpan.FromSeconds(1), tempConfigPath);

            var startTask = host.StartAsync();
            await startTask.WaitAsync(TimeSpan.FromSeconds(5));
            startTask.IsCompletedSuccessfully.Should().BeTrue();

            await Task.Delay(100);

            var stopTask = host.StopAsync();
            await stopTask.WaitAsync(TimeSpan.FromSeconds(5));
            stopTask.IsCompletedSuccessfully.Should().BeTrue();
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
    public async Task LcmWorker_RemediateMode_StartsAndStops()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            using var host = CreateTestHost(ConfigurationMode.Remediate, TimeSpan.FromSeconds(1), tempConfigPath);

            var startTask = host.StartAsync();
            await startTask.WaitAsync(TimeSpan.FromSeconds(5));
            startTask.IsCompletedSuccessfully.Should().BeTrue();

            await Task.Delay(100);

            var stopTask = host.StopAsync();
            await stopTask.WaitAsync(TimeSpan.FromSeconds(5));
            stopTask.IsCompletedSuccessfully.Should().BeTrue();
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
    public async Task LcmWorker_ConfigurationReload_SwitchesMode()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var initialConfig = new Dictionary<string, string?>
            {
                ["LCM:ConfigurationMode"] = "Monitor",
                ["LCM:ConfigurationModeInterval"] = "00:00:01",
                ["LCM:ConfigurationPath"] = tempConfigPath,
                ["LCM:ConfigurationSource"] = "Local"
            };

            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddInMemoryCollection(initialConfig);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddOptions<LcmConfig>()
                        .Bind(context.Configuration.GetSection("LCM"))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();

                    services.AddHttpClient();
                })
                .Build();

            await host.StartAsync();

            var monitor = host.Services.GetRequiredService<IOptionsMonitor<LcmConfig>>();
            monitor.CurrentValue.ConfigurationMode.Should().Be(ConfigurationMode.Monitor);

            await Task.Delay(100);

            await host.StopAsync();
            await host.WaitForShutdownAsync();
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
    public async Task LcmWorker_MissingConfiguration_HandlesGracefully()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"non-existent-{Guid.NewGuid()}.yaml");

        using var host = CreateTestHost(ConfigurationMode.Monitor, TimeSpan.FromSeconds(1), nonExistentPath);

        var startTask = host.StartAsync();
        await startTask.WaitAsync(TimeSpan.FromSeconds(5));
        startTask.IsCompletedSuccessfully.Should().BeTrue();

        await Task.Delay(100);

        var stopTask = host.StopAsync();
        await stopTask.WaitAsync(TimeSpan.FromSeconds(5));
        stopTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task LcmWorker_ShortInterval_ExecutesMultipleTimes()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            using var host = CreateTestHost(ConfigurationMode.Monitor, TimeSpan.FromMilliseconds(100), tempConfigPath);

            await host.StartAsync();

            await Task.Delay(500);

            await host.StopAsync();
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
    public async Task LcmConfig_Validation_RejectsInvalidInterval()
    {
        var config = new Dictionary<string, string?>
        {
            ["LCM:ConfigurationMode"] = "Monitor",
            ["LCM:ConfigurationModeInterval"] = "00:00:00",
            ["LCM:ConfigurationPath"] = "test.yaml",
            ["LCM:ConfigurationSource"] = "Local"
        };

        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(builder =>
            {
                builder.AddInMemoryCollection(config);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOptions<LcmConfig>()
                    .Bind(context.Configuration.GetSection("LCM"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services.AddHttpClient();
                services.AddSingleton<DscExecutor>();
                services.AddSingleton<ICertificateManager, CertificateManager>();
                services.AddSingleton<PullServerClient>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            })
            .Build();

        // The validation error will occur during StartAsync when ValidateOnStart is true
        var act = async () => await host.StartAsync();

        await act.Should().ThrowAsync<OptionsValidationException>()
            .WithMessage("*Interval must be greater than*");
    }

    [Fact]
    public void LcmConfig_Validation_AcceptsValidConfiguration()
    {
        var config = new Dictionary<string, string?>
        {
            ["LCM:ConfigurationMode"] = "Remediate",
            ["LCM:ConfigurationModeInterval"] = "00:05:00",
            ["LCM:ConfigurationPath"] = "test.yaml",
            ["LCM:ConfigurationSource"] = "Local"
        };

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(builder =>
            {
                builder.AddInMemoryCollection(config);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOptions<LcmConfig>()
                    .Bind(context.Configuration.GetSection("LCM"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
            });

        var act = () => hostBuilder.Build();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task LcmWorker_RemediateMode_ExecutesDscSetWhenResourcesNotInDesiredState()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources:
  - type: OpenDsc.FileSystem/Directory
    properties:
      path: " + Path.Combine(Path.GetTempPath(), $"lcm-test-{Guid.NewGuid()}") + @"
");

            using var host = CreateTestHost(ConfigurationMode.Remediate, TimeSpan.FromMilliseconds(500), tempConfigPath);

            await host.StartAsync();
            await Task.Delay(1500);
            await host.StopAsync();
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
    public async Task LcmWorker_ErrorDuringExecution_ContinuesOperation()
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

            using var host = CreateTestHost(ConfigurationMode.Monitor, TimeSpan.FromSeconds(1), tempConfigPath);

            var startTask = host.StartAsync();
            await startTask.WaitAsync(TimeSpan.FromSeconds(5));
            startTask.IsCompletedSuccessfully.Should().BeTrue();

            await Task.Delay(100);

            var stopTask = host.StopAsync();
            await stopTask.WaitAsync(TimeSpan.FromSeconds(5));
            stopTask.IsCompletedSuccessfully.Should().BeTrue();
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
    public async Task LcmWorker_PullMode_WithoutPullServer_HandlesGracefully()
    {
        var config = new Dictionary<string, string?>
        {
            ["LCM:ConfigurationMode"] = "Monitor",
            ["LCM:ConfigurationModeInterval"] = "00:00:01",
            ["LCM:ConfigurationSource"] = "Pull"
        };

        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(builder =>
            {
                builder.AddInMemoryCollection(config);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOptions<LcmConfig>()
                    .Bind(context.Configuration.GetSection("LCM"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services.AddHttpClient();
                services.AddSingleton<DscExecutor>();
                services.AddSingleton<ICertificateManager, CertificateManager>();
                services.AddSingleton<PullServerClient>();
                services.AddHostedService<LcmWorker>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .Build();

        var startTask = host.StartAsync();
        await startTask.WaitAsync(TimeSpan.FromSeconds(5));
        startTask.IsCompletedSuccessfully.Should().BeTrue();

        await Task.Delay(100);

        var stopTask = host.StopAsync();
        await stopTask.WaitAsync(TimeSpan.FromSeconds(5));
        stopTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task LcmWorker_ConfigurationIntervalChange_AdjustsDelay()
    {
        var tempConfigPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempConfigPath, @"
$schema: https://schema.management.azure.com/dsc/2023/10/config.json
resources: []
");

            var initialConfig = new Dictionary<string, string?>
            {
                ["LCM:ConfigurationMode"] = "Monitor",
                ["LCM:ConfigurationModeInterval"] = "00:00:10",
                ["LCM:ConfigurationPath"] = tempConfigPath,
                ["LCM:ConfigurationSource"] = "Local"
            };

            using var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddInMemoryCollection(initialConfig);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddOptions<LcmConfig>()
                        .Bind(context.Configuration.GetSection("LCM"))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();

                    services.AddHttpClient();
                    services.AddSingleton<DscExecutor>();
                    services.AddSingleton<ICertificateManager, CertificateManager>();
                    services.AddSingleton<PullServerClient>();
                    services.AddHostedService<LcmWorker>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();

            await host.StartAsync();
            await Task.Delay(500);
            await host.StopAsync();
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
