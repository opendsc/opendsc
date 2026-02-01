// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authentication;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Data;

/// <summary>
/// Extension methods for configuring the database provider.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Adds the ServerDbContext with the configured database provider.
    /// </summary>
    public static IServiceCollection AddServerDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue("Database:Provider", "SQLite")!;
        var connectionString = configuration.GetValue<string>("Database:ConnectionString")
            ?? "Data Source=opendsc-server.db";

        services.AddDbContext<ServerDbContext>(options =>
        {
            _ = provider.ToUpperInvariant() switch
            {
                "SQLITE" => options.UseSqlite(connectionString),
                "SQLSERVER" => options.UseSqlServer(connectionString),
                "POSTGRESQL" => options.UseNpgsql(connectionString),
                _ => throw new InvalidOperationException(
                    $"Unsupported database provider: {provider}. " +
                    "Supported providers: SQLite, SqlServer, PostgreSQL")
            };
        });

        return services;
    }

    /// <summary>
    /// Ensures the database is created and migrations are applied.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ServerDbContext>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        try
        {
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database initialized successfully");

            await SeedInitialAdminKeyAsync(context, configuration, logger);

            if (environment.IsEnvironment("Testing"))
            {
                await SeedTestRegistrationKeyAsync(context, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    /// <summary>
    /// Seeds a test registration key for test environments.
    /// </summary>
    private static async Task SeedTestRegistrationKeyAsync(
        ServerDbContext context,
        ILogger logger)
    {
        var existing = await context.RegistrationKeys
            .Where(k => k.Key == "test-registration-key")
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            existing.ExpiresAt = DateTimeOffset.UtcNow.AddYears(100);
            await context.SaveChangesAsync();
            logger.LogInformation("Test registration key expiry updated");
            return;
        }

        context.RegistrationKeys.Add(new RegistrationKey
        {
            Key = "test-registration-key",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddYears(100)
        });

        await context.SaveChangesAsync();
        logger.LogInformation("Test registration key seeded successfully");
    }

    /// <summary>
    /// Seeds the initial admin key from configuration if not already set.
    /// </summary>
    private static async Task SeedInitialAdminKeyAsync(
        ServerDbContext context,
        IConfiguration configuration,
        ILogger logger)
    {
        var existingSettings = await context.ServerSettings
            .FirstOrDefaultAsync(s => s.Id == 1);

        if (existingSettings is not null &&
            !string.IsNullOrEmpty(existingSettings.AdminApiKeyHash) &&
            !string.IsNullOrEmpty(existingSettings.AdminApiKeySalt))
        {
            return;
        }

        var initialAdminKey = configuration.GetValue<string>("Server:InitialAdminKey");

        if (string.IsNullOrWhiteSpace(initialAdminKey))
        {
            logger.LogWarning(
                "No initial admin key configured. Set 'Server:InitialAdminKey' in appsettings.json " +
                "or environment variable 'Server__InitialAdminKey' to seed the admin key on first startup");
            return;
        }

        var hash = ApiKeyAuthHandler.HashPasswordPbkdf2(initialAdminKey, out var salt);

        if (existingSettings is null)
        {
            context.ServerSettings.Add(new ServerSettings
            {
                Id = 1,
                AdminApiKeyHash = hash,
                AdminApiKeySalt = salt
            });
        }
        else
        {
            existingSettings.AdminApiKeyHash = hash;
            existingSettings.AdminApiKeySalt = salt;
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Initial admin key seeded successfully");
    }
}
