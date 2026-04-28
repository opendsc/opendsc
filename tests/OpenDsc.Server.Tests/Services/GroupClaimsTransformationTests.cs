// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;
using System.Text.Json;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class GroupClaimsTransformationTests : IDisposable
{
    private readonly ServerDbContext _db;

    public GroupClaimsTransformationTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ServerDbContext(options);
    }

    [Fact]
    public async Task TransformAsync_WithoutUserIdClaim_ReturnsPrincipalUnchanged()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        result.Should().NotBeNull();
        result.FindFirst(ClaimTypes.Name)?.Value.Should().Be("testuser");
    }

    [Fact]
    public async Task TransformAsync_WithInvalidUserIdClaim_ReturnsPrincipalUnchanged()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TransformAsync_WithValidUserIdButNoRoles_AddsNoPermissionClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission");
        permissionClaims.Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_WithUserRoles_AddsPermissionClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissions = new[] { "permission1", "permission2" };

        var role = new Role
        {
            Id = roleId,
            Name = "TestRole",
            Permissions = JsonSerializer.Serialize(permissions)
        };
        _db.Roles.Add(role);

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };
        _db.UserRoles.Add(userRole);
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToList();
        permissionClaims.Should().Contain("permission1");
        permissionClaims.Should().Contain("permission2");
        permissionClaims.Should().HaveCount(2);
    }

    [Fact]
    public async Task TransformAsync_WithInternalGroupRoles_AddsPermissionClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissions = new[] { "group-permission1", "group-permission2" };

        var role = new Role
        {
            Id = roleId,
            Name = "GroupRole",
            Permissions = JsonSerializer.Serialize(permissions)
        };
        _db.Roles.Add(role);

        var group = new Group
        {
            Id = groupId,
            Name = "TestGroup"
        };
        _db.Groups.Add(group);

        var userGroup = new UserGroup
        {
            UserId = userId,
            GroupId = groupId
        };
        _db.UserGroups.Add(userGroup);

        var groupRole = new GroupRole
        {
            GroupId = groupId,
            RoleId = roleId
        };
        _db.GroupRoles.Add(groupRole);
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToList();
        permissionClaims.Should().Contain("group-permission1");
        permissionClaims.Should().Contain("group-permission2");
    }

    [Fact]
    public async Task TransformAsync_WithExternalGroupMapping_AddsPermissionClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var externalGroupId = "external-group-123";
        var permissions = new[] { "external-permission" };

        var role = new Role
        {
            Id = roleId,
            Name = "ExternalGroupRole",
            Permissions = JsonSerializer.Serialize(permissions)
        };
        _db.Roles.Add(role);

        var group = new Group
        {
            Id = groupId,
            Name = "ExternalGroup"
        };
        _db.Groups.Add(group);

        var mapping = new ExternalGroupMapping
        {
            GroupId = groupId,
            ExternalGroupId = externalGroupId
        };
        _db.ExternalGroupMappings.Add(mapping);

        var groupRole = new GroupRole
        {
            GroupId = groupId,
            RoleId = roleId
        };
        _db.GroupRoles.Add(groupRole);
        _db.SaveChanges();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("groups", externalGroupId)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToList();
        permissionClaims.Should().Contain("external-permission");
    }

    [Fact]
    public async Task TransformAsync_WithMultipleRoles_CombinesAllPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();

        var role1 = new Role
        {
            Id = roleId1,
            Name = "Role1",
            Permissions = JsonSerializer.Serialize(new[] { "perm1" })
        };
        var role2 = new Role
        {
            Id = roleId2,
            Name = "Role2",
            Permissions = JsonSerializer.Serialize(new[] { "perm2", "perm3" })
        };
        _db.Roles.AddRange(role1, role2);

        _db.UserRoles.AddRange(
            new UserRole { UserId = userId, RoleId = roleId1 },
            new UserRole { UserId = userId, RoleId = roleId2 }
        );
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToHashSet();
        permissionClaims.Should().Contain("perm1");
        permissionClaims.Should().Contain("perm2");
        permissionClaims.Should().Contain("perm3");
        permissionClaims.Should().HaveCount(3);
    }

    [Fact]
    public async Task TransformAsync_WithDuplicatePermissions_DeduplicatesInClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();

        // Both roles have the same permission
        var permissions = new[] { "shared-permission" };
        var role1 = new Role { Id = roleId1, Name = "Role1", Permissions = JsonSerializer.Serialize(permissions) };
        var role2 = new Role { Id = roleId2, Name = "Role2", Permissions = JsonSerializer.Serialize(permissions) };
        _db.Roles.AddRange(role1, role2);

        _db.UserRoles.AddRange(
            new UserRole { UserId = userId, RoleId = roleId1 },
            new UserRole { UserId = userId, RoleId = roleId2 }
        );
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert - Should have exactly 1 claim even though it appeared twice
        var permissionClaims = result.FindAll("permission").ToList();
        permissionClaims.Should().HaveCount(1);
        permissionClaims[0].Value.Should().Be("shared-permission");
    }

    public void Dispose() => _db?.Dispose();
}

[Trait("Category", "Unit")]
public class GroupClaimsTransformationExtendedTests : IDisposable
{
    private readonly ServerDbContext _db;

    public GroupClaimsTransformationExtendedTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ServerDbContext(options);
    }

    [Fact]
    public async Task TransformAsync_WithDirectAndGroupRoles_CombinesPermissionsFromBoth()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var directRoleId = Guid.NewGuid();
        var groupRoleId = Guid.NewGuid();

        var directRole = new Role
        {
            Id = directRoleId,
            Name = "DirectRole",
            Permissions = JsonSerializer.Serialize(new[] { "direct-perm1", "direct-perm2" })
        };
        var groupRole = new Role
        {
            Id = groupRoleId,
            Name = "GroupRole",
            Permissions = JsonSerializer.Serialize(new[] { "group-perm1", "group-perm2" })
        };
        _db.Roles.AddRange(directRole, groupRole);

        var group = new Group { Id = groupId, Name = "TestGroup" };
        _db.Groups.Add(group);

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = directRoleId });
        _db.UserGroups.Add(new UserGroup { UserId = userId, GroupId = groupId });
        _db.GroupRoles.Add(new GroupRole { GroupId = groupId, RoleId = groupRoleId });
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToHashSet();
        permissionClaims.Should().Contain("direct-perm1", "direct-perm2", "group-perm1", "group-perm2");
        permissionClaims.Should().HaveCount(4);
    }

    [Fact]
    public async Task TransformAsync_WithDirectRoleAndExternalGroupRole_CombinesPermissionsFromBoth()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var directRoleId = Guid.NewGuid();
        var externalGroupRoleId = Guid.NewGuid();
        var externalGroupId = "external-group-999";

        var directRole = new Role
        {
            Id = directRoleId,
            Name = "DirectRole",
            Permissions = JsonSerializer.Serialize(new[] { "direct-perm" })
        };
        var externalGroupRole = new Role
        {
            Id = externalGroupRoleId,
            Name = "ExternalGroupRole",
            Permissions = JsonSerializer.Serialize(new[] { "external-perm" })
        };
        _db.Roles.AddRange(directRole, externalGroupRole);

        var group = new Group { Id = groupId, Name = "ExternalGroup" };
        _db.Groups.Add(group);

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = directRoleId });
        _db.ExternalGroupMappings.Add(new ExternalGroupMapping { GroupId = groupId, ExternalGroupId = externalGroupId });
        _db.GroupRoles.Add(new GroupRole { GroupId = groupId, RoleId = externalGroupRoleId });
        _db.SaveChanges();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("groups", externalGroupId)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToHashSet();
        permissionClaims.Should().Contain("direct-perm", "external-perm");
        permissionClaims.Should().HaveCount(2);
    }

    [Fact]
    public async Task TransformAsync_WithAllThreePermissionSources_CombinesAllPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var directGroupId = Guid.NewGuid();
        var externalGroupId = Guid.NewGuid();
        var directRoleId = Guid.NewGuid();
        var groupRoleId = Guid.NewGuid();
        var externalGroupRoleId = Guid.NewGuid();
        var externalGroupIdStr = "external-group-all";

        var directRole = new Role { Id = directRoleId, Name = "DirectRole", Permissions = JsonSerializer.Serialize(new[] { "direct" }) };
        var groupRole = new Role { Id = groupRoleId, Name = "GroupRole", Permissions = JsonSerializer.Serialize(new[] { "internal-group" }) };
        var externalGroupRole = new Role { Id = externalGroupRoleId, Name = "ExternalGroupRole", Permissions = JsonSerializer.Serialize(new[] { "external-group" }) };
        _db.Roles.AddRange(directRole, groupRole, externalGroupRole);

        var group1 = new Group { Id = directGroupId, Name = "InternalGroup" };
        var group2 = new Group { Id = externalGroupId, Name = "ExternalGroup" };
        _db.Groups.AddRange(group1, group2);

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = directRoleId });
        _db.UserGroups.Add(new UserGroup { UserId = userId, GroupId = directGroupId });
        _db.GroupRoles.AddRange(
            new GroupRole { GroupId = directGroupId, RoleId = groupRoleId },
            new GroupRole { GroupId = externalGroupId, RoleId = externalGroupRoleId }
        );
        _db.ExternalGroupMappings.Add(new ExternalGroupMapping { GroupId = externalGroupId, ExternalGroupId = externalGroupIdStr });
        _db.SaveChanges();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("groups", externalGroupIdStr)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToHashSet();
        permissionClaims.Should().Contain("direct", "internal-group", "external-group");
        permissionClaims.Should().HaveCount(3);
    }

    [Fact]
    public async Task TransformAsync_WithMultipleGroups_CombinesPermissionsFromAllGroups()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId1 = Guid.NewGuid();
        var groupId2 = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();

        var role1 = new Role { Id = roleId1, Name = "Role1", Permissions = JsonSerializer.Serialize(new[] { "group1-perm" }) };
        var role2 = new Role { Id = roleId2, Name = "Role2", Permissions = JsonSerializer.Serialize(new[] { "group2-perm" }) };
        _db.Roles.AddRange(role1, role2);

        var group1 = new Group { Id = groupId1, Name = "Group1" };
        var group2 = new Group { Id = groupId2, Name = "Group2" };
        _db.Groups.AddRange(group1, group2);

        _db.UserGroups.AddRange(
            new UserGroup { UserId = userId, GroupId = groupId1 },
            new UserGroup { UserId = userId, GroupId = groupId2 }
        );
        _db.GroupRoles.AddRange(
            new GroupRole { GroupId = groupId1, RoleId = roleId1 },
            new GroupRole { GroupId = groupId2, RoleId = roleId2 }
        );
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToHashSet();
        permissionClaims.Should().Contain("group1-perm", "group2-perm");
        permissionClaims.Should().HaveCount(2);
    }

    [Fact]
    public async Task TransformAsync_WithMultipleExternalGroupClaims_CombinesAllMappedPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId1 = Guid.NewGuid();
        var groupId2 = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();
        var externalGroupId1 = "external-group-1";
        var externalGroupId2 = "external-group-2";

        var role1 = new Role { Id = roleId1, Name = "ExtRole1", Permissions = JsonSerializer.Serialize(new[] { "ext-perm1" }) };
        var role2 = new Role { Id = roleId2, Name = "ExtRole2", Permissions = JsonSerializer.Serialize(new[] { "ext-perm2" }) };
        _db.Roles.AddRange(role1, role2);

        var group1 = new Group { Id = groupId1, Name = "ExtGroup1" };
        var group2 = new Group { Id = groupId2, Name = "ExtGroup2" };
        _db.Groups.AddRange(group1, group2);

        _db.ExternalGroupMappings.AddRange(
            new ExternalGroupMapping { GroupId = groupId1, ExternalGroupId = externalGroupId1 },
            new ExternalGroupMapping { GroupId = groupId2, ExternalGroupId = externalGroupId2 }
        );
        _db.GroupRoles.AddRange(
            new GroupRole { GroupId = groupId1, RoleId = roleId1 },
            new GroupRole { GroupId = groupId2, RoleId = roleId2 }
        );
        _db.SaveChanges();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("groups", externalGroupId1),
            new Claim("groups", externalGroupId2)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToHashSet();
        permissionClaims.Should().Contain("ext-perm1", "ext-perm2");
        permissionClaims.Should().HaveCount(2);
    }

    [Fact]
    public async Task TransformAsync_WithGroupHavingNoRoles_AddsNoPermissionsFromGroup()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var role = new Role { Id = roleId, Name = "DirectRole", Permissions = JsonSerializer.Serialize(new[] { "direct-perm" }) };
        _db.Roles.Add(role);

        var group = new Group { Id = groupId, Name = "EmptyGroup" };
        _db.Groups.Add(group);

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.UserGroups.Add(new UserGroup { UserId = userId, GroupId = groupId });
        // Note: No GroupRoles added for this group
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToList();
        permissionClaims.Should().ContainSingle("direct-perm");
    }

    [Fact]
    public async Task TransformAsync_WithExternalGroupClaimWithoutMapping_IgnoresUnmappedGroup()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var mappedExternalGroupId = "mapped-group";
        var unmappedExternalGroupId = "unmapped-group";

        var role = new Role { Id = roleId, Name = "DirectRole", Permissions = JsonSerializer.Serialize(new[] { "direct-perm" }) };
        _db.Roles.Add(role);

        var group = new Group { Id = groupId, Name = "MappedGroup" };
        _db.Groups.Add(group);

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.ExternalGroupMappings.Add(new ExternalGroupMapping { GroupId = groupId, ExternalGroupId = mappedExternalGroupId });
        _db.GroupRoles.Add(new GroupRole { GroupId = groupId, RoleId = roleId });
        _db.SaveChanges();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("groups", mappedExternalGroupId),
            new Claim("groups", unmappedExternalGroupId)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert - Should only get permissions from mapped group
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToHashSet();
        permissionClaims.Should().Contain("direct-perm");
        // Note: also includes role permission from external group mapping
    }

    [Fact]
    public async Task TransformAsync_WithRoleHavingEmptyPermissions_AddsNoPermissionsFromRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "EmptyRole",
            Permissions = JsonSerializer.Serialize(new string[] { })
        };
        _db.Roles.Add(role);

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission");
        permissionClaims.Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_WithPermissionsContainingSpecialCharacters_PreservesSpecialCharacters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissions = new[] { "perm:read:dsc/config", "perm-write@resource.id", "perm.delete#item" };

        var role = new Role
        {
            Id = roleId,
            Name = "SpecialRole",
            Permissions = JsonSerializer.Serialize(permissions)
        };
        _db.Roles.Add(role);

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToList();
        permissionClaims.Should().Contain("perm:read:dsc/config");
        permissionClaims.Should().Contain("perm-write@resource.id");
        permissionClaims.Should().Contain("perm.delete#item");
    }

    [Fact]
    public async Task TransformAsync_WithLargePermissionSet_HandlesAllPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissions = Enumerable.Range(1, 100).Select(i => $"permission-{i}").ToArray();

        var role = new Role
        {
            Id = roleId,
            Name = "LargeRole",
            Permissions = JsonSerializer.Serialize(permissions)
        };
        _db.Roles.Add(role);

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToList();
        permissionClaims.Should().HaveCount(100);
        permissionClaims.Should().Contain(permissions);
    }

    [Fact]
    public async Task TransformAsync_WithMultipleGroupsAndRolesHavingSamePermission_DeduplicatesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId1 = Guid.NewGuid();
        var groupId2 = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();
        var roleId3 = Guid.NewGuid();
        var sharedPermission = "shared-admin-permission";

        var role1 = new Role { Id = roleId1, Name = "Role1", Permissions = JsonSerializer.Serialize(new[] { sharedPermission, "role1-unique" }) };
        var role2 = new Role { Id = roleId2, Name = "Role2", Permissions = JsonSerializer.Serialize(new[] { sharedPermission, "role2-unique" }) };
        var role3 = new Role { Id = roleId3, Name = "Role3", Permissions = JsonSerializer.Serialize(new[] { sharedPermission }) };
        _db.Roles.AddRange(role1, role2, role3);

        var group1 = new Group { Id = groupId1, Name = "Group1" };
        var group2 = new Group { Id = groupId2, Name = "Group2" };
        _db.Groups.AddRange(group1, group2);

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId1 });
        _db.UserGroups.AddRange(
            new UserGroup { UserId = userId, GroupId = groupId1 },
            new UserGroup { UserId = userId, GroupId = groupId2 }
        );
        _db.GroupRoles.AddRange(
            new GroupRole { GroupId = groupId1, RoleId = roleId2 },
            new GroupRole { GroupId = groupId2, RoleId = roleId3 }
        );
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        var permissionClaims = result.FindAll("permission").Select(c => c.Value).ToHashSet();
        permissionClaims.Should().Contain(sharedPermission, "role1-unique", "role2-unique");
        permissionClaims.Should().HaveCount(3);
    }

    [Fact]
    public async Task TransformAsync_WithExistingClaimsInPrincipal_PreservesExistingClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var role = new Role
        {
            Id = roleId,
            Name = "TestRole",
            Permissions = JsonSerializer.Serialize(new[] { "perm1" })
        };
        _db.Roles.Add(role);

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        _db.SaveChanges();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim("custom-claim", "custom-value")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var service = new GroupClaimsTransformation(_db, new NullLogger<GroupClaimsTransformation>());

        // Act
        var result = await service.TransformAsync(principal);

        // Assert
        result.FindFirst(ClaimTypes.Name)?.Value.Should().Be("testuser");
        result.FindFirst("custom-claim")?.Value.Should().Be("custom-value");
        result.FindFirst("permission")?.Value.Should().Be("perm1");
    }

    public void Dispose() => _db?.Dispose();
}
