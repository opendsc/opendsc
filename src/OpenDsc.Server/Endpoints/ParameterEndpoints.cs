// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authentication;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Endpoints;

public static class ParameterEndpoints
{
    public static IEndpointRouteBuilder MapParameterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/parameters")
            .WithTags("Parameters")
            .RequireAuthorization(policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    PersonalAccessTokenHandler.SchemeName));

        group.MapPut("/{scopeTypeId:guid}/{configurationId:guid}", CreateOrUpdateParameter)
            .WithName("CreateOrUpdateParameter")
            .WithDescription("Create or update a parameter file for a scope type and configuration");

        group.MapGet("/{scopeTypeId:guid}/{configurationId:guid}/versions", GetParameterVersions)
            .WithName("GetParameterVersions")
            .WithDescription("Get all parameter file versions for a scope type and configuration");

        group.MapPut("/{scopeTypeId:guid}/{configurationId:guid}/versions/{version}/activate", ActivateParameterVersion)
            .WithName("ActivateParameterVersion")
            .WithDescription("Activate a specific parameter version");

        group.MapDelete("/{scopeTypeId:guid}/{configurationId:guid}/versions/{version}", DeleteParameterVersion)
            .WithName("DeleteParameterVersion")
            .WithDescription("Delete a parameter version (only if not active)");

        var nodeGroup = app.MapGroup("/api/v1/nodes/{nodeId:guid}/parameters")
            .WithTags("Parameters");

        nodeGroup.MapGet("/provenance", GetNodeParameterProvenance)
            .WithName("GetNodeParameterProvenance")
            .WithDescription("Get parameter provenance showing which scope provided each value");

        return app;
    }

    private static async Task<Results<Ok<ParameterFileDto>, BadRequest<string>, NotFound, ForbidHttpResult>> CreateOrUpdateParameter(
        Guid scopeTypeId,
        Guid configurationId,
        [FromBody] CreateParameterRequest request,
        ServerDbContext db,
        IConfiguration config,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var scopeType = await db.ScopeTypes.FindAsync(scopeTypeId);
        if (scopeType is null)
        {
            return TypedResults.NotFound();
        }

        var configuration = await db.Configurations.FindAsync(configurationId);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        // Find or create ParameterSchema to check permissions
        var parameterSchema = await db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configurationId);

        var userId = userContext.GetCurrentUserId();
        if (userId == null)
        {
            return TypedResults.Forbid();
        }

        // For existing parameter schemas, check Modify permission
        if (parameterSchema is not null)
        {
            if (!await authService.CanModifyParameterAsync(userId.Value, parameterSchema.Id))
            {
                return TypedResults.Forbid();
            }
        }
        else
        {
            // No parameter schema exists yet; require permission to modify the parent configuration
            if (!await authService.CanModifyConfigurationAsync(userId.Value, configurationId))
            {
                return TypedResults.Forbid();
            }
        }

        if (scopeType.AllowsValues && string.IsNullOrWhiteSpace(request.ScopeValue))
        {
            return TypedResults.BadRequest($"Scope type '{scopeType.Name}' requires a scope value");
        }

        if (!scopeType.AllowsValues && !string.IsNullOrWhiteSpace(request.ScopeValue))
        {
            return TypedResults.BadRequest($"Scope type '{scopeType.Name}' does not allow scope values");
        }

        if (scopeType.AllowsValues && !string.IsNullOrWhiteSpace(request.ScopeValue))
        {
            var scopeValueExists = await db.ScopeValues
                .AnyAsync(sv => sv.ScopeTypeId == scopeTypeId && sv.Value == request.ScopeValue);

            if (!scopeValueExists)
            {
                return TypedResults.BadRequest($"Scope value '{request.ScopeValue}' does not exist for scope type '{scopeType.Name}'");
            }
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return TypedResults.BadRequest("Parameter content is required");
        }

        var checksum = ComputeChecksum(request.Content);

        var existingVersion = await db.ParameterFiles
            .Include(pf => pf.ParameterSchema)
            .FirstOrDefaultAsync(pf =>
                pf.ScopeTypeId == scopeTypeId &&
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeValue == request.ScopeValue &&
                pf.Version == request.Version);

        var dataDir = config["DataDirectory"] ?? "data";
        var filePath = scopeType.AllowsValues && !string.IsNullOrWhiteSpace(request.ScopeValue)
            ? Path.Combine(dataDir, "parameters", configuration.Name, scopeType.Name, request.ScopeValue, "parameters.yaml")
            : Path.Combine(dataDir, "parameters", configuration.Name, scopeType.Name, "parameters.yaml");

        var fileDir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
        {
            Directory.CreateDirectory(fileDir);
        }
        await File.WriteAllTextAsync(filePath, request.Content);

        if (existingVersion is not null)
        {
            existingVersion.Checksum = checksum;
            existingVersion.ContentType = request.ContentType ?? "application/x-yaml";
            await db.SaveChangesAsync();

            return TypedResults.Ok(new ParameterFileDto
            {
                Id = existingVersion.Id,
                ScopeTypeId = existingVersion.ScopeTypeId,
                ConfigurationId = existingVersion.ParameterSchema!.ConfigurationId,
                ScopeValue = existingVersion.ScopeValue,
                Version = existingVersion.Version,
                Checksum = existingVersion.Checksum,
                IsActive = existingVersion.IsActive,
                IsDraft = existingVersion.IsDraft,
                CreatedAt = existingVersion.CreatedAt
            });
        }

        // Create ParameterSchema if it doesn't exist
        if (parameterSchema is null)
        {
            parameterSchema = new ParameterSchema
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configurationId,
                SchemaHash = string.Empty,
                SchemaDefinition = "{}",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.ParameterSchemas.Add(parameterSchema);
            await db.SaveChangesAsync();

            // Auto-grant Manage permission to creator
            await authService.GrantParameterPermissionAsync(
                parameterSchema.Id,
                userId.Value,
                PrincipalType.User,
                ResourcePermission.Manage,
                userId.Value);
        }

        var parameterFile = new ParameterFile
        {
            Id = Guid.NewGuid(),
            ScopeTypeId = scopeTypeId,
            ParameterSchemaId = parameterSchema.Id,
            ScopeValue = request.ScopeValue,
            Version = request.Version,
            ContentType = request.ContentType ?? "application/x-yaml",
            Checksum = checksum,
            IsDraft = request.IsDraft ?? true,
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ParameterFiles.Add(parameterFile);
        await db.SaveChangesAsync();

        return TypedResults.Ok(new ParameterFileDto
        {
            Id = parameterFile.Id,
            ScopeTypeId = parameterFile.ScopeTypeId,
            ConfigurationId = parameterFile.ParameterSchema!.ConfigurationId,
            ScopeValue = parameterFile.ScopeValue,
            Version = parameterFile.Version,
            Checksum = parameterFile.Checksum,
            IsActive = parameterFile.IsActive,
            IsDraft = parameterFile.IsDraft,
            CreatedAt = parameterFile.CreatedAt
        });
    }

    private static async Task<Results<Ok<List<ParameterFileDto>>, NotFound, ForbidHttpResult>> GetParameterVersions(
        Guid scopeTypeId,
        Guid configurationId,
        [FromQuery] string? scopeValue,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var scopeType = await db.ScopeTypes.FindAsync(scopeTypeId);
        if (scopeType is null)
        {
            return TypedResults.NotFound();
        }

        var parameterSchema = await db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configurationId);

        if (parameterSchema is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanReadParameterAsync(userId.Value, parameterSchema.Id))
        {
            return TypedResults.Forbid();
        }

        var versions = await db.ParameterFiles
            .Include(pf => pf.ParameterSchema)
            .Where(pf =>
                pf.ScopeTypeId == scopeTypeId &&
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeValue == scopeValue)
            .ToListAsync();

        var orderedVersions = versions
            .OrderByDescending(pf => pf.CreatedAt)
            .Select(pf => new ParameterFileDto
            {
                Id = pf.Id,
                ScopeTypeId = pf.ScopeTypeId,
                ConfigurationId = pf.ParameterSchema!.ConfigurationId,
                ScopeValue = pf.ScopeValue,
                Version = pf.Version,
                Checksum = pf.Checksum,
                IsActive = pf.IsActive,
                IsDraft = pf.IsDraft,
                CreatedAt = pf.CreatedAt
            })
            .ToList();

        return TypedResults.Ok(orderedVersions);
    }

    private static async Task<Results<Ok<ParameterFileDto>, NotFound, Conflict<string>, ForbidHttpResult>> ActivateParameterVersion(
        Guid scopeTypeId,
        Guid configurationId,
        string version,
        [FromQuery] string? scopeValue,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var parameterFile = await db.ParameterFiles
            .Include(pf => pf.ParameterSchema)
            .FirstOrDefaultAsync(pf =>
                pf.ScopeTypeId == scopeTypeId &&
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeValue == scopeValue &&
                pf.Version == version);

        if (parameterFile is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanModifyParameterAsync(userId.Value, parameterFile.ParameterSchemaId))
        {
            return TypedResults.Forbid();
        }

        var currentlyActive = await db.ParameterFiles
            .Include(pf => pf.ParameterSchema)
            .Where(pf =>
                pf.ScopeTypeId == scopeTypeId &&
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeValue == scopeValue &&
                pf.IsActive)
            .ToListAsync();

        foreach (var activeFile in currentlyActive)
        {
            activeFile.IsActive = false;
        }

        parameterFile.IsActive = true;
        parameterFile.IsDraft = false;
        await db.SaveChangesAsync();

        return TypedResults.Ok(new ParameterFileDto
        {
            Id = parameterFile.Id,
            ScopeTypeId = parameterFile.ScopeTypeId,
            ConfigurationId = parameterFile.ParameterSchema!.ConfigurationId,
            ScopeValue = parameterFile.ScopeValue,
            Version = parameterFile.Version,
            Checksum = parameterFile.Checksum,
            IsActive = parameterFile.IsActive,
            IsDraft = parameterFile.IsDraft,
            CreatedAt = parameterFile.CreatedAt
        });
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> DeleteParameterVersion(
        Guid scopeTypeId,
        Guid configurationId,
        string version,
        [FromQuery] string? scopeValue,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var parameterFile = await db.ParameterFiles
            .Include(pf => pf.ParameterSchema)
            .FirstOrDefaultAsync(pf =>
                pf.ScopeTypeId == scopeTypeId &&
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeValue == scopeValue &&
                pf.Version == version);

        if (parameterFile is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanManageParameterAsync(userId.Value, parameterFile.ParameterSchemaId))
        {
            return TypedResults.Forbid();
        }

        if (parameterFile.IsActive)
        {
            return TypedResults.Conflict("Cannot delete an active parameter version");
        }

        db.ParameterFiles.Remove(parameterFile);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ParameterProvenanceDto>, NotFound>> GetNodeParameterProvenance(
        Guid nodeId,
        [FromQuery] Guid? configurationId,
        ServerDbContext db,
        IParameterMerger merger,
        IConfiguration config)
    {
        var node = await db.Nodes.FindAsync(nodeId);
        if (node is null)
        {
            return TypedResults.NotFound();
        }

        if (!configurationId.HasValue)
        {
            var nodeConfig = await db.NodeConfigurations
                .Include(nc => nc.Configuration)
                .FirstOrDefaultAsync(nc => nc.NodeId == nodeId);

            if (nodeConfig is null)
            {
                return TypedResults.NotFound();
            }

            configurationId = nodeConfig.ConfigurationId;
        }

        var configuration = await db.Configurations.FindAsync([configurationId!.Value]);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var dataDir = config["DataDirectory"] ?? "data";
        var parameterSources = new List<ParameterSource>();

        var nodeTags = await db.NodeTags
            .Include(nt => nt.ScopeValue)
            .ThenInclude(sv => sv.ScopeType)
            .Where(nt => nt.NodeId == nodeId)
            .OrderBy(nt => nt.ScopeValue.ScopeType.Precedence)
            .ToListAsync();

        var scopeTypes = new HashSet<Guid>(nodeTags.Select(nt => nt.ScopeValue.ScopeTypeId));

        var defaultScopeType = await db.ScopeTypes
            .FirstOrDefaultAsync(st => st.Name == "Default");

        if (defaultScopeType != null && !scopeTypes.Contains(defaultScopeType.Id))
        {
            var defaultParamFile = await db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                .FirstOrDefaultAsync(pf =>
                    pf.ParameterSchema!.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == defaultScopeType.Id &&
                    pf.IsActive);

            if (defaultParamFile != null)
            {
                var defaultPath = Path.Combine(dataDir, "parameters", configuration.Name, "Default", "parameters.yaml");

                if (File.Exists(defaultPath))
                {
                    var content = await File.ReadAllTextAsync(defaultPath);
                    parameterSources.Add(new ParameterSource
                    {
                        ScopeTypeName = "Default",
                        ScopeValue = null,
                        Precedence = defaultScopeType.Precedence,
                        Content = content
                    });
                }
            }
        }

        foreach (var tag in nodeTags)
        {
            var paramFile = await db.ParameterFiles
                .Where(pf =>
                    pf.ParameterSchema!.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == tag.ScopeValue.ScopeTypeId &&
                    pf.ScopeValue == tag.ScopeValue.Value &&
                    pf.IsActive)
                .FirstOrDefaultAsync();

            if (paramFile is null)
            {
                continue;
            }

            var filePath = Path.Combine(dataDir, "parameters", configuration.Name, tag.ScopeValue.ScopeType.Name, tag.ScopeValue.Value, "parameters.yaml");

            if (!File.Exists(filePath))
            {
                continue;
            }

            var content = await File.ReadAllTextAsync(filePath);
            parameterSources.Add(new ParameterSource
            {
                ScopeTypeName = tag.ScopeValue.ScopeType.Name,
                ScopeValue = tag.ScopeValue.Value,
                Precedence = tag.ScopeValue.ScopeType.Precedence,
                Content = content
            });
        }

        var nodeScopeType = await db.ScopeTypes
            .FirstOrDefaultAsync(st => st.Name == "Node");

        if (nodeScopeType != null)
        {
            var nodeParamFile = await db.ParameterFiles
                .Where(pf =>
                    pf.ParameterSchema!.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == nodeScopeType.Id &&
                    pf.ScopeValue == node.Fqdn &&
                    pf.IsActive)
                .FirstOrDefaultAsync();

            if (nodeParamFile != null)
            {
                var nodePath = Path.Combine(dataDir, "parameters", configuration.Name, "Node", node.Fqdn, "parameters.yaml");

                if (File.Exists(nodePath))
                {
                    var content = await File.ReadAllTextAsync(nodePath);
                    parameterSources.Add(new ParameterSource
                    {
                        ScopeTypeName = "Node",
                        ScopeValue = node.Fqdn,
                        Precedence = nodeScopeType.Precedence,
                        Content = content
                    });
                }
            }
        }

        if (parameterSources.Count == 0)
        {
            return TypedResults.Ok(new ParameterProvenanceDto
            {
                NodeId = nodeId,
                ConfigurationId = configurationId.Value,
                MergedParameters = "{}",
                Provenance = new Dictionary<string, ParameterSourceInfo>()
            });
        }

        var result = merger.MergeWithProvenance(parameterSources);

        var provenance = result.Provenance.ToDictionary(
            kvp => kvp.Key,
            kvp => new ParameterSourceInfo
            {
                ScopeTypeName = kvp.Value.ScopeTypeName,
                ScopeValue = kvp.Value.ScopeValue,
                Precedence = kvp.Value.Precedence,
                Value = kvp.Value.Value,
                OverriddenBy = kvp.Value.OverriddenValues?.Select(ov => new ScopeInfo
                {
                    ScopeTypeName = ov.ScopeTypeName,
                    ScopeValue = ov.ScopeValue,
                    Precedence = ov.Precedence,
                    Value = ov.Value
                }).ToList()
            });

        return TypedResults.Ok(new ParameterProvenanceDto
        {
            NodeId = nodeId,
            ConfigurationId = configurationId.Value,
            MergedParameters = result.MergedContent,
            Provenance = provenance
        });
    }

    private static string ComputeChecksum(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class ParameterFileDto
{
    public required Guid Id { get; init; }
    public required Guid ScopeTypeId { get; init; }
    public required Guid ConfigurationId { get; init; }
    public string? ScopeValue { get; init; }
    public required string Version { get; init; }
    public required string Checksum { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsDraft { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateParameterRequest
{
    public string? ScopeValue { get; init; }
    public required string Version { get; init; }
    public required string Content { get; init; }
    public string? ContentType { get; init; }
    public bool? IsDraft { get; init; }
}

public sealed class ParameterProvenanceDto
{
    public required Guid NodeId { get; init; }
    public required Guid ConfigurationId { get; init; }
    public required string MergedParameters { get; init; }
    public required Dictionary<string, ParameterSourceInfo> Provenance { get; init; }
}

public sealed class ParameterSourceInfo
{
    public required string ScopeTypeName { get; init; }
    public string? ScopeValue { get; init; }
    public required int Precedence { get; init; }
    public required object? Value { get; init; }
    public List<ScopeInfo>? OverriddenBy { get; init; }
}

public sealed class ScopeInfo
{
    public required string ScopeTypeName { get; init; }
    public string? ScopeValue { get; init; }
    public required int Precedence { get; init; }
    public required object? Value { get; init; }
}
