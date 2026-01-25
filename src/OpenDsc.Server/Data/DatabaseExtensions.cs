// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

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

        try
        {
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }
}
