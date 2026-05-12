// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class AdminRolePermissionsTests : IClassFixture<ServerWebApplicationFactory>
{
    private readonly ServerWebApplicationFactory _factory;

    public AdminRolePermissionsTests(ServerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminRole_ContainsAllCurrentSystemPermissions()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        // Act
        var adminRole = await db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == "Administrator" && r.IsSystemRole, TestContext.Current.CancellationToken);

        // Assert
        adminRole.Should().NotBeNull();
        var adminPermissions = JsonSerializer.Deserialize<string[]>(adminRole!.Permissions) ?? [];

        // Admin role should have all permissions in the system
        var expectedPermissions = Permissions.AllScopes;

        adminPermissions.Should().HaveCount(expectedPermissions.Count);
        adminPermissions.Should().Contain(expectedPermissions);
    }

    [Fact]
    public async Task AdminRole_IncludesAllPermissionCategories()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        // Act
        var adminRole = await db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == "Administrator" && r.IsSystemRole, TestContext.Current.CancellationToken);

        var adminPermissions = JsonSerializer.Deserialize<string[]>(adminRole!.Permissions) ?? [];
        var adminSet = new HashSet<string>(adminPermissions, StringComparer.Ordinal);

        // Assert - check each permission category
        adminSet.Should().Contain(ServerPermissions.All);
        adminSet.Should().Contain(NodePermissions.All);
        adminSet.Should().Contain(ReportPermissions.All);
        adminSet.Should().Contain(RetentionPermissions.All);
        adminSet.Should().Contain(ConfigurationPermissions.All);
        adminSet.Should().Contain(CompositeConfigurationPermissions.All);
        adminSet.Should().Contain(ParameterPermissions.All);
        adminSet.Should().Contain(ScopePermissions.All);
    }

    [Fact]
    public async Task AdminRole_IsUpdatedWhenPermissionsChange()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        var adminRole = await db.Roles
            .FirstOrDefaultAsync(r => r.Name == "Administrator" && r.IsSystemRole, TestContext.Current.CancellationToken);

        adminRole.Should().NotBeNull();

        // Simulate the role being outdated by setting it to an old permission list
        var oldPermissions = new[] { NodePermissions.Read, ServerPermissions.SettingsRead };
        adminRole!.Permissions = JsonSerializer.Serialize(oldPermissions);
        db.Roles.Update(adminRole);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Call the update method
        await DatabaseSeeder.EnsureAdminRoleHasAllPermissionsAsync(db, LoggerMock.Create<string>());

        // Assert
        var updatedRole = await db.Roles
            .AsNoTracking()
            .FirstAsync(r => r.Name == "Administrator" && r.IsSystemRole, TestContext.Current.CancellationToken);

        var updatedPermissions = JsonSerializer.Deserialize<string[]>(updatedRole.Permissions) ?? [];

        // Should now have all permissions
        updatedPermissions.Should().HaveCount(Permissions.AllScopes.Count);
        updatedPermissions.Should().Contain(Permissions.AllScopes);
    }

    [Fact]
    public async Task AdminRole_NotUpdatedWhenAlreadyHasAllPermissions()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        var adminRole = await db.Roles
            .FirstOrDefaultAsync(r => r.Name == "Administrator" && r.IsSystemRole, TestContext.Current.CancellationToken);

        adminRole.Should().NotBeNull();

        // Record the modification time
        var originalModifiedAt = adminRole!.ModifiedAt;
        await Task.Delay(10, TestContext.Current.CancellationToken); // Small delay to ensure timestamp would be different if updated

        // Act - Call the update method when role already has all permissions
        await DatabaseSeeder.EnsureAdminRoleHasAllPermissionsAsync(db, LoggerMock.Create<string>());

        // Assert
        var reloadedRole = await db.Roles
            .AsNoTracking()
            .FirstAsync(r => r.Name == "Administrator" && r.IsSystemRole, TestContext.Current.CancellationToken);

        // ModifiedAt should not have changed since no update was needed
        reloadedRole.ModifiedAt.Should().Be(originalModifiedAt);
    }

    [Fact]
    public async Task AdminUser_ResolverReturnsAllPermissions()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        var adminUser = await db.Users
            .FirstOrDefaultAsync(u => u.Username == "admin", TestContext.Current.CancellationToken);

        adminUser.Should().NotBeNull();

        // Act - Resolve permissions for the admin user
        var resolvedPermissions = await RbacPermissionResolver.ResolveUserAndInternalGroupPermissionsAsync(db, adminUser!.Id);

        // Assert - Admin user should have all permissions
        resolvedPermissions.Should().Contain(Permissions.AllScopes);
        resolvedPermissions.Should().HaveCount(Permissions.AllScopes.Count);
    }

    [Fact]
    public async Task LimitedUser_ResolverReturnsOnlyAssignedPermissions()
    {
        // Arrange
        await _factory.CreateUserWithPermissionsAsync(
            username: "limited-user",
            permissions: [NodePermissions.Read, ReportPermissions.Read]);

        using var userScope = _factory.Services.CreateScope();
        var userDb = userScope.ServiceProvider.GetRequiredService<ServerDbContext>();
        var limitedUser = userDb.Users
            .First(u => u.Username == "limited-user");

        // Act
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
        var resolvedPermissions = await RbacPermissionResolver.ResolveUserAndInternalGroupPermissionsAsync(db, limitedUser.Id);

        // Assert - Should only have the assigned permissions
        resolvedPermissions.Should().HaveCount(2);
        resolvedPermissions.Should().Contain(NodePermissions.Read);
        resolvedPermissions.Should().Contain(ReportPermissions.Read);
        resolvedPermissions.Should().NotContain(ServerPermissions.SettingsRead);
        resolvedPermissions.Should().NotContain(NodePermissions.Write);
    }
}

/// <summary>
/// Mock logger for testing database seeding methods.
/// </summary>
internal static class LoggerMock
{
    public static ILogger<T> Create<T>() where T : class
    {
        return new MockLogger<T>();
    }

    private class MockLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // Silent logger for testing
        }
    }
}

