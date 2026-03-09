// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NuGet.Versioning;

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

        group.MapPut("/{scopeTypeId:guid}/{configurationId:guid}/versions/{version}/publish", PublishParameterVersion)
            .WithName("PublishParameterVersion")
            .WithDescription("Publish a specific parameter version");

        group.MapDelete("/{scopeTypeId:guid}/{configurationId:guid}/versions/{version}", DeleteParameterVersion)
            .WithName("DeleteParameterVersion")
            .WithDescription("Delete a parameter version (only if not active)");

        group.MapGet("/{scopeTypeId:guid}/{configurationId:guid}/majors", GetMajorVersions)
            .WithName("GetMajorVersions")
            .WithDescription("Get all major versions with parameter files");

        group.MapGet("/{scopeTypeId:guid}/{configurationId:guid}/majors/{major:int}", GetActiveParameterForMajor)
            .WithName("GetActiveParameterForMajor")
            .WithDescription("Get active parameter file for a specific major version");

        var nodeGroup = app.MapGroup("/api/v1/nodes/{nodeId:guid}/parameters")
            .WithTags("Parameters");

        nodeGroup.MapGet("/provenance", GetNodeParameterProvenance)
            .WithName("GetNodeParameterProvenance")
            .WithDescription("Get parameter provenance showing which scope provided each value");

        var configGroup = app.MapGroup("/api/v1/configurations/{configurationName}/parameters")
            .WithTags("Parameters")
            .RequireAuthorization(policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    PersonalAccessTokenHandler.SchemeName));

        configGroup.MapPut("", UploadParameterSchema)
            .WithName("UploadParameterSchema")
            .WithDescription("Upload a parameter schema for a configuration")
            .DisableAntiforgery();

        configGroup.MapPost("/validate", ValidateParameterFile)
            .WithName("ValidateParameterFile")
            .WithDescription("Validate a parameter file against a schema version");

        configGroup.MapPost("/parameter-files", UploadParameterFile)
            .WithName("UploadParameterFile")
            .WithDescription("Upload a parameter file for a specific scope")
            .DisableAntiforgery();

        return app;
    }

    private static async Task<Results<Ok<ParameterFileDto>, BadRequest<string>, NotFound, ForbidHttpResult>> CreateOrUpdateParameter(
        Guid scopeTypeId,
        Guid configurationId,
        [FromBody] CreateParameterRequest request,
        ServerDbContext db,
        IConfiguration config,
        IResourceAuthorizationService authService,
        IUserContextService userContext,
        IParameterValidator parameterValidator)
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

        if (!SemanticVersion.TryParse(request.Version, out var semVer))
        {
            return TypedResults.BadRequest($"Version '{request.Version}' is not a valid semantic version");
        }

        var majorVersion = semVer.Major;

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

            if (request.IsPassthrough != true && !string.IsNullOrWhiteSpace(parameterSchema.GeneratedJsonSchema))
            {
                var validationResult = parameterValidator.Validate(parameterSchema.GeneratedJsonSchema, request.Content ?? string.Empty);
                if (!validationResult.IsValid)
                {
                    var errorMessages = string.Join("; ", validationResult.Errors?.Select(e => $"{e.Path}: {e.Message}") ?? []);
                    return TypedResults.BadRequest($"Parameter validation failed: {errorMessages}");
                }
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

        if (scopeType.Name == "Default")
        {
            if (!string.IsNullOrWhiteSpace(request.ScopeValue))
            {
                return TypedResults.BadRequest("The 'Default' scope type does not accept a scope value.");
            }
        }
        else if (scopeType.Name == "Node")
        {
            if (string.IsNullOrWhiteSpace(request.ScopeValue))
            {
                return TypedResults.BadRequest("Scope type 'Node' requires a node FQDN as the scope value.");
            }

            var nodeExists = await db.Nodes.AnyAsync(n => n.Fqdn == request.ScopeValue);
            if (!nodeExists)
            {
                return TypedResults.BadRequest($"Node '{request.ScopeValue}' is not registered.");
            }
        }
        else if (scopeType.ValueMode == ScopeValueMode.Restricted)
        {
            if (string.IsNullOrWhiteSpace(request.ScopeValue))
            {
                return TypedResults.BadRequest($"Scope type '{scopeType.Name}' is restricted and requires a scope value.");
            }

            var scopeValueExists = await db.ScopeValues
                .AnyAsync(sv => sv.ScopeTypeId == scopeTypeId && sv.Value == request.ScopeValue);

            if (!scopeValueExists)
            {
                return TypedResults.BadRequest($"Scope value '{request.ScopeValue}' does not exist for scope type '{scopeType.Name}'. Only predefined values are allowed.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.ScopeValue))
            {
                return TypedResults.BadRequest($"Scope type '{scopeType.Name}' requires a scope value.");
            }
        }

        if (request.IsPassthrough != true && string.IsNullOrWhiteSpace(request.Content))
        {
            return TypedResults.BadRequest("Parameter content is required");
        }

        var isPassthrough = request.IsPassthrough == true;
        var checksum = isPassthrough ? "passthrough" : ComputeChecksum(request.Content!);

        var existingVersion = await db.ParameterFiles
            .Include(pf => pf.ParameterSchema)
            .FirstOrDefaultAsync(pf =>
                pf.ScopeTypeId == scopeTypeId &&
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeValue == request.ScopeValue &&
                pf.Version == request.Version);

        var dataDir = config["DataDirectory"] ?? "data";
        var filePath = !string.IsNullOrWhiteSpace(request.ScopeValue)
            ? Path.Combine(dataDir, "parameters", configuration.Name, scopeType.Name, request.ScopeValue, $"v{request.Version}", "parameters.yaml")
            : Path.Combine(dataDir, "parameters", configuration.Name, scopeType.Name, $"v{request.Version}", "parameters.yaml");

        if (existingVersion is not null)
        {
            if (existingVersion.Status != ParameterVersionStatus.Draft)
            {
                return TypedResults.BadRequest("Cannot modify a published parameter version. Published versions are immutable. Create a new version instead.");
            }

            if (!isPassthrough)
            {
                var fileDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
                {
                    Directory.CreateDirectory(fileDir);
                }
                await File.WriteAllTextAsync(filePath, request.Content!);
            }

            existingVersion.Checksum = checksum;
            existingVersion.IsPassthrough = isPassthrough;
            existingVersion.ContentType = isPassthrough ? null : (request.ContentType ?? "application/x-yaml");
            await db.SaveChangesAsync();

            return TypedResults.Ok(new ParameterFileDto
            {
                Id = existingVersion.Id,
                ScopeTypeId = existingVersion.ScopeTypeId,
                ConfigurationId = existingVersion.ParameterSchema!.ConfigurationId,
                ScopeValue = existingVersion.ScopeValue,
                Version = existingVersion.Version,
                MajorVersion = existingVersion.MajorVersion,
                Checksum = existingVersion.Checksum,
                Status = existingVersion.Status,
                IsPassthrough = existingVersion.IsPassthrough,
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
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
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
            MajorVersion = majorVersion,
            ContentType = isPassthrough ? null : (request.ContentType ?? "application/x-yaml"),
            Checksum = checksum,
            Status = ParameterVersionStatus.Draft,
            IsPassthrough = isPassthrough,
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (!isPassthrough)
        {
            var newFileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(newFileDir) && !Directory.Exists(newFileDir))
            {
                Directory.CreateDirectory(newFileDir);
            }
            await File.WriteAllTextAsync(filePath, request.Content!);
        }

        db.ParameterFiles.Add(parameterFile);
        await db.SaveChangesAsync();

        return TypedResults.Ok(new ParameterFileDto
        {
            Id = parameterFile.Id,
            ScopeTypeId = parameterFile.ScopeTypeId,
            ConfigurationId = parameterFile.ParameterSchema!.ConfigurationId,
            ScopeValue = parameterFile.ScopeValue,
            Version = parameterFile.Version,
            MajorVersion = parameterFile.MajorVersion,
            Checksum = parameterFile.Checksum,
            Status = parameterFile.Status,
            IsPassthrough = parameterFile.IsPassthrough,
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
                MajorVersion = pf.MajorVersion,
                Checksum = pf.Checksum,
                Status = pf.Status,
                IsPassthrough = pf.IsPassthrough,
                CreatedAt = pf.CreatedAt
            })
            .ToList();

        return TypedResults.Ok(orderedVersions);
    }

    private static async Task<Results<Ok<ParameterFileDto>, NotFound, Conflict<string>, ForbidHttpResult>> PublishParameterVersion(
        Guid scopeTypeId,
        Guid configurationId,
        string version,
        [FromQuery] string? scopeValue,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext,
        IParameterValidator parameterValidator,
        IConfiguration config)
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

        if (parameterFile.NeedsMigration && !parameterFile.IsPassthrough)
        {
            return TypedResults.Conflict("Cannot publish parameter file that needs migration. Please resolve migration issues first.");
        }

        if (!parameterFile.IsPassthrough && !string.IsNullOrWhiteSpace(parameterFile.ParameterSchema?.GeneratedJsonSchema))
        {
            var configuration = await db.Configurations.FindAsync(configurationId);
            if (configuration is null)
            {
                return TypedResults.NotFound();
            }

            var scopeType = await db.ScopeTypes.FindAsync(scopeTypeId);
            if (scopeType is null)
            {
                return TypedResults.NotFound();
            }

            var dataDir = config["DataDirectory"] ?? "data";
            var filePath = !string.IsNullOrWhiteSpace(scopeValue)
                ? Path.Combine(dataDir, "parameters", configuration.Name, scopeType.Name, scopeValue, $"v{version}", "parameters.yaml")
                : Path.Combine(dataDir, "parameters", configuration.Name, scopeType.Name, $"v{version}", "parameters.yaml");

            if (!File.Exists(filePath))
            {
                return TypedResults.Conflict("Parameter file does not exist on disk");
            }

            var content = await File.ReadAllTextAsync(filePath);
            var validationResult = parameterValidator.Validate(parameterFile.ParameterSchema.GeneratedJsonSchema, content);

            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors?.Select(e => $"{e.Path}: {e.Message}") ?? []);
                return TypedResults.Conflict($"Cannot publish parameter file with validation errors: {errorMessages}");
            }
        }

        var currentlyActive = await db.ParameterFiles
            .Include(pf => pf.ParameterSchema)
            .Where(pf =>
                pf.ScopeTypeId == scopeTypeId &&
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeValue == scopeValue &&
                pf.MajorVersion == parameterFile.MajorVersion &&
                pf.Status == ParameterVersionStatus.Published)
            .ToListAsync();

        foreach (var activeFile in currentlyActive)
        {
            activeFile.Status = ParameterVersionStatus.Archived;
        }

        parameterFile.Status = ParameterVersionStatus.Published;
        await db.SaveChangesAsync();

        return TypedResults.Ok(new ParameterFileDto
        {
            Id = parameterFile.Id,
            ScopeTypeId = parameterFile.ScopeTypeId,
            ConfigurationId = parameterFile.ParameterSchema!.ConfigurationId,
            ScopeValue = parameterFile.ScopeValue,
            Version = parameterFile.Version,
            MajorVersion = parameterFile.MajorVersion,
            Checksum = parameterFile.Checksum,
            Status = parameterFile.Status,
            IsPassthrough = parameterFile.IsPassthrough,
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

        if (parameterFile.Status == ParameterVersionStatus.Published)
        {
            return TypedResults.Conflict("Cannot delete a published parameter version");
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
                    pf.Status == ParameterVersionStatus.Published);

            if (defaultParamFile != null && !defaultParamFile.IsPassthrough)
            {
                var defaultPath = Path.Combine(dataDir, "parameters", configuration.Name, "Default", $"v{defaultParamFile.Version}", "parameters.yaml");

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
                    pf.Status == ParameterVersionStatus.Published)
                .FirstOrDefaultAsync();

            if (paramFile is null || paramFile.IsPassthrough)
            {
                continue;
            }

            var filePath = Path.Combine(dataDir, "parameters", configuration.Name, tag.ScopeValue.ScopeType.Name, tag.ScopeValue.Value, $"v{paramFile.Version}", "parameters.yaml");

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
                    pf.Status == ParameterVersionStatus.Published)
                .FirstOrDefaultAsync();

            if (nodeParamFile != null && !nodeParamFile.IsPassthrough)
            {
                var nodePath = Path.Combine(dataDir, "parameters", configuration.Name, "Node", node.Fqdn, $"v{nodeParamFile.Version}", "parameters.yaml");

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

    private static async Task<Results<Ok<List<MajorVersionDto>>, NotFound, ForbidHttpResult>> GetMajorVersions(
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

        var allFiles = await db.ParameterFiles
            .Where(pf =>
                pf.ScopeTypeId == scopeTypeId &&
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeValue == scopeValue)
            .Select(pf => new { pf.MajorVersion, pf.Version, pf.CreatedAt, pf.Status, pf.NeedsMigration })
            .ToListAsync();

        var majorVersions = allFiles
            .GroupBy(pf => pf.MajorVersion)
            .Select(g => new MajorVersionDto
            {
                MajorVersion = g.Key,
                VersionCount = g.Count(),
                HasActive = g.Any(pf => pf.Status == ParameterVersionStatus.Published),
                LatestVersion = g.OrderByDescending(pf => pf.CreatedAt).First().Version,
                HasMigrationNeeded = g.Any(pf => pf.NeedsMigration)
            })
            .OrderByDescending(m => m.MajorVersion)
            .ToList();

        return TypedResults.Ok(majorVersions);
    }

    private static async Task<Results<Ok<ParameterFileDto>, NotFound, ForbidHttpResult>> GetActiveParameterForMajor(
        Guid scopeTypeId,
        Guid configurationId,
        int major,
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

        var activeParameter = await db.ParameterFiles
            .Include(pf => pf.ParameterSchema)
            .FirstOrDefaultAsync(pf =>
                pf.ScopeTypeId == scopeTypeId &&
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeValue == scopeValue &&
                pf.MajorVersion == major &&
                pf.Status == ParameterVersionStatus.Published);

        if (activeParameter is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new ParameterFileDto
        {
            Id = activeParameter.Id,
            ScopeTypeId = activeParameter.ScopeTypeId,
            ConfigurationId = activeParameter.ParameterSchema!.ConfigurationId,
            ScopeValue = activeParameter.ScopeValue,
            Version = activeParameter.Version,
            MajorVersion = activeParameter.MajorVersion,
            Checksum = activeParameter.Checksum,
            Status = activeParameter.Status,
            IsPassthrough = activeParameter.IsPassthrough,
            CreatedAt = activeParameter.CreatedAt
        });
    }

    private static async Task<Results<Ok, Conflict<PublishResultDto>, BadRequest<string>, NotFound>> UploadParameterSchema(
        string configurationName,
        [FromForm] string version,
        [FromForm] IFormFile parametersFile,
        ServerDbContext db,
        IParameterSchemaBuilder schemaBuilder,
        IParameterCompatibilityService compatibilityService,
        IParameterValidator validator)
    {
        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configurationName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        if (!SemanticVersion.TryParse(version, out var semVer))
        {
            return TypedResults.BadRequest($"Version '{version}' is not a valid semantic version");
        }

        string content;
        using (var reader = new StreamReader(parametersFile.OpenReadStream()))
        {
            content = await reader.ReadToEndAsync();
        }

        // Check if schema already exists for this version
        var existingSchema = await db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id && ps.SchemaVersion == version);

        if (existingSchema is not null)
        {
            return TypedResults.BadRequest($"Parameter schema version '{version}' already exists");
        }

        // Parse the content to extract parameters block
        var parametersBlock = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
        if (parametersBlock is null || !parametersBlock.TryGetValue("parameters", out var paramsObj))
        {
            return TypedResults.BadRequest("Parameter schema must contain a 'parameters' object");
        }

        var paramsJson = JsonSerializer.Serialize(paramsObj);
        var paramDefinitions = JsonSerializer.Deserialize<Dictionary<string, ParameterDefinition>>(paramsJson);

        if (paramDefinitions is null)
        {
            return TypedResults.BadRequest("Failed to parse parameter definitions");
        }

        // Generate JSON Schema
        var jsonSchemaObj = schemaBuilder.BuildJsonSchema(paramDefinitions);
        var jsonSchema = schemaBuilder.SerializeSchema(jsonSchemaObj);

        // Check for existing schema (previous version) to compare
        var allSchemas = await db.ParameterSchemas
            .Where(ps => ps.ConfigurationId == configuration.Id)
            .ToListAsync();

        var previousSchema = allSchemas
            .OrderByDescending(ps => ps.UpdatedAt)
            .FirstOrDefault();

        // Perform compatibility checking if there's a previous version
        if (previousSchema is not null && !string.IsNullOrWhiteSpace(previousSchema.GeneratedJsonSchema))
        {
            if (string.IsNullOrWhiteSpace(previousSchema.SchemaVersion))
            {
                return TypedResults.BadRequest("Previous schema version is missing");
            }

            var compatibilityReport = compatibilityService.CompareSchemas(
                previousSchema.GeneratedJsonSchema,
                jsonSchema,
                previousSchema.SchemaVersion,
                version);

            // Check if version allows breaking changes
            var previousSemVer = SemanticVersion.Parse(previousSchema.SchemaVersion);
            var isMajorVersionBump = semVer.Major > previousSemVer.Major;

            if (compatibilityReport.HasBreakingChanges && !isMajorVersionBump)
            {
                // Validate all existing parameter files
                var parameterFiles = await db.ParameterFiles
                    .Where(pf => pf.ParameterSchemaId == previousSchema.Id)
                    .ToListAsync();

                var migrationRequirements = new List<ParameterFileMigrationStatusDto>();

                foreach (var paramFile in parameterFiles)
                {
                    var scopeType = await db.ScopeTypes.FindAsync(paramFile.ScopeTypeId);
                    if (scopeType is null) continue;

                    // TODO: Read the actual parameter file content and validate
                    // For now, just mark all affected files as needing migration
                    migrationRequirements.Add(new ParameterFileMigrationStatusDto
                    {
                        ScopeTypeName = scopeType.Name,
                        ScopeValue = paramFile.ScopeValue ?? "Global",
                        Version = paramFile.Version,
                        MajorVersion = paramFile.MajorVersion,
                        NeedsMigration = true,
                        Errors = []
                    });
                }

                return TypedResults.Conflict(new PublishResultDto
                {
                    Success = false,
                    CompatibilityReport = new CompatibilityReportDto
                    {
                        HasBreakingChanges = compatibilityReport.HasBreakingChanges,
                        BreakingChanges = compatibilityReport.BreakingChanges?.Select(c => new ParameterChangeDto
                        {
                            ParameterName = c.ParameterName,
                            ChangeType = c.ChangeType,
                            Details = c.Description ?? string.Empty
                        }).ToList(),
                        NonBreakingChanges = compatibilityReport.NonBreakingChanges?.Select(c => new ParameterChangeDto
                        {
                            ParameterName = c.ParameterName,
                            ChangeType = c.ChangeType,
                            Details = c.Description ?? string.Empty
                        }).ToList()
                    },
                    MigrationRequirements = migrationRequirements
                });
            }
        }

        // Create new parameter schema
        var newSchema = new ParameterSchema
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configuration.Id,
            SchemaVersion = version,
            GeneratedJsonSchema = jsonSchema,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.ParameterSchemas.Add(newSchema);
        await db.SaveChangesAsync();

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<ValidationResultDto>, BadRequest<string>, NotFound>> ValidateParameterFile(
        string configurationName,
        [FromQuery] string version,
        [FromBody] System.Text.Json.JsonElement parameterContent,
        ServerDbContext db,
        IParameterValidator validator)
    {
        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configurationName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        if (!SemanticVersion.TryParse(version, out _))
        {
            return TypedResults.BadRequest($"Version '{version}' is not a valid semantic version");
        }

        var parameterSchema = await db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id && ps.SchemaVersion == version);

        if (parameterSchema is null)
        {
            return TypedResults.NotFound();
        }

        var jsonSchema = parameterSchema.GeneratedJsonSchema;
        if (string.IsNullOrWhiteSpace(jsonSchema))
        {
            return TypedResults.BadRequest("No JSON schema available for this configuration version");
        }

        var validationResult = validator.Validate(jsonSchema, parameterContent.ToString());

        return TypedResults.Ok(new ValidationResultDto
        {
            IsValid = validationResult.IsValid,
            Errors = validationResult.Errors?.Select(e => new ValidationErrorDto
            {
                Path = e.Path,
                Message = e.Message,
                Code = e.Code
            }).ToList()
        });
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound>> UploadParameterFile(
        string configurationName,
        [FromForm] string scopeTypeName,
        [FromForm] string version,
        [FromForm] IFormFile parametersFile,
        [FromForm] string? scopeValue,
        ServerDbContext db,
        IConfiguration config,
        IParameterValidator validator)
    {
        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configurationName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var scopeType = await db.ScopeTypes.FirstOrDefaultAsync(st => st.Name == scopeTypeName);
        if (scopeType is null)
        {
            return TypedResults.BadRequest($"Scope type '{scopeTypeName}' not found");
        }

        if (!SemanticVersion.TryParse(version, out var semVer))
        {
            return TypedResults.BadRequest($"Version '{version}' is not a valid semantic version");
        }

        // Find the parameter schema for this configuration and version
        var parameterSchema = await db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id && ps.SchemaVersion == version);

        if (parameterSchema is null)
        {
            return TypedResults.BadRequest($"Parameter schema version '{version}' not found. Upload the schema first.");
        }

        string content;
        using (var reader = new StreamReader(parametersFile.OpenReadStream()))
        {
            content = await reader.ReadToEndAsync();
        }

        // Validate parameter content against schema
        if (!string.IsNullOrWhiteSpace(parameterSchema.GeneratedJsonSchema))
        {
            var validationResult = validator.Validate(parameterSchema.GeneratedJsonSchema, content);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors?.Select(e => $"{e.Path}: {e.Message}") ?? []);
                return TypedResults.BadRequest($"Parameter validation failed: {errorMessages}");
            }
        }

        var checksum = ComputeChecksum(content);

        // Check if parameter file already exists
        var existingFile = await db.ParameterFiles
            .FirstOrDefaultAsync(pf =>
                pf.ParameterSchemaId == parameterSchema.Id &&
                pf.ScopeTypeId == scopeType.Id &&
                pf.Version == version &&
                pf.ScopeValue == (scopeValue ?? string.Empty));

        var dataDir = config["DataDirectory"] ?? "data";
        var filePath = !string.IsNullOrWhiteSpace(scopeValue)
            ? Path.Combine(dataDir, "parameters", configuration.Name, scopeType.Name, scopeValue, $"v{version}", "parameters.yaml")
            : Path.Combine(dataDir, "parameters", configuration.Name, scopeType.Name, $"v{version}", "parameters.yaml");

        var fileDir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
        {
            Directory.CreateDirectory(fileDir);
        }
        await File.WriteAllTextAsync(filePath, content);

        if (existingFile is not null)
        {
            existingFile.Checksum = checksum;
            existingFile.ContentType = "application/json";
            await db.SaveChangesAsync();
            return TypedResults.Ok();
        }

        // Create new parameter file
        var newFile = new ParameterFile
        {
            Id = Guid.NewGuid(),
            ParameterSchemaId = parameterSchema.Id,
            ScopeTypeId = scopeType.Id,
            ScopeValue = scopeValue ?? string.Empty,
            Version = version,
            MajorVersion = semVer.Major,
            Checksum = checksum,
            ContentType = "application/json",
            Status = ParameterVersionStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ParameterFiles.Add(newFile);
        await db.SaveChangesAsync();

        return TypedResults.Ok();
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
    public required int MajorVersion { get; init; }
    public required string Checksum { get; init; }
    public required ParameterVersionStatus Status { get; init; }
    public required bool IsPassthrough { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateParameterRequest
{
    public string? ScopeValue { get; init; }
    public required string Version { get; init; }
    public string? Content { get; init; }
    public string? ContentType { get; init; }
    public bool? IsPassthrough { get; init; }
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

public sealed class MajorVersionDto
{
    public required int MajorVersion { get; init; }
    public required int VersionCount { get; init; }
    public required bool HasActive { get; init; }
    public required string LatestVersion { get; init; }
    public required bool HasMigrationNeeded { get; init; }
}

public sealed class ScopeInfo
{
    public required string ScopeTypeName { get; init; }
    public string? ScopeValue { get; init; }
    public required int Precedence { get; init; }
    public required object? Value { get; init; }
}

public sealed class ValidationResultDto
{
    public required bool IsValid { get; init; }
    public List<ValidationErrorDto>? Errors { get; init; }
}

public sealed class ValidationErrorDto
{
    public required string Path { get; init; }
    public required string Message { get; init; }
    public required string Code { get; init; }
}

public sealed class PublishResultDto
{
    public required bool Success { get; init; }
    public CompatibilityReportDto? CompatibilityReport { get; init; }
    public List<ParameterFileMigrationStatusDto>? MigrationRequirements { get; init; }
}

public sealed class CompatibilityReportDto
{
    public required bool HasBreakingChanges { get; init; }
    public List<ParameterChangeDto>? BreakingChanges { get; init; }
    public List<ParameterChangeDto>? NonBreakingChanges { get; init; }
}

public sealed class ParameterChangeDto
{
    public required string ParameterName { get; init; }
    public required string ChangeType { get; init; }
    public required string Details { get; init; }
}

public sealed class ParameterFileMigrationStatusDto
{
    public required string ScopeTypeName { get; init; }
    public required string ScopeValue { get; init; }
    public required string Version { get; init; }
    public required int MajorVersion { get; init; }
    public required bool NeedsMigration { get; init; }
    public required List<ValidationErrorDto> Errors { get; init; }
}
