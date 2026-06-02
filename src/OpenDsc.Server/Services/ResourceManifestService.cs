// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.ResourceManifests;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

using YamlDotNet.Serialization;

namespace OpenDsc.Server.Services;

public sealed class ResourceManifestService(
    ServerDbContext db,
    ILogger<ResourceManifestService> logger,
    IJsonYamlConverter jsonYamlConverter) : IResourceManifestService
{
    public async Task<IReadOnlyList<ResourceManifestSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var manifestData = await db.ResourceManifests
            .AsNoTracking()
            .OrderBy(m => m.TypeName)
            .Select(m => new
            {
                m.Id,
                m.TypeName,
                m.Description,
                m.Kind,
                VersionCount = m.Versions.Count,
                m.CreatedAt,
                m.UpdatedAt,
                m.Versions
            })
            .ToListAsync(cancellationToken);

        return manifestData
            .Select(m => new ResourceManifestSummary
            {
                Id = m.Id,
                TypeName = m.TypeName,
                Description = m.Description,
                Kind = m.Kind,
                VersionCount = m.VersionCount,
                Tags = m.Versions
                    .Where(v => !string.IsNullOrEmpty(v.TagsJson))
                    .SelectMany(v => JsonSerializer.Deserialize<string[]>(v.TagsJson!) ?? Array.Empty<string>())
                    .Distinct()
                    .ToArray(),
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            })
            .ToList();
    }

    public async Task<ResourceManifestDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var manifest = await db.ResourceManifests
            .AsNoTracking()
            .Include(m => m.Versions)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        return manifest is null ? null : MapToDetails(manifest);
    }

    public async Task<ResourceManifestDetails?> GetByTypeNameAsync(string typeName, CancellationToken cancellationToken = default)
    {
        var manifest = await db.ResourceManifests
            .AsNoTracking()
            .Include(m => m.Versions)
            .FirstOrDefaultAsync(m => m.TypeName == typeName, cancellationToken);

        return manifest is null ? null : MapToDetails(manifest);
    }

    public async Task<ResourceManifestVersionDetails?> GetVersionDetailsAsync(Guid manifestId, string version, CancellationToken cancellationToken = default)
    {
        var v = await db.ResourceManifestVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ManifestId == manifestId && x.Version == version, cancellationToken);

        return v is null ? null : MapToVersionDetails(v);
    }

    public async Task<ResourceManifestVersionDetails?> GetVersionDetailsByIdAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        var v = await db.ResourceManifestVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken);

        return v is null ? null : MapToVersionDetails(v);
    }

    public async Task<ResourceManifestVersionDetails> ImportAsync(
        string manifestJson,
        bool isBuiltIn = false,
        CancellationToken cancellationToken = default)
    {
        var parsed = ParseManifest(manifestJson);

        var now = DateTimeOffset.UtcNow;

        var manifest = await db.ResourceManifests
            .FirstOrDefaultAsync(m => m.TypeName == parsed.TypeName, cancellationToken);

        if (manifest is null)
        {
            manifest = new ResourceManifest
            {
                Id = Guid.NewGuid(),
                TypeName = parsed.TypeName,
                Description = parsed.Description,
                Kind = parsed.Kind,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.ResourceManifests.Add(manifest);
        }
        else
        {
            manifest.Description = parsed.Description;
            manifest.Kind = parsed.Kind;
            manifest.UpdatedAt = now;
        }

        var existing = await db.ResourceManifestVersions
            .FirstOrDefaultAsync(v => v.ManifestId == manifest.Id && v.Version == parsed.Version, cancellationToken);

        if (existing is not null)
        {
            existing.ManifestJson = manifestJson;
            existing.InstanceSchemaJson = parsed.InstanceSchemaJson;
            existing.TagsJson = parsed.TagsJson;
            existing.IsBuiltIn = isBuiltIn;
        }
        else
        {
            existing = new ResourceManifestVersion
            {
                Id = Guid.NewGuid(),
                ManifestId = manifest.Id,
                Version = parsed.Version,
                ManifestJson = manifestJson,
                InstanceSchemaJson = parsed.InstanceSchemaJson,
                TagsJson = parsed.TagsJson,
                IsBuiltIn = isBuiltIn,
                ImportedAt = now
            };
            db.ResourceManifestVersions.Add(existing);
        }

        await db.SaveChangesAsync(cancellationToken);

        return MapToVersionDetails(existing);
    }

    public async Task<int> ImportManyAsync(string content, string? fileName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        var isYaml = fileName is not null &&
            (fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
             fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));

        // Convert YAML to JSON first
        string jsonContent;
        if (isYaml)
        {
            var deserializer = new DeserializerBuilder().Build();
            var serializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();
            var yamlObject = deserializer.Deserialize(content);
            jsonContent = serializer.Serialize(yamlObject);
        }
        else
        {
            jsonContent = content;
        }

        // Collect individual manifest JSON strings from two formats:
        // 1. Object with resources array: { "resources": [...] }
        // 2. Single manifest object
        var manifests = new List<string>();

        try
        {
            var parsed = JsonNode.Parse(jsonContent);

            if (parsed is JsonObject obj)
            {
                // Check for Format 1: { "resources": [...] }
                if (obj.TryGetPropertyValue("resources", out var resourcesNode) && resourcesNode is JsonArray resourcesArray)
                {
                    foreach (var item in resourcesArray)
                    {
                        if (item is not null)
                            manifests.Add(item.ToJsonString());
                    }
                }
                else
                {
                    // Format 2: Single manifest object
                    manifests.Add(jsonContent.Trim());
                }
            }
            else
            {
                throw new ArgumentException("Content must be a JSON manifest object or object with 'resources' array.");
            }
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON format.", ex);
        }

        if (manifests.Count == 0)
        {
            throw new ArgumentException("No manifests found in the provided content.");
        }

        int imported = 0;
        var errors = new List<string>();
        foreach (var manifestJson in manifests)
        {
            // Each entry may be a resource list entry with a nested "manifest" property,
            // or a direct manifest JSON object.
            string targetJson = manifestJson;
            try
            {
                var node = JsonNode.Parse(manifestJson);
                var nested = node?["manifest"];
                if (nested is not null)
                    targetJson = nested.ToJsonString();
            }
            catch (JsonException) { }

            try
            {
                await ImportAsync(targetJson, isBuiltIn: false, cancellationToken);
                imported++;
            }
            catch (ArgumentException ex)
            {
                errors.Add(ex.Message);
                logger.LogDebug(ex, "Skipping manifest entry that could not be parsed.");
            }
        }

        if (imported == 0 && errors.Count > 0)
            throw new ArgumentException(errors[0]);

        return imported;
    }

    public async Task<DiscoveryResult> DiscoverFromDscCliAsync(CancellationToken cancellationToken = default)
    {
        string rawOutput;
        try
        {
            rawOutput = await RunDscResourceListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to run 'dsc resource list'. DSC may not be installed.");
            throw new InvalidOperationException("Failed to run 'dsc resource list'. Ensure DSC is installed and on PATH.", ex);
        }

        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            return new DiscoveryResult { Discovered = 0, Imported = 0, Updated = 0, Failed = 0 };
        }

        // dsc resource list outputs one JSON object per line (NDJSON)
        var lines = rawOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        int imported = 0;
        int updated = 0;
        int failed = 0;
        int discovered = 0;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            discovered++;

            // Each line is a resource summary object; the manifest JSON is embedded in the "manifest" property
            JsonNode? lineNode;
            try
            {
                lineNode = JsonNode.Parse(line);
            }
            catch (JsonException)
            {
                logger.LogDebug("Skipping non-JSON line from dsc resource list output.");
                failed++;
                continue;
            }

            // Try to extract the embedded manifest JSON
            var manifestNode = lineNode?["manifest"];
            string manifestJson;
            if (manifestNode is not null)
            {
                manifestJson = manifestNode.ToJsonString();
            }
            else
            {
                // Fall back to the resource list entry itself if no embedded manifest
                manifestJson = line;
            }

            try
            {
                var outcome = await ImportAndTrackAsync(manifestJson, cancellationToken);
                if (outcome == ImportOutcome.New)
                    imported++;
                else if (outcome == ImportOutcome.Updated)
                    updated++;
                // Unchanged outcomes don't increment any counter
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Skipping manifest entry that could not be parsed.");
                failed++;
            }
        }

        return new DiscoveryResult
        {
            Discovered = discovered,
            Imported = imported,
            Updated = updated,
            Failed = failed
        };
    }

    private enum ImportOutcome { New, Updated, Unchanged }

    private async Task<ImportOutcome> ImportAndTrackAsync(string manifestJson, CancellationToken cancellationToken)
    {
        var parsed = ParseManifest(manifestJson);

        var manifest = await db.ResourceManifests
            .FirstOrDefaultAsync(m => m.TypeName == parsed.TypeName, cancellationToken);

        bool isNewManifest = manifest is null;

        if (manifest is null)
        {
            manifest = new ResourceManifest
            {
                Id = Guid.NewGuid(),
                TypeName = parsed.TypeName,
                Description = parsed.Description,
                Kind = parsed.Kind,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.ResourceManifests.Add(manifest);
        }
        else
        {
            manifest.Description = parsed.Description;
            manifest.Kind = parsed.Kind;
            manifest.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var existing = await db.ResourceManifestVersions
            .FirstOrDefaultAsync(v => v.ManifestId == manifest.Id && v.Version == parsed.Version, cancellationToken);

        if (existing is null)
        {
            // New version
            var newVersion = new ResourceManifestVersion
            {
                Id = Guid.NewGuid(),
                ManifestId = manifest.Id,
                Version = parsed.Version,
                ManifestJson = manifestJson,
                InstanceSchemaJson = parsed.InstanceSchemaJson,
                TagsJson = parsed.TagsJson,
                IsBuiltIn = true,
                ImportedAt = DateTimeOffset.UtcNow
            };
            db.ResourceManifestVersions.Add(newVersion);
            await db.SaveChangesAsync(cancellationToken);
            return ImportOutcome.New;
        }

        // Existing version - check if content has actually changed
        // Normalize both JSON strings to canonical form for proper comparison
        bool contentChanged = HasManifestContentChanged(existing.ManifestJson, manifestJson) ||
                            HasManifestContentChanged(existing.InstanceSchemaJson, parsed.InstanceSchemaJson) ||
                            existing.TagsJson != parsed.TagsJson;

        if (contentChanged)
        {
            existing.ManifestJson = manifestJson;
            existing.InstanceSchemaJson = parsed.InstanceSchemaJson;
            existing.TagsJson = parsed.TagsJson;
            existing.IsBuiltIn = true;
            await db.SaveChangesAsync(cancellationToken);
            return ImportOutcome.Updated;
        }

        return ImportOutcome.Unchanged;
    }

    private static bool HasManifestContentChanged(string? existing, string? incoming)
    {
        // If both are null or empty, no change
        if (string.IsNullOrWhiteSpace(existing) && string.IsNullOrWhiteSpace(incoming))
            return false;

        // If one is null/empty and the other isn't, there's a change
        if (string.IsNullOrWhiteSpace(existing) != string.IsNullOrWhiteSpace(incoming))
            return true;

        try
        {
            // Both are non-null at this point
            // Parse both JSON strings and compare the parsed structures
            var existingNode = JsonNode.Parse(existing!);
            var incomingNode = JsonNode.Parse(incoming!);

            return !JsonNode.DeepEquals(existingNode, incomingNode);
        }
        catch
        {
            // If parsing fails, fall back to string comparison
            return existing != incoming;
        }
    }

    public async Task DeleteManifestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var manifest = await db.ResourceManifests.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException($"Resource manifest '{id}' not found.");

        db.ResourceManifests.Remove(manifest);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await db.ResourceManifestVersions.FindAsync([versionId], cancellationToken)
            ?? throw new KeyNotFoundException($"Resource manifest version '{versionId}' not found.");

        db.ResourceManifestVersions.Remove(version);
        await db.SaveChangesAsync(cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static async Task<string> RunDscResourceListAsync(CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "dsc",
            Arguments = "resource list",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return output;
    }

    public async Task<string?> GetResourceSchemaAsync(string typeName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "dsc",
                Arguments = $"resource schema -r {typeName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // The output is YAML, convert it to JSON for storage
                var jsonSchema = jsonYamlConverter.ConvertYamlToJson(output);
                return !string.IsNullOrWhiteSpace(jsonSchema) ? jsonSchema : null;
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch schema for resource '{TypeName}'", typeName);
            return null;
        }
    }

    private sealed record ParsedManifest(
        string TypeName,
        string Version,
        string? Description,
        string Kind,
        string? InstanceSchemaJson,
        string? TagsJson);

    private static ParsedManifest ParseManifest(string manifestJson)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(manifestJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid manifest JSON.", nameof(manifestJson), ex);
        }

        using (doc)
        {
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
            {
                throw new ArgumentException("Manifest JSON must contain a 'type' property.", nameof(manifestJson));
            }

            if (!root.TryGetProperty("version", out var versionEl) || versionEl.ValueKind != JsonValueKind.String)
            {
                throw new ArgumentException("Manifest JSON must contain a 'version' property.", nameof(manifestJson));
            }

            var typeName = typeEl.GetString()!;
            var version = versionEl.GetString()!;

            string? description = null;
            if (root.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String)
            {
                description = descEl.GetString();
            }

            var kind = "resource";
            if (root.TryGetProperty("kind", out var kindEl) && kindEl.ValueKind == JsonValueKind.String)
            {
                kind = kindEl.GetString() ?? "resource";
            }

            // Extract embedded instance schema if present
            string? instanceSchemaJson = null;
            if (root.TryGetProperty("schema", out var schemaEl))
            {
                if (schemaEl.TryGetProperty("embedded", out var embeddedEl))
                {
                    instanceSchemaJson = embeddedEl.GetRawText();
                }
            }

            // Extract tags
            string? tagsJson = null;
            if (root.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
            {
                tagsJson = tagsEl.GetRawText();
            }

            return new ParsedManifest(typeName, version, description, kind, instanceSchemaJson, tagsJson);
        }
    }

    private static ResourceManifestDetails MapToDetails(ResourceManifest manifest)
    {
        return new ResourceManifestDetails
        {
            Id = manifest.Id,
            TypeName = manifest.TypeName,
            Description = manifest.Description,
            Kind = manifest.Kind,
            CreatedAt = manifest.CreatedAt,
            UpdatedAt = manifest.UpdatedAt,
            Versions = manifest.Versions
                .OrderByDescending(v => v.ImportedAt)
                .Select(v => new ResourceManifestVersionSummary
                {
                    Id = v.Id,
                    Version = v.Version,
                    IsBuiltIn = v.IsBuiltIn,
                    Tags = ParseTags(v.TagsJson),
                    ImportedAt = v.ImportedAt
                })
                .ToList()
        };
    }

    private static ResourceManifestVersionDetails MapToVersionDetails(ResourceManifestVersion v)
    {
        return new ResourceManifestVersionDetails
        {
            Id = v.Id,
            Version = v.Version,
            ManifestJson = v.ManifestJson,
            InstanceSchemaJson = v.InstanceSchemaJson,
            IsBuiltIn = v.IsBuiltIn,
            Tags = ParseTags(v.TagsJson),
            ImportedAt = v.ImportedAt
        };
    }

    private static string[]? ParseTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(tagsJson);
        }
        catch
        {
            return null;
        }
    }
}
