// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using NuGet.Versioning;

using OpenDsc.Server.Authorization;
using OpenDsc.Contracts.CompositeConfigurations;
using OpenDsc.Contracts.Permissions;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public interface ICompositeConfigurationService
{
    Task<List<CompositeConfigurationSummary>> GetCompositeConfigurationsAsync();
    Task<CompositeConfigurationDetails?> GetCompositeConfigurationAsync(string name);
    Task<List<CompositeConfigurationVersionDetails>?> GetVersionsAsync(string name);
    Task<CompositeConfigurationVersionDetails?> GetVersionAsync(string name, string version);
    Task<List<ChildConfigurationOption>> GetAvailableChildConfigurationsAsync(IEnumerable<Guid> excludeIds);
    Task<List<int>> GetAvailableMajorVersionsAsync(Guid configurationId);
    Task<CompositeConfigurationDetails> CreateCompositeConfigurationAsync(string name, string? description);
    Task<CompositeConfigurationVersionDetails> CreateVersionAsync(string name, string version);
    Task CreateVersionFromExistingAsync(string name, string sourceVersion, string newVersion);
    Task PublishVersionAsync(string name, string version);
    Task DeleteCompositeConfigurationAsync(Guid compositeId);
    Task DeleteCompositeConfigurationByNameAsync(string name);
    Task DeleteVersionAsync(string name, string version);
    Task<CompositeConfigurationItemDetails> AddChildAsync(string name, string version, Guid childConfigId, int? majorVersion, int order);
    Task<CompositeConfigurationItemDetails> AddChildByNameAsync(string name, string version, string childConfigurationName, int majorVersion, int order);
    Task<CompositeConfigurationItemDetails> UpdateChildAsync(Guid itemId, int? majorVersion, int order);
    Task RemoveChildAsync(Guid itemId);
    Task ReorderChildAsync(Guid itemId, int newOrder);
    Task<List<PermissionEntry>?> GetPermissionsAsync(string compositeName);
    Task GrantPermissionAsync(string compositeName, Guid principalId, string principalType, string level);
    Task RevokePermissionAsync(string compositeName, Guid principalId, string principalType);
}

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

    public async Task<List<CompositeConfigurationSummary>> GetCompositeConfigurationsAsync()
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
            .ToListAsync();

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

    public async Task<CompositeConfigurationDetails?> GetCompositeConfigurationAsync(string name)
    {
        var composite = await _db.CompositeConfigurations
            .Include(c => c.Versions)
                .ThenInclude(v => v.Items)
                    .ThenInclude(i => i.ChildConfiguration)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Name == name);

        if (composite is null)
        {
            return null;
        }

        return MapToDetailsDto(composite);
    }

    public async Task<List<CompositeConfigurationVersionDetails>?> GetVersionsAsync(string name)
    {
        var composite = await _db.CompositeConfigurations
            .Include(c => c.Versions)
                .ThenInclude(v => v.Items)
                    .ThenInclude(i => i.ChildConfiguration)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (composite is null)
        {
            return null;
        }

        return composite.Versions
            .OrderByDescending(v => v.CreatedAt)
            .Select(MapToVersionDto)
            .ToList();
    }

    public async Task<CompositeConfigurationVersionDetails?> GetVersionAsync(string name, string version)
    {
        var compositeVersion = await _db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .Include(v => v.Items)
                .ThenInclude(i => i.ChildConfiguration)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version);

        if (compositeVersion is null)
        {
            return null;
        }

        return MapToVersionDto(compositeVersion);
    }

    public async Task<List<ChildConfigurationOption>> GetAvailableChildConfigurationsAsync(IEnumerable<Guid> excludeIds)
    {
        var excludeList = excludeIds.ToList();

        var configs = await _db.Configurations
            .Include(c => c.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .Where(c => !excludeList.Contains(c.Id) && c.Versions.Any(v => v.Status == ConfigurationVersionStatus.Published))
            .OrderBy(c => c.Name)
            .ToListAsync();

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

    public async Task<List<int>> GetAvailableMajorVersionsAsync(Guid configurationId)
    {
        var versions = await _db.ConfigurationVersions
            .Where(v => v.ConfigurationId == configurationId && v.Status == ConfigurationVersionStatus.Published)
            .Select(v => v.Version)
            .ToListAsync();

        return versions
            .Select(v => SemanticVersion.TryParse(v, out var sv) ? sv.Major : -1)
            .Where(m => m >= 0)
            .Distinct()
            .OrderBy(m => m)
            .ToList();
    }

    public async Task<CompositeConfigurationDetails> CreateCompositeConfigurationAsync(
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Composite configuration name is required.", nameof(name));
        }

        if (await _db.CompositeConfigurations.AnyAsync(c => c.Name == name))
        {
            throw new InvalidOperationException($"Composite configuration '{name}' already exists.");
        }

        var composite = new CompositeConfiguration
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            EntryPoint = "main.dsc.yaml",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.CompositeConfigurations.Add(composite);
        await _db.SaveChangesAsync();

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
        string version)
    {
        if (!SemanticVersion.TryParse(version, out var sv))
        {
            throw new ArgumentException($"Version '{version}' is not a valid semantic version.", nameof(version));
        }

        var composite = await _db.CompositeConfigurations
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Name == name)
            ?? throw new KeyNotFoundException($"Composite configuration '{name}' not found.");

        if (composite.Versions.Any(v => v.Version == version))
        {
            throw new InvalidOperationException($"Version '{version}' already exists.");
        }

        var newVersion = new CompositeConfigurationVersion
        {
            Id = Guid.NewGuid(),
            CompositeConfigurationId = composite.Id,
            Version = version,
            PrereleaseChannel = string.IsNullOrEmpty(sv.Release) ? null : sv.Release,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.CompositeConfigurationVersions.Add(newVersion);
        composite.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

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
        string sourceVersion,
        string newVersion)
    {
        if (!SemanticVersion.TryParse(newVersion, out var sv))
        {
            throw new ArgumentException($"Version '{newVersion}' is not a valid semantic version.", nameof(newVersion));
        }

        var composite = await _db.CompositeConfigurations
            .FirstOrDefaultAsync(c => c.Name == name)
            ?? throw new KeyNotFoundException($"Composite configuration '{name}' not found.");

        if (await _db.CompositeConfigurationVersions.AnyAsync(v =>
                v.CompositeConfigurationId == composite.Id && v.Version == newVersion))
        {
            throw new InvalidOperationException($"Version '{newVersion}' already exists.");
        }

        var source = await _db.CompositeConfigurationVersions
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.CompositeConfigurationId == composite.Id && v.Version == sourceVersion)
            ?? throw new KeyNotFoundException($"Source version '{sourceVersion}' not found.");

        var version = new CompositeConfigurationVersion
        {
            Id = Guid.NewGuid(),
            CompositeConfigurationId = composite.Id,
            Version = newVersion,
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
        await _db.SaveChangesAsync();
    }

    public async Task PublishVersionAsync(string name, string version)
    {
        var compositeVersion = await _db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version)
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

        await _db.SaveChangesAsync();
    }

    public async Task DeleteCompositeConfigurationAsync(Guid compositeId)
    {
        var composite = await _db.CompositeConfigurations
            .Include(c => c.NodeConfigurations)
            .FirstOrDefaultAsync(c => c.Id == compositeId)
            ?? throw new KeyNotFoundException("Composite configuration not found.");

        if (composite.NodeConfigurations.Count > 0)
        {
            throw new InvalidOperationException($"'{composite.Name}' is currently assigned to one or more nodes. Unassign it before deleting.");
        }

        _db.CompositeConfigurations.Remove(composite);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteVersionAsync(string name, string version)
    {
        var compositeVersion = await _db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .Include(v => v.NodeConfigurations)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version)
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
        await _db.SaveChangesAsync();
    }

    public async Task<CompositeConfigurationItemDetails> AddChildAsync(
        string name,
        string version,
        Guid childConfigId,
        int? majorVersion,
        int order)
    {
        var compositeVersion = await _db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version)
            ?? throw new KeyNotFoundException($"Composite configuration version '{version}' not found.");

        if (compositeVersion.Status != ConfigurationVersionStatus.Draft)
        {
            throw new InvalidOperationException("Cannot add children to a published version.");
        }

        if (await _db.CompositeConfigurations.AnyAsync(c => c.Id == childConfigId))
        {
            throw new InvalidOperationException("Cannot add a composite configuration as a child. Only regular configurations are allowed.");
        }

        var childConfig = await _db.Configurations.FirstOrDefaultAsync(c => c.Id == childConfigId)
            ?? throw new KeyNotFoundException("Child configuration not found.");

        if (compositeVersion.Items.Any(i => i.ChildConfigurationId == childConfigId))
        {
            throw new InvalidOperationException($"Child configuration '{childConfig.Name}' is already in this composite version.");
        }

        var item = new CompositeConfigurationItem
        {
            Id = Guid.NewGuid(),
            CompositeConfigurationVersionId = compositeVersion.Id,
            ChildConfigurationId = childConfigId,
            MajorVersion = majorVersion,
            Order = order
        };

        _db.CompositeConfigurationItems.Add(item);
        compositeVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

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
        int? majorVersion,
        int order)
    {
        var item = await _db.CompositeConfigurationItems
            .Include(i => i.CompositeConfigurationVersion)
                .ThenInclude(v => v.CompositeConfiguration)
            .Include(i => i.ChildConfiguration)
            .FirstOrDefaultAsync(i => i.Id == itemId)
            ?? throw new KeyNotFoundException("Child configuration item not found.");

        if (item.CompositeConfigurationVersion.Status != ConfigurationVersionStatus.Draft)
        {
            throw new InvalidOperationException("Cannot modify a published version.");
        }

        item.MajorVersion = majorVersion;
        item.Order = order;
        item.CompositeConfigurationVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        return new CompositeConfigurationItemDetails
        {
            Id = item.Id,
            ChildConfigurationId = item.ChildConfigurationId,
            ChildConfigurationName = item.ChildConfiguration.Name,
            MajorVersion = item.MajorVersion,
            Order = item.Order
        };
    }

    public async Task RemoveChildAsync(Guid itemId)
    {
        var item = await _db.CompositeConfigurationItems
            .Include(i => i.CompositeConfigurationVersion)
                .ThenInclude(v => v.CompositeConfiguration)
            .FirstOrDefaultAsync(i => i.Id == itemId)
            ?? throw new KeyNotFoundException("Child configuration item not found.");

        if (item.CompositeConfigurationVersion.Status != ConfigurationVersionStatus.Draft)
        {
            throw new InvalidOperationException("Cannot modify a published version.");
        }

        _db.CompositeConfigurationItems.Remove(item);
        item.CompositeConfigurationVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task ReorderChildAsync(Guid itemId, int newOrder)
    {
        var item = await _db.CompositeConfigurationItems
            .Include(i => i.CompositeConfigurationVersion)
                .ThenInclude(v => v.CompositeConfiguration)
            .FirstOrDefaultAsync(i => i.Id == itemId)
            ?? throw new KeyNotFoundException("Child configuration item not found.");

        if (item.CompositeConfigurationVersion.Status != ConfigurationVersionStatus.Draft)
        {
            throw new InvalidOperationException("Cannot modify a published version.");
        }

        item.Order = newOrder;
        item.CompositeConfigurationVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteCompositeConfigurationByNameAsync(string name)
    {
        var composite = await _db.CompositeConfigurations
            .Include(c => c.NodeConfigurations)
            .FirstOrDefaultAsync(c => c.Name == name)
            ?? throw new KeyNotFoundException($"Composite configuration '{name}' not found.");

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanManageCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{name}'.");
        }

        if (composite.NodeConfigurations.Count > 0)
        {
            throw new InvalidOperationException($"Cannot delete composite configuration that is assigned to nodes.");
        }

        _db.CompositeConfigurations.Remove(composite);
        await _db.SaveChangesAsync();
    }

    public async Task<CompositeConfigurationItemDetails> AddChildByNameAsync(
        string name,
        string version,
        string childConfigurationName,
        int majorVersion,
        int order)
    {
        var compositeVersion = await _db.CompositeConfigurationVersions
            .Include(v => v.CompositeConfiguration)
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.CompositeConfiguration.Name == name && v.Version == version)
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

        if (await _db.CompositeConfigurations.AnyAsync(c => c.Name == childConfigurationName))
        {
            throw new InvalidOperationException("Cannot add a composite configuration as a child. Only regular configurations are allowed.");
        }

        var childConfig = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == childConfigurationName)
            ?? throw new KeyNotFoundException($"Child configuration '{childConfigurationName}' not found.");

        if (compositeVersion.Items.Any(i => i.ChildConfigurationId == childConfig.Id))
        {
            throw new InvalidOperationException($"Child configuration '{childConfigurationName}' is already in this composite version.");
        }

        var publishedVersions = await _db.ConfigurationVersions
            .Where(v => v.ConfigurationId == childConfig.Id && v.Status == ConfigurationVersionStatus.Published)
            .Select(v => v.Version)
            .ToListAsync();

        var majorVersionExists = publishedVersions.Any(v =>
        {
            var match = System.Text.RegularExpressions.Regex.Match(v, @"^(\d+)");
            return match.Success && int.TryParse(match.Groups[1].Value, out var major) && major == majorVersion;
        });

        if (!majorVersionExists)
        {
            throw new InvalidOperationException($"No published version with major version {majorVersion} found for configuration '{childConfigurationName}'.");
        }

        var item = new CompositeConfigurationItem
        {
            Id = Guid.NewGuid(),
            CompositeConfigurationVersionId = compositeVersion.Id,
            ChildConfigurationId = childConfig.Id,
            MajorVersion = majorVersion,
            Order = order
        };

        _db.CompositeConfigurationItems.Add(item);
        compositeVersion.CompositeConfiguration.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return new CompositeConfigurationItemDetails
        {
            Id = item.Id,
            ChildConfigurationId = item.ChildConfigurationId,
            ChildConfigurationName = childConfig.Name,
            MajorVersion = item.MajorVersion,
            Order = item.Order
        };
    }

    public async Task<List<PermissionEntry>?> GetPermissionsAsync(string compositeName)
    {
        var composite = await _db.CompositeConfigurations.FirstOrDefaultAsync(c => c.Name == compositeName);
        if (composite is null)
        {
            return null;
        }

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanManageCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{compositeName}'.");
        }

        var acl = await _authService.GetCompositeConfigurationAclAsync(composite.Id);
        return await BuildPermissionEntriesAsync(acl.Select(p => (p.PrincipalType, p.PrincipalId, p.PermissionLevel, p.GrantedAt, p.GrantedByUserId)));
    }

    public async Task GrantPermissionAsync(string compositeName, Guid principalId, string principalType, string level)
    {
        var composite = await _db.CompositeConfigurations.FirstOrDefaultAsync(c => c.Name == compositeName)
            ?? throw new KeyNotFoundException($"Composite configuration '{compositeName}' not found.");

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanManageCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{compositeName}'.");
        }

        if (!Enum.TryParse<PrincipalType>(principalType, ignoreCase: true, out var parsedPrincipalType))
        {
            throw new ArgumentException($"Invalid principal type '{principalType}'. Must be 'User' or 'Group'.", nameof(principalType));
        }

        if (!Enum.TryParse<ResourcePermission>(level, ignoreCase: true, out var parsedLevel))
        {
            throw new ArgumentException($"Invalid permission level '{level}'. Must be 'Read', 'Modify', or 'Manage'.", nameof(level));
        }

        if (parsedPrincipalType == PrincipalType.User && !await _db.Users.AnyAsync(u => u.Id == principalId))
        {
            throw new KeyNotFoundException($"User '{principalId}' not found.");
        }

        if (parsedPrincipalType == PrincipalType.Group && !await _db.Groups.AnyAsync(g => g.Id == principalId))
        {
            throw new KeyNotFoundException($"Group '{principalId}' not found.");
        }

        await _authService.GrantCompositeConfigurationPermissionAsync(composite.Id, principalId, parsedPrincipalType, parsedLevel, userId.Value);
    }

    public async Task RevokePermissionAsync(string compositeName, Guid principalId, string principalType)
    {
        var composite = await _db.CompositeConfigurations.FirstOrDefaultAsync(c => c.Name == compositeName)
            ?? throw new KeyNotFoundException($"Composite configuration '{compositeName}' not found.");

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanManageCompositeConfigurationAsync(userId.Value, composite.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to composite configuration '{compositeName}'.");
        }

        if (!Enum.TryParse<PrincipalType>(principalType, ignoreCase: true, out var parsedPrincipalType))
        {
            throw new ArgumentException($"Invalid principal type '{principalType}'. Must be 'User' or 'Group'.", nameof(principalType));
        }

        await _authService.RevokeCompositeConfigurationPermissionAsync(composite.Id, principalId, parsedPrincipalType);
    }

    private async Task<List<PermissionEntry>> BuildPermissionEntriesAsync(
        IEnumerable<(PrincipalType PrincipalType, Guid PrincipalId, ResourcePermission Level, DateTimeOffset GrantedAt, Guid? GrantedByUserId)> entries)
    {
        var list = entries.ToList();
        var userIds = list.Where(e => e.PrincipalType == PrincipalType.User).Select(e => e.PrincipalId).ToList();
        var groupIds = list.Where(e => e.PrincipalType == PrincipalType.Group).Select(e => e.PrincipalId).ToList();

        var userNames = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username);

        var groupNames = await _db.Groups
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Name);

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

    private static CompositeConfigurationDetails MapToDetailsDto(CompositeConfiguration composite) =>
        new()
        {
            Id = composite.Id,
            Name = composite.Name,
            Description = composite.Description,
            EntryPoint = composite.EntryPoint,
            Versions = composite.Versions
                .OrderByDescending(v => v.CreatedAt)
                .Select(MapToVersionDto)
                .ToList(),
            CreatedAt = composite.CreatedAt,
            UpdatedAt = composite.UpdatedAt
        };

    private static CompositeConfigurationVersionDetails MapToVersionDto(CompositeConfigurationVersion v) =>
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
