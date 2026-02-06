// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

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
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        try
        {
            if (environment.IsEnvironment("Testing"))
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                // Use EnsureCreatedAsync until migrations are added
                await context.Database.EnsureCreatedAsync();
            }

            logger.LogInformation("Database initialized successfully");

            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            await DatabaseSeeder.SeedRolesAsync(context, logger);
            await DatabaseSeeder.SeedDefaultGroupsAsync(context, logger);
            await DatabaseSeeder.SeedSystemScopeTypesAsync(context, logger);
            await DatabaseSeeder.SeedInitialAdminAsync(context, passwordHasher, logger);

            if (environment.IsEnvironment("Testing"))
            {
                await SeedTestRegistrationKeyAsync(context, logger);
                await SeedTestDataAsync(context, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }
    /// <summary>
    /// Ensures the database is created and migrations are applied for testing.
    /// </summary>
    public static async Task EnsureDatabaseInitialized(
        ServerDbContext context,
        ILogger logger)
    {
        try
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            logger.LogInformation("Database initialized successfully");

            var passwordHasher = new PasswordHasher();

            await DatabaseSeeder.SeedRolesAsync(context, logger);
            await DatabaseSeeder.SeedDefaultGroupsAsync(context, logger);
            await DatabaseSeeder.SeedSystemScopeTypesAsync(context, logger);
            await DatabaseSeeder.SeedInitialAdminAsync(context, passwordHasher, logger);
            await SeedTestRegistrationKeyAsync(context, logger);
            await SeedTestDataAsync(context, logger);
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
        var lcmKeyExists = await context.RegistrationKeys
            .AnyAsync(k => k.Key == "test-lcm-registration-key");

        if (!lcmKeyExists)
        {
            context.RegistrationKeys.Add(new RegistrationKey
            {
                Key = "test-lcm-registration-key",
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddYears(100)
            });
        }

        var testKeyExists = await context.RegistrationKeys
            .AnyAsync(k => k.Key == "test-registration-key");

        if (!testKeyExists)
        {
            context.RegistrationKeys.Add(new RegistrationKey
            {
                Key = "test-registration-key",
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddYears(100)
            });
        }

        if (!lcmKeyExists || !testKeyExists)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Test registration key seeded successfully");
        }
    }

    /// <summary>
    /// Seeds test data for test environments.
    /// </summary>
    private static async Task SeedTestDataAsync(
        ServerDbContext context,
        ILogger logger)
    {
        var existingConfig = await context.Configurations
            .AnyAsync(c => c.Name == "test-config");

        if (existingConfig)
        {
            logger.LogInformation("Test data already exists");
            return;
        }

        // Seed server settings
        var serverSettingsExists = await context.ServerSettings.AnyAsync();
        if (!serverSettingsExists)
        {
            context.ServerSettings.Add(new ServerSettings
            {
                Id = 1,
                CertificateRotationInterval = TimeSpan.FromDays(60)
            });
        }

        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "test-config",
            Description = "Test configuration",
            EntryPoint = "main.dsc.yaml",
            IsServerManaged = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Configurations.Add(config);

        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            IsDraft = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.ConfigurationVersions.Add(version);

        var configFile = new ConfigurationFile
        {
            Id = Guid.NewGuid(),
            VersionId = version.Id,
            RelativePath = "main.dsc.yaml",
            ContentType = "text/yaml",
            Checksum = "test-checksum",

            CreatedAt = DateTimeOffset.UtcNow
        };

        context.ConfigurationFiles.Add(configFile);

        await context.SaveChangesAsync();
        logger.LogInformation("Test data seeded successfully");
    }
}
