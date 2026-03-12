// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.IntegrationTests;

public class ServerWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ServerDbContext>>();
            services.RemoveAll<ServerDbContext>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<ServerDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.EnableSensitiveDataLogging();
            });
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ServerDbContext>>();

            DatabaseExtensions.EnsureDatabaseInitialized(db, logger).GetAwaiter().GetResult();
        }

        return host;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        // Login with default admin credentials
        var loginRequest = new { username = "admin", password = "admin" };
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        if (!loginResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to authenticate test client");
        }

        return client;
    }

    /// <summary>
    /// Creates a user with specific global permissions and returns an authenticated HTTP client.
    /// </summary>
    public async Task<HttpClient> CreateUserWithPermissionsAsync(string username, string[] permissions)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
        var hasher = new PasswordHasher();

        var password = "Password123!";
        var (hash, salt) = hasher.HashPassword(password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = $"{username}@test.local",
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = $"TestRole_{username}",
            IsSystemRole = false,
            Permissions = JsonSerializer.Serialize(permissions),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Roles.Add(role);

        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

        await db.SaveChangesAsync();

        var client = CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password });

        if (!loginResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to authenticate test user '{username}'");
        }

        return client;
    }

    /// <summary>
    /// Creates a user with NO global permissions (authenticated but unauthorized for everything).
    /// </summary>
    public Task<HttpClient> CreateUnprivilegedUserAsync(string username = "unprivileged-test-user")
        => CreateUserWithPermissionsAsync(username, []);

    /// <summary>
    /// Creates an authenticated client for testing (synchronous wrapper).
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        return CreateAuthenticatedClientAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates an authenticated client for testing (synchronous wrapper with API key - deprecated).
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string apiKey)
    {
        // For backward compatibility, ignore the API key and use the new auth system
        return CreateAuthenticatedClient();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        base.Dispose(disposing);
    }
}
