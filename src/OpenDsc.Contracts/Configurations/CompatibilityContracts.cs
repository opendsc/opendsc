// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// A validation error produced when checking parameter values against a JSON schema.
/// </summary>
public sealed class ValidationError
{
    public required string Path { get; init; }
    public required string Message { get; init; }
    public required string Code { get; init; }
    public string? ExpectedType { get; init; }
    public string? ActualType { get; init; }
}

/// <summary>
/// A single schema change detected during compatibility analysis.
/// </summary>
public sealed class SchemaChange
{
    public required string ParameterName { get; init; }
    public required string ChangeType { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Migration status of a parameter file affected by a schema change.
/// </summary>
public sealed class ParameterFileMigrationStatus
{
    public required Guid FileId { get; init; }
    public required string ScopeTypeName { get; init; }
    public string? ScopeValue { get; init; }
    public required string Version { get; init; }
    public required bool NeedsMigration { get; init; }
    public List<ValidationError>? Errors { get; init; }
}

/// <summary>
/// Report of schema compatibility between two configuration versions.
/// </summary>
public sealed class CompatibilityReport
{
    public required string OldVersion { get; init; }
    public required string NewVersion { get; init; }
    public required int NewMajorVersion { get; init; }
    public required bool HasBreakingChanges { get; init; }
    public required List<SchemaChange> BreakingChanges { get; init; }
    public required List<SchemaChange> NonBreakingChanges { get; init; }
    public required List<ParameterFileMigrationStatus> AffectedParameterFiles { get; init; }
}
