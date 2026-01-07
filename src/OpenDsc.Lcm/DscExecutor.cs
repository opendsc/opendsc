// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace OpenDsc.Lcm;

public partial class DscExecutor(ILogger<DscExecutor> logger)
{
    public async Task<DscResult> ExecuteTestAsync(string configPath, LcmConfig config, LogLevel traceLevel, CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandAsync("test", configPath, config, traceLevel, cancellationToken);
    }

    public async Task<DscResult> ExecuteSetAsync(string configPath, LcmConfig config, LogLevel traceLevel, CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandAsync("set", configPath, config, traceLevel, cancellationToken);
    }

    private static string? FindExecutableInPath()
    {
        string executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dsc.exe" : "dsc";
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv == null) return null;
        var paths = pathEnv.Split(Path.PathSeparator);
        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, executableName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }
        return null;
    }

    private async Task<DscResult> ExecuteCommandAsync(string operation, string configPath, LcmConfig config, LogLevel traceLevel, CancellationToken cancellationToken)
    {
        var arguments = BuildArguments(operation, configPath, traceLevel);

#pragma warning disable CA1873
        if (logger.IsEnabled(LogLevel.Information))
        {
            LogExecutingDscCommand(string.Join(" ", arguments));
        }
#pragma warning restore CA1873

        var dscPath = config.DscExecutablePath
                      ?? FindExecutableInPath()
                      ?? throw new InvalidOperationException("DSC executable not found in PATH. Please ensure DSC is installed and available in PATH, or set DscExecutablePath in the LCM configuration.");

        var startInfo = new ProcessStartInfo
        {
            FileName = dscPath,
            Arguments = string.Join(" ", arguments.Select(arg => $"\"{arg}\"")),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(configPath) ?? Environment.CurrentDirectory
        };

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException("Failed to start DSC process");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        ParseAndLogDscMessages(stderr);

        var result = ParseDscResult(stdout, process.ExitCode);

        LogDscCommandCompleted(operation, process.ExitCode);

        return result;
    }

    private static List<string> BuildArguments(string operation, string configPath, LogLevel traceLevel)
    {
        var arguments = new List<string>
        {
            "--trace-level", MapLogLevelToTraceLevel(traceLevel),
            "--trace-format", "json",
            "--progress-format", "none",
            "config",
            operation,
            "--file", configPath,
            "--output-format", "json"
        };

        return arguments;
    }

    private static string MapLogLevelToTraceLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => "trace",
        LogLevel.Debug => "debug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "error",
        LogLevel.Critical => "error",
        _ => "info"
    };

    private void ParseAndLogDscMessages(string stderr)
    {
        if (string.IsNullOrWhiteSpace(stderr))
        {
            return;
        }

        var lines = stderr.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            try
            {
                var message = JsonSerializer.Deserialize(line, SourceGenerationContext.Default.DscMessage);
                if (message != null)
                {
                    LogDscMessage(message);
                }
            }
            catch (JsonException)
            {
                LogDscPlainTextMessage(line);
            }
        }
    }

    private void LogDscMessage(DscMessage message)
    {
        var level = message.Level?.ToLowerInvariant() switch
        {
            "error" => LogLevel.Error,
            "warn" or "warning" => LogLevel.Warning,
            "info" => LogLevel.Information,
            "debug" => LogLevel.Debug,
            "trace" => LogLevel.Trace,
            _ => LogLevel.Information
        };

        switch (level)
        {
            case LogLevel.Error:
                LogDscErrorMessage(message.Level, message.Fields?.Message);
                break;
            case LogLevel.Warning:
                LogDscWarningMessage(message.Level, message.Fields?.Message);
                break;
            case LogLevel.Information:
                LogDscInfoMessage(message.Level, message.Fields?.Message);
                break;
            case LogLevel.Debug:
                LogDscDebugMessage(message.Level, message.Fields?.Message);
                break;
            case LogLevel.Trace:
                LogDscTraceMessage(message.Level, message.Fields?.Message);
                break;
        }
    }

    private DscResult ParseDscResult(string stdout, int exitCode)
    {
        if (string.IsNullOrWhiteSpace(stdout))
        {
            return new DscResult { ExitCode = exitCode };
        }

        try
        {
            var result = JsonSerializer.Deserialize(stdout, SourceGenerationContext.Default.DscResult);

            if (result != null)
            {
                result.ExitCode = exitCode;
                return result;
            }
        }
        catch (JsonException ex)
        {
            LogFailedToParseDscJsonResult(ex);
        }

        return new DscResult { ExitCode = exitCode };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing DSC command: dsc {Arguments}")]
    private partial void LogExecutingDscCommand(string arguments);

    [LoggerMessage(Level = LogLevel.Information, Message = "DSC command '{Command}' completed with exit code {ExitCode}")]
    private partial void LogDscCommandCompleted(string command, int exitCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DSC malformed JSON: {Message}")]
    private partial void LogDscPlainTextMessage(string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to parse DSC JSON result")]
    private partial void LogFailedToParseDscJsonResult(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "DSC {Level}: {Message}")]
    private partial void LogDscErrorMessage(string? level, string? message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DSC {Level}: {Message}")]
    private partial void LogDscWarningMessage(string? level, string? message);

    [LoggerMessage(Level = LogLevel.Information, Message = "DSC {Level}: {Message}")]
    private partial void LogDscInfoMessage(string? level, string? message);

    [LoggerMessage(Level = LogLevel.Debug, Message = "DSC {Level}: {Message}")]
    private partial void LogDscDebugMessage(string? level, string? message);

    [LoggerMessage(Level = LogLevel.Trace, Message = "DSC {Level}: {Message}")]
    private partial void LogDscTraceMessage(string? level, string? message);
}
