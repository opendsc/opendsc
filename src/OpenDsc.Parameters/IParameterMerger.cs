// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Parameters;

/// <summary>
/// Defines a service for merging parameter files across multiple scopes.
/// </summary>
public interface IParameterMerger
{
    /// <summary>
    /// Merges multiple parameter files in precedence order (first = lowest precedence, last = highest precedence).
    /// </summary>
    /// <param name="parameterFiles">Collection of parameter file contents in precedence order.</param>
    /// <param name="options">Options controlling merge behavior.</param>
    /// <returns>Merged parameter content.</returns>
    string Merge(IEnumerable<string> parameterFiles, MergeOptions? options = null);

    /// <summary>
    /// Merges multiple parameter files with provenance tracking.
    /// </summary>
    /// <param name="parameterFiles">Collection of parameter files with scope information.</param>
    /// <param name="options">Options controlling merge behavior.</param>
    /// <returns>Merged result with provenance information.</returns>
    MergeResult MergeWithProvenance(IEnumerable<ParameterSource> parameterFiles, MergeOptions? options = null);
}

/// <summary>
/// Options for controlling parameter merge behavior.
/// </summary>
public sealed class MergeOptions
{
    /// <summary>
    /// Output format for merged parameters. Default is YAML.
    /// </summary>
    public ParameterFormat OutputFormat { get; set; } = ParameterFormat.Yaml;

    /// <summary>
    /// Whether to include comments in YAML output. Default is false.
    /// </summary>
    public bool IncludeComments { get; set; }
}

/// <summary>
/// Parameter file format.
/// </summary>
public enum ParameterFormat
{
    /// <summary>
    /// YAML format.
    /// </summary>
    Yaml,

    /// <summary>
    /// JSON format.
    /// </summary>
    Json
}

/// <summary>
/// Represents a parameter file with its source information.
/// </summary>
public sealed class ParameterSource
{
    /// <summary>
    /// The scope name.
    /// </summary>
    public required string ScopeName { get; set; }

    /// <summary>
    /// The precedence value for this scope.
    /// </summary>
    public required int Precedence { get; set; }

    /// <summary>
    /// The parameter file content (YAML or JSON).
    /// </summary>
    public required string Content { get; set; }
}

/// <summary>
/// Result of a merge operation with provenance tracking.
/// </summary>
public sealed class MergeResult
{
    /// <summary>
    /// The merged parameter content in the specified output format.
    /// </summary>
    public required string MergedContent { get; set; }

    /// <summary>
    /// Provenance information for each parameter path.
    /// </summary>
    public required Dictionary<string, ParameterProvenance> Provenance { get; set; }
}

/// <summary>
/// Tracks the origin of a parameter value.
/// </summary>
public sealed class ParameterProvenance
{
    /// <summary>
    /// The scope name where this value comes from.
    /// </summary>
    public required string ScopeName { get; set; }

    /// <summary>
    /// The precedence of this scope.
    /// </summary>
    public required int Precedence { get; set; }

    /// <summary>
    /// The parameter value.
    /// </summary>
    public required object? Value { get; set; }

    /// <summary>
    /// Values that were overridden by this value.
    /// </summary>
    public List<ScopeValue>? OverriddenValues { get; set; }
}

/// <summary>
/// Represents a parameter value from a specific scope.
/// </summary>
public sealed class ScopeValue
{
    /// <summary>
    /// The scope name where this value comes from.
    /// </summary>
    public required string ScopeName { get; set; }

    /// <summary>
    /// The precedence of this scope.
    /// </summary>
    public required int Precedence { get; set; }

    /// <summary>
    /// The parameter value.
    /// </summary>
    public required object? Value { get; set; }
}

