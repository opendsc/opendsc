// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace OpenDsc.Resource.Logging;

/// <summary>
/// Logger provider for DSC resources that outputs JSON messages to stderr.
/// Respects the DSC_TRACE_LEVEL environment variable for log filtering.
/// </summary>
public sealed class DscResourceLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minimumLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="DscResourceLoggerProvider"/> class.
    /// Reads DSC_TRACE_LEVEL environment variable to determine minimum log level.
    /// </summary>
    public DscResourceLoggerProvider()
    {
        _minimumLevel = GetMinimumLevelFromEnvironment();
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new DscResourceLogger(categoryName, _minimumLevel);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }

    private static LogLevel GetMinimumLevelFromEnvironment()
    {
        var traceLevel = Environment.GetEnvironmentVariable("DSC_TRACE_LEVEL");

        if (string.IsNullOrWhiteSpace(traceLevel))
        {
            return LogLevel.Information;
        }

        return traceLevel.ToLowerInvariant() switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Information,
            "warn" => LogLevel.Warning,
            "error" => LogLevel.Error,
            _ => LogLevel.Information
        };
    }
}

/// <summary>
/// Logger implementation that outputs DSC-compatible JSON messages to stderr.
/// </summary>
internal sealed class DscResourceLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LogLevel _minimumLevel;

    public DscResourceLogger(string categoryName, LogLevel minimumLevel)
    {
        _categoryName = categoryName;
        _minimumLevel = minimumLevel;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= _minimumLevel;
    }

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);

        if (string.IsNullOrEmpty(message) && exception == null)
        {
            return;
        }

        if (exception != null && string.IsNullOrEmpty(message))
        {
            message = exception.ToString();
        }
        else if (exception != null)
        {
            message = $"{message}{Environment.NewLine}{exception}";
        }

        WriteJsonMessage(logLevel, message);
    }

    private static void WriteJsonMessage(LogLevel logLevel, string message)
    {
        string json = logLevel switch
        {
            LogLevel.Trace => SerializeMessage(new Trace { Message = message }),
            LogLevel.Debug => SerializeMessage(new Trace { Message = message }),
            LogLevel.Information => SerializeMessage(new Info { Message = message }),
            LogLevel.Warning => SerializeMessage(new Warning { Message = message }),
            LogLevel.Error => SerializeMessage(new Error { Message = message }),
            LogLevel.Critical => SerializeMessage(new Error { Message = message }),
            _ => SerializeMessage(new Info { Message = message })
        };

        Console.Error.WriteLine(json);
    }

    private static string SerializeMessage<T>(T message) where T : class
    {
#if NET6_0_OR_GREATER
        return JsonSerializer.Serialize(message, typeof(T), SourceGenerationContext.Default);
#else
        return JsonSerializer.Serialize(message);
#endif
    }
}
