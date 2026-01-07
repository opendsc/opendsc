// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace OpenDsc.Lcm;

public partial class LcmWorker(IConfiguration configuration, IOptions<LcmConfig> lcmOptions, DscExecutor dscExecutor, ILogger<LcmWorker> logger) : BackgroundService
{
    private static string TimeSpanFormat => "c";
    private LcmConfig _currentConfig = new();
    private IDisposable? _changeToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            LoadConfiguration();
            LogLevel traceLevel = GetTraceLevelFromConfiguration(configuration);

            _changeToken = ChangeToken.OnChange(
                configuration.GetReloadToken,
                OnConfigurationReloaded);

            LogOperatingInMode(_currentConfig.ConfigurationMode);

            switch (_currentConfig.ConfigurationMode)
            {
                case ConfigurationMode.Monitor:
                    await ExecuteMonitorModeAsync(_currentConfig, traceLevel, stoppingToken);
                    break;

                case ConfigurationMode.Remediate:
                    await ExecuteRemediateModeAsync(_currentConfig, traceLevel, stoppingToken);
                    break;

                default:
                    LogUnknownLcmMode(_currentConfig.ConfigurationMode);
                    break;
            }
        }
        catch (Exception ex)
        {
            LogCriticalErrorInLcmService(ex);
            throw;
        }
        finally
        {
            _changeToken?.Dispose();
        }

        LogLcmServiceStopping();
    }

    private void LoadConfiguration()
    {
        _currentConfig = lcmOptions.Value;
    }

    private void OnConfigurationReloaded()
    {
        LogConfigurationReloaded();
        LoadConfiguration();
        // Note: Mode changes require service restart for simplicity
        // Dynamic mode switching could be implemented if needed
    }

    private async Task ExecuteMonitorModeAsync(LcmConfig config, LogLevel traceLevel, CancellationToken stoppingToken)
    {
#pragma warning disable CA1873
        if (logger.IsEnabled(LogLevel.Information))
        {
            LogStartingMonitorMode(config.ConfigurationModeInterval.ToString(TimeSpanFormat));
        }
#pragma warning restore CA1873

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(config.FullConfigurationPath) || !File.Exists(config.FullConfigurationPath))
                {
                    LogConfigurationNotAvailableSkippingDscTest();
                }
                else
                {
                    var result = await dscExecutor.ExecuteTestAsync(config.FullConfigurationPath, config, traceLevel, stoppingToken);
                    LogDscResult("Test", result);
                }

                await Task.Delay(config.ConfigurationModeInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                LogErrorDuringMonitorCycle(ex);
                // Continue monitoring despite errors
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(config.ConfigurationModeInterval.TotalSeconds, 60)), stoppingToken);
            }
        }
    }

    private async Task ExecuteRemediateModeAsync(LcmConfig config, LogLevel traceLevel, CancellationToken stoppingToken)
    {
#pragma warning disable CA1873
        if (logger.IsEnabled(LogLevel.Information))
        {
            LogStartingRemediateMode(config.ConfigurationModeInterval.ToString(TimeSpanFormat));
        }
#pragma warning restore CA1873

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(config.FullConfigurationPath) || !File.Exists(config.FullConfigurationPath))
                {
                    LogConfigurationNotAvailableSkippingDscOperations(config.FullConfigurationPath!);
                }
                else
                {
                    var testResult = await dscExecutor.ExecuteTestAsync(config.FullConfigurationPath, config, traceLevel, stoppingToken);
                    LogDscResult("Test", testResult);

                    var needsCorrection = (testResult.Resources?.Count(r => r.Result?.InDesiredState == false) ?? 0) > 0;

                    if (needsCorrection)
                    {
                        LogResourcesNotInDesiredStateApplyingCorrections();
                        var setResult = await dscExecutor.ExecuteSetAsync(config.FullConfigurationPath, config, traceLevel, stoppingToken);
                        LogDscResult("Correction Set", setResult);

                        if (setResult.RestartRequired?.Count > 0)
                        {
                            LogCorrectionChangesRequireSystemRestart();
                        }
                    }
                    else
                    {
                        LogAllResourcesAreInDesiredState();
                    }
                }

                await Task.Delay(config.ConfigurationModeInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogErrorDuringRemediateCycle(ex);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(config.ConfigurationModeInterval.TotalSeconds, 60)), stoppingToken);
            }
        }
    }

    private void LogDscResult(string operation, DscResult result)
    {
        bool isOperationSuccessful = operation.Contains("Test", StringComparison.OrdinalIgnoreCase)
            ? result.AllResourcesInDesiredState
            : !result.HadErrors;

        if (isOperationSuccessful)
        {
            LogDscOperationCompletedSuccessfully(operation);
        }
        else
        {
            LogDscOperationCompletedWithIssues(operation, result.ExitCode);
        }

        if (result.Resources?.Count > 0)
        {
            var totalResources = result.Resources!.Count;
            int inDesiredState, notInDesiredState, unknownState;

            if (operation.Contains("set", StringComparison.OrdinalIgnoreCase) && !result.HadErrors)
            {
                inDesiredState = result.Resources.Count(r => r.Result?.InDesiredState == true || r.Result?.InDesiredState == null);
                notInDesiredState = result.Resources.Count(r => r.Result?.InDesiredState == false);
                unknownState = 0;
            }
            else
            {
                inDesiredState = result.Resources.Count(r => r.Result?.InDesiredState == true);
                notInDesiredState = result.Resources.Count(r => r.Result?.InDesiredState == false);
                unknownState = result.Resources.Count(r => r.Result?.InDesiredState == null);
            }

            LogResourceStatus(inDesiredState, totalResources, notInDesiredState, unknownState);

            if (logger.IsEnabled(LogLevel.Warning))
            {
                foreach (var resource in result.Resources.Where(r => r.Result?.InDesiredState == false))
                {
                    LogResourceNotInDesiredState(resource.Type, resource.Name);
                }
            }
        }

#pragma warning disable CA1873
        if (logger.IsEnabled(LogLevel.Warning) && result.RestartRequired?.Count > 0)
        {
            LogDscOperationRequiresRestart(string.Join(", ", result.RestartRequired!.Select(r => r.Type ?? "unknown")));
        }
#pragma warning restore CA1873
    }

    private static LogLevel GetTraceLevelFromConfiguration(IConfiguration configuration)
    {
        string logLevelString = configuration["Logging:LogLevel:OpenDsc.Lcm"] ?? configuration["Logging:LogLevel:Default"] ?? "Information";
        return Enum.Parse<LogLevel>(logLevelString);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "LCM operating in {ConfigurationMode} mode")]
    private partial void LogOperatingInMode(ConfigurationMode configurationMode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unknown LCM mode: {ConfigurationMode}")]
    private partial void LogUnknownLcmMode(ConfigurationMode configurationMode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Critical error in LCM service")]
    private partial void LogCriticalErrorInLcmService(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "LCM service stopping")]
    private partial void LogLcmServiceStopping();

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting monitor mode with {ConfigurationModeInterval} intervals")]
    private partial void LogStartingMonitorMode(string configurationModeInterval);

    [LoggerMessage(Level = LogLevel.Error, Message = "Configuration not available, skipping DSC test")]
    private partial void LogConfigurationNotAvailableSkippingDscTest();

    [LoggerMessage(Level = LogLevel.Error, Message = "Error during monitor cycle")]
    private partial void LogErrorDuringMonitorCycle(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting remediate mode with {ConfigurationModeInterval} intervals")]
    private partial void LogStartingRemediateMode(string configurationModeInterval);

    [LoggerMessage(Level = LogLevel.Error, Message = "Configuration not available, skipping DSC operations: {ConfigurationPath}")]
    private partial void LogConfigurationNotAvailableSkippingDscOperations(string configurationPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Resources not in desired state, applying corrections")]
    private partial void LogResourcesNotInDesiredStateApplyingCorrections();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Correction changes require system restart")]
    private partial void LogCorrectionChangesRequireSystemRestart();

    [LoggerMessage(Level = LogLevel.Debug, Message = "All resources are in desired state")]
    private partial void LogAllResourcesAreInDesiredState();

    [LoggerMessage(Level = LogLevel.Error, Message = "Error during remediate cycle")]
    private partial void LogErrorDuringRemediateCycle(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "DSC {Operation} completed successfully")]
    private partial void LogDscOperationCompletedSuccessfully(string operation);

    [LoggerMessage(Level = LogLevel.Information, Message = "DSC {Operation} completed with issues. Exit code: {ExitCode}")]
    private partial void LogDscOperationCompletedWithIssues(string operation, int exitCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Resource status: {InDesired}/{Total} in desired state, {NotInDesired} not in desired state, {Unknown} unknown")]
    private partial void LogResourceStatus(int inDesired, int total, int notInDesired, int unknown);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Resource instance '{Name}' with type '{Type}' is not in desired state")]
    private partial void LogResourceNotInDesiredState(string? type, string? name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DSC operation requires restart: {Requirements}")]
    private partial void LogDscOperationRequiresRestart(string requirements);

    [LoggerMessage(Level = LogLevel.Information, Message = "Configuration reloaded, changes will take effect on next cycle")]
    private partial void LogConfigurationReloaded();
}
