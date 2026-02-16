// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

using NuGet.Versioning;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenDsc.Server.Services;

public partial class ParameterSchemaService(
    ServerDbContext dbContext,
    IParameterSchemaBuilder schemaBuilder) : IParameterSchemaService
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    [GeneratedRegex(@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$")]
    private static partial Regex SemVerRegex();

    public Task<string?> ParseParameterBlockAsync(string configurationContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configurationContent))
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            var configDocument = YamlDeserializer.Deserialize<Dictionary<string, object>>(configurationContent);

            if (configDocument == null || !configDocument.TryGetValue("parameters", out var parametersObj))
            {
                return Task.FromResult<string?>(null);
            }

            var parametersJson = JsonSerializer.Serialize(parametersObj, SourceGenerationContext.Default.Object);
            return Task.FromResult<string?>(parametersJson);
        }
        catch (Exception)
        {
            return Task.FromResult<string?>(null);
        }
    }

    public SchemaChanges DetectSchemaChanges(string? oldSchemaDefinition, string? newSchemaDefinition)
    {
        if (string.IsNullOrWhiteSpace(oldSchemaDefinition) && string.IsNullOrWhiteSpace(newSchemaDefinition))
        {
            return new SchemaChanges(false, false, true, [], [], []);
        }

        if (string.IsNullOrWhiteSpace(oldSchemaDefinition))
        {
            var addedParams = ExtractParameterNames(newSchemaDefinition);
            return new SchemaChanges(false, addedParams.Count > 0, false, [], [], addedParams);
        }

        if (string.IsNullOrWhiteSpace(newSchemaDefinition))
        {
            var removedParams = ExtractParameterNames(oldSchemaDefinition);
            return new SchemaChanges(removedParams.Count > 0, false, false, removedParams, [], []);
        }

        var oldParams = ExtractParameterNames(oldSchemaDefinition);
        var newParams = ExtractParameterNames(newSchemaDefinition);

        var removed = oldParams.Except(newParams, StringComparer.OrdinalIgnoreCase).ToList();
        var added = newParams.Except(oldParams, StringComparer.OrdinalIgnoreCase).ToList();

        var hasBreaking = removed.Count > 0;
        var hasAdditive = added.Count > 0;
        var isIdentical = removed.Count == 0 && added.Count == 0;

        return new SchemaChanges(hasBreaking, hasAdditive, isIdentical, removed, [], added);
    }

    public SemVerValidationResult ValidateSemVerCompliance(string newVersion, string? oldVersion, SchemaChanges changes)
    {
        var violations = new List<string>();

        if (!IsSemVerValid(newVersion))
        {
            violations.Add($"Version '{newVersion}' is not a valid semantic version (must follow MAJOR.MINOR.PATCH format)");
            return new SemVerValidationResult(false, null, violations);
        }

        if (string.IsNullOrWhiteSpace(oldVersion))
        {
            return new SemVerValidationResult(true, null, []);
        }

        if (!IsSemVerValid(oldVersion))
        {
            violations.Add($"Previous version '{oldVersion}' is not a valid semantic version");
            return new SemVerValidationResult(false, null, violations);
        }

        var newParts = ParseSemVer(newVersion);
        var oldParts = ParseSemVer(oldVersion);

        var majorIncreased = newParts.Major > oldParts.Major;
        var minorIncreased = newParts.Major == oldParts.Major && newParts.Minor > oldParts.Minor;
        var patchIncreased = newParts.Major == oldParts.Major && newParts.Minor == oldParts.Minor && newParts.Patch > oldParts.Patch;

        string? expectedComponent = null;

        if (changes.HasBreakingChanges)
        {
            expectedComponent = "MAJOR";
            if (!majorIncreased)
            {
                violations.Add($"Breaking changes detected (removed parameters: {string.Join(", ", changes.RemovedParameters)}) require MAJOR version increment");
            }
        }
        else if (changes.HasAdditiveChanges)
        {
            expectedComponent = "MINOR";
            if (!majorIncreased && !minorIncreased)
            {
                violations.Add($"New parameters added ({string.Join(", ", changes.AddedParameters)}) require at least MINOR version increment");
            }
        }
        else if (changes.IsIdentical)
        {
            expectedComponent = "PATCH";
            if (!majorIncreased && !minorIncreased && !patchIncreased)
            {
                violations.Add("No parameter schema changes detected, but version must still increment (PATCH expected for bug fixes)");
            }
        }
        else
        {
            expectedComponent = "PATCH";
        }

        return new SemVerValidationResult(violations.Count == 0, expectedComponent, violations);
    }

    public async Task<ParameterSchema> GenerateAndStoreSchemaAsync(Guid configurationId, string parametersJson, string version, CancellationToken cancellationToken = default)
    {
        if (!SemanticVersion.TryParse(version, out var semVer))
        {
            throw new ArgumentException($"Invalid semantic version: {version}", nameof(version));
        }

        // Parse parameters from JSON
        var parametersDict = JsonSerializer.Deserialize<Dictionary<string, ParameterDefinition>>(parametersJson);
        if (parametersDict == null)
        {
            throw new ArgumentException("Invalid parameters JSON", nameof(parametersJson));
        }

        // Build JSON Schema
        var jsonSchema = schemaBuilder.BuildJsonSchema(parametersDict);
        var schemaString = schemaBuilder.SerializeSchema(jsonSchema);

        // Check if schema already exists for this configuration
        var existingSchema = await dbContext.ParameterSchemas
            .FirstOrDefaultAsync(s => s.ConfigurationId == configurationId, cancellationToken);

        if (existingSchema != null)
        {
            // Update existing schema
            existingSchema.GeneratedJsonSchema = schemaString;
            existingSchema.SchemaVersion = version;
            existingSchema.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            // Create new schema
            existingSchema = new ParameterSchema
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configurationId,
                GeneratedJsonSchema = schemaString,
                SchemaVersion = version,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            dbContext.ParameterSchemas.Add(existingSchema);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return existingSchema;
    }

    public async Task<ParameterSchema?> FindOrCreateSchemaAsync(Guid configurationId, string? schemaDefinition, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaDefinition))
        {
            return null;
        }

        // Try to find existing schema for this configuration
        var existingSchema = await dbContext.ParameterSchemas
            .FirstOrDefaultAsync(s => s.ConfigurationId == configurationId, cancellationToken);

        return existingSchema;
    }

    private static List<string> ExtractParameterNames(string? schemaDefinition)
    {
        if (string.IsNullOrWhiteSpace(schemaDefinition))
        {
            return [];
        }

        try
        {
            var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaDefinition);
            return obj?.Keys.ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static bool IsSemVerValid(string version)
    {
        return SemVerRegex().IsMatch(version);
    }

    private static (int Major, int Minor, int Patch) ParseSemVer(string version)
    {
        var match = SemVerRegex().Match(version);
        if (!match.Success)
        {
            return (0, 0, 0);
        }

        return (
            int.Parse(match.Groups["major"].Value),
            int.Parse(match.Groups["minor"].Value),
            int.Parse(match.Groups["patch"].Value)
        );
    }
}
