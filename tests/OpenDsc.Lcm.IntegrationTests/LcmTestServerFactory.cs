// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using OpenDsc.Server.Data;

namespace OpenDsc.Lcm.IntegrationTests;

public class LcmTestServerFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ServerDbContext>>();
            services.RemoveAll<ServerDbContext>();

            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", null);

            services.AddAuthorizationBuilder()
                .AddPolicy("Node", policy => policy
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes("TestScheme")
                    .RequireRole("Node"))
                .AddPolicy("Admin", policy => policy
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes("TestScheme")
                    .RequireRole("Admin"));

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

            db.RegistrationKeys.Add(new OpenDsc.Server.Entities.RegistrationKey
            {
                Id = Guid.NewGuid(),
                Key = "test-lcm-registration-key",
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
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

    private sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Console.WriteLine("TestAuthHandler.HandleAuthenticateAsync called");
            var claims = new List<Claim> { new(ClaimTypes.Role, "Node") };

            var path = Request.Path.Value ?? string.Empty;
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 2 && segments[0] == "api" && segments[1] == "v1" && segments[2] == "nodes" && Guid.TryParse(segments[3], out var nodeId))
            {
                claims.Add(new Claim("node_id", nodeId.ToString()));
            }

            var identity = new ClaimsIdentity(claims, "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestScheme");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
