// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authentication;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

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

        group.MapDelete("/{name}", DeleteConfiguration)
            .WithName("DeleteConfiguration")
            .WithDescription("Delete a configuration and all its versions");

        group.MapDelete("/{name}/versions/{version}", DeleteConfigurationVersion)
            .WithName("DeleteConfigurationVersion")
            .WithDescription("Delete a specific version (only if draft and not active)");
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
            EntryPoint = c.EntryPoint,
            IsServerManaged = c.IsServerManaged,
            VersionCount = c.Versions.Count,
            LatestVersion = c.Versions.OrderByDescending(v => v.CreatedAt).Select(v => v.Version).FirstOrDefault(),
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
        IUserContextService userContext)
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
            EntryPoint = entryPoint,
            IsServerManaged = request.IsServerManaged,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Configurations.Add(configuration);

        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configuration.Id,
            Version = request.Version ?? "1.0.0",
            IsDraft = request.IsDraft,
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
            EntryPoint = configuration.EntryPoint,
            IsServerManaged = configuration.IsServerManaged,
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
            EntryPoint = config.EntryPoint,
            IsServerManaged = config.IsServerManaged,
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
                IsDraft = v.IsDraft,
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
        IUserContextService userContext)
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

        if (!files.Any(f => f.FileName == configuration.EntryPoint))
        {
            return TypedResults.BadRequest($"Entry point file '{configuration.EntryPoint}' not found in uploaded files");
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
            IsDraft = request.IsDraft,
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

        var versionDto = new ConfigurationVersionDto
        {
            Version = version.Version,
            IsDraft = version.IsDraft,
            PrereleaseChannel = version.PrereleaseChannel,
            FileCount = files.Count,
            CreatedAt = version.CreatedAt,
            CreatedBy = version.CreatedBy
        };

        return TypedResults.Created($"/api/v1/configurations/{name}/versions/{version.Version}", versionDto);
    }

    private static async Task<Results<Ok<ConfigurationVersionDto>, NotFound, BadRequest<string>, ForbidHttpResult>> PublishConfigurationVersion(
        string name,
        string version,
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
        if (userId == null || !await authService.CanModifyConfigurationAsync(userId.Value, config.Id))
        {
            return TypedResults.Forbid();
        }

        var configVersion = await db.ConfigurationVersions
            .Include(v => v.Files)
            .FirstOrDefaultAsync(v => v.ConfigurationId == config.Id && v.Version == version);

        if (configVersion is null)
        {
            return TypedResults.NotFound();
        }

        if (!configVersion.IsDraft)
        {
            return TypedResults.BadRequest("Version is already published");
        }

        if (configVersion.Files.Count == 0)
        {
            return TypedResults.BadRequest("Cannot publish version with no files");
        }

        configVersion.IsDraft = false;
        await db.SaveChangesAsync();

        var versionDto = new ConfigurationVersionDto
        {
            Version = configVersion.Version,
            IsDraft = configVersion.IsDraft,
            PrereleaseChannel = configVersion.PrereleaseChannel,
            FileCount = configVersion.Files.Count,
            CreatedAt = configVersion.CreatedAt,
            CreatedBy = configVersion.CreatedBy
        };

        return TypedResults.Ok(versionDto);
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

        if (!configVersion.IsDraft)
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
}

public sealed class ConfigurationSummaryDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string EntryPoint { get; init; }
    public required bool IsServerManaged { get; init; }
    public required int VersionCount { get; init; }
    public string? LatestVersion { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class ConfigurationDetailsDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string EntryPoint { get; init; }
    public required bool IsServerManaged { get; init; }
    public string? LatestVersion { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateConfigurationDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? EntryPoint { get; init; }
    public bool IsServerManaged { get; init; } = true;
    public string? Version { get; init; }
    public bool IsDraft { get; init; } = true;
}

public sealed class ConfigurationVersionDto
{
    public required string Version { get; init; }
    public required bool IsDraft { get; init; }
    public string? PrereleaseChannel { get; init; }
    public required int FileCount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public sealed class CreateConfigurationVersionDto
{
    public required string Version { get; init; }
    public bool IsDraft { get; init; } = true;
    public string? PrereleaseChannel { get; init; }
}
