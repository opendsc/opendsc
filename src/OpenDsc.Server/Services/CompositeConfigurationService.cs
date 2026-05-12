// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using NuGet.Versioning;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.CompositeConfigurations;
using OpenDsc.Contracts.Permissions;
using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public sealed class CompositeConfigurationService : ICompositeConfigurationService
{
    private readonly ServerDbContext _db;
    private readonly IResourceAuthorizationService _authService;
    private readonly IUserContextService _userContext;

    public CompositeConfigurationService(
        ServerDbContext db,
        IResourceAuthorizationService authService,
        IUserContextService userContext)
    {
        _db = db;
        _authService = authService;
        _userContext = userContext;
    }

    public async Task<List<CompositeConfigurationSummary>> GetCompositeConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == null)
        {
            return [];
        }

        var readableIds = await _authService.GetReadableCompositeConfigurationIdsAsync(userId.Value);

        var composites = await _db.CompositeConfigurations
            .Where(c => readableIds.Contains(c.Id))
            .Include(c => c.Versions)
            .ToListAsync(cancellationToken);

        return composites.Select(c => new CompositeConfigurationSummary
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            EntryPoint = c.EntryPoint,
            VersionCount = c.Versions.Count,
            LatestVersion = VersionResolver.LatestSemver(c.Versions.Select(v => v.Version)),
            HasPublishedVersion = c.Versions.Any(v => v.Status == ConfigurationVersionStatus.Published),
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();
    }

    public async Task<CompositeConfigurationDetails?> GetCompositeConfigurationAsync(string name, CancellationToken cancellationToken = default)
    {
        var composite = await _db.CompositeConfigurations
            .Include(c => c.Versions)
                .ThenInclude(v => v.Items)
                    .ThenInclude(i => i.ChildConfiguration)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);

        if (composite is null)
        {
            return null;
        }

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanReadCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{name}'.");
        }

        return MapToDetails(composite);
    }

    public async Task<List<CompositeConfigurationVersionDetails>?> GetVersionsAsync(string name, CancellationToken cancellationToken = default)
    {
        var composite = await _db.CompositeConfigurations
            .Include(c => c.Versions)
                .ThenInclude(v => v.Items)
                    .ThenInclude(i => i.ChildConfiguration)
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);

        if (composite is null)
        {
            return null;
        }

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanReadCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{name}'.");
        }

        return composite.Versions
            .OrderByDescending(v => v.CreatedAt)
            .Select(MapToVersion)
            .ToList();
    }

    public async Task<CompositeConfigurationVersionDetails?> GetVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        var compositeVersion = await _db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .Include(v => v.Items)
                .ThenInclude(i => i.ChildConfiguration)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version, cancellationToken);

        if (compositeVersion is null)
        {
            return null;
        }

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanReadCompositeConfigurationAsync(userId.Value, compositeVersion.CompositeConfigurationId))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{name}'.");
        }

        return MapToVersion(compositeVersion);
    }

    public async Task<List<ChildConfigurationOption>> GetAvailableChildConfigurationsAsync(IEnumerable<Guid> excludeIds, CancellationToken cancellationToken = default)
    {
        var excludeList = excludeIds.ToList();

        var configs = await _db.Configurations
            .Include(c => c.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .Where(c => !excludeList.Contains(c.Id) && c.Versions.Any(v => v.Status == ConfigurationVersionStatus.Published))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return configs.Select(c => new ChildConfigurationOption
        {
            Id = c.Id,
            Name = c.Name,
            AvailableMajorVersions = c.Versions
                .Select(v => SemanticVersion.TryParse(v.Version, out var sv) ? sv.Major : -1)
                .Where(m => m >= 0)
                .Distinct()
                .OrderBy(m => m)
                .ToList()
        }).ToList();
    }

    public async Task<List<int>> GetAvailableMajorVersionsAsync(Guid configurationId, CancellationToken cancellationToken = default)
    {
        var versions = await _db.ConfigurationVersions
            .Where(v => v.ConfigurationId == configurationId && v.Status == ConfigurationVersionStatus.Published)
            .Select(v => v.Version)
            .ToListAsync(cancellationToken);

        return versions
            .Select(v => SemanticVersion.TryParse(v, out var sv) ? sv.Major : -1)
            .Where(m => m >= 0)
            .Distinct()
            .OrderBy(m => m)
            .ToList();
    }

    public async Task<CompositeConfigurationDetails> CreateAsync(
        CreateCompositeConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Composite configuration name is required.", nameof(request));
        }

        if (await _db.CompositeConfigurations.AnyAsync(c => c.Name == request.Name, cancellationToken))
        {
            throw new InvalidOperationException($"Composite configuration '{request.Name}' already exists.");
        }

        var composite = new CompositeConfiguration
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
            EntryPoint = request.EntryPoint ?? "main.dsc.yaml",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.CompositeConfigurations.Add(composite);
        await _db.SaveChangesAsync(cancellationToken);

        var userId = _userContext.GetCurrentUserId();
        if (userId.HasValue && !await _authService.HasGlobalPermissionAsync(userId.Value, CompositeConfigurationPermissions.AdminOverride))
        {
            await _authService.GrantCompositeConfigurationPermissionAsync(
                composite.Id,
                userId.Value,
                PrincipalType.User,
                ResourcePermission.Manage,
                userId.Value);
        }

        return new CompositeConfigurationDetails
        {
            Id = composite.Id,
            Name = composite.Name,
            Description = composite.Description,
            EntryPoint = composite.EntryPoint,
            Versions = [],
            CreatedAt = composite.CreatedAt,
            UpdatedAt = composite.UpdatedAt
        };
    }

    public async Task<CompositeConfigurationVersionDetails> CreateVersionAsync(
        string name,
        CreateCompositeConfigurationVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!SemanticVersion.TryParse(request.Version, out var sv))
        {
            throw new ArgumentException($"Version '{request.Version}' is not a valid semantic version.", nameof(request));
        }

        var composite = await _db.CompositeConfigurations
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Composite configuration '{name}' not found.");

        if (composite.Versions.Any(v => v.Version == request.Version))
        {
            throw new InvalidOperationException($"Version '{request.Version}' already exists.");
        }

        var newVersion = new CompositeConfigurationVersion
        {
            Id = Guid.NewGuid(),
            CompositeConfigurationId = composite.Id,
            Version = request.Version,
            PrereleaseChannel = string.IsNullOrEmpty(sv.Release) ? request.PrereleaseChannel : sv.Release,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.CompositeConfigurationVersions.Add(newVersion);
        composite.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new CompositeConfigurationVersionDetails
        {
            Id = newVersion.Id,
            Version = newVersion.Version,
            Status = newVersion.Status,
            PrereleaseChannel = newVersion.PrereleaseChannel,
            Items = [],
            CreatedAt = newVersion.CreatedAt,
            CreatedBy = newVersion.CreatedBy
        };
    }

    public async Task CreateVersionFromExistingAsync(
        string name,
        CreateCompositeVersionFromExistingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!SemanticVersion.TryParse(request.NewVersion, out var sv))
        {
            throw new ArgumentException($"Version '{request.NewVersion}' is not a valid semantic version.", nameof(request));
        }

        var composite = await _db.CompositeConfigurations
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Composite configuration '{name}' not found.");

        if (await _db.CompositeConfigurationVersions.AnyAsync(v =>
                v.CompositeConfigurationId == composite.Id && v.Version == request.NewVersion, cancellationToken))
        {
            throw new InvalidOperationException($"Version '{request.NewVersion}' already exists.");
        }

        var source = await _db.CompositeConfigurationVersions
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.CompositeConfigurationId == composite.Id && v.Version == request.SourceVersion, cancellationToken)
            ?? throw new KeyNotFoundException($"Source version '{request.SourceVersion}' not found.");

        var version = new CompositeConfigurationVersion
        {
            Id = Guid.NewGuid(),
            CompositeConfigurationId = composite.Id,
            Version = request.NewVersion,
            PrereleaseChannel = string.IsNullOrEmpty(sv.Release) ? null : sv.Release,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.CompositeConfigurationVersions.Add(version);

        foreach (var item in source.Items.OrderBy(i => i.Order))
        {
            _db.CompositeConfigurationItems.Add(new CompositeConfigurationItem
            {
                Id = Guid.NewGuid(),
                CompositeConfigurationVersionId = version.Id,
                ChildConfigurationId = item.ChildConfigurationId,
                MajorVersion = item.MajorVersion,
                Order = item.Order
            });
        }

        composite.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task PublishVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        var compositeVersion = await _db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version, cancellationToken)
            ?? throw new KeyNotFoundException($"Version '{version}' not found.");

        if (compositeVersion.Status != ConfigurationVersionStatus.Draft)
        {
            throw new InvalidOperationException($"Version '{version}' is already published.");
        }

        if (!compositeVersion.Items.Any())
        {
            throw new InvalidOperationException("Cannot publish a composite configuration version with no child configurations.");
        }

        compositeVersion.Status = ConfigurationVersionStatus.Published;
        compositeVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        var composite = await _db.CompositeConfigurations
            .Include(c => c.NodeConfigurations)
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Composite configuration '{name}' not found.");

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanManageCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{name}'.");
        }

        if (composite.NodeConfigurations.Count > 0)
        {
            throw new InvalidOperationException("Cannot delete composite configuration that is assigned to nodes.");
        }

        _db.CompositeConfigurations.Remove(composite);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        var compositeVersion = await _db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .Include(v => v.NodeConfigurations)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version, cancellationToken)
            ?? throw new KeyNotFoundException($"Version '{version}' not found.");

        if (compositeVersion.Status != ConfigurationVersionStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot delete published version '{version}'.");
        }

        if (compositeVersion.NodeConfigurations.Count > 0)
        {
            throw new InvalidOperationException($"Version '{version}' is actively used by nodes and cannot be deleted.");
        }

        _db.CompositeConfigurationVersions.Remove(compositeVersion);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CompositeConfigurationItemDetails> AddChildAsync(
        string name,
        string version,
        AddChildConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        var compositeVersion = await _db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version, cancellationToken)
            ?? throw new KeyNotFoundException($"Composite configuration version '{version}' not found.");

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanModifyCompositeConfigurationAsync(userId.Value, compositeVersion.CompositeConfigurationId))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{name}'.");
        }

        if (compositeVersion.Status != ConfigurationVersionStatus.Draft)
        {
            throw new InvalidOperationException("Cannot add children to a published version.");
        }

        if (await _db.CompositeConfigurations.AnyAsync(c => c.Name == request.ChildConfigurationName, cancellationToken))
        {
            throw new InvalidOperationException("Cannot add a composite configuration as a child. Only regular configurations are allowed.");
        }

        var childConfig = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == request.ChildConfigurationName, cancellationToken)
            ?? throw new KeyNotFoundException($"Child configuration '{request.ChildConfigurationName}' not found.");

        if (compositeVersion.Items.Any(i => i.ChildConfigurationId == childConfig.Id))
        {
            throw new InvalidOperationException($"Child configuration '{request.ChildConfigurationName}' is already in this composite version.");
        }

        var publishedVersions = await _db.ConfigurationVersions
            .Where(v => v.ConfigurationId == childConfig.Id && v.Status == ConfigurationVersionStatus.Published)
            .Select(v => v.Version)
            .ToListAsync(cancellationToken);

        var majorVersionExists = publishedVersions.Any(v =>
        {
            var match = System.Text.RegularExpressions.Regex.Match(v, @"^(\d+)");
            return match.Success && int.TryParse(match.Groups[1].Value, out var major) && major == request.MajorVersion;
        });

        if (!majorVersionExists)
        {
            throw new InvalidOperationException($"No published version with major version {request.MajorVersion} found for configuration '{request.ChildConfigurationName}'.");
        }

        var item = new CompositeConfigurationItem
        {
            Id = Guid.NewGuid(),
            CompositeConfigurationVersionId = compositeVersion.Id,
            ChildConfigurationId = childConfig.Id,
            MajorVersion = request.MajorVersion,
            Order = request.Order
        };

        _db.CompositeConfigurationItems.Add(item);
        compositeVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new CompositeConfigurationItemDetails
        {
            Id = item.Id,
            ChildConfigurationId = item.ChildConfigurationId,
            ChildConfigurationName = childConfig.Name,
            MajorVersion = item.MajorVersion,
            Order = item.Order
        };
    }

    public async Task<CompositeConfigurationItemDetails> UpdateChildAsync(
        Guid itemId,
        UpdateChildConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.CompositeConfigurationItems
            .Include(i => i.CompositeConfigurationVersion)
                .ThenInclude(v => v.CompositeConfiguration)
            .Include(i => i.ChildConfiguration)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken)
            ?? throw new KeyNotFoundException("Child configuration item not found.");

        if (item.CompositeConfigurationVersion.Status != ConfigurationVersionStatus.Draft)
        {
            throw new InvalidOperationException("Cannot modify a published version.");
        }

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanModifyCompositeConfigurationAsync(userId.Value, item.CompositeConfigurationVersion.CompositeConfigurationId))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{item.CompositeConfigurationVersion.CompositeConfiguration.Name}'.");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveVersion))
        {
            var versionExists = await _db.ConfigurationVersions.AnyAsync(
                v => v.ConfigurationId == item.ChildConfigurationId &&
                     v.Version == request.ActiveVersion &&
                     v.Status == ConfigurationVersionStatus.Published,
                cancellationToken);

            if (!versionExists)
            {
                throw new InvalidOperationException($"Invalid ActiveVersion '{request.ActiveVersion}'. A published version for child configuration '{item.ChildConfiguration.Name}' was not found.");
            }
        }

        item.ActiveVersion = request.ActiveVersion;
        item.Order = request.Order;
        item.CompositeConfigurationVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new CompositeConfigurationItemDetails
        {
            Id = item.Id,
            ChildConfigurationId = item.ChildConfigurationId,
            ChildConfigurationName = item.ChildConfiguration.Name,
            ActiveVersion = item.ActiveVersion,
            MajorVersion = item.MajorVersion,
            Order = item.Order
        };
    }

    public async Task RemoveChildAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.CompositeConfigurationItems
            .Include(i => i.CompositeConfigurationVersion)
                .ThenInclude(v => v.CompositeConfiguration)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken)
            ?? throw new KeyNotFoundException("Child configuration item not found.");

        if (item.CompositeConfigurationVersion.Status != ConfigurationVersionStatus.Draft)
        {
            throw new InvalidOperationException("Cannot modify a published version.");
        }

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanModifyCompositeConfigurationAsync(userId.Value, item.CompositeConfigurationVersion.CompositeConfigurationId))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{item.CompositeConfigurationVersion.CompositeConfiguration.Name}'.");
        }

        _db.CompositeConfigurationItems.Remove(item);
        item.CompositeConfigurationVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderChildAsync(Guid itemId, int newOrder, CancellationToken cancellationToken = default)
    {
        var item = await _db.CompositeConfigurationItems
            .Include(i => i.CompositeConfigurationVersion)
                .ThenInclude(v => v.CompositeConfiguration)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken)
            ?? throw new KeyNotFoundException("Child configuration item not found.");

        if (item.CompositeConfigurationVersion.Status != ConfigurationVersionStatus.Draft)
        {
            throw new InvalidOperationException("Cannot modify a published version.");
        }

        item.Order = newOrder;
        item.CompositeConfigurationVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<PermissionEntry>?> GetPermissionsAsync(string name, CancellationToken cancellationToken = default)
    {
        var composite = await _db.CompositeConfigurations.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
        if (composite is null)
        {
            return null;
        }

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanManageCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{name}'.");
        }

        var acl = await _authService.GetCompositeConfigurationAclAsync(composite.Id);
        return await BuildPermissionEntriesAsync(acl.Select(p => (p.PrincipalType, p.PrincipalId, p.PermissionLevel, p.GrantedAt, p.GrantedByUserId)), cancellationToken);
    }

    public async Task GrantPermissionAsync(string name, GrantPermissionRequest request, CancellationToken cancellationToken = default)
    {
        var composite = await _db.CompositeConfigurations.FirstOrDefaultAsync(c => c.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Composite configuration '{name}' not found.");

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanManageCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{name}'.");
        }

        if (!Enum.TryParse<PrincipalType>(request.PrincipalType, ignoreCase: true, out var parsedPrincipalType))
        {
            throw new ArgumentException($"Invalid principal type '{request.PrincipalType}'. Must be 'User' or 'Group'.", nameof(request));
        }

        if (!Enum.TryParse<ResourcePermission>(request.Level, ignoreCase: true, out var parsedLevel))
        {
            throw new ArgumentException($"Invalid permission level '{request.Level}'. Must be 'Read', 'Modify', or 'Manage'.", nameof(request));
        }

        if (parsedPrincipalType == PrincipalType.User && !await _db.Users.AnyAsync(u => u.Id == request.PrincipalId, cancellationToken))
        {
            throw new KeyNotFoundException($"User '{request.PrincipalId}' not found.");
        }

        if (parsedPrincipalType == PrincipalType.Group && !await _db.Groups.AnyAsync(g => g.Id == request.PrincipalId, cancellationToken))
        {
            throw new KeyNotFoundException($"Group '{request.PrincipalId}' not found.");
        }

        await _authService.GrantCompositeConfigurationPermissionAsync(composite.Id, request.PrincipalId, parsedPrincipalType, parsedLevel, userId.Value);
    }

    public async Task RevokePermissionAsync(string name, RevokePermissionRequest request, CancellationToken cancellationToken = default)
    {
        var composite = await _db.CompositeConfigurations.FirstOrDefaultAsync(c => c.Name == name, cancellationToken)
            ?? throw new KeyNotFoundException($"Composite configuration '{name}' not found.");

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanManageCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{name}'.");
        }

        if (!Enum.TryParse<PrincipalType>(request.PrincipalType, ignoreCase: true, out var parsedPrincipalType))
        {
            throw new ArgumentException($"Invalid principal type '{request.PrincipalType}'. Must be 'User' or 'Group'.", nameof(request));
        }

        await _authService.RevokeCompositeConfigurationPermissionAsync(composite.Id, request.PrincipalId, parsedPrincipalType);
    }

    private async Task<List<PermissionEntry>> BuildPermissionEntriesAsync(
        IEnumerable<(PrincipalType PrincipalType, Guid PrincipalId, ResourcePermission Level, DateTimeOffset GrantedAt, Guid? GrantedByUserId)> entries,
        CancellationToken cancellationToken)
    {
        var list = entries.ToList();
        var userIds = list.Where(e => e.PrincipalType == PrincipalType.User).Select(e => e.PrincipalId).ToList();
        var groupIds = list.Where(e => e.PrincipalType == PrincipalType.Group).Select(e => e.PrincipalId).ToList();

        var userNames = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

        var groupNames = await _db.Groups
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken);

        return list.Select(e => new PermissionEntry
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

    private static CompositeConfigurationDetails MapToDetails(CompositeConfiguration composite) =>
        new()
        {
            Id = composite.Id,
            Name = composite.Name,
            Description = composite.Description,
            EntryPoint = composite.EntryPoint,
            Versions = composite.Versions
                .OrderByDescending(v => v.CreatedAt)
                .Select(MapToVersion)
                .ToList(),
            CreatedAt = composite.CreatedAt,
            UpdatedAt = composite.UpdatedAt
        };

    private static CompositeConfigurationVersionDetails MapToVersion(CompositeConfigurationVersion v) =>
        new()
        {
            Id = v.Id,
            Version = v.Version,
            Status = v.Status,
            PrereleaseChannel = v.PrereleaseChannel,
            Items = v.Items
                .OrderBy(i => i.Order)
                .Select(i => new CompositeConfigurationItemDetails
                {
                    Id = i.Id,
                    ChildConfigurationId = i.ChildConfigurationId,
                    ChildConfigurationName = i.ChildConfiguration?.Name ?? string.Empty,
                    ActiveVersion = i.ActiveVersion,
                    MajorVersion = i.MajorVersion,
                    Order = i.Order
                })
                .ToList(),
            CreatedAt = v.CreatedAt,
            CreatedBy = v.CreatedBy
        };
}
