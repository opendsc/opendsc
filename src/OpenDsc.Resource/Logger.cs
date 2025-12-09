// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

namespace OpenDsc.Resource;

/// <summary>
/// Provides logging functionality for DSC resources to output structured log messages to stderr.
/// </summary>
public static class Logger
{
    /// <summary>
    /// Writes an informational message to the standard error stream in JSON format.
    /// </summary>
    /// <param name="message">The informational message to write.</param>
    public static void WriteInfo(string message)
    {
        var infoMessage = new Info() { Message = message };
#if NET6_0_OR_GREATER
        string json = JsonSerializer.Serialize(infoMessage, typeof(Info), SourceGenerationContext.Default);
#else
        string json = JsonSerializer.Serialize(infoMessage, typeof(Info));
#endif
        Console.Error.WriteLine(json);
    }

    /// <summary>
    /// Writes a warning message to the standard error stream in JSON format.
    /// </summary>
    /// <param name="message">The warning message to write.</param>
    public static void WriteWarning(string message)
    {
        var warningMessage = new Warning() { Message = message };
#if NET6_0_OR_GREATER
        string json = JsonSerializer.Serialize(warningMessage, typeof(Warning), SourceGenerationContext.Default);
#else
        string json = JsonSerializer.Serialize(warningMessage);
#endif
        Console.Error.WriteLine(json);
    }

    /// <summary>
    /// Writes an error message to the standard error stream in JSON format.
    /// </summary>
    /// <param name="message">The error message to write.</param>
    public static void WriteError(string message)
    {
        var errorMessage = new Error() { Message = message };
#if NET6_0_OR_GREATER
        string json = JsonSerializer.Serialize(errorMessage, typeof(Error), SourceGenerationContext.Default);
#else
        string json = JsonSerializer.Serialize(errorMessage);
#endif
        Console.Error.WriteLine(json);
    }

    /// <summary>
    /// Writes a trace/debug message to the standard error stream in JSON format.
    /// </summary>
    /// <param name="message">The trace message to write.</param>
    public static void WriteTrace(string message)
    {
        var traceMessage = new Trace() { Message = message };
#if NET6_0_OR_GREATER
        string json = JsonSerializer.Serialize(traceMessage, typeof(Trace), SourceGenerationContext.Default);
#else
        string json = JsonSerializer.Serialize(traceMessage);
#endif
        Console.Error.WriteLine(json);
    }
}
