// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Contracts.Parameters;

/// <summary>
/// Parameter version lifecycle status.
/// </summary>
public enum ParameterVersionStatus
{
    Draft,
    Published
}

/// <summary>
/// Request to create or update a parameter version.
/// </summary>
public sealed class CreateParameterRequest
{
    /// <summary>
    /// Optional scope value.
    /// </summary>
    public string? ScopeValue { get; set; }

    /// <summary>
    /// Semantic version string for the parameter version.
    /// </summary>
    [JsonRequired]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Parameter content in YAML form.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Optional content type.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Whether the parameter version should pass through without local content.
    /// </summary>
    public bool? IsPassthrough { get; set; }
}

/// <summary>
/// Request to update the content of an existing parameter version.
/// </summary>
public sealed class UpdateParameterRequest
{
    /// <summary>
    /// Updated parameter content in YAML form.
    /// </summary>
    [JsonRequired]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Details about a parameter version.
/// </summary>
public sealed class ParameterVersionDetails
{
    public Guid Id { get; set; }

    public Guid ScopeTypeId { get; set; }

    public Guid ConfigurationId { get; set; }

    public string? ScopeValue { get; set; }

    public string Version { get; set; } = string.Empty;

    public int MajorVersion { get; set; }

    public string Checksum { get; set; } = string.Empty;

    public ParameterVersionStatus Status { get; set; }

    public bool IsPassthrough { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Provenance details for a node's merged parameters.
/// </summary>
public sealed class ParameterProvenanceDetails
{
    public Guid NodeId { get; set; }

    public Guid ConfigurationId { get; set; }

    public string MergedParameters { get; set; } = string.Empty;

    public Dictionary<string, ParameterSourceInfo> Provenance { get; set; } = [];

    public string? PrereleaseChannel { get; set; }
}

/// <summary>
/// Information about a single value source within provenance.
/// </summary>
public sealed class ParameterSourceInfo
{
    public string ScopeTypeName { get; set; } = string.Empty;

    public string? ScopeValue { get; set; }

    public int Precedence { get; set; }

    public object? Value { get; set; }

    public List<ScopeInfo>? OverriddenBy { get; set; }

    public string? ResolvedVersion { get; set; }

    public bool IsPrerelease { get; set; }
}

/// <summary>
/// Information about a scope entry in provenance.
/// </summary>
public sealed class ScopeInfo
{
    public string ScopeTypeName { get; set; } = string.Empty;

    public string? ScopeValue { get; set; }

    public int Precedence { get; set; }

    public object? Value { get; set; }
}

/// <summary>
/// Summary of available major versions for a parameter set.
/// </summary>
public sealed class MajorVersionSummary
{
    public int MajorVersion { get; set; }

    public int VersionCount { get; set; }

    public bool HasActive { get; set; }

    public string LatestVersion { get; set; } = string.Empty;

    public bool HasMigrationNeeded { get; set; }
}

/// <summary>
/// Resolution preview for a node.
/// </summary>
public sealed class ParameterResolutionDetails
{
    public Guid NodeId { get; set; }

    public Guid ConfigurationId { get; set; }

    public string? PrereleaseChannel { get; set; }

    public List<ScopeResolutionDetails> Scopes { get; set; } = [];
}

/// <summary>
/// Resolution details for a single scope.
/// </summary>
public sealed class ScopeResolutionDetails
{
    public string ScopeTypeName { get; set; } = string.Empty;

    public string? ScopeValue { get; set; }

    public string? ResolvedVersion { get; set; }

    public bool IsPrerelease { get; set; }

    public bool NoPublishedVersion { get; set; }
}

/// <summary>
/// Validation response for parameter content.
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; set; }

    public List<ValidationError>? Errors { get; set; }
}

/// <summary>
/// Validation error details.
/// </summary>
public sealed class ValidationError
{
    public string Path { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Result of a publish operation.
/// </summary>
public sealed class PublishResult
{
    public bool Success { get; set; }

    public CompatibilityReport? CompatibilityReport { get; set; }

    public List<ParameterFileMigrationStatus>? MigrationRequirements { get; set; }
}

/// <summary>
/// Compatibility report for schema evolution.
/// </summary>
public sealed class CompatibilityReport
{
    public bool HasBreakingChanges { get; set; }

    public List<ParameterChange>? BreakingChanges { get; set; }

    public List<ParameterChange>? NonBreakingChanges { get; set; }
}

/// <summary>
/// A single parameter change entry.
/// </summary>
public sealed class ParameterChange
{
    public string ParameterName { get; set; } = string.Empty;

    public string ChangeType { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Migration status for an individual parameter file.
/// </summary>
public sealed class ParameterFileMigrationStatus
{
    public string ScopeTypeName { get; set; } = string.Empty;

    public string ScopeValue { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public int MajorVersion { get; set; }

    public bool NeedsMigration { get; set; }

    public List<ValidationError> Errors { get; set; } = [];
}