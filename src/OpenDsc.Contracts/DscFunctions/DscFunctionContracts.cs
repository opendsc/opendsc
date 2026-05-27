// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.DscFunctions;

/// <summary>
/// Describes a DSC configuration document function.
/// </summary>
public sealed class DscFunctionInfo
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int? MinArgs { get; init; }
    public int? MaxArgs { get; init; }
    public required IReadOnlyList<string> ParameterTypes { get; init; }
    public string? ReturnType { get; init; }
}

/// <summary>
/// Request to mock-evaluate a DSC function expression.
/// </summary>
public sealed class EvaluateDscFunctionRequest
{
    /// <summary>
    /// The function name, e.g. "concat", "base64", "envvar".
    /// </summary>
    public required string FunctionName { get; init; }

    /// <summary>
    /// Arguments to pass. Scalars are JSON primitives; references use the same
    /// inline-expression syntax as DSC config documents, e.g. "[parameters('name')]".
    /// </summary>
    public required IReadOnlyList<object?> Args { get; init; }

    /// <summary>
    /// Optional mock environment variable values for preview.
    /// Key = variable name, Value = mock string value.
    /// </summary>
    public Dictionary<string, string>? MockEnvVars { get; init; }

    /// <summary>
    /// Optional mock parameter values for preview.
    /// Key = parameter name, Value = mock value (JSON-serialized).
    /// </summary>
    public Dictionary<string, string>? MockParameters { get; init; }

    /// <summary>
    /// Optional mock variable values for preview.
    /// Key = variable name, Value = mock value (JSON-serialized).
    /// </summary>
    public Dictionary<string, string>? MockVariables { get; init; }
}

/// <summary>
/// Result of a mock DSC function evaluation.
/// </summary>
public sealed class EvaluateDscFunctionResult
{
    public required string FunctionExpression { get; init; }
    public string? ResultJson { get; init; }
    public string? Error { get; init; }
    public bool Success => Error is null;
}
