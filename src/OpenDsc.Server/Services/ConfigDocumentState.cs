// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Services;

// ---------------------------------------------------------------------------
// Config document in-memory state model
// ---------------------------------------------------------------------------

/// <summary>
/// Represents the full state of a DSC configuration document being edited in the form UI.
/// </summary>
public sealed class ConfigDocumentState
{
    public string Schema { get; set; } =
        "https://aka.ms/dsc/schemas/v3/config/document.json";

    /// <summary>Parameters block. Key = parameter name.</summary>
    public Dictionary<string, ConfigParameterState> Parameters { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Variables block. Key = variable name, Value = raw value (string, number, bool, or null).</summary>
    public Dictionary<string, object?> Variables { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Ordered list of top-level resource instances.</summary>
    public List<ConfigResourceState> Resources { get; set; } = [];
}

/// <summary>A single DSC configuration parameter definition.</summary>
public sealed class ConfigParameterState
{
    public string Type { get; set; } = "string";
    public string? Description { get; set; }
    public object? DefaultValue { get; set; }
    public List<object>? AllowedValues { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
}

/// <summary>A single DSC resource instance within a configuration.</summary>
public sealed class ConfigResourceState
{
    /// <summary>Unique instance name (the resource block identifier).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Fully qualified type name, e.g. "OpenDsc.Windows/Service".</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Optional semver version constraint for the resource.</summary>
    public string? Version { get; set; }

    /// <summary>
    /// Properties for this resource instance.
    /// Value is a <see cref="ConfigPropertyValue"/> which can be a literal or a function expression.
    /// </summary>
    public Dictionary<string, ConfigPropertyValue> Properties { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// dependsOn list. Each entry is a resourceId expression string, e.g. "[resourceId('Type', 'Name')]".
    /// </summary>
    public List<string> DependsOn { get; set; } = [];

    /// <summary>
    /// For group/assertion resources that contain nested resources.
    /// </summary>
    public List<ConfigResourceState> NestedResources { get; set; } = [];
}

/// <summary>
/// A selectable resource reference used for depends-on dropdowns.
/// </summary>
public sealed record ResourceReferenceOption(string Label, string Expression, string Type, string Name);

/// <summary>
/// A property value that is either a literal scalar or a DSC function expression.
/// </summary>
public sealed class ConfigPropertyValue
{
    /// <summary>When true, the property value is a DSC function expression.</summary>
    public bool IsFunction { get; set; }

    /// <summary>Literal value (string, long, double, bool, null) when <see cref="IsFunction"/> is false.</summary>
    public object? RawValue { get; set; }

    /// <summary>Function expression when <see cref="IsFunction"/> is true.</summary>
    public DscFunctionExpression? Function { get; set; }

    public static ConfigPropertyValue Literal(object? value) =>
        new() { RawValue = value };

    public static ConfigPropertyValue FromFunction(DscFunctionExpression fn) =>
        new() { IsFunction = true, Function = fn };
}

/// <summary>An inline DSC function expression used as a property value.</summary>
public sealed class DscFunctionExpression
{
    public string FunctionName { get; set; } = string.Empty;
    public List<FunctionArg> Args { get; set; } = [];

    /// <summary>
    /// Renders the expression as a DSC inline string, e.g.
    /// <c>[concat(parameters('prefix'), '-', variables('suffix'))]</c>.
    /// </summary>
    public string ToInlineString()
    {
        var args = string.Join(", ", Args.Select(a => a.ToExpressionPart()));
        return $"[{FunctionName}({args})]";
    }
}

/// <summary>A single argument to a DSC function — either a literal value or a nested function call.</summary>
public sealed class FunctionArg
{
    public bool IsNested { get; set; }
    public object? LiteralValue { get; set; }
    public DscFunctionExpression? NestedFunction { get; set; }

    public string ToExpressionPart()
    {
        if (IsNested && NestedFunction is not null)
        {
            return NestedFunction.ToInlineString()[1..^1]; // strip outer brackets
        }

        return LiteralValue switch
        {
            string s => $"'{s.Replace("'", "''")}'",
            bool b => b ? "true" : "false",
            null => "null()",
            _ => LiteralValue.ToString() ?? "null()"
        };
    }

    public static FunctionArg Literal(object? value) => new() { LiteralValue = value };
    public static FunctionArg Nested(DscFunctionExpression fn) => new() { IsNested = true, NestedFunction = fn };
}
