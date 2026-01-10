// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenDsc.Schema;

namespace OpenDsc.Lcm;

public partial class LcmWorker(IConfiguration configuration, IOptionsMonitor<LcmConfig> lcmMonitor, DscExecutor dscExecutor, ILogger<LcmWorker> logger) : BackgroundService
{
    private static string TimeSpanFormat => "c";
    private ConfigurationMode _currentMode = ConfigurationMode.Monitor;
    private CancellationTokenSource? _modeChangeCts;
    private IDisposable? _configChangeToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _configChangeToken = lcmMonitor.OnChange((config, name) =>
            {
                try
                {
                    OnConfigurationReloaded(config);
                }
                catch (OptionsValidationException ex)
                {
                    foreach (string failure in ex.Failures)
                    {
                        LogConfigurationValidationError(failure);
                    }
                }
            });

            _currentMode = lcmMonitor.CurrentValue.ConfigurationMode;
            LogOperatingInMode(_currentMode);

            while (!stoppingToken.IsCancellationRequested)
            {
                _modeChangeCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                try
                {
                    var config = lcmMonitor.CurrentValue;

                    switch (config.ConfigurationMode)
                    {
                        case ConfigurationMode.Monitor:
                            await ExecuteMonitorModeAsync(config, stoppingToken, _modeChangeCts.Token);
                            break;

                        case ConfigurationMode.Remediate:
                            await ExecuteRemediateModeAsync(config, stoppingToken, _modeChangeCts.Token);
                            break;

                        default:
                            LogUnknownLcmMode(config.ConfigurationMode);
                            break;
                    }
                }
                catch (OperationCanceledException) when (_modeChangeCts.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
                {
                    var newMode = lcmMonitor.CurrentValue.ConfigurationMode;
                    LogModeSwitched(_currentMode, newMode);
                    _currentMode = newMode;
                    continue;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                finally
                {
                    _modeChangeCts?.Dispose();
                    _modeChangeCts = null;
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            LogCriticalErrorInLcmService(ex);
            throw;
        }
        finally
        {
            _configChangeToken?.Dispose();
            _modeChangeCts?.Dispose();
        }

        LogLcmServiceStopping();
    }

    private void OnConfigurationReloaded(LcmConfig newConfig)
    {
        LogConfigurationReloaded();

        if (newConfig.ConfigurationMode != _currentMode)
        {
            LogModeChangeDetected(_currentMode, newConfig.ConfigurationMode);
            _modeChangeCts?.Cancel();
        }
    }

    private async Task ExecuteMonitorModeAsync(LcmConfig config, CancellationToken stoppingToken, CancellationToken modeChangeToken)
    {
#pragma warning disable CA1873
        if (logger.IsEnabled(LogLevel.Information))
        {
            LogStartingMonitorMode(config.ConfigurationModeInterval.ToString(TimeSpanFormat));
        }
#pragma warning restore CA1873

        while (!stoppingToken.IsCancellationRequested && !modeChangeToken.IsCancellationRequested)
        {
            var currentConfig = lcmMonitor.CurrentValue;
            var currentInterval = currentConfig.ConfigurationModeInterval;

            try
            {
                if (string.IsNullOrWhiteSpace(currentConfig.ConfigurationPath) || !File.Exists(currentConfig.ConfigurationPath))
                {
                    LogConfigurationNotAvailableSkippingDscTest();
                }
                else
                {
                    var traceLevel = GetTraceLevelFromConfiguration(configuration);
                    var (result, exitCode) = await dscExecutor.ExecuteTestAsync(currentConfig.ConfigurationPath, currentConfig, traceLevel, stoppingToken);
                    LogDscTestResult(result, exitCode);
                }

                await InterruptibleDelayAsync(currentInterval, currentInterval, stoppingToken, modeChangeToken);
            }
            catch (OperationCanceledException) when (modeChangeToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogErrorDuringMonitorCycle(ex);
                var errorDelay = TimeSpan.FromSeconds(Math.Min(currentInterval.TotalSeconds, 60));
                await InterruptibleDelayAsync(errorDelay, currentInterval, stoppingToken, modeChangeToken);
            }
        }
    }

    private async Task ExecuteRemediateModeAsync(LcmConfig config, CancellationToken stoppingToken, CancellationToken modeChangeToken)
    {
#pragma warning disable CA1873
        if (logger.IsEnabled(LogLevel.Information))
        {
            LogStartingRemediateMode(config.ConfigurationModeInterval.ToString(TimeSpanFormat));
        }
#pragma warning restore CA1873

        while (!stoppingToken.IsCancellationRequested && !modeChangeToken.IsCancellationRequested)
        {
            var currentConfig = lcmMonitor.CurrentValue;
            var currentInterval = currentConfig.ConfigurationModeInterval;
            var configPathBeforeTest = currentConfig.ConfigurationPath;

            try
            {
                if (string.IsNullOrWhiteSpace(currentConfig.ConfigurationPath) || !File.Exists(currentConfig.ConfigurationPath))
                {
                    LogConfigurationNotAvailableSkippingDscOperations(currentConfig.ConfigurationPath!);
                }
                else
                {
                    var traceLevel = GetTraceLevelFromConfiguration(configuration);
                    var (testResult, testExitCode) = await dscExecutor.ExecuteTestAsync(currentConfig.ConfigurationPath, currentConfig, traceLevel, stoppingToken);
                    LogDscTestResult(testResult, testExitCode);

                    var configPathAfterTest = lcmMonitor.CurrentValue.ConfigurationPath;
                    if (configPathBeforeTest != configPathAfterTest || modeChangeToken.IsCancellationRequested)
                    {
                        LogConfigurationChangedSkippingSet();
                        continue;
                    }

                    var needsCorrection = (testResult.Results?.Count(r =>
                    {
                        var testOp = JsonSerializer.Deserialize(r.Result, OpenDsc.Schema.SourceGenerationContext.Default.DscTestOperationResult);
                        return testOp?.InDesiredState == false;
                    }) ?? 0) > 0;

                    if (needsCorrection)
                    {
                        LogResourcesNotInDesiredStateApplyingCorrections();
                        traceLevel = GetTraceLevelFromConfiguration(configuration);
                        var (setResult, setExitCode) = await dscExecutor.ExecuteSetAsync(currentConfig.ConfigurationPath, currentConfig, traceLevel, stoppingToken);
                        LogDscSetResult(setResult, setExitCode);

                        if (setResult.Metadata?.MicrosoftDsc?.RestartRequired?.Count > 0)
                        {
                            LogCorrectionChangesRequireSystemRestart();
                        }
                    }
                    else
                    {
                        LogAllResourcesAreInDesiredState();
                    }
                }

                await InterruptibleDelayAsync(currentInterval, currentInterval, stoppingToken, modeChangeToken);
            }
            catch (OperationCanceledException) when (modeChangeToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogErrorDuringRemediateCycle(ex);
                var errorDelay = TimeSpan.FromSeconds(Math.Min(currentInterval.TotalSeconds, 60));
                await InterruptibleDelayAsync(errorDelay, currentInterval, stoppingToken, modeChangeToken);
            }
        }
    }

    private async Task InterruptibleDelayAsync(TimeSpan delay, TimeSpan originalInterval, CancellationToken stoppingToken, CancellationToken modeChangeToken)
    {
        var elapsed = TimeSpan.Zero;
        var pollInterval = TimeSpan.FromSeconds(1);

        while (elapsed < delay && !stoppingToken.IsCancellationRequested && !modeChangeToken.IsCancellationRequested)
        {
            await Task.Delay(pollInterval, stoppingToken);
            elapsed += pollInterval;

            var currentInterval = lcmMonitor.CurrentValue.ConfigurationModeInterval;
            if (currentInterval != originalInterval)
            {
#pragma warning disable CA1873
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    LogConfigurationIntervalChanged(originalInterval.ToString(TimeSpanFormat), currentInterval.ToString(TimeSpanFormat));
                }
#pragma warning restore CA1873

                break;
            }
        }
    }

    private void LogDscTestResult(DscResult result, int exitCode)
    {
        bool allInDesiredState = result.Results?.All(r =>
        {
            var testOp = JsonSerializer.Deserialize(r.Result, OpenDsc.Schema.SourceGenerationContext.Default.DscTestOperationResult);
            return testOp?.InDesiredState == true;
        }) ?? true;

        if (allInDesiredState)
        {
            LogDscOperationCompletedSuccessfully("Test");
        }
        else
        {
            LogDscOperationCompletedWithIssues("Test", exitCode);
        }

        if (result.Results?.Count > 0)
        {
            var totalResources = result.Results!.Count;
            var inDesiredState = result.Results.Count(r =>
            {
                var testOp = JsonSerializer.Deserialize(r.Result, OpenDsc.Schema.SourceGenerationContext.Default.DscTestOperationResult);
                return testOp?.InDesiredState == true;
            });
            var notInDesiredState = result.Results.Count(r =>
            {
                var testOp = JsonSerializer.Deserialize(r.Result, OpenDsc.Schema.SourceGenerationContext.Default.DscTestOperationResult);
                return testOp?.InDesiredState == false;
            });

            LogResourceStatus(inDesiredState, totalResources, notInDesiredState);

            if (logger.IsEnabled(LogLevel.Warning))
            {
                foreach (var resource in result.Results.Where(r =>
                {
                    var testOp = JsonSerializer.Deserialize(r.Result, OpenDsc.Schema.SourceGenerationContext.Default.DscTestOperationResult);
                    return testOp?.InDesiredState == false;
                }))
                {
                    LogResourceNotInDesiredState(resource.Type, resource.Name);
                }
            }
        }

        LogRestartRequirements(result);
    }

    private void LogDscSetResult(DscResult result, int exitCode)
    {
        if (!result.HadErrors)
        {
            LogDscOperationCompletedSuccessfully("Set");
        }
        else
        {
            LogDscOperationCompletedWithIssues("Set", exitCode);
        }

        if (result.Results?.Count > 0)
        {
            var totalResources = result.Results!.Count;
            var successfulResources = !result.HadErrors ? totalResources : 0;
            var failedResources = result.HadErrors ? totalResources : 0;

            LogSetResourceStatus(successfulResources, totalResources, failedResources);
        }

        LogRestartRequirements(result);
    }

    private void LogRestartRequirements(DscResult result)
    {
#pragma warning disable CA1873
        if (logger.IsEnabled(LogLevel.Warning) && result.Metadata?.MicrosoftDsc?.RestartRequired?.Count > 0)
        {
            foreach (var req in result.Metadata.MicrosoftDsc.RestartRequired!)
            {
                if (req.System is not null)
                {
                    LogDscOperationRequiresSystemRestart(req.System);
                }
                else if (req.Service is not null)
                {
                    LogDscOperationRequiresServiceRestart(req.Service);
                }
                else if (req.Process is not null && req.Process.Name is not null && req.Process.Id.HasValue)
                {
                    LogDscOperationRequiresProcessRestart(req.Process.Name, req.Process.Id.Value);
                }
            }
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Resource status: {InDesired}/{Total} in desired state, {NotInDesired} not in desired state")]
    private partial void LogResourceStatus(int inDesired, int total, int notInDesired);

    [LoggerMessage(Level = LogLevel.Information, Message = "Set operation status: {Successful}/{Total} resources successfully applied, {Failed} failed")]
    private partial void LogSetResourceStatus(int successful, int total, int failed);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Resource instance '{Name}' with type '{Type}' is not in desired state")]
    private partial void LogResourceNotInDesiredState(string? type, string? name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DSC operation requires system restart: {ComputerName}")]
    private partial void LogDscOperationRequiresSystemRestart(string computerName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DSC operation requires service restart: {ServiceName}")]
    private partial void LogDscOperationRequiresServiceRestart(string serviceName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DSC operation requires process restart: {ProcessName} (ID: {ProcessId})")]
    private partial void LogDscOperationRequiresProcessRestart(string processName, uint processId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Configuration reloaded, changes will take effect on next cycle")]
    private partial void LogConfigurationReloaded();

    [LoggerMessage(Level = LogLevel.Error, Message = "Configuration validation error: {ErrorMessage}")]
    private partial void LogConfigurationValidationError(string errorMessage);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Mode change detected from {OldMode} to {NewMode}, cancelling current mode")]
    private partial void LogModeChangeDetected(ConfigurationMode oldMode, ConfigurationMode newMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Mode switched from {OldMode} to {NewMode}")]
    private partial void LogModeSwitched(ConfigurationMode oldMode, ConfigurationMode newMode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Configuration interval changed from {OldInterval} to {NewInterval}, interrupting delay")]
    private partial void LogConfigurationIntervalChanged(string oldInterval, string newInterval);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Configuration changed after test, skipping set operation")]
    private partial void LogConfigurationChangedSkippingSet();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Trace level for DSC operations: {TraceLevel}")]
    private partial void LogTraceLevelForDsc(LogLevel traceLevel);
}
