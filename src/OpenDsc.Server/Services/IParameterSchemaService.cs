// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public interface IParameterSchemaService
{
    /// <summary>
    /// Calculates SHA256 hash of parameter schema definition.
    /// </summary>
    string CalculateSchemaHash(string schemaDefinition);

    /// <summary>
    /// Parses parameter block from DSC configuration YAML.
    /// </summary>
    Task<string?> ParseParameterBlockAsync(string configurationContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects schema changes between two parameter schemas.
    /// </summary>
    SchemaChanges DetectSchemaChanges(string? oldSchemaDefinition, string? newSchemaDefinition);

    /// <summary>
    /// Validates SemVer compliance based on schema changes.
    /// </summary>
    SemVerValidationResult ValidateSemVerCompliance(string newVersion, string? oldVersion, SchemaChanges changes);

    /// <summary>
    /// Finds or creates parameter schema for a configuration version.
    /// </summary>
    Task<ParameterSchema?> FindOrCreateSchemaAsync(Guid configurationId, string? schemaDefinition, CancellationToken cancellationToken = default);
}

public record SchemaChanges(
    bool HasBreakingChanges,
    bool HasAdditiveChanges,
    bool IsIdentical,
    List<string> RemovedParameters,
    List<string> RenamedParameters,
    List<string> AddedParameters);

public record SemVerValidationResult(
    bool IsValid,
    string? ExpectedVersionComponent,
    List<string> Violations);
