// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenDsc.Server.Services;

public partial class ParameterSchemaService(ServerDbContext dbContext) : IParameterSchemaService
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    [GeneratedRegex(@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$")]
    private static partial Regex SemVerRegex();

    public string CalculateSchemaHash(string schemaDefinition)
    {
        if (string.IsNullOrWhiteSpace(schemaDefinition))
        {
            return string.Empty;
        }

        var normalized = NormalizeSchemaDefinition(schemaDefinition);
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

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

        var oldHash = CalculateSchemaHash(oldSchemaDefinition);
        var newHash = CalculateSchemaHash(newSchemaDefinition);

        if (oldHash == newHash)
        {
            return new SchemaChanges(false, false, true, [], [], []);
        }

        var oldParams = ExtractParameterNames(oldSchemaDefinition);
        var newParams = ExtractParameterNames(newSchemaDefinition);

        var removed = oldParams.Except(newParams, StringComparer.OrdinalIgnoreCase).ToList();
        var added = newParams.Except(oldParams, StringComparer.OrdinalIgnoreCase).ToList();

        var hasBreaking = removed.Count > 0;
        var hasAdditive = added.Count > 0;

        return new SchemaChanges(hasBreaking, hasAdditive, false, removed, [], added);
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

    public async Task<ParameterSchema?> FindOrCreateSchemaAsync(Guid configurationId, string? schemaDefinition, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaDefinition))
        {
            return null;
        }

        var hash = CalculateSchemaHash(schemaDefinition);

        var existingSchema = await dbContext.ParameterSchemas
            .FirstOrDefaultAsync(s => s.ConfigurationId == configurationId && s.SchemaHash == hash, cancellationToken);

        if (existingSchema != null)
        {
            return existingSchema;
        }

        var newSchema = new ParameterSchema
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configurationId,
            SchemaHash = hash,
            SchemaDefinition = schemaDefinition,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.ParameterSchemas.Add(newSchema);
        await dbContext.SaveChangesAsync(cancellationToken);

        return newSchema;
    }

    private static string NormalizeSchemaDefinition(string schemaDefinition)
    {
        try
        {
            var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaDefinition);
            if (obj == null)
            {
                return schemaDefinition;
            }

            var sortedKeys = obj.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            var normalized = new Dictionary<string, object>();
            foreach (var key in sortedKeys)
            {
                normalized[key] = obj[key];
            }

            return JsonSerializer.Serialize(normalized, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            return schemaDefinition;
        }
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
