// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

using OpenDsc.Server.Data;

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
