// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NuGet.Versioning;

using OpenDsc.Server.Authentication;
using OpenDsc.Server.Contracts;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenDsc.Server.Endpoints;

public static class ConfigurationEndpoints
{
    public static void MapConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/configurations")
            .RequireAuthorization(policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    PersonalAccessTokenHandler.SchemeName))
            .WithTags("Configurations");

        group.MapGet("/", GetConfigurations)
            .WithName("GetConfigurations")
            .WithDescription("Get all configurations");

        group.MapPost("/", CreateConfiguration)
            .WithName("CreateConfiguration")
            .WithDescription("Create a new configuration")
            .DisableAntiforgery();

        group.MapGet("/{name}", GetConfigurationDetails)
            .WithName("GetConfigurationDetails")
            .WithDescription("Get configuration details");

        group.MapGet("/{name}/versions", GetConfigurationVersions)
            .WithName("GetConfigurationVersions")
            .WithDescription("Get all versions of a configuration");

        group.MapPost("/{name}/versions", CreateConfigurationVersion)
            .WithName("CreateConfigurationVersion")
            .WithDescription("Create a new version of a configuration")
            .DisableAntiforgery();

        group.MapPut("/{name}/versions/{version}/publish", PublishConfigurationVersion)
            .WithName("PublishConfigurationVersion")
            .WithDescription("Publish a draft configuration version");

        group.MapPatch("/{name}", UpdateConfiguration)
            .WithName("UpdateConfiguration")
            .WithDescription("Update configuration settings such as description and server-managed parameters");

        group.MapDelete("/{name}", DeleteConfiguration)
            .WithName("DeleteConfiguration")
            .WithDescription("Delete a configuration and all its versions");

        group.MapDelete("/{name}/versions/{version}", DeleteConfigurationVersion)
            .WithName("DeleteConfigurationVersion")
            .WithDescription("Delete a specific version (only if draft and not active)");

        group.MapGet("/{name}/versions/{version}/files/{*filePath}", DownloadConfigurationFile)
            .WithName("DownloadConfigurationFile")
            .WithDescription("Download a specific file from a configuration version");

        group.MapGet("/{name}/permissions", GetConfigurationPermissions)
            .WithName("GetConfigurationPermissions")
            .WithDescription("List all permission grants on a configuration");

        group.MapPut("/{name}/permissions", GrantConfigurationPermission)
            .WithName("GrantConfigurationPermission")
            .WithDescription("Grant or update a permission on a configuration");

        group.MapDelete("/{name}/permissions/{principalType}/{principalId:guid}", RevokeConfigurationPermission)
            .WithName("RevokeConfigurationPermission")
            .WithDescription("Revoke a permission on a configuration");
    }

    private static async Task<Ok<List<ConfigurationSummaryDto>>> GetConfigurations(
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null)
        {
            return TypedResults.Ok(new List<ConfigurationSummaryDto>());
        }

        var readableIds = await authService.GetReadableConfigurationIdsAsync(userId.Value);

        var configs = await db.Configurations
            .Where(c => readableIds.Contains(c.Id))
            .Include(c => c.Versions)
            .ToListAsync();

        var result = configs.Select(c => new ConfigurationSummaryDto
        {
            Name = c.Name,
            Description = c.Description,
            UseServerManagedParameters = c.UseServerManagedParameters,
            VersionCount = c.Versions.Count,
            LatestVersion = VersionResolver.LatestSemver(c.Versions.Select(v => v.Version)),
            CreatedAt = c.CreatedAt
        }).ToList();

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Created<ConfigurationDetailsDto>, BadRequest<string>, Conflict<string>>> CreateConfiguration(
        [FromForm] CreateConfigurationDto request,
        IFormFileCollection files,
        ServerDbContext db,
        IConfiguration config,
        IResourceAuthorizationService authService,
        IUserContextService userContext,
        IParameterSchemaBuilder schemaBuilder,
        IParameterCompatibilityService compatibilityService)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest("Configuration name is required");
        }

        if (files.Count == 0)
        {
            return TypedResults.BadRequest("At least one file is required");
        }

        if (await db.Configurations.AnyAsync(c => c.Name == request.Name))
        {
            return TypedResults.Conflict($"Configuration '{request.Name}' already exists");
        }

        var entryPoint = request.EntryPoint ?? "main.dsc.yaml";
        if (!files.Any(f => f.FileName == entryPoint))
        {
            return TypedResults.BadRequest($"Entry point file '{entryPoint}' not found in uploaded files");
        }

        var configuration = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            UseServerManagedParameters = request.UseServerManagedParameters,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Configurations.Add(configuration);

        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configuration.Id,
            Version = request.Version ?? "1.0.0",
            EntryPoint = entryPoint,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ConfigurationVersions.Add(version);

        var dataDir = config["DataDirectory"] ?? "data";
        var versionDir = Path.Combine(dataDir, "configurations", request.Name, $"v{version.Version}");
        Directory.CreateDirectory(versionDir);

        foreach (var file in files)
        {
            var relativePath = file.FileName;
            if (relativePath.Contains("../", StringComparison.Ordinal) || relativePath.Contains("..\\", StringComparison.Ordinal))
            {
                return TypedResults.BadRequest($"Invalid file path: {relativePath}");
            }

            var filePath = Path.Combine(versionDir, relativePath);
            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            var checksum = await ComputeFileChecksumAsync(filePath);

            var configFile = new ConfigurationFile
            {
                Id = Guid.NewGuid(),
                VersionId = version.Id,
                RelativePath = relativePath,
                ContentType = file.ContentType,
                Checksum = checksum,
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.ConfigurationFiles.Add(configFile);
        }

        await db.SaveChangesAsync();

        // Extract and generate parameter schema from entry point file
        var entryPointPath = Path.Combine(versionDir, version.EntryPoint);
        if (File.Exists(entryPointPath))
        {
            var entryPointContent = await File.ReadAllTextAsync(entryPointPath);
            var parametersBlock = ExtractParametersFromYaml(entryPointContent);

            if (parametersBlock != null && parametersBlock.Count > 0)
            {
                // Build JSON schema
                var paramDefinitions = ConvertToParameterDefinitions(parametersBlock);
                var jsonSchemaObj = schemaBuilder.BuildJsonSchema(paramDefinitions);
                var jsonSchema = schemaBuilder.SerializeSchema(jsonSchemaObj);

                // Create parameter schema (no need to check for previous version since this is the first)
                var paramSchema = new ParameterSchema
                {
                    Id = Guid.NewGuid(),
                    ConfigurationId = configuration.Id,
                    SchemaVersion = version.Version,
                    GeneratedJsonSchema = jsonSchema,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                db.ParameterSchemas.Add(paramSchema);
                version.ParameterSchemaId = paramSchema.Id;

                await db.SaveChangesAsync();
            }
        }

        var userId = userContext.GetCurrentUserId();
        if (userId.HasValue)
        {
            await authService.GrantConfigurationPermissionAsync(
                configuration.Id,
                userId.Value,
                PrincipalType.User,
                ResourcePermission.Manage,
                userId.Value);
        }

        var details = new ConfigurationDetailsDto
        {
            Name = configuration.Name,
            Description = configuration.Description,
            UseServerManagedParameters = configuration.UseServerManagedParameters,
            LatestVersion = version.Version,
            CreatedAt = configuration.CreatedAt
        };

        return TypedResults.Created($"/api/v1/configurations/{configuration.Name}", details);
    }

    private static async Task<Results<Ok<ConfigurationDetailsDto>, NotFound, ForbidHttpResult>> GetConfigurationDetails(
        string name,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var config = await db.Configurations
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (config is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanReadConfigurationAsync(userId.Value, config.Id))
        {
            return TypedResults.Forbid();
        }

        var latestVersion = config.Versions
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => v.Version)
            .FirstOrDefault();

        var details = new ConfigurationDetailsDto
        {
            Name = config.Name,
            Description = config.Description,
            UseServerManagedParameters = config.UseServerManagedParameters,
            LatestVersion = latestVersion,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };

        return TypedResults.Ok(details);
    }

    private static async Task<Results<Ok<List<ConfigurationVersionDto>>, NotFound, ForbidHttpResult>> GetConfigurationVersions(
        string name,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var config = await db.Configurations
            .Include(c => c.Versions)
            .ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (config is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanReadConfigurationAsync(userId.Value, config.Id))
        {
            return TypedResults.Forbid();
        }

        var versions = config.Versions
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new ConfigurationVersionDto
            {
                Version = v.Version,
                EntryPoint = v.EntryPoint,
                Status = v.Status,
                PrereleaseChannel = v.PrereleaseChannel,
                FileCount = v.Files.Count,
                CreatedAt = v.CreatedAt,
                CreatedBy = v.CreatedBy
            })
            .ToList();

        return TypedResults.Ok(versions);
    }

    private static async Task<Results<Created<ConfigurationVersionDto>, NotFound, BadRequest<string>, ForbidHttpResult>> CreateConfigurationVersion(
        string name,
        [FromForm] CreateConfigurationVersionDto request,
        IFormFileCollection files,
        ServerDbContext db,
        IConfiguration config,
        IResourceAuthorizationService authService,
        IUserContextService userContext,
        IParameterSchemaBuilder schemaBuilder,
        IParameterCompatibilityService compatibilityService)
    {
        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == name);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanModifyConfigurationAsync(userId.Value, configuration.Id))
        {
            return TypedResults.Forbid();
        }

        if (files.Count == 0)
        {
            return TypedResults.BadRequest("At least one file is required");
        }

        // Resolve entry point: prefer explicit request value, fall back to latest version's entry point
        var latestVersionEntryPoint = (await db.ConfigurationVersions
            .Where(v => v.ConfigurationId == configuration.Id)
            .Select(v => new { v.EntryPoint, v.CreatedAt })
            .ToListAsync())
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => v.EntryPoint)
            .FirstOrDefault();

        var entryPoint = request.EntryPoint ?? latestVersionEntryPoint ?? "main.dsc.yaml";

        if (!files.Any(f => f.FileName == entryPoint))
        {
            return TypedResults.BadRequest($"Entry point file '{entryPoint}' not found in uploaded files");
        }

        var versionNumber = request.Version ?? "1.0.0";

        if (await db.ConfigurationVersions.AnyAsync(v => v.ConfigurationId == configuration.Id && v.Version == versionNumber))
        {
            return TypedResults.BadRequest($"Version '{versionNumber}' already exists for configuration '{name}'");
        }

        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configuration.Id,
            Version = versionNumber,
            EntryPoint = entryPoint,
            PrereleaseChannel = request.PrereleaseChannel,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ConfigurationVersions.Add(version);

        var dataDir = config["DataDirectory"] ?? "data";
        var versionDir = Path.Combine(dataDir, "configurations", name, $"v{version.Version}");
        Directory.CreateDirectory(versionDir);

        foreach (var file in files)
        {
            var relativePath = file.FileName;
            if (relativePath.Contains("../", StringComparison.Ordinal) || relativePath.Contains("..\\", StringComparison.Ordinal))
            {
                return TypedResults.BadRequest($"Invalid file path: {relativePath}");
            }

            var filePath = Path.Combine(versionDir, relativePath);
            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            var checksum = await ComputeFileChecksumAsync(filePath);

            var configFile = new ConfigurationFile
            {
                Id = Guid.NewGuid(),
                VersionId = version.Id,
                RelativePath = relativePath,
                ContentType = file.ContentType,
                Checksum = checksum,
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.ConfigurationFiles.Add(configFile);
        }

        await db.SaveChangesAsync();

        // Extract and generate parameter schema from entry point file
        var entryPointPath = Path.Combine(versionDir, version.EntryPoint);
        if (File.Exists(entryPointPath))
        {
            var entryPointContent = await File.ReadAllTextAsync(entryPointPath);
            var parametersBlock = ExtractParametersFromYaml(entryPointContent);

            if (parametersBlock != null && parametersBlock.Count > 0)
            {
                // Build JSON schema
                var paramDefinitions = ConvertToParameterDefinitions(parametersBlock);
                var jsonSchemaObj = schemaBuilder.BuildJsonSchema(paramDefinitions);
                var jsonSchema = schemaBuilder.SerializeSchema(jsonSchemaObj);

                // Check for previous version's schema
                var allSchemas = await db.ParameterSchemas
                    .Where(ps => ps.ConfigurationId == configuration.Id)
                    .ToListAsync();

                var previousSchema = allSchemas
                    .OrderByDescending(ps => ps.UpdatedAt)
                    .FirstOrDefault();

                // Validate semver compatibility if previous schema exists
                if (previousSchema != null && !string.IsNullOrWhiteSpace(previousSchema.GeneratedJsonSchema) && !string.IsNullOrWhiteSpace(previousSchema.SchemaVersion))
                {
                    if (!SemanticVersion.TryParse(previousSchema.SchemaVersion, out var prevSemVer))
                    {
                        return TypedResults.BadRequest("Previous schema has invalid version");
                    }

                    if (!SemanticVersion.TryParse(versionNumber, out var newSemVer))
                    {
                        return TypedResults.BadRequest($"Version '{versionNumber}' is not a valid semantic version");
                    }

                    // Compare schemas
                    var compatibilityReport = compatibilityService.CompareSchemas(
                        previousSchema.GeneratedJsonSchema,
                        jsonSchema,
                        previousSchema.SchemaVersion,
                        versionNumber);

                    // Check if breaking changes violate semver
                    var isMajorVersionBump = newSemVer.Major > prevSemVer.Major;
                    if (compatibilityReport.HasBreakingChanges && !isMajorVersionBump)
                    {
                        var changeDetails = string.Join("; ",
                            compatibilityReport.BreakingChanges.Select(c => c.ChangeType));
                        var versionType = newSemVer.Major == prevSemVer.Major && newSemVer.Minor == prevSemVer.Minor ? "patch" : "minor";
                        return TypedResults.BadRequest(
                            $"Parameter schema has breaking changes ({changeDetails}) which are not allowed in {versionType} versions. Use a major version bump to allow breaking changes.");
                    }
                }

                // Create or reuse parameter schema
                var existingSchema = await db.ParameterSchemas
                    .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id && ps.SchemaVersion == versionNumber);

                if (existingSchema == null)
                {
                    var paramSchema = new ParameterSchema
                    {
                        Id = Guid.NewGuid(),
                        ConfigurationId = configuration.Id,
                        SchemaVersion = versionNumber,
                        GeneratedJsonSchema = jsonSchema,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    db.ParameterSchemas.Add(paramSchema);
                    version.ParameterSchemaId = paramSchema.Id;
                }
                else
                {
                    version.ParameterSchemaId = existingSchema.Id;
                    existingSchema.UpdatedAt = DateTimeOffset.UtcNow;
                }

                await db.SaveChangesAsync();
            }
        }

        var versionDto = new ConfigurationVersionDto
        {
            Version = version.Version,
            EntryPoint = version.EntryPoint,
            Status = version.Status,
            PrereleaseChannel = version.PrereleaseChannel,
            FileCount = files.Count,
            CreatedAt = version.CreatedAt,
            CreatedBy = version.CreatedBy
        };

        return TypedResults.Created($"/api/v1/configurations/{name}/versions/{version.Version}", versionDto);
    }

    private static async Task<Results<Ok<ConfigurationVersionDto>, NotFound, BadRequest<string>, Conflict<CompatibilityReport>, ForbidHttpResult>> PublishConfigurationVersion(
        string name,
        string version,
        ServerDbContext db,
        IConfiguration config,
        IResourceAuthorizationService authService,
        IUserContextService userContext,
        IParameterSchemaService parameterSchemaService,
        IParameterCompatibilityService compatibilityService,
        IParameterValidator parameterValidator)
    {
        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == name);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanModifyConfigurationAsync(userId.Value, configuration.Id))
        {
            return TypedResults.Forbid();
        }

        var configVersion = await db.ConfigurationVersions
            .Include(v => v.Files)
            .FirstOrDefaultAsync(v => v.ConfigurationId == configuration.Id && v.Version == version);

        if (configVersion is null)
        {
            return TypedResults.NotFound();
        }

        if (configVersion.Status != ConfigurationVersionStatus.Draft)
        {
            return TypedResults.BadRequest("Version is already published");
        }

        if (configVersion.Files.Count == 0)
        {
            return TypedResults.BadRequest("Cannot publish version with no files");
        }

        // Parse semantic version
        if (!NuGet.Versioning.SemanticVersion.TryParse(version, out var semVer))
        {
            return TypedResults.BadRequest($"Version '{version}' is not a valid semantic version");
        }

        var newMajor = semVer.Major;

        // Read entry point configuration file to extract parameters block
        var dataDir = config["DataDirectory"] ?? "data";
        var entryPointPath = Path.Combine(dataDir, "configurations", name, $"v{version}", configVersion.EntryPoint);

        if (!File.Exists(entryPointPath))
        {
            return TypedResults.BadRequest($"Entry point file '{configVersion.EntryPoint}' not found");
        }

        var entryPointContent = await File.ReadAllTextAsync(entryPointPath);
        var parametersJson = await parameterSchemaService.ParseParameterBlockAsync(entryPointContent);

        // Find or create ParameterSchema
        var parameterSchema = await db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id);

        CompatibilityReport? compatibilityReport = null;

        if (parametersJson != null)
        {
            // Generate JSON Schema from parameters block
            await parameterSchemaService.GenerateAndStoreSchemaAsync(configuration.Id, parametersJson, version);

            // Reload schema after generation
            parameterSchema = await db.ParameterSchemas
                .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id);

            // Check for previous schema version to compare
            if (parameterSchema != null && !string.IsNullOrWhiteSpace(parameterSchema.SchemaVersion) &&
                parameterSchema.SchemaVersion != version)
            {
                if (!NuGet.Versioning.SemanticVersion.TryParse(parameterSchema.SchemaVersion, out var oldSemVer))
                {
                    return TypedResults.BadRequest($"Previous schema version '{parameterSchema.SchemaVersion}' is not a valid semantic version");
                }

                var oldMajor = oldSemVer.Major;

                // Compare schemas for compatibility
                compatibilityReport = compatibilityService.CompareSchemas(
                    parameterSchema.GeneratedJsonSchema,
                    parameterSchema.GeneratedJsonSchema,
                    parameterSchema.SchemaVersion,
                    version);

                // Enforce semver rules
                if (semVer.Major == oldSemVer.Major)
                {
                    // Same major version - breaking changes not allowed
                    if (compatibilityReport.HasBreakingChanges)
                    {
                        // Populate affected parameter files
                        var affectedFiles = await db.ParameterFiles
                            .Include(pf => pf.ScopeType)
                            .Where(pf => pf.ParameterSchemaId == parameterSchema.Id && pf.MajorVersion == oldMajor)
                            .ToListAsync();

                        compatibilityReport = new CompatibilityReport
                        {
                            OldVersion = compatibilityReport.OldVersion,
                            NewVersion = compatibilityReport.NewVersion,
                            NewMajorVersion = compatibilityReport.NewMajorVersion,
                            HasBreakingChanges = compatibilityReport.HasBreakingChanges,
                            BreakingChanges = compatibilityReport.BreakingChanges,
                            NonBreakingChanges = compatibilityReport.NonBreakingChanges,
                            AffectedParameterFiles = affectedFiles.Select(f => new ParameterFileMigrationStatus
                            {
                                FileId = f.Id,
                                ScopeTypeName = f.ScopeType.Name,
                                ScopeValue = f.ScopeValue,
                                Version = f.Version,
                                NeedsMigration = true,
                                Errors = null
                            }).ToList()
                        };

                        return TypedResults.Conflict(compatibilityReport);
                    }
                }
                else if (semVer.Major > oldMajor)
                {
                    // New major version - auto-copy active parameters with migration flags
                    var activeParameters = await db.ParameterFiles
                        .Include(pf => pf.ScopeType)
                        .Where(pf => pf.ParameterSchemaId == parameterSchema.Id &&
                                     pf.MajorVersion == oldMajor &&
                                     pf.Status == ParameterVersionStatus.Published)
                        .ToListAsync();

                    foreach (var activeParam in activeParameters)
                    {
                        // Check if parameter file already exists for new major version
                        var existsInNewMajor = await db.ParameterFiles
                            .AnyAsync(pf => pf.ParameterSchemaId == parameterSchema.Id &&
                                            pf.MajorVersion == newMajor &&
                                            pf.ScopeTypeId == activeParam.ScopeTypeId &&
                                            pf.ScopeValue == activeParam.ScopeValue);

                        if (!existsInNewMajor)
                        {
                            // Read parameter file content
                            var paramFilePath = !string.IsNullOrWhiteSpace(activeParam.ScopeValue)
                                ? Path.Combine(dataDir, "parameters", name, activeParam.ScopeType.Name, activeParam.ScopeValue, "parameters.yaml")
                                : Path.Combine(dataDir, "parameters", name, activeParam.ScopeType.Name, "parameters.yaml");

                            string? validationErrors = null;
                            if (File.Exists(paramFilePath) && !string.IsNullOrWhiteSpace(parameterSchema.GeneratedJsonSchema))
                            {
                                var content = await File.ReadAllTextAsync(paramFilePath);
                                var validationResult = parameterValidator.Validate(parameterSchema.GeneratedJsonSchema, content);

                                if (!validationResult.IsValid)
                                {
                                    validationErrors = System.Text.Json.JsonSerializer.Serialize(
                                        validationResult.Errors,
                                        SourceGenerationContext.Default.ListValidationError);
                                }
                            }

                            // Create new parameter file entry for new major version
                            var newMajorVer = $"{newMajor}.0.0";
                            var newParameterFile = new ParameterFile
                            {
                                Id = Guid.NewGuid(),
                                ParameterSchemaId = parameterSchema.Id,
                                ScopeTypeId = activeParam.ScopeTypeId,
                                ScopeValue = activeParam.ScopeValue,
                                Version = newMajorVer,
                                MajorVersion = newMajor,
                                Checksum = activeParam.Checksum,
                                ContentType = activeParam.ContentType,
                                Status = ParameterVersionStatus.Published,
                                NeedsMigration = validationErrors != null || compatibilityReport!.HasBreakingChanges,
                                ValidationErrors = validationErrors,
                                CreatedAt = DateTimeOffset.UtcNow
                            };

                            db.ParameterFiles.Add(newParameterFile);
                        }
                    }

                    await db.SaveChangesAsync();
                }
            }
        }

        configVersion.Status = ConfigurationVersionStatus.Published;
        await db.SaveChangesAsync();

        var versionDto = new ConfigurationVersionDto
        {
            Version = configVersion.Version,
            EntryPoint = configVersion.EntryPoint,
            Status = configVersion.Status,
            PrereleaseChannel = configVersion.PrereleaseChannel,
            FileCount = configVersion.Files.Count,
            CreatedAt = configVersion.CreatedAt,
            CreatedBy = configVersion.CreatedBy
        };

        return TypedResults.Ok(versionDto);
    }

    private static async Task<Results<Ok<ConfigurationDetailsDto>, NotFound, Conflict<ErrorResponse>, ForbidHttpResult>> UpdateConfiguration(
        string name,
        [FromBody] UpdateConfigurationDto request,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var configuration = await db.Configurations
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanManageConfigurationAsync(userId.Value, configuration.Id))
        {
            return TypedResults.Forbid();
        }

        if (request.UseServerManagedParameters == false && configuration.UseServerManagedParameters)
        {
            var activeParamFiles = await db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                .Where(pf => pf.ParameterSchema!.ConfigurationId == configuration.Id && pf.Status == ParameterVersionStatus.Published)
                .ToListAsync();

            if (activeParamFiles.Count > 0)
            {
                return TypedResults.Conflict(new ErrorResponse
                {
                    Error = $"Cannot disable server-managed parameters: {activeParamFiles.Count} active parameter file(s) exist. Deactivate them first."
                });
            }
        }

        if (request.Description is not null)
        {
            configuration.Description = request.Description;
        }

        if (request.UseServerManagedParameters.HasValue)
        {
            configuration.UseServerManagedParameters = request.UseServerManagedParameters.Value;
        }

        configuration.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var latestVersion = configuration.Versions
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => v.Version)
            .FirstOrDefault();

        var details = new ConfigurationDetailsDto
        {
            Name = configuration.Name,
            Description = configuration.Description,
            UseServerManagedParameters = configuration.UseServerManagedParameters,
            LatestVersion = latestVersion,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt
        };

        return TypedResults.Ok(details);
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> DeleteConfiguration(
        string name,
        ServerDbContext db,
        IConfiguration config,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var configuration = await db.Configurations
            .Include(c => c.Versions)
            .Include(c => c.NodeConfigurations)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanManageConfigurationAsync(userId.Value, configuration.Id))
        {
            return TypedResults.Forbid();
        }

        if (configuration.NodeConfigurations.Count > 0)
        {
            return TypedResults.Conflict($"Cannot delete configuration assigned to {configuration.NodeConfigurations.Count} nodes");
        }

        var dataDir = config["DataDirectory"] ?? "data";
        var configDir = Path.Combine(dataDir, "configurations", name);
        if (Directory.Exists(configDir))
        {
            Directory.Delete(configDir, true);
        }

        db.Configurations.Remove(configuration);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> DeleteConfigurationVersion(
        string name,
        string version,
        ServerDbContext db,
        IConfiguration config,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == name);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanManageConfigurationAsync(userId.Value, configuration.Id))
        {
            return TypedResults.Forbid();
        }

        var configVersion = await db.ConfigurationVersions
            .Include(v => v.NodeConfigurations)
            .FirstOrDefaultAsync(v => v.ConfigurationId == configuration.Id && v.Version == version);

        if (configVersion is null)
        {
            return TypedResults.NotFound();
        }

        if (configVersion.Status != ConfigurationVersionStatus.Draft)
        {
            return TypedResults.Conflict("Cannot delete published version");
        }

        if (configVersion.NodeConfigurations.Count > 0)
        {
            return TypedResults.Conflict($"Cannot delete version assigned to {configVersion.NodeConfigurations.Count} nodes");
        }

        var dataDir = config["DataDirectory"] ?? "data";
        var versionDir = Path.Combine(dataDir, "configurations", name, $"v{version}");
        if (Directory.Exists(versionDir))
        {
            Directory.Delete(versionDir, true);
        }

        db.ConfigurationVersions.Remove(configVersion);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<string> ComputeFileChecksumAsync(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<Results<FileStreamHttpResult, NotFound, ForbidHttpResult>> DownloadConfigurationFile(
        string name,
        string version,
        string filePath,
        ServerDbContext db,
        IConfiguration config,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var configEntity = await db.Configurations
            .Include(c => c.Versions.Where(v => v.Version == version))
            .ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (configEntity == null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanReadConfigurationAsync(userId.Value, configEntity.Id))
        {
            return TypedResults.Forbid();
        }

        var configVersion = configEntity.Versions.FirstOrDefault();
        if (configVersion == null)
        {
            return TypedResults.NotFound();
        }

        var file = configVersion.Files.FirstOrDefault(f => f.RelativePath == filePath);
        if (file == null)
        {
            return TypedResults.NotFound();
        }

        var dataDir = config["DataDirectory"] ?? "data";
        var fullPath = Path.Combine(dataDir, "configurations", name, $"v{version}", filePath);

        if (!File.Exists(fullPath))
        {
            return TypedResults.NotFound();
        }

        var stream = File.OpenRead(fullPath);
        var fileName = Path.GetFileName(filePath);
        return TypedResults.File(stream, file.ContentType ?? "application/octet-stream", fileName);
    }

    private static Dictionary<string, object>? ExtractParametersFromYaml(string yamlContent)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var document = deserializer.Deserialize<Dictionary<object, object>>(yamlContent);
            if (document?.TryGetValue("parameters", out var parametersObj) == true)
            {
                // YamlDotNet deserializes to Dictionary<object, object>, convert keys to strings
                if (parametersObj is Dictionary<object, object> paramDict)
                {
                    return paramDict.ToDictionary(
                        kvp => kvp.Key.ToString() ?? string.Empty,
                        kvp => kvp.Value);
                }
            }
        }
        catch
        {
            // If parsing fails, return null - configuration can exist without parameters
        }

        return null;
    }

    private static Dictionary<string, ParameterDefinition> ConvertToParameterDefinitions(Dictionary<string, object> parametersBlock)
    {
        var result = new Dictionary<string, ParameterDefinition>();

        foreach (var param in parametersBlock)
        {
            if (param.Value is Dictionary<object, object> paramObj)
            {
                var def = new ParameterDefinition
                {
                    Type = paramObj.ContainsKey("type") ? paramObj["type"]?.ToString() ?? "string" : "string",
                    Description = paramObj.ContainsKey("description") ? paramObj["description"]?.ToString() : null,
                    DefaultValue = paramObj.ContainsKey("defaultValue") ? paramObj["defaultValue"] : null,
                    AllowedValues = paramObj.ContainsKey("allowedValues") && paramObj["allowedValues"] is List<object> list ? list.ToArray() : null,
                    MinLength = paramObj.ContainsKey("minLength") ? paramObj["minLength"] as int? : null,
                    MaxLength = paramObj.ContainsKey("maxLength") ? paramObj["maxLength"] as int? : null,
                    MinValue = paramObj.ContainsKey("minValue") ? paramObj["minValue"] as int? : null,
                    MaxValue = paramObj.ContainsKey("maxValue") ? paramObj["maxValue"] as int? : null
                };
                result[param.Key] = def;
            }
        }

        return result;
    }

    private static async Task<Results<Ok<List<PermissionEntryDto>>, NotFound, ForbidHttpResult>> GetConfigurationPermissions(
        string name,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var config = await db.Configurations.FirstOrDefaultAsync(c => c.Name == name);
        if (config is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanManageConfigurationAsync(userId.Value, config.Id))
        {
            return TypedResults.Forbid();
        }

        var acl = await authService.GetConfigurationAclAsync(config.Id);
        var result = await BuildPermissionEntries(
            acl.Select(p => (p.PrincipalType, p.PrincipalId, p.PermissionLevel, p.GrantedAt, p.GrantedByUserId)), db);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, ForbidHttpResult>> GrantConfigurationPermission(
        string name,
        [FromBody] PermissionGrantRequest request,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var config = await db.Configurations.FirstOrDefaultAsync(c => c.Name == name);
        if (config is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanManageConfigurationAsync(userId.Value, config.Id))
        {
            return TypedResults.Forbid();
        }

        if (!Enum.TryParse<PrincipalType>(request.PrincipalType, ignoreCase: true, out var principalType))
        {
            return TypedResults.BadRequest($"Invalid principal type '{request.PrincipalType}'. Must be 'User' or 'Group'.");
        }

        if (!Enum.TryParse<ResourcePermission>(request.Level, ignoreCase: true, out var level))
        {
            return TypedResults.BadRequest($"Invalid permission level '{request.Level}'. Must be 'Read', 'Modify', or 'Manage'.");
        }

        if (principalType == PrincipalType.User && !await db.Users.AnyAsync(u => u.Id == request.PrincipalId))
        {
            return TypedResults.NotFound();
        }

        if (principalType == PrincipalType.Group && !await db.Groups.AnyAsync(g => g.Id == request.PrincipalId))
        {
            return TypedResults.NotFound();
        }

        await authService.GrantConfigurationPermissionAsync(config.Id, request.PrincipalId, principalType, level, userId.Value);
        return TypedResults.Ok();
    }

    private static async Task<Results<NoContent, BadRequest<string>, NotFound, ForbidHttpResult>> RevokeConfigurationPermission(
        string name,
        string principalType,
        Guid principalId,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var config = await db.Configurations.FirstOrDefaultAsync(c => c.Name == name);
        if (config is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanManageConfigurationAsync(userId.Value, config.Id))
        {
            return TypedResults.Forbid();
        }

        if (!Enum.TryParse<PrincipalType>(principalType, ignoreCase: true, out var parsedPrincipalType))
        {
            return TypedResults.BadRequest($"Invalid principal type '{principalType}'. Must be 'User' or 'Group'.");
        }

        await authService.RevokeConfigurationPermissionAsync(config.Id, principalId, parsedPrincipalType);
        return TypedResults.NoContent();
    }

    private static async Task<List<PermissionEntryDto>> BuildPermissionEntries(
        IEnumerable<(PrincipalType PrincipalType, Guid PrincipalId, ResourcePermission Level, DateTimeOffset GrantedAt, Guid? GrantedByUserId)> entries,
        ServerDbContext db)
    {
        var list = entries.ToList();
        var userIds = list.Where(e => e.PrincipalType == PrincipalType.User).Select(e => e.PrincipalId).ToList();
        var groupIds = list.Where(e => e.PrincipalType == PrincipalType.Group).Select(e => e.PrincipalId).ToList();

        var userNames = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username);

        var groupNames = await db.Groups
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Name);

        return list.Select(e => new PermissionEntryDto
        {
            PrincipalType = e.PrincipalType.ToString(),
            PrincipalId = e.PrincipalId,
            PrincipalName = e.PrincipalType == PrincipalType.User
                ? userNames.GetValueOrDefault(e.PrincipalId, "Unknown")
                : groupNames.GetValueOrDefault(e.PrincipalId, "Unknown"),
            Level = e.Level.ToString(),
            GrantedAt = e.GrantedAt,
            GrantedByUserId = e.GrantedByUserId
        }).ToList();
    }
}

public sealed class ConfigurationSummaryDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool UseServerManagedParameters { get; init; }
    public required int VersionCount { get; init; }
    public string? LatestVersion { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class ConfigurationDetailsDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool UseServerManagedParameters { get; init; }
    public string? LatestVersion { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateConfigurationDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? EntryPoint { get; init; }
    public bool UseServerManagedParameters { get; init; } = true;
    public string? Version { get; init; }
}

public sealed class UpdateConfigurationDto
{
    public string? Description { get; init; }
    public bool? UseServerManagedParameters { get; init; }
}

public sealed class ConfigurationVersionDto
{
    public required string Version { get; init; }
    public required string EntryPoint { get; init; }
    public required ConfigurationVersionStatus Status { get; init; }
    public string? PrereleaseChannel { get; init; }
    public required int FileCount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public sealed class CreateConfigurationVersionDto
{
    public required string Version { get; init; }
    public string? EntryPoint { get; init; }
    public string? PrereleaseChannel { get; init; }
}
