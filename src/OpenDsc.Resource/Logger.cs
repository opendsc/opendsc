// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

namespace OpenDsc.Resource;

public static class Logger
{

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
