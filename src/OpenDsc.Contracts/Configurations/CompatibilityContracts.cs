// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// A validation error produced when checking parameter values against a JSON schema.
/// </summary>
public sealed class ValidationError
{
    /// <summary>
    /// The JSON path to the property that failed validation (e.g., "$.parameters.name").
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable description of the validation error.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The error code identifying the type of validation failure (e.g., "required", "type", "pattern").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The expected type according to the schema (e.g., "string", "number", "object").
    /// </summary>
    public string? ExpectedType { get; set; }

    /// <summary>
    /// The actual type of the provided value (e.g., "string", "number", "object").
    /// </summary>
    public string? ActualType { get; set; }
}

/// <summary>
/// A single schema change detected during compatibility analysis.
/// </summary>
public sealed class SchemaChange
{
    /// <summary>
    /// The name of the parameter affected by the change.
    /// </summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// The type of change (e.g., "added", "removed", "modified", "type_changed").
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// The original/old value (if applicable to the change type).
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// The new value (if applicable to the change type).
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Additional details about the schema change.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Migration status of a parameter file affected by a schema change.
/// </summary>
public sealed class ParameterFileMigrationStatus
{
    /// <summary>
    /// The unique identifier of the parameter file.
    /// </summary>
    public Guid FileId { get; set; }

    /// <summary>
    /// The name of the scope type this parameter is associated with.
    /// </summary>
    public string ScopeTypeName { get; set; } = string.Empty;

    /// <summary>
    /// The specific scope value (or null for configuration-level parameters).
    /// </summary>
    public string? ScopeValue { get; set; }

    /// <summary>
    /// The parameter version identifier.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether this parameter file requires migration to conform to the new schema.
    /// </summary>
    public bool NeedsMigration { get; set; }

    /// <summary>
    /// Validation errors encountered if the parameter doesn't conform to the new schema.
    /// </summary>
    public IReadOnlyList<ValidationError>? Errors { get; set; }
}

/// <summary>
/// Report of schema compatibility between two configuration versions.
/// </summary>
public sealed class CompatibilityReport
{
    /// <summary>
    /// The source configuration version identifier.
    /// </summary>
    public string OldVersion { get; set; } = string.Empty;

    /// <summary>
    /// The target configuration version identifier.
    /// </summary>
    public string NewVersion { get; set; } = string.Empty;

    /// <summary>
    /// The major version number of the new configuration.
    /// </summary>
    public int NewMajorVersion { get; set; }

    /// <summary>
    /// Whether the schema change introduces breaking changes that may require migration.
    /// </summary>
    public bool HasBreakingChanges { get; set; }

    /// <summary>
    /// The list of breaking schema changes (changes that may break parameter compatibility).
    /// </summary>
    public IReadOnlyList<SchemaChange> BreakingChanges { get; set; } = [];

    /// <summary>
    /// The list of non-breaking schema changes (additions and non-breaking modifications).
    /// </summary>
    public IReadOnlyList<SchemaChange> NonBreakingChanges { get; set; } = [];

    /// <summary>
    /// Parameter files affected by the schema change and their migration status.
    /// </summary>
    public IReadOnlyList<ParameterFileMigrationStatus> AffectedParameterFiles { get; set; } = [];
}
