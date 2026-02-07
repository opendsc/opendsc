// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authentication;
using OpenDsc.Server.Contracts;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Endpoints;

public static class CompositeConfigurationEndpoints
{
    public static void MapCompositeConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/composite-configurations")
            .RequireAuthorization(policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    PersonalAccessTokenHandler.SchemeName))
            .WithTags("Composite Configurations");

        group.MapGet("/", GetCompositeConfigurations)
            .WithName("GetCompositeConfigurations")
            .WithDescription("Get all composite configurations");

        group.MapPost("/", CreateCompositeConfiguration)
            .WithName("CreateCompositeConfiguration")
            .WithDescription("Create a new composite configuration");

        group.MapGet("/{name}", GetCompositeConfigurationDetails)
            .WithName("GetCompositeConfigurationDetails")
            .WithDescription("Get composite configuration details");

        group.MapDelete("/{name}", DeleteCompositeConfiguration)
            .WithName("DeleteCompositeConfiguration")
            .WithDescription("Delete a composite configuration and all its versions");

        group.MapPost("/{name}/versions", CreateCompositeConfigurationVersion)
            .WithName("CreateCompositeConfigurationVersion")
            .WithDescription("Create a new version of a composite configuration (draft)");

        group.MapGet("/{name}/versions", GetCompositeConfigurationVersions)
            .WithName("GetCompositeConfigurationVersions")
            .WithDescription("Get all versions of a composite configuration");

        group.MapGet("/{name}/versions/{version}", GetCompositeConfigurationVersionDetails)
            .WithName("GetCompositeConfigurationVersionDetails")
            .WithDescription("Get details of a specific composite configuration version");

        group.MapPut("/{name}/versions/{version}/publish", PublishCompositeConfigurationVersion)
            .WithName("PublishCompositeConfigurationVersion")
            .WithDescription("Publish a draft composite configuration version");

        group.MapDelete("/{name}/versions/{version}", DeleteCompositeConfigurationVersion)
            .WithName("DeleteCompositeConfigurationVersion")
            .WithDescription("Delete a specific version (only if draft and not active)");

        group.MapPost("/{name}/versions/{version}/children", AddChildConfiguration)
            .WithName("AddChildConfiguration")
            .WithDescription("Add a child configuration to a draft composite version");

        group.MapPut("/{name}/versions/{version}/children/{childId}", UpdateChildConfiguration)
            .WithName("UpdateChildConfiguration")
            .WithDescription("Update a child configuration in a draft composite version");

        group.MapDelete("/{name}/versions/{version}/children/{childId}", RemoveChildConfiguration)
            .WithName("RemoveChildConfiguration")
            .WithDescription("Remove a child configuration from a draft composite version");
    }

    private static async Task<Ok<List<CompositeConfigurationSummaryDto>>> GetCompositeConfigurations(
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null)
        {
            return TypedResults.Ok(new List<CompositeConfigurationSummaryDto>());
        }

        var readableIds = await authService.GetReadableCompositeConfigurationIdsAsync(userId.Value);

        var composites = await db.CompositeConfigurations
            .Where(c => readableIds.Contains(c.Id))
            .Include(c => c.Versions)
            .ToListAsync();

        var result = composites.Select(c => new CompositeConfigurationSummaryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            EntryPoint = c.EntryPoint,
            VersionCount = c.Versions.Count,
            LatestVersion = c.Versions.OrderByDescending(v => v.CreatedAt).Select(v => v.Version).FirstOrDefault(),
            CreatedAt = c.CreatedAt
        }).ToList();

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Created<CompositeConfigurationDetailsDto>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> CreateCompositeConfiguration(
        CreateCompositeConfigurationRequest request,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Composite configuration name is required" });
        }

        if (await db.CompositeConfigurations.AnyAsync(c => c.Name == request.Name))
        {
            return TypedResults.Conflict(new ErrorResponse { Error = $"Composite configuration '{request.Name}' already exists" });
        }

        var composite = new CompositeConfiguration
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            EntryPoint = request.EntryPoint,
            IsServerManaged = request.IsServerManaged,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.CompositeConfigurations.Add(composite);
        await db.SaveChangesAsync();

        var userId = userContext.GetCurrentUserId();
        if (userId.HasValue)
        {
            await authService.GrantCompositeConfigurationPermissionAsync(
                composite.Id,
                userId.Value,
                PrincipalType.User,
                ResourcePermission.Manage,
                userId.Value);
        }

        var details = new CompositeConfigurationDetailsDto
        {
            Id = composite.Id,
            Name = composite.Name,
            Description = composite.Description,
            EntryPoint = composite.EntryPoint,
            IsServerManaged = composite.IsServerManaged,
            Versions = [],
            CreatedAt = composite.CreatedAt,
            UpdatedAt = composite.UpdatedAt
        };

        return TypedResults.Created($"/api/v1/composite-configurations/{composite.Name}", details);
    }

    private static async Task<Results<Ok<CompositeConfigurationDetailsDto>, NotFound, ForbidHttpResult>> GetCompositeConfigurationDetails(
        string name,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var composite = await db.CompositeConfigurations
            .Include(c => c.Versions)
            .ThenInclude(v => v.Items)
            .ThenInclude(i => i.ChildConfiguration)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (composite is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanReadCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            return TypedResults.Forbid();
        }

        var details = new CompositeConfigurationDetailsDto
        {
            Id = composite.Id,
            Name = composite.Name,
            Description = composite.Description,
            EntryPoint = composite.EntryPoint,
            IsServerManaged = composite.IsServerManaged,
            Versions = composite.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new CompositeConfigurationVersionDto
            {
                Id = v.Id,
                Version = v.Version,
                IsDraft = v.IsDraft,
                IsArchived = v.IsArchived,
                PrereleaseChannel = v.PrereleaseChannel,
                Items = v.Items.OrderBy(i => i.Order).Select(i => new CompositeConfigurationItemDto
                {
                    Id = i.Id,
                    ChildConfigurationId = i.ChildConfigurationId,
                    ChildConfigurationName = i.ChildConfiguration.Name,
                    ActiveVersion = i.ActiveVersion,
                    Order = i.Order
                }).ToList(),
                CreatedAt = v.CreatedAt,
                CreatedBy = v.CreatedBy
            }).ToList(),
            CreatedAt = composite.CreatedAt,
            UpdatedAt = composite.UpdatedAt
        };

        return TypedResults.Ok(details);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ErrorResponse>, ForbidHttpResult>> DeleteCompositeConfiguration(
        string name,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var composite = await db.CompositeConfigurations
            .Include(c => c.NodeConfigurations)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (composite is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanManageCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            return TypedResults.Forbid();
        }

        if (composite.NodeConfigurations.Count > 0)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Cannot delete composite configuration that is assigned to nodes" });
        }

        db.CompositeConfigurations.Remove(composite);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<Created<CompositeConfigurationVersionDto>, NotFound, BadRequest<ErrorResponse>, Conflict<ErrorResponse>, ForbidHttpResult>> CreateCompositeConfigurationVersion(
        string name,
        CreateCompositeConfigurationVersionRequest request,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var composite = await db.CompositeConfigurations
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (composite is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanModifyCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            return TypedResults.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Version))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Version is required" });
        }

        if (composite.Versions.Any(v => v.Version == request.Version))
        {
            return TypedResults.Conflict(new ErrorResponse { Error = $"Version '{request.Version}' already exists for composite configuration '{name}'" });
        }

        var version = new CompositeConfigurationVersion
        {
            Id = Guid.NewGuid(),
            CompositeConfigurationId = composite.Id,
            Version = request.Version,
            IsDraft = true,
            PrereleaseChannel = request.PrereleaseChannel,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.CompositeConfigurationVersions.Add(version);

        composite.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        var dto = new CompositeConfigurationVersionDto
        {
            Id = version.Id,
            Version = version.Version,
            IsDraft = version.IsDraft,
            IsArchived = version.IsArchived,
            PrereleaseChannel = version.PrereleaseChannel,
            Items = [],
            CreatedAt = version.CreatedAt,
            CreatedBy = version.CreatedBy
        };

        return TypedResults.Created($"/api/v1/composite-configurations/{name}/versions/{version.Version}", dto);
    }

    private static async Task<Results<Ok<List<CompositeConfigurationVersionDto>>, NotFound, ForbidHttpResult>> GetCompositeConfigurationVersions(
        string name,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var composite = await db.CompositeConfigurations
            .Include(c => c.Versions)
            .ThenInclude(v => v.Items)
            .ThenInclude(i => i.ChildConfiguration)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (composite is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanReadCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            return TypedResults.Forbid();
        }

        var versions = composite.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new CompositeConfigurationVersionDto
        {
            Id = v.Id,
            Version = v.Version,
            IsDraft = v.IsDraft,
            IsArchived = v.IsArchived,
            PrereleaseChannel = v.PrereleaseChannel,
            Items = v.Items.OrderBy(i => i.Order).Select(i => new CompositeConfigurationItemDto
            {
                Id = i.Id,
                ChildConfigurationId = i.ChildConfigurationId,
                ChildConfigurationName = i.ChildConfiguration.Name,
                ActiveVersion = i.ActiveVersion,
                Order = i.Order
            }).ToList(),
            CreatedAt = v.CreatedAt,
            CreatedBy = v.CreatedBy
        }).ToList();

        return TypedResults.Ok(versions);
    }

    private static async Task<Results<Ok<CompositeConfigurationVersionDto>, NotFound, ForbidHttpResult>> GetCompositeConfigurationVersionDetails(
        string name,
        string version,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var compositeVersion = await db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .Include(v => v.Items)
            .ThenInclude(i => i.ChildConfiguration)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version);

        if (compositeVersion is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanReadCompositeConfigurationAsync(userId.Value, compositeVersion.CompositeConfigurationId))
        {
            return TypedResults.Forbid();
        }

        var dto = new CompositeConfigurationVersionDto
        {
            Id = compositeVersion.Id,
            Version = compositeVersion.Version,
            IsDraft = compositeVersion.IsDraft,
            IsArchived = compositeVersion.IsArchived,
            PrereleaseChannel = compositeVersion.PrereleaseChannel,
            Items = compositeVersion.Items.OrderBy(i => i.Order).Select(i => new CompositeConfigurationItemDto
            {
                Id = i.Id,
                ChildConfigurationId = i.ChildConfigurationId,
                ChildConfigurationName = i.ChildConfiguration.Name,
                ActiveVersion = i.ActiveVersion,
                Order = i.Order
            }).ToList(),
            CreatedAt = compositeVersion.CreatedAt,
            CreatedBy = compositeVersion.CreatedBy
        };

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok, NotFound, BadRequest<ErrorResponse>, ForbidHttpResult>> PublishCompositeConfigurationVersion(
        string name,
        string version,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var compositeVersion = await db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version);

        if (compositeVersion is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanModifyCompositeConfigurationAsync(userId.Value, compositeVersion.CompositeConfigurationId))
        {
            return TypedResults.Forbid();
        }

        if (!compositeVersion.IsDraft)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Version is already published" });
        }

        compositeVersion.IsDraft = false;
        compositeVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return TypedResults.Ok();
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ErrorResponse>, ForbidHttpResult>> DeleteCompositeConfigurationVersion(
        string name,
        string version,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var compositeVersion = await db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .Include(v => v.NodeConfigurations)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version);

        if (compositeVersion is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanManageCompositeConfigurationAsync(userId.Value, compositeVersion.CompositeConfigurationId))
        {
            return TypedResults.Forbid();
        }

        if (!compositeVersion.IsDraft)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Cannot delete published version" });
        }

        if (compositeVersion.NodeConfigurations.Count > 0)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Cannot delete version that is actively used by nodes" });
        }

        db.CompositeConfigurationVersions.Remove(compositeVersion);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<Created<CompositeConfigurationItemDto>, NotFound, BadRequest<ErrorResponse>, Conflict<ErrorResponse>, ForbidHttpResult>> AddChildConfiguration(
        string name,
        string version,
        AddChildConfigurationRequest request,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var compositeVersion = await db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version);

        if (compositeVersion is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanModifyCompositeConfigurationAsync(userId.Value, compositeVersion.CompositeConfigurationId))
        {
            return TypedResults.Forbid();
        }

        if (!compositeVersion.IsDraft)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Cannot modify published version" });
        }

        // Check if child is actually a composite (prevent nesting)
        var isComposite = await db.CompositeConfigurations
            .AnyAsync(c => c.Name == request.ChildConfigurationName);

        if (isComposite)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Cannot add a composite configuration as a child. Composite configurations can only contain regular configurations." });
        }

        var childConfig = await db.Configurations
            .FirstOrDefaultAsync(c => c.Name == request.ChildConfigurationName);

        if (childConfig is null)
        {
            return TypedResults.NotFound();
        }

        // Check if child already exists in this version
        if (compositeVersion.Items.Any(i => i.ChildConfigurationId == childConfig.Id))
        {
            return TypedResults.Conflict(new ErrorResponse { Error = $"Child configuration '{request.ChildConfigurationName}' is already in this composite version" });
        }

        // Validate ActiveVersion if provided
        if (!string.IsNullOrWhiteSpace(request.ActiveVersion))
        {
            var versionExists = await db.ConfigurationVersions
                .AnyAsync(v => v.Version == request.ActiveVersion && v.ConfigurationId == childConfig.Id);

            if (!versionExists)
            {
                return TypedResults.BadRequest(new ErrorResponse { Error = $"Invalid ActiveVersion for configuration '{request.ChildConfigurationName}'" });
            }
        }

        var item = new CompositeConfigurationItem
        {
            Id = Guid.NewGuid(),
            CompositeConfigurationVersionId = compositeVersion.Id,
            ChildConfigurationId = childConfig.Id,
            ActiveVersion = request.ActiveVersion,
            Order = request.Order
        };

        db.CompositeConfigurationItems.Add(item);
        compositeVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        // Reload to get navigation properties
        await db.Entry(item).Reference(i => i.ChildConfiguration).LoadAsync();

        var dto = new CompositeConfigurationItemDto
        {
            Id = item.Id,
            ChildConfigurationId = item.ChildConfigurationId,
            ChildConfigurationName = item.ChildConfiguration.Name,
            ActiveVersion = item.ActiveVersion,
            Order = item.Order
        };

        return TypedResults.Created($"/api/v1/composite-configurations/{name}/versions/{version}/children/{item.Id}", dto);
    }

    private static async Task<Results<Ok<CompositeConfigurationItemDto>, NotFound, BadRequest<ErrorResponse>, ForbidHttpResult>> UpdateChildConfiguration(
        string name,
        string version,
        Guid childId,
        UpdateChildConfigurationRequest request,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var item = await db.CompositeConfigurationItems
            .Include(i => i.CompositeConfigurationVersion)
            .ThenInclude(v => v.CompositeConfiguration)
            .Include(i => i.ChildConfiguration)
            .FirstOrDefaultAsync(i => i.Id == childId &&
                                     i.CompositeConfigurationVersion.CompositeConfiguration.Name == name &&
                                     i.CompositeConfigurationVersion.Version == version);

        if (item is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanModifyCompositeConfigurationAsync(userId.Value, item.CompositeConfigurationVersion.CompositeConfigurationId))
        {
            return TypedResults.Forbid();
        }

        if (!item.CompositeConfigurationVersion.IsDraft)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Cannot modify published version" });
        }

        // Validate ActiveVersion if provided
        if (!string.IsNullOrWhiteSpace(request.ActiveVersion))
        {
            var versionExists = await db.ConfigurationVersions
                .AnyAsync(v => v.Version == request.ActiveVersion && v.ConfigurationId == item.ChildConfigurationId);

            if (!versionExists)
            {
                return TypedResults.BadRequest(new ErrorResponse { Error = "Invalid ActiveVersion for this configuration" });
            }
        }

        item.ActiveVersion = request.ActiveVersion;
        item.Order = request.Order;
        item.CompositeConfigurationVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        var dto = new CompositeConfigurationItemDto
        {
            Id = item.Id,
            ChildConfigurationId = item.ChildConfigurationId,
            ChildConfigurationName = item.ChildConfiguration.Name,
            ActiveVersion = item.ActiveVersion,
            Order = item.Order
        };

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ErrorResponse>, ForbidHttpResult>> RemoveChildConfiguration(
        string name,
        string version,
        Guid childId,
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        var item = await db.CompositeConfigurationItems
            .Include(i => i.CompositeConfigurationVersion)
            .ThenInclude(v => v.CompositeConfiguration)
            .FirstOrDefaultAsync(i => i.Id == childId &&
                                     i.CompositeConfigurationVersion.CompositeConfiguration.Name == name &&
                                     i.CompositeConfigurationVersion.Version == version);

        if (item is null)
        {
            return TypedResults.NotFound();
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanModifyCompositeConfigurationAsync(userId.Value, item.CompositeConfigurationVersion.CompositeConfigurationId))
        {
            return TypedResults.Forbid();
        }

        if (!item.CompositeConfigurationVersion.IsDraft)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Cannot modify published version" });
        }

        db.CompositeConfigurationItems.Remove(item);
        item.CompositeConfigurationVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}
