// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

#pragma warning disable xUnit1051

using System.Text.Json;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class ResourceAuthorizationServiceTests : IDisposable
{
    private readonly ServerDbContext _db;

    public ResourceAuthorizationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ServerDbContext(options);
    }

    // Configuration Permission Tests

    [Fact]
    public async Task CanReadConfigurationAsync_WithAdminOverride_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "Admin",
            Permissions = JsonSerializer.Serialize(new[] { ConfigurationPermissions.AdminOverride })
        };
        _db.Roles.Add(role);
        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanReadConfigurationAsync(userId, configurationId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanReadConfigurationAsync_WithDirectReadPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();

        _db.ConfigurationPermissions.Add(new ConfigurationPermission
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configurationId,
            PrincipalId = userId,
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Read,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanReadConfigurationAsync(userId, configurationId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanReadConfigurationAsync_WithoutPermission_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanReadConfigurationAsync(userId, configurationId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanModifyConfigurationAsync_WithModifyPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();

        _db.ConfigurationPermissions.Add(new ConfigurationPermission
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configurationId,
            PrincipalId = userId,
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Modify,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanModifyConfigurationAsync(userId, configurationId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanModifyConfigurationAsync_WithReadPermissionOnly_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();

        _db.ConfigurationPermissions.Add(new ConfigurationPermission
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configurationId,
            PrincipalId = userId,
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Read,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanModifyConfigurationAsync(userId, configurationId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanManageConfigurationAsync_WithManagePermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();

        _db.ConfigurationPermissions.Add(new ConfigurationPermission
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configurationId,
            PrincipalId = userId,
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Manage,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanManageConfigurationAsync(userId, configurationId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanReadConfigurationAsync_WithGroupPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();

        _db.UserGroups.Add(new UserGroup { UserId = userId, GroupId = groupId });
        _db.ConfigurationPermissions.Add(new ConfigurationPermission
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configurationId,
            PrincipalId = groupId,
            PrincipalType = PrincipalType.Group,
            PermissionLevel = ResourcePermission.Read,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanReadConfigurationAsync(userId, configurationId);

        // Assert
        result.Should().BeTrue();
    }

    // Composite Configuration Tests

    [Fact]
    public async Task CanReadCompositeConfigurationAsync_WithAdminOverride_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var compositeConfigId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "Admin",
            Permissions = JsonSerializer.Serialize(new[] { CompositeConfigurationPermissions.AdminOverride })
        };
        _db.Roles.Add(role);
        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanReadCompositeConfigurationAsync(userId, compositeConfigId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanModifyCompositeConfigurationAsync_WithPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var compositeConfigId = Guid.NewGuid();

        _db.CompositeConfigurationPermissions.Add(new CompositeConfigurationPermission
        {
            Id = Guid.NewGuid(),
            CompositeConfigurationId = compositeConfigId,
            PrincipalId = userId,
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Modify,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanModifyCompositeConfigurationAsync(userId, compositeConfigId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanManageCompositeConfigurationAsync_WithPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var compositeConfigId = Guid.NewGuid();

        _db.CompositeConfigurationPermissions.Add(new CompositeConfigurationPermission
        {
            Id = Guid.NewGuid(),
            CompositeConfigurationId = compositeConfigId,
            PrincipalId = userId,
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Manage,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanManageCompositeConfigurationAsync(userId, compositeConfigId);

        // Assert
        result.Should().BeTrue();
    }

    // Parameter Tests

    [Fact]
    public async Task CanReadParameterAsync_WithAdminOverride_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var parameterId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "Admin",
            Permissions = JsonSerializer.Serialize(new[] { ParameterPermissions.AdminOverride })
        };
        _db.Roles.Add(role);
        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanReadParameterAsync(userId, parameterId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanModifyParameterAsync_WithPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var parameterId = Guid.NewGuid();

        _db.ParameterPermissions.Add(new ParameterPermission
        {
            Id = Guid.NewGuid(),
            ParameterId = parameterId,
            PrincipalId = userId,
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Modify,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanModifyParameterAsync(userId, parameterId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanManageParameterAsync_WithPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var parameterId = Guid.NewGuid();

        _db.ParameterPermissions.Add(new ParameterPermission
        {
            Id = Guid.NewGuid(),
            ParameterId = parameterId,
            PrincipalId = userId,
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Manage,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.CanManageParameterAsync(userId, parameterId);

        // Assert
        result.Should().BeTrue();
    }

    // Get Readable IDs Tests

    [Fact]
    public async Task GetReadableConfigurationIdsAsync_WithAdminOverride_ReturnsAllConfigurations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var config1Id = Guid.NewGuid();
        var config2Id = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "Admin",
            Permissions = JsonSerializer.Serialize(new[] { ConfigurationPermissions.AdminOverride })
        };
        _db.Roles.Add(role);
        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.Configurations.AddRange(
            new Configuration { Id = config1Id, Name = "Config1" },
            new Configuration { Id = config2Id, Name = "Config2" }
        );
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.GetReadableConfigurationIdsAsync(userId);

        // Assert
        result.Should().Contain(config1Id);
        result.Should().Contain(config2Id);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetReadableConfigurationIdsAsync_WithUserPermissions_ReturnsOnlyAuthorizedConfigurations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var config1Id = Guid.NewGuid();
        var config2Id = Guid.NewGuid();

        _db.Configurations.AddRange(
            new Configuration { Id = config1Id, Name = "Config1" },
            new Configuration { Id = config2Id, Name = "Config2" }
        );
        _db.ConfigurationPermissions.Add(new ConfigurationPermission
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config1Id,
            PrincipalId = userId,
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Read,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.GetReadableConfigurationIdsAsync(userId);

        // Assert
        result.Should().Contain(config1Id);
        result.Should().NotContain(config2Id);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetReadableConfigurationIdsAsync_WithGroupPermissions_ReturnsGroupAuthorizedConfigurations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var config1Id = Guid.NewGuid();

        _db.UserGroups.Add(new UserGroup { UserId = userId, GroupId = groupId });
        _db.Configurations.Add(new Configuration { Id = config1Id, Name = "Config1" });
        _db.ConfigurationPermissions.Add(new ConfigurationPermission
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config1Id,
            PrincipalId = groupId,
            PrincipalType = PrincipalType.Group,
            PermissionLevel = ResourcePermission.Read,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.GetReadableConfigurationIdsAsync(userId);

        // Assert
        result.Should().Contain(config1Id);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetReadableCompositeConfigurationIdsAsync_WithAdminOverride_ReturnsAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var compositeId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "Admin",
            Permissions = JsonSerializer.Serialize(new[] { CompositeConfigurationPermissions.AdminOverride })
        };
        _db.Roles.Add(role);
        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.CompositeConfigurations.Add(new CompositeConfiguration { Id = compositeId, Name = "CompositeConfig" });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.GetReadableCompositeConfigurationIdsAsync(userId);

        // Assert
        result.Should().Contain(compositeId);
    }

    [Fact]
    public async Task GetReadableParameterIdsAsync_WithAdminOverride_ReturnsAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "Admin",
            Permissions = JsonSerializer.Serialize(new[] { ParameterPermissions.AdminOverride })
        };
        _db.Roles.Add(role);
        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.GetReadableParameterIdsAsync(userId);

        // Assert
        result.Should().NotBeNull();
    }

    // Grant Permission Tests

    [Fact]
    public async Task GrantConfigurationPermissionAsync_NewPermission_CreatesPermission()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var principalId = Guid.NewGuid();
        var grantedByUserId = Guid.NewGuid();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        await service.GrantConfigurationPermissionAsync(configurationId, principalId, PrincipalType.User, ResourcePermission.Read, grantedByUserId);

        // Assert
        var permission = await _db.ConfigurationPermissions
            .FirstOrDefaultAsync(p => p.ConfigurationId == configurationId && p.PrincipalId == principalId);
        permission.Should().NotBeNull();
        permission!.PermissionLevel.Should().Be(ResourcePermission.Read);
    }

    [Fact]
    public async Task GrantConfigurationPermissionAsync_ExistingPermission_UpdatesPermission()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var principalId = Guid.NewGuid();
        var grantedByUserId = Guid.NewGuid();
        var grantedByUserId2 = Guid.NewGuid();

        _db.ConfigurationPermissions.Add(new ConfigurationPermission
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configurationId,
            PrincipalId = principalId,
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Read,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = grantedByUserId
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        await service.GrantConfigurationPermissionAsync(configurationId, principalId, PrincipalType.User, ResourcePermission.Manage, grantedByUserId2);

        // Assert
        var permission = await _db.ConfigurationPermissions
            .FirstOrDefaultAsync(p => p.ConfigurationId == configurationId && p.PrincipalId == principalId);
        permission.Should().NotBeNull();
        permission!.PermissionLevel.Should().Be(ResourcePermission.Manage);
        permission.GrantedByUserId.Should().Be(grantedByUserId2);
    }

    [Fact]
    public async Task GrantCompositeConfigurationPermissionAsync_NewPermission_CreatesPermission()
    {
        // Arrange
        var compositeConfigId = Guid.NewGuid();
        var principalId = Guid.NewGuid();
        var grantedByUserId = Guid.NewGuid();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        await service.GrantCompositeConfigurationPermissionAsync(compositeConfigId, principalId, PrincipalType.Group, ResourcePermission.Modify, grantedByUserId);

        // Assert
        var permission = await _db.CompositeConfigurationPermissions
            .FirstOrDefaultAsync(p => p.CompositeConfigurationId == compositeConfigId);
        permission.Should().NotBeNull();
        permission!.PermissionLevel.Should().Be(ResourcePermission.Modify);
    }

    [Fact]
    public async Task GrantParameterPermissionAsync_NewPermission_CreatesPermission()
    {
        // Arrange
        var parameterId = Guid.NewGuid();
        var principalId = Guid.NewGuid();
        var grantedByUserId = Guid.NewGuid();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        await service.GrantParameterPermissionAsync(parameterId, principalId, PrincipalType.User, ResourcePermission.Read, grantedByUserId);

        // Assert
        var permission = await _db.ParameterPermissions
            .FirstOrDefaultAsync(p => p.ParameterId == parameterId);
        permission.Should().NotBeNull();
        permission!.PermissionLevel.Should().Be(ResourcePermission.Read);
    }

    // Revoke Permission Tests

    [Fact]
    public async Task RevokeConfigurationPermissionAsync_ExistingPermission_RemovesPermission()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var principalId = Guid.NewGuid();
        var permId = Guid.NewGuid();

        _db.ConfigurationPermissions.Add(new ConfigurationPermission
        {
            Id = permId,
            ConfigurationId = configurationId,
            PrincipalId = principalId,
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Read,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        await service.RevokeConfigurationPermissionAsync(configurationId, principalId, PrincipalType.User);

        // Assert
        var permission = await _db.ConfigurationPermissions.FirstOrDefaultAsync(p => p.Id == permId);
        permission.Should().BeNull();
    }

    [Fact]
    public async Task RevokeConfigurationPermissionAsync_NonExistentPermission_DoesNotThrow()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var principalId = Guid.NewGuid();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act & Assert - should not throw
        await service.RevokeConfigurationPermissionAsync(configurationId, principalId, PrincipalType.User);
    }

    [Fact]
    public async Task RevokeCompositeConfigurationPermissionAsync_ExistingPermission_RemovesPermission()
    {
        // Arrange
        var compositeConfigId = Guid.NewGuid();
        var principalId = Guid.NewGuid();
        var permId = Guid.NewGuid();

        _db.CompositeConfigurationPermissions.Add(new CompositeConfigurationPermission
        {
            Id = permId,
            CompositeConfigurationId = compositeConfigId,
            PrincipalId = principalId,
            PrincipalType = PrincipalType.Group,
            PermissionLevel = ResourcePermission.Read,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        await service.RevokeCompositeConfigurationPermissionAsync(compositeConfigId, principalId, PrincipalType.Group);

        // Assert
        var permission = await _db.CompositeConfigurationPermissions.FirstOrDefaultAsync(p => p.Id == permId);
        permission.Should().BeNull();
    }

    [Fact]
    public async Task RevokeParameterPermissionAsync_ExistingPermission_RemovesPermission()
    {
        // Arrange
        var parameterId = Guid.NewGuid();
        var principalId = Guid.NewGuid();
        var permId = Guid.NewGuid();

        _db.ParameterPermissions.Add(new ParameterPermission
        {
            Id = permId,
            ParameterId = parameterId,
            PrincipalId = principalId,
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Read,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        await service.RevokeParameterPermissionAsync(parameterId, principalId, PrincipalType.User);

        // Assert
        var permission = await _db.ParameterPermissions.FirstOrDefaultAsync(p => p.Id == permId);
        permission.Should().BeNull();
    }

    // Get ACL Tests

    [Fact]
    public async Task GetConfigurationAclAsync_ReturnsAllPermissionsForConfiguration()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var perm1Id = Guid.NewGuid();
        var perm2Id = Guid.NewGuid();

        _db.ConfigurationPermissions.AddRange(
            new ConfigurationPermission
            {
                Id = perm1Id,
                ConfigurationId = configurationId,
                PrincipalId = Guid.NewGuid(),
                PrincipalType = PrincipalType.User,
                PermissionLevel = ResourcePermission.Read,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedByUserId = Guid.NewGuid()
            },
            new ConfigurationPermission
            {
                Id = perm2Id,
                ConfigurationId = configurationId,
                PrincipalId = Guid.NewGuid(),
                PrincipalType = PrincipalType.Group,
                PermissionLevel = ResourcePermission.Manage,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedByUserId = Guid.NewGuid()
            }
        );
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.GetConfigurationAclAsync(configurationId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Id == perm1Id);
        result.Should().Contain(p => p.Id == perm2Id);
    }

    [Fact]
    public async Task GetCompositeConfigurationAclAsync_ReturnsAllPermissionsForCompositeConfiguration()
    {
        // Arrange
        var compositeConfigId = Guid.NewGuid();
        var permId = Guid.NewGuid();

        _db.CompositeConfigurationPermissions.Add(new CompositeConfigurationPermission
        {
            Id = permId,
            CompositeConfigurationId = compositeConfigId,
            PrincipalId = Guid.NewGuid(),
            PrincipalType = PrincipalType.User,
            PermissionLevel = ResourcePermission.Read,
            GrantedAt = DateTimeOffset.UtcNow,
            GrantedByUserId = Guid.NewGuid()
        });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.GetCompositeConfigurationAclAsync(compositeConfigId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(permId);
    }

    [Fact]
    public async Task GetParameterAclAsync_ReturnsAllPermissionsForParameter()
    {
        // Arrange
        var parameterId = Guid.NewGuid();
        var perm1Id = Guid.NewGuid();
        var perm2Id = Guid.NewGuid();

        _db.ParameterPermissions.AddRange(
            new ParameterPermission
            {
                Id = perm1Id,
                ParameterId = parameterId,
                PrincipalId = Guid.NewGuid(),
                PrincipalType = PrincipalType.User,
                PermissionLevel = ResourcePermission.Read,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedByUserId = Guid.NewGuid()
            },
            new ParameterPermission
            {
                Id = perm2Id,
                ParameterId = parameterId,
                PrincipalId = Guid.NewGuid(),
                PrincipalType = PrincipalType.User,
                PermissionLevel = ResourcePermission.Modify,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedByUserId = Guid.NewGuid()
            }
        );
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.GetParameterAclAsync(parameterId);

        // Assert
        result.Should().HaveCount(2);
    }

    // Global Permission Tests

    [Fact]
    public async Task HasGlobalPermissionAsync_WithWildcardPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "SuperAdmin",
            Permissions = JsonSerializer.Serialize(new[] { "*" })
        };
        _db.Roles.Add(role);
        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.HasGlobalPermissionAsync(userId, "any.permission");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasGlobalPermissionAsync_WithSpecificPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "Editor",
            Permissions = JsonSerializer.Serialize(new[] { "configurations.read", "configurations.write" })
        };
        _db.Roles.Add(role);
        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.HasGlobalPermissionAsync(userId, "configurations.read");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasGlobalPermissionAsync_WithoutPermission_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.HasGlobalPermissionAsync(userId, "any.permission");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasGlobalPermissionAsync_WithGroupPermission_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "GroupRole",
            Permissions = JsonSerializer.Serialize(new[] { "nodes.read" })
        };
        _db.Roles.Add(role);
        _db.UserGroups.Add(new UserGroup { UserId = userId, GroupId = groupId });
        _db.GroupRoles.Add(new GroupRole { GroupId = groupId, RoleId = roleId });
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result = await service.HasGlobalPermissionAsync(userId, "nodes.read");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasGlobalPermissionAsync_WithMultipleRolesAndPermissions_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();

        var role1 = new Role
        {
            Id = roleId1,
            Name = "Role1",
            Permissions = JsonSerializer.Serialize(new[] { "configurations.read" })
        };
        var role2 = new Role
        {
            Id = roleId2,
            Name = "Role2",
            Permissions = JsonSerializer.Serialize(new[] { "configurations.write" })
        };
        _db.Roles.AddRange(role1, role2);
        _db.UserRoles.AddRange(
            new UserRole { UserId = userId, RoleId = roleId1 },
            new UserRole { UserId = userId, RoleId = roleId2 }
        );
        _db.SaveChanges();

        var service = new ResourceAuthorizationService(_db, new NullLogger<ResourceAuthorizationService>());

        // Act
        var result1 = await service.HasGlobalPermissionAsync(userId, "configurations.read");
        var result2 = await service.HasGlobalPermissionAsync(userId, "configurations.write");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }

    public void Dispose() => _db?.Dispose();
}

#pragma warning restore xUnit1051
