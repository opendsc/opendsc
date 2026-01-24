// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using OpenDsc.Schema;

namespace OpenDsc.Lcm;

public partial class DscExecutor(ILogger<DscExecutor> logger, ILoggerFactory loggerFactory)
{
    private readonly ILogger _dscLogger = loggerFactory.CreateLogger("DSC");

    public async Task<(DscResult Result, int ExitCode)> ExecuteTestAsync(string configPath, LcmConfig config, LogLevel traceLevel, CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandAsync("test", configPath, config, traceLevel, cancellationToken);
    }

    public async Task<(DscResult Result, int ExitCode)> ExecuteSetAsync(string configPath, LcmConfig config, LogLevel traceLevel, CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandAsync("set", configPath, config, traceLevel, cancellationToken);
    }

    private static string? FindExecutableInPath()
    {
        string executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dsc.exe" : "dsc";
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv == null) return null;
        var paths = pathEnv.Split(Path.PathSeparator);
        return paths
            .Select(path => Path.Combine(path, executableName))
            .FirstOrDefault(File.Exists);
    }

    protected virtual async Task<(DscResult Result, int ExitCode)> ExecuteCommandAsync(string operation, string configPath, LcmConfig config, LogLevel traceLevel, CancellationToken cancellationToken)
    {
        var arguments = BuildArguments(operation, configPath, traceLevel);

#pragma warning disable CA1873
        if (logger.IsEnabled(LogLevel.Debug))
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

        var result = ParseDscResult(stdout);
        var exitCode = process.ExitCode;

        LogDscCommandCompleted(operation, exitCode);

        return (result, exitCode);
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
                var message = JsonSerializer.Deserialize(line, SourceGenerationContext.Default.DscTraceMessage);
                if (message != null)
                {
                    LogDscTraceMessage(message);
                }
            }
            catch (JsonException)
            {
                LogDscPlainTextMessage(line);
            }
        }
    }

    private void LogDscTraceMessage(DscTraceMessage message)
    {
        if (string.IsNullOrEmpty(message.Fields?.Message)) return;

        var level = message.Level switch
        {
            DscTraceLevel.Error => LogLevel.Error,
            DscTraceLevel.Warn => LogLevel.Warning,
            DscTraceLevel.Info => LogLevel.Information,
            DscTraceLevel.Debug => LogLevel.Debug,
            DscTraceLevel.Trace => LogLevel.Trace,
            _ => LogLevel.Information
        };

        switch (level)
        {
            case LogLevel.Error:
                LogDscErrorMessage(message.Fields.Message);
                break;
            case LogLevel.Warning:
                LogDscWarningMessage(message.Fields.Message);
                break;
            case LogLevel.Information:
                LogDscInfoMessage(message.Fields.Message);
                break;
            case LogLevel.Debug:
                LogDscDebugMessage(message.Fields.Message);
                break;
            case LogLevel.Trace:
                LogDscTraceMessage(message.Fields.Message);
                break;
        }
    }

    private DscResult ParseDscResult(string stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout))
        {
            throw new InvalidOperationException("DSC command returned no output. This indicates a failure in the DSC execution.");
        }

        try
        {
            var result = JsonSerializer.Deserialize(stdout, SourceGenerationContext.Default.DscResult);

            if (result != null)
            {
                return result;
            }

            throw new InvalidOperationException("DSC command returned invalid JSON that deserialized to null.");
        }
        catch (JsonException ex)
        {
            LogFailedToParseDscJsonResult(ex);
            throw new InvalidOperationException("Failed to parse DSC JSON result. The output may be malformed.", ex);
        }
    }

    [LoggerMessage(EventId = EventIds.DscCommandExecuting, Level = LogLevel.Debug, Message = "Executing DSC command: dsc {Arguments}")]
    private partial void LogExecutingDscCommand(string arguments);

    [LoggerMessage(EventId = EventIds.DscCommandCompleted, Level = LogLevel.Information, Message = "DSC command '{Command}' completed with exit code {ExitCode}")]
    private partial void LogDscCommandCompleted(string command, int exitCode);

    [LoggerMessage(EventId = EventIds.DscMalformedJson, Level = LogLevel.Warning, Message = "DSC malformed JSON: {Message}")]
    private partial void LogDscPlainTextMessage(string message);

    [LoggerMessage(EventId = EventIds.DscParseError, Level = LogLevel.Warning, Message = "Failed to parse DSC JSON result")]
    private partial void LogFailedToParseDscJsonResult(Exception ex);

    [LoggerMessage(EventId = EventIds.DscErrorMessage, Level = LogLevel.Error, Message = "{Message}")]
    private partial void LogDscErrorMessage(string message);

    [LoggerMessage(EventId = EventIds.DscWarningMessage, Level = LogLevel.Warning, Message = "{Message}")]
    private partial void LogDscWarningMessage(string message);

    [LoggerMessage(EventId = EventIds.DscInfoMessage, Level = LogLevel.Information, Message = "{Message}")]
    private partial void LogDscInfoMessage(string message);

    [LoggerMessage(EventId = EventIds.DscDebugMessage, Level = LogLevel.Debug, Message = "{Message}")]
    private partial void LogDscDebugMessage(string message);

    [LoggerMessage(EventId = EventIds.DscTraceMessage, Level = LogLevel.Trace, Message = "{Message}")]
    private partial void LogDscTraceMessage(string message);
}
