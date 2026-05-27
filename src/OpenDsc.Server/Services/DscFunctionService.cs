// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using OpenDsc.Contracts.DscFunctions;

namespace OpenDsc.Server.Services;

/// <summary>
/// Evaluates DSC configuration document functions in-process.
/// Covers all pure functions and supports mock values for context-dependent functions.
/// </summary>
public sealed class DscFunctionService : IDscFunctionService
{
    // The static catalog of DSC v3 configuration document functions
    private static readonly IReadOnlyList<DscFunctionInfo> Catalog =
    [
        new DscFunctionInfo
        {
            Name = "concat",
            Description = "Concatenates multiple strings or arrays into a single value.",
            MinArgs = 1,
            MaxArgs = null,
            ParameterTypes = ["string|array"],
            ReturnType = "string|array"
        },
        new DscFunctionInfo
        {
            Name = "base64",
            Description = "Encodes a string as Base64.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["string"],
            ReturnType = "string"
        },
        new DscFunctionInfo
        {
            Name = "base64Decode",
            Description = "Decodes a Base64-encoded string.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["string"],
            ReturnType = "string"
        },
        new DscFunctionInfo
        {
            Name = "envvar",
            Description = "Returns the value of an environment variable.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["string"],
            ReturnType = "string"
        },
        new DscFunctionInfo
        {
            Name = "parameters",
            Description = "Returns the value of a configuration parameter.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["string"],
            ReturnType = "any"
        },
        new DscFunctionInfo
        {
            Name = "variables",
            Description = "Returns the value of a configuration variable.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["string"],
            ReturnType = "any"
        },
        new DscFunctionInfo
        {
            Name = "reference",
            Description = "Returns the output of another resource instance.",
            MinArgs = 1,
            MaxArgs = 2,
            ParameterTypes = ["string", "string"],
            ReturnType = "object"
        },
        new DscFunctionInfo
        {
            Name = "resourceId",
            Description = "Returns the qualified resource ID string for a named instance.",
            MinArgs = 1,
            MaxArgs = 2,
            ParameterTypes = ["string", "string"],
            ReturnType = "string"
        },
        new DscFunctionInfo
        {
            Name = "int",
            Description = "Converts a value to an integer.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["string|number"],
            ReturnType = "integer"
        },
        new DscFunctionInfo
        {
            Name = "string",
            Description = "Converts a value to its string representation.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["any"],
            ReturnType = "string"
        },
        new DscFunctionInfo
        {
            Name = "bool",
            Description = "Converts a value to a boolean.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["string|boolean"],
            ReturnType = "boolean"
        },
        new DscFunctionInfo
        {
            Name = "null",
            Description = "Returns null.",
            MinArgs = 0,
            MaxArgs = 0,
            ParameterTypes = [],
            ReturnType = "null"
        },
        new DscFunctionInfo
        {
            Name = "createArray",
            Description = "Creates an array from the provided values.",
            MinArgs = 0,
            MaxArgs = null,
            ParameterTypes = ["any"],
            ReturnType = "array"
        },
        new DscFunctionInfo
        {
            Name = "createObject",
            Description = "Creates an object from alternating key/value argument pairs.",
            MinArgs = 0,
            MaxArgs = null,
            ParameterTypes = ["string", "any"],
            ReturnType = "object"
        },
        new DscFunctionInfo
        {
            Name = "div",
            Description = "Performs integer division.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["integer", "integer"],
            ReturnType = "integer"
        },
        new DscFunctionInfo
        {
            Name = "mod",
            Description = "Returns the modulo (remainder) of integer division.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["integer", "integer"],
            ReturnType = "integer"
        },
        new DscFunctionInfo
        {
            Name = "mul",
            Description = "Multiplies two integers.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["integer", "integer"],
            ReturnType = "integer"
        },
        new DscFunctionInfo
        {
            Name = "add",
            Description = "Adds two integers.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["integer", "integer"],
            ReturnType = "integer"
        },
        new DscFunctionInfo
        {
            Name = "sub",
            Description = "Subtracts the second integer from the first.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["integer", "integer"],
            ReturnType = "integer"
        },
        new DscFunctionInfo
        {
            Name = "min",
            Description = "Returns the smaller of two integers.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["integer", "integer"],
            ReturnType = "integer"
        },
        new DscFunctionInfo
        {
            Name = "max",
            Description = "Returns the larger of two integers.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["integer", "integer"],
            ReturnType = "integer"
        },
        new DscFunctionInfo
        {
            Name = "toLower",
            Description = "Converts a string to lower case.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["string"],
            ReturnType = "string"
        },
        new DscFunctionInfo
        {
            Name = "toUpper",
            Description = "Converts a string to upper case.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["string"],
            ReturnType = "string"
        },
        new DscFunctionInfo
        {
            Name = "trim",
            Description = "Removes leading and trailing whitespace from a string.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["string"],
            ReturnType = "string"
        },
        new DscFunctionInfo
        {
            Name = "substring",
            Description = "Returns a substring starting at a given index, optionally limited by length.",
            MinArgs = 2,
            MaxArgs = 3,
            ParameterTypes = ["string", "integer", "integer"],
            ReturnType = "string"
        },
        new DscFunctionInfo
        {
            Name = "length",
            Description = "Returns the length of a string or array.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["string|array"],
            ReturnType = "integer"
        },
        new DscFunctionInfo
        {
            Name = "empty",
            Description = "Returns true if a string or array is empty.",
            MinArgs = 1,
            MaxArgs = 1,
            ParameterTypes = ["string|array"],
            ReturnType = "boolean"
        },
        new DscFunctionInfo
        {
            Name = "contains",
            Description = "Returns true if a string contains the specified substring.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["string", "string"],
            ReturnType = "boolean"
        },
        new DscFunctionInfo
        {
            Name = "startsWith",
            Description = "Returns true if a string starts with the specified prefix.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["string", "string"],
            ReturnType = "boolean"
        },
        new DscFunctionInfo
        {
            Name = "endsWith",
            Description = "Returns true if a string ends with the specified suffix.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["string", "string"],
            ReturnType = "boolean"
        },
        new DscFunctionInfo
        {
            Name = "indexOf",
            Description = "Returns the index of the first occurrence of a substring, or -1 if not found.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["string", "string"],
            ReturnType = "integer"
        },
        new DscFunctionInfo
        {
            Name = "lastIndexOf",
            Description = "Returns the index of the last occurrence of a substring, or -1 if not found.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["string", "string"],
            ReturnType = "integer"
        },
        new DscFunctionInfo
        {
            Name = "replace",
            Description = "Replaces all occurrences of a search string with a replacement string.",
            MinArgs = 3,
            MaxArgs = 3,
            ParameterTypes = ["string", "string", "string"],
            ReturnType = "string"
        },
        new DscFunctionInfo
        {
            Name = "split",
            Description = "Splits a string into an array using a delimiter.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["string", "string"],
            ReturnType = "array"
        },
        new DscFunctionInfo
        {
            Name = "join",
            Description = "Joins an array of strings into a single string with a delimiter.",
            MinArgs = 2,
            MaxArgs = 2,
            ParameterTypes = ["array", "string"],
            ReturnType = "string"
        },
        new DscFunctionInfo
        {
            Name = "path",
            Description = "Combines path segments using the OS path separator.",
            MinArgs = 1,
            MaxArgs = null,
            ParameterTypes = ["string"],
            ReturnType = "string"
        },
    ];

    public IReadOnlyList<DscFunctionInfo> GetFunctions() => Catalog;

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
