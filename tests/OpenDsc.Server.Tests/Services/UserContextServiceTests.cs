// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;
using System.Text.Json;

using AwesomeAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Moq;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class UserContextServiceTests
{
    [Fact]
    public void GetCurrentUserId_WithValidUserIdClaim_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        var result = service.GetCurrentUserId();

        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_WithoutUserIdClaim_ReturnsNull()
    {
        var claims = Array.Empty<Claim>();
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        var result = service.GetCurrentUserId();

        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_WithInvalidUserIdClaim_ReturnsNull()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "invalid-guid") };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        var result = service.GetCurrentUserId();

        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_WithoutHttpContext_ReturnsNull()
    {
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        var result = service.GetCurrentUserId();

        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUsername_WithValidUsernameClaim_ReturnsUsername()
    {
        var username = "testuser";
        var claims = new[] { new Claim(ClaimTypes.Name, username) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        var result = service.GetCurrentUsername();

        result.Should().Be(username);
    }

    [Fact]
    public void GetCurrentUsername_WithoutUsernameClaim_ReturnsNull()
    {
        var claims = Array.Empty<Claim>();
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        var result = service.GetCurrentUsername();

        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUsername_WithoutHttpContext_ReturnsNull()
    {
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        var result = service.GetCurrentUsername();

        result.Should().BeNull();
    }

    [Fact]
    public void GetIpAddress_WithValidRemoteIpAddress_ReturnsIpAddress()
    {
        var expectedIp = "192.168.1.1";
        var mockConnection = new Mock<ConnectionInfo>();
        mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse(expectedIp));

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.Connection).Returns(mockConnection.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        var result = service.GetIpAddress();

        result.Should().Be(expectedIp);
    }

    [Fact]
    public void GetIpAddress_WithoutRemoteIpAddress_ReturnsNull()
    {
        var mockConnection = new Mock<ConnectionInfo>();
        mockConnection.Setup(x => x.RemoteIpAddress).Returns((System.Net.IPAddress)null!);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.Connection).Returns(mockConnection.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        var result = service.GetIpAddress();

        result.Should().BeNull();
    }

    [Fact]
    public void GetIpAddress_WithoutHttpContext_ReturnsNull()
    {
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        var result = service.GetIpAddress();

        result.Should().BeNull();
    }

    [Fact]
    public void GetIpAddress_WithIPv6Address_ReturnsCorrectFormat()
    {
        var expectedIp = "::1";
        var mockConnection = new Mock<ConnectionInfo>();
        mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse(expectedIp));

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.Connection).Returns(mockConnection.Object);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        var result = service.GetIpAddress();

        result.Should().Be(expectedIp);
    }
}

/// <summary>
/// Database integration tests for UserContextService.
/// Tests user context resolution from claims with database entities.
/// </summary>
[Trait("Category", "Unit")]
public class UserContextServiceDatabaseIntegrationTests : IDisposable
{
    private readonly ServerDbContext _db;

    public UserContextServiceDatabaseIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ServerDbContext(options);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    #region User Context Resolution Tests

    [Fact]
    public void GetCurrentUserId_WithValidUserInDatabase_ReturnsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().Be(userId);
        _db.Users.FirstOrDefault(u => u.Id == userId).Should().NotBeNull();
    }

    [Fact]
    public void GetCurrentUsername_WithValidUserInDatabase_ReturnsUsername()
    {
        // Arrange
        const string username = "databaseuser";
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = username,
            Email = "user@example.com",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.Name, username) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUsername();

        // Assert
        result.Should().Be(username);
    }

    #endregion

    #region Permission Extraction Tests

    [Fact]
    public void GetCurrentUserId_WithUserHavingDirectRoles_ReturnsUserIdWithPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissions = new[] { "users.read", "users.write" };

        var user = new User
        {
            Id = userId,
            Username = "roleuser",
            Email = "roleuser@example.com",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);

        var role = new Role
        {
            Id = roleId,
            Name = "Editor",
            Permissions = JsonSerializer.Serialize(permissions),
            CreatedAt = DateTimeOffset.UtcNow
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

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().Be(userId);
        _db.UserRoles.Where(ur => ur.UserId == userId).Should().HaveCount(1);
        _db.Roles.FirstOrDefault(r => r.Id == roleId)?.Permissions
            .Should().Contain("users.read");
    }

    [Fact]
    public void GetCurrentUserId_WithUserInGroupWithRoles_ReturnsUserIdWithGroupPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissions = new[] { "groups.read", "groups.manage" };

        var user = new User
        {
            Id = userId,
            Username = "groupuser",
            Email = "groupuser@example.com",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);

        var group = new Group
        {
            Id = groupId,
            Name = "Administrators",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Groups.Add(group);

        var role = new Role
        {
            Id = roleId,
            Name = "Admin",
            Permissions = JsonSerializer.Serialize(permissions),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Roles.Add(role);

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

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().Be(userId);
        _db.UserGroups.Where(ug => ug.UserId == userId).Should().HaveCount(1);
        _db.GroupRoles.Where(gr => gr.GroupId == groupId).Should().HaveCount(1);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void GetCurrentUserId_WithNullHttpContext_ReturnsNull()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null!);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_WithNullClaims_ReturnsNull()
    {
        // Arrange
        var identity = new ClaimsIdentity(Array.Empty<Claim>());
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_WithInvalidGuidFormat_ReturnsNull()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-valid-guid"),
            new Claim(ClaimTypes.NameIdentifier, "12345"),
            new Claim(ClaimTypes.NameIdentifier, "")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_WithUserIdNotInDatabase_ReturnsUserIdAnyway()
    {
        // Arrange
        var userId = Guid.NewGuid(); // User not added to database

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().Be(userId); // Service returns claim value regardless of DB presence
        _db.Users.FirstOrDefault(u => u.Id == userId).Should().BeNull();
    }

    [Fact]
    public void GetCurrentUsername_WithEmptyUsername_ReturnsEmpty()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Name, string.Empty) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUsername();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCurrentUsername_WithSpecialCharactersInUsername_ReturnsUsername()
    {
        // Arrange
        const string username = "user@domain.com\\special-user_123";
        var claims = new[] { new Claim(ClaimTypes.Name, username) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUsername();

        // Assert
        result.Should().Be(username);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void GetCurrentUserId_WithMultipleUsersInDatabase_ReturnsCorrectUser()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();

        _db.Users.AddRange(
            new User { Id = userId1, Username = "user1", Email = "user1@example.com", CreatedAt = DateTimeOffset.UtcNow },
            new User { Id = userId2, Username = "user2", Email = "user2@example.com", CreatedAt = DateTimeOffset.UtcNow },
            new User { Id = userId3, Username = "user3", Email = "user3@example.com", CreatedAt = DateTimeOffset.UtcNow }
        );
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId2.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().Be(userId2);
        _db.Users.Should().HaveCount(3);
    }

    [Fact]
    public void GetCurrentUserId_WithInactiveUser_StillReturnsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "inactiveuser",
            Email = "inactive@example.com",
            IsActive = false, // Inactive user
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().Be(userId); // Service returns ID regardless of active status
        _db.Users.FirstOrDefault(u => u.Id == userId)?.IsActive.Should().BeFalse();
    }

    [Fact]
    public void GetCurrentUserId_WithUserHavingMultipleRoles_ReturnsCorrectUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Username = "multiroleuser",
            Email = "multirole@example.com",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);

        var role1 = new Role { Id = roleId1, Name = "Viewer", Permissions = JsonSerializer.Serialize(new[] { "read" }), CreatedAt = DateTimeOffset.UtcNow };
        var role2 = new Role { Id = roleId2, Name = "Editor", Permissions = JsonSerializer.Serialize(new[] { "write" }), CreatedAt = DateTimeOffset.UtcNow };
        _db.Roles.AddRange(role1, role2);

        _db.UserRoles.AddRange(
            new UserRole { UserId = userId, RoleId = roleId1 },
            new UserRole { UserId = userId, RoleId = roleId2 }
        );
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().Be(userId);
        _db.UserRoles.Where(ur => ur.UserId == userId).Should().HaveCount(2);
    }

    [Fact]
    public void GetCurrentUserId_WithUserInMultipleGroups_ReturnsCorrectUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groupId1 = Guid.NewGuid();
        var groupId2 = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Username = "multigroupuser",
            Email = "multigroup@example.com",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);

        var group1 = new Group { Id = groupId1, Name = "Group1", CreatedAt = DateTimeOffset.UtcNow };
        var group2 = new Group { Id = groupId2, Name = "Group2", CreatedAt = DateTimeOffset.UtcNow };
        _db.Groups.AddRange(group1, group2);

        _db.UserGroups.AddRange(
            new UserGroup { UserId = userId, GroupId = groupId1 },
            new UserGroup { UserId = userId, GroupId = groupId2 }
        );
        _db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.User).Returns(principal);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var service = new UserContextService(mockHttpContextAccessor.Object);

        // Act
        var result = service.GetCurrentUserId();

        // Assert
        result.Should().Be(userId);
        _db.UserGroups.Where(ug => ug.UserId == userId).Should().HaveCount(2);
    }

    #endregion
}
