// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

using OpenDsc.Server.Data;

namespace OpenDsc.Lcm.IntegrationTests;

public class LcmTestServerFactory : WebApplicationFactory<Program>
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

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ServerDbContext>();

            db.Database.EnsureCreated();

            db.ServerSettings.Add(new OpenDsc.Server.Entities.ServerSettings
            {
                Id = 1,
                RegistrationKey = "test-lcm-registration-key",
                AdminApiKeyHash = OpenDsc.Server.Authentication.ApiKeyAuthHandler.HashApiKey("test-lcm-admin-key"),
                KeyRotationInterval = TimeSpan.FromDays(30)
            });

            db.Configurations.Add(new OpenDsc.Server.Entities.Configuration
            {
                Id = Guid.NewGuid(),
                Name = "test-config",
                Content = @"
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources: []
",
                Checksum = "test-checksum",
                CreatedAt = DateTimeOffset.UtcNow,
                ModifiedAt = DateTimeOffset.UtcNow
            });

            db.SaveChanges();
        });

        builder.UseEnvironment("Testing");
        builder.UseUrls("http://localhost:0");
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
