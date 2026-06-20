// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using OpenDsc.Contracts.DscFunctions;
using OpenDsc.Server.Mcp;

namespace OpenDsc.Server.Services;

/// <summary>
/// Evaluates DSC configuration document functions in-process.
/// Covers all pure functions and supports mock values for context-dependent functions.
/// Uses the DSC MCP server as the primary source for available functions.
/// </summary>
public sealed class DscFunctionService : IDscFunctionService
{
    private readonly IMcpClient _mcpClient;
    private readonly ILogger<DscFunctionService> _logger;
    private IReadOnlyList<DscFunctionInfo>? _cachedFunctions;

    public DscFunctionService(IMcpClient mcpClient, ILogger<DscFunctionService> logger)
    {
        _mcpClient = mcpClient;
        _logger = logger;
    }

    public IReadOnlyList<DscFunctionInfo> GetFunctions()
    {
        // Return cached functions from MCP server, or empty if not yet loaded
        return _cachedFunctions ?? [];
    }

    public async Task<IReadOnlyList<DscFunctionInfo>> GetFunctionsAsync(CancellationToken cancellationToken = default)
    {
        // Return cached functions if available
        if (_cachedFunctions is not null)
        {
            return _cachedFunctions;
        }

        // Initialize MCP client if not already connected
        if (!_mcpClient.IsConnected)
        {
            await _mcpClient.InitializeAsync(cancellationToken);
        }

        // Fetch from MCP server
        _cachedFunctions = await _mcpClient.ListFunctionsAsync(cancellationToken);
        _logger.LogInformation("Loaded {Count} DSC functions from MCP server", _cachedFunctions.Count);
        return _cachedFunctions;
    }

    public EvaluateDscFunctionResult Evaluate(EvaluateDscFunctionRequest request)
    {
        var expression = BuildExpression(request.FunctionName, request.Args);
        try
        {
            var result = EvaluateFunction(
                request.FunctionName,
                request.Args,
                request.MockEnvVars ?? [],
                request.MockParameters ?? [],
                request.MockVariables ?? []);

            return new EvaluateDscFunctionResult
            {
                FunctionExpression = expression,
                ResultJson = JsonSerializer.Serialize(result)
            };
        }
        catch (Exception ex)
        {
            return new EvaluateDscFunctionResult
            {
                FunctionExpression = expression,
                Error = ex.Message
            };
        }
    }

    private static string BuildExpression(string name, IReadOnlyList<object?> args)
    {
        var sb = new StringBuilder();
        sb.Append('[').Append(name).Append('(');
        for (int i = 0; i < args.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var arg = args[i];
            if (arg is string s)
            {
                sb.Append('\'').Append(s.Replace("'", "\\'")).Append('\'');
            }
            else
            {
                sb.Append(arg);
            }
        }
        sb.Append(")]");
        return sb.ToString();
    }

    private static object? EvaluateFunction(
        string name,
        IReadOnlyList<object?> args,
        Dictionary<string, string> envVars,
        Dictionary<string, string> parameters,
        Dictionary<string, string> variables)
    {
        // Resolve each argument — if it's itself a function expression, evaluate it first
        var resolved = args.Select(a => ResolveArg(a, envVars, parameters, variables)).ToList();

        return name switch
        {
            "concat" => EvalConcat(resolved),
            "base64" => Convert.ToBase64String(Encoding.UTF8.GetBytes(AsString(resolved, 0))),
            "base64Decode" => Encoding.UTF8.GetString(Convert.FromBase64String(AsString(resolved, 0))),
            "envvar" => envVars.TryGetValue(AsString(resolved, 0), out var ev) ? ev : $"$({AsString(resolved, 0)})",
            "parameters" => parameters.TryGetValue(AsString(resolved, 0), out var pv) ? JsonNode.Parse(pv) : $"$(parameters({AsString(resolved, 0)}))",
            "variables" => variables.TryGetValue(AsString(resolved, 0), out var vv) ? JsonNode.Parse(vv) : $"$(variables({AsString(resolved, 0)}))",
            "resourceId" => resolved.Count >= 2 ? $"[resourceId('{resolved[0]}', '{resolved[1]}')]" : $"[resourceId('{resolved[0]}')]",
            "reference" => $"$(reference({AsString(resolved, 0)}))",
            "int" => Convert.ToInt64(resolved[0]),
            "string" => resolved[0]?.ToString() ?? "null",
            "bool" => ParseBool(resolved[0]),
            "null" => null,
            "createArray" => resolved.ToArray(),
            "toLower" => AsString(resolved, 0).ToLowerInvariant(),
            "toUpper" => AsString(resolved, 0).ToUpperInvariant(),
            "trim" => AsString(resolved, 0).Trim(),
            "substring" => EvalSubstring(resolved),
            "length" => EvalLength(resolved),
            "empty" => EvalEmpty(resolved),
            "contains" => AsString(resolved, 0).Contains(AsString(resolved, 1), StringComparison.Ordinal),
            "startsWith" => AsString(resolved, 0).StartsWith(AsString(resolved, 1), StringComparison.Ordinal),
            "endsWith" => AsString(resolved, 0).EndsWith(AsString(resolved, 1), StringComparison.Ordinal),
            "indexOf" => (long)AsString(resolved, 0).IndexOf(AsString(resolved, 1), StringComparison.Ordinal),
            "lastIndexOf" => (long)AsString(resolved, 0).LastIndexOf(AsString(resolved, 1), StringComparison.Ordinal),
            "replace" => AsString(resolved, 0).Replace(AsString(resolved, 1), AsString(resolved, 2), StringComparison.Ordinal),
            "split" => AsString(resolved, 0).Split(AsString(resolved, 1)),
            "join" => string.Join(AsString(resolved, 1), (IEnumerable<string>)(resolved[0] as IEnumerable<string> ?? [])),
            "add" => AsLong(resolved, 0) + AsLong(resolved, 1),
            "sub" => AsLong(resolved, 0) - AsLong(resolved, 1),
            "mul" => AsLong(resolved, 0) * AsLong(resolved, 1),
            "div" => AsLong(resolved, 0) / AsLong(resolved, 1),
            "mod" => AsLong(resolved, 0) % AsLong(resolved, 1),
            "min" => Math.Min(AsLong(resolved, 0), AsLong(resolved, 1)),
            "max" => Math.Max(AsLong(resolved, 0), AsLong(resolved, 1)),
            "path" => Path.Combine(resolved.Select(r => r?.ToString() ?? "").ToArray()),
            _ => throw new InvalidOperationException($"Unknown function '{name}'.")
        };
    }

    private static object? ResolveArg(
        object? arg,
        Dictionary<string, string> envVars,
        Dictionary<string, string> parameters,
        Dictionary<string, string> variables)
    {
        if (arg is not string s)
        {
            return arg;
        }

        // If the arg is itself a DSC inline expression, evaluate it
        var trimmed = s.Trim();
        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            var inner = trimmed[1..^1].Trim();
            var (funcName, innerArgs) = ParseFunctionCall(inner);
            if (funcName is not null)
            {
                return EvaluateFunction(funcName, innerArgs, envVars, parameters, variables);
            }
        }

        return arg;
    }

    private static (string? FuncName, List<object?> Args) ParseFunctionCall(string expression)
    {
        var parenIdx = expression.IndexOf('(');
        if (parenIdx < 0 || !expression.TrimEnd().EndsWith(')'))
        {
            return (null, []);
        }

        var name = expression[..parenIdx].Trim();
        var argsStr = expression[(parenIdx + 1)..expression.LastIndexOf(')')].Trim();

        var argList = new List<object?>();
        if (!string.IsNullOrWhiteSpace(argsStr))
        {
            // Simple CSV split — handles quoted strings and nested brackets partially
            foreach (var token in SplitArgs(argsStr))
            {
                var t = token.Trim();
                if (t.StartsWith('\'') && t.EndsWith('\''))
                {
                    argList.Add(t[1..^1]);
                }
                else if (long.TryParse(t, out var l))
                {
                    argList.Add(l);
                }
                else if (t.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    argList.Add(true);
                }
                else if (t.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    argList.Add(false);
                }
                else
                {
                    argList.Add(t);
                }
            }
        }

        return (name, argList);
    }

    private static IEnumerable<string> SplitArgs(string argsStr)
    {
        int depth = 0;
        int start = 0;
        for (int i = 0; i < argsStr.Length; i++)
        {
            var c = argsStr[i];
            if (c is '(' or '[') depth++;
            else if (c is ')' or ']') depth--;
            else if (c == ',' && depth == 0)
            {
                yield return argsStr[start..i];
                start = i + 1;
            }
        }
        yield return argsStr[start..];
    }

    private static object? EvalConcat(List<object?> resolved)
    {
        if (resolved.Count == 0) return string.Empty;
        if (resolved[0] is Array || resolved[0] is IEnumerable<object>)
        {
            var combined = new List<object?>();
            foreach (var item in resolved)
            {
                if (item is object[] arr) combined.AddRange(arr);
                else if (item is List<object?> lst) combined.AddRange(lst);
                else combined.Add(item);
            }
            return combined.ToArray();
        }
        return string.Concat(resolved.Select(r => r?.ToString() ?? ""));
    }

    private static object? EvalSubstring(List<object?> resolved)
    {
        var str = AsString(resolved, 0);
        var start = (int)AsLong(resolved, 1);
        if (resolved.Count >= 3)
        {
            var len = (int)AsLong(resolved, 2);
            return str.Substring(start, Math.Min(len, str.Length - start));
        }
        return str[start..];
    }

    private static object? EvalLength(List<object?> resolved)
    {
        return resolved[0] switch
        {
            string s => (long)s.Length,
            Array a => (long)a.Length,
            _ => (long)(resolved[0]?.ToString()?.Length ?? 0)
        };
    }

    private static object? EvalEmpty(List<object?> resolved)
    {
        return resolved[0] switch
        {
            string s => s.Length == 0,
            Array a => a.Length == 0,
            _ => resolved[0] is null
        };
    }

    private static string AsString(List<object?> args, int index) =>
        args.Count > index ? args[index]?.ToString() ?? string.Empty : string.Empty;

    private static long AsLong(List<object?> args, int index)
    {
        var v = args.Count > index ? args[index] : null;
        return v switch
        {
            long l => l,
            int i => i,
            string s when long.TryParse(s, out var parsed) => parsed,
            _ => 0
        };
    }

    private static bool ParseBool(object? value) => value switch
    {
        bool b => b,
        string s => s.Equals("true", StringComparison.OrdinalIgnoreCase),
        long l => l != 0,
        _ => false
    };
}
