// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Parameters;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Endpoints;

public static class ParameterEndpoints
{
    public static IEndpointRouteBuilder MapParameterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/parameters")
            .WithTags("Parameters")
            .RequireAuthorization("Admin");

        group.MapPut("/{scopeName}/{configurationName}", CreateOrUpdateParameter)
            .WithName("CreateOrUpdateParameter")
            .WithDescription("Create or update a parameter version for a scope and configuration");

        group.MapGet("/{scopeName}/{configurationName}/versions", GetParameterVersions)
            .WithName("GetParameterVersions")
            .WithDescription("Get all parameter versions for a scope and configuration");

        group.MapPut("/{scopeName}/{configurationName}/versions/{version}/activate", ActivateParameterVersion)
            .WithName("ActivateParameterVersion")
            .WithDescription("Activate a specific parameter version");

        group.MapDelete("/{scopeName}/{configurationName}/versions/{version}", DeleteParameterVersion)
            .WithName("DeleteParameterVersion")
            .WithDescription("Delete a parameter version (only if not active)");

        var nodeGroup = app.MapGroup("/api/v1/nodes/{nodeId:guid}/parameters")
            .WithTags("Parameters");

        nodeGroup.MapGet("/provenance", GetNodeParameterProvenance)
            .WithName("GetNodeParameterProvenance")
            .WithDescription("Get parameter provenance showing which scope provided each value");

        return app;
    }

    private static async Task<Results<Ok<ParameterVersionDto>, BadRequest<string>, NotFound>> CreateOrUpdateParameter(
        string scopeName,
        string configurationName,
        [FromBody] CreateParameterRequest request,
        ServerDbContext db,
        IConfiguration config)
    {
        var scope = await db.Scopes.FirstOrDefaultAsync(s => s.Name == scopeName);
        if (scope is null)
        {
            return TypedResults.NotFound();
        }

        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configurationName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return TypedResults.BadRequest("Parameter content is required");
        }

        var checksum = ComputeChecksum(request.Content);

        var existingVersion = await db.ParameterVersions
            .FirstOrDefaultAsync(p => p.ScopeId == scope.Id
                && p.ConfigurationId == configuration.Id
                && p.Version == request.Version);

        if (existingVersion is not null)
        {
            existingVersion.Checksum = checksum;
            existingVersion.ContentType = request.ContentType ?? "application/x-yaml";

            var dataDir = config["DataDirectory"] ?? "data";
            var filePath = Path.Combine(dataDir, "parameters", scopeName, configurationName, $"v{request.Version}", "parameters.yaml");
            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }
            await File.WriteAllTextAsync(filePath, request.Content);

            await db.SaveChangesAsync();

            return TypedResults.Ok(new ParameterVersionDto
            {
                ScopeName = scopeName,
                ConfigurationName = configurationName,
                Version = existingVersion.Version,
                ContentType = existingVersion.ContentType,
                IsDraft = existingVersion.IsDraft,
                IsActive = existingVersion.IsActive,
                CreatedAt = existingVersion.CreatedAt
            });
        }

        var parameterVersion = new ParameterVersion
        {
            Id = Guid.NewGuid(),
            ScopeId = scope.Id,
            ConfigurationId = configuration.Id,
            Version = request.Version,
            ContentType = request.ContentType ?? "application/x-yaml",
            Checksum = checksum,
            IsDraft = request.IsDraft,
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ParameterVersions.Add(parameterVersion);

        var dataDirectory = config["DataDirectory"] ?? "data";
        var paramFilePath = Path.Combine(dataDirectory, "parameters", scopeName, configurationName, $"v{request.Version}", "parameters.yaml");
        var paramFileDir = Path.GetDirectoryName(paramFilePath);
        if (!string.IsNullOrEmpty(paramFileDir) && !Directory.Exists(paramFileDir))
        {
            Directory.CreateDirectory(paramFileDir);
        }
        await File.WriteAllTextAsync(paramFilePath, request.Content);

        await db.SaveChangesAsync();

        return TypedResults.Ok(new ParameterVersionDto
        {
            ScopeName = scopeName,
            ConfigurationName = configurationName,
            Version = parameterVersion.Version,
            ContentType = parameterVersion.ContentType,
            IsDraft = parameterVersion.IsDraft,
            IsActive = parameterVersion.IsActive,
            CreatedAt = parameterVersion.CreatedAt
        });
    }

    private static async Task<Results<Ok<List<ParameterVersionDto>>, NotFound>> GetParameterVersions(
        string scopeName,
        string configurationName,
        ServerDbContext db)
    {
        var scope = await db.Scopes.FirstOrDefaultAsync(s => s.Name == scopeName);
        if (scope is null)
        {
            return TypedResults.NotFound();
        }

        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configurationName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var versions = await db.ParameterVersions
            .Where(p => p.ScopeId == scope.Id && p.ConfigurationId == configuration.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ParameterVersionDto
            {
                ScopeName = scopeName,
                ConfigurationName = configurationName,
                Version = p.Version,
                ContentType = p.ContentType,
                IsDraft = p.IsDraft,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return TypedResults.Ok(versions);
    }

    private static async Task<Results<Ok<ParameterVersionDto>, NotFound, BadRequest<string>>> ActivateParameterVersion(
        string scopeName,
        string configurationName,
        string version,
        ServerDbContext db)
    {
        var scope = await db.Scopes.FirstOrDefaultAsync(s => s.Name == scopeName);
        if (scope is null)
        {
            return TypedResults.NotFound();
        }

        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configurationName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var parameterVersion = await db.ParameterVersions
            .FirstOrDefaultAsync(p => p.ScopeId == scope.Id
                && p.ConfigurationId == configuration.Id
                && p.Version == version);

        if (parameterVersion is null)
        {
            return TypedResults.NotFound();
        }

        if (parameterVersion.IsDraft)
        {
            return TypedResults.BadRequest("Cannot activate a draft parameter version. Publish it first.");
        }

        var currentActive = await db.ParameterVersions
            .Where(p => p.ScopeId == scope.Id
                && p.ConfigurationId == configuration.Id
                && p.IsActive)
            .ToListAsync();

        foreach (var active in currentActive)
        {
            active.IsActive = false;
        }

        parameterVersion.IsActive = true;
        await db.SaveChangesAsync();

        return TypedResults.Ok(new ParameterVersionDto
        {
            ScopeName = scopeName,
            ConfigurationName = configurationName,
            Version = parameterVersion.Version,
            ContentType = parameterVersion.ContentType,
            IsDraft = parameterVersion.IsDraft,
            IsActive = parameterVersion.IsActive,
            CreatedAt = parameterVersion.CreatedAt
        });
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>>> DeleteParameterVersion(
        string scopeName,
        string configurationName,
        string version,
        ServerDbContext db,
        IConfiguration config)
    {
        var scope = await db.Scopes.FirstOrDefaultAsync(s => s.Name == scopeName);
        if (scope is null)
        {
            return TypedResults.NotFound();
        }

        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configurationName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var parameterVersion = await db.ParameterVersions
            .FirstOrDefaultAsync(p => p.ScopeId == scope.Id
                && p.ConfigurationId == configuration.Id
                && p.Version == version);

        if (parameterVersion is null)
        {
            return TypedResults.NotFound();
        }

        if (parameterVersion.IsActive)
        {
            return TypedResults.Conflict("Cannot delete an active parameter version. Deactivate it first.");
        }

        var dataDir = config["DataDirectory"] ?? "data";
        var filePath = Path.Combine(dataDir, "parameters", scopeName, configurationName, $"v{version}");
        if (Directory.Exists(filePath))
        {
            Directory.Delete(filePath, true);
        }

        db.ParameterVersions.Remove(parameterVersion);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ParameterProvenanceDto>, NotFound>> GetNodeParameterProvenance(
        Guid nodeId,
        [FromQuery] string? configurationName,
        ServerDbContext db,
        IConfiguration config,
        IParameterMerger merger)
    {
        var node = await db.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId);
        if (node is null)
        {
            return TypedResults.NotFound();
        }

        var scopeAssignments = await db.NodeScopeAssignments
            .Include(nsa => nsa.Scope)
            .Where(nsa => nsa.NodeId == nodeId)
            .OrderBy(nsa => nsa.Scope.Precedence)
            .Select(nsa => nsa.Scope)
            .ToListAsync();

        Configuration? configuration = null;
        if (!string.IsNullOrWhiteSpace(configurationName))
        {
            configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configurationName);
            if (configuration is null)
            {
                return TypedResults.NotFound();
            }
        }

        var parameterSources = new List<ParameterSource>();
        var dataDir = config["DataDirectory"] ?? "data";

        foreach (var scope in scopeAssignments)
        {
            var query = db.ParameterVersions
                .Where(p => p.ScopeId == scope.Id && p.IsActive);

            if (configuration is not null)
            {
                query = query.Where(p => p.ConfigurationId == configuration.Id);
            }

            var activeVersions = await query.ToListAsync();

            foreach (var version in activeVersions)
            {
                var filePath = Path.Combine(dataDir, "parameters", scope.Name,
                    configuration?.Name ?? "unknown", $"v{version.Version}", "parameters.yaml");

                if (File.Exists(filePath))
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    parameterSources.Add(new ParameterSource
                    {
                        ScopeName = scope.Name,
                        Precedence = scope.Precedence,
                        Content = content
                    });
                }
            }
        }

        var mergeResult = merger.MergeWithProvenance(parameterSources);

        return TypedResults.Ok(new ParameterProvenanceDto
        {
            NodeId = nodeId,
            ConfigurationName = configurationName,
            MergedContent = mergeResult.MergedContent,
            Provenance = mergeResult.Provenance.ToDictionary(
                kvp => kvp.Key,
                kvp => new ProvenanceInfo
                {
                    ScopeName = kvp.Value.ScopeName,
                    Precedence = kvp.Value.Precedence,
                    Value = kvp.Value.Value,
                    OverriddenBy = kvp.Value.OverriddenValues?.Select(sv => new ScopeValueInfo
                    {
                        ScopeName = sv.ScopeName,
                        Precedence = sv.Precedence,
                        Value = sv.Value
                    }).ToList()
                })
        });
    }

    private static string ComputeChecksum(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class ParameterVersionDto
{
    public required string ScopeName { get; init; }
    public required string ConfigurationName { get; init; }
    public required string Version { get; init; }
    public string? ContentType { get; init; }
    public required bool IsDraft { get; init; }
    public required bool IsActive { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateParameterRequest
{
    public required string Version { get; init; }
    public required string Content { get; init; }
    public string? ContentType { get; init; }
    public bool IsDraft { get; init; } = true;
}

public sealed class ParameterProvenanceDto
{
    public required Guid NodeId { get; init; }
    public string? ConfigurationName { get; init; }
    public required string MergedContent { get; init; }
    public required Dictionary<string, ProvenanceInfo> Provenance { get; init; }
}

public sealed class ProvenanceInfo
{
    public required string ScopeName { get; init; }
    public required int Precedence { get; init; }
    public required object? Value { get; init; }
    public List<ScopeValueInfo>? OverriddenBy { get; init; }
}

public sealed class ScopeValueInfo
{
    public required string ScopeName { get; init; }
    public required int Precedence { get; init; }
    public required object? Value { get; init; }
}
