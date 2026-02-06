// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;

using Microsoft.Data.Sqlite;

using Xunit;

namespace OpenDsc.Lcm.FunctionalTests;

public sealed class ServerFixture : IAsyncLifetime
{
    private IContainer? _serverContainer;
    private IFutureDockerImage? _serverImage;
    private string? _sqliteDbPath;

    public string BaseUrl { get; private set; } = string.Empty;
    public string AdminApiKey { get; private set; } = "test-admin-key-12345";
    public string RegistrationKey { get; private set; } = "test-registration-key-12345";

    public async Task InitializeAsync()
    {
        // Create SQLite database for the server
        _sqliteDbPath = Path.Combine(Path.GetTempPath(), $"opendsc-test-{Guid.NewGuid()}.db");
        var connectionString = $"Data Source={_sqliteDbPath}";

        // Initialize database schema
        using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE ServerSettings (
                    Id INTEGER PRIMARY KEY,
                    AdminApiKey TEXT NOT NULL,
                    RegistrationKey TEXT NOT NULL,
                    KeyRotationInterval TEXT NOT NULL,
                    Version INTEGER NOT NULL
                );
                INSERT INTO ServerSettings (Id, AdminApiKey, RegistrationKey, KeyRotationInterval, Version)
                VALUES (1, @adminKey, @regKey, '7.00:00:00', 1);

                CREATE TABLE Nodes (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    ApiKey TEXT NOT NULL,
                    RegisteredAt TEXT NOT NULL,
                    LastSeen TEXT,
                    ConfigurationId TEXT,
                    Version INTEGER NOT NULL
                );

                CREATE TABLE Configurations (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Checksum TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    Version INTEGER NOT NULL
                );

                CREATE TABLE Reports (
                    Id TEXT PRIMARY KEY,
                    NodeId TEXT NOT NULL,
                    Operation TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    SubmittedAt TEXT NOT NULL,
                    Version INTEGER NOT NULL,
                    FOREIGN KEY (NodeId) REFERENCES Nodes(Id)
                );
            ";
            command.Parameters.AddWithValue("@adminKey", AdminApiKey);
            command.Parameters.AddWithValue("@regKey", RegistrationKey);
            await command.ExecuteNonQueryAsync();
        }

        // Build Docker image for OpenDSC Server
        var solutionDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));

        _serverImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(solutionDir)
            .WithDockerfile("src/OpenDsc.Server/Dockerfile")
            .WithName($"opendsc-server-test:{Guid.NewGuid():N}")
            .WithCleanUp(true)
            .Build();

        await _serverImage.CreateAsync();

        // Start server container
        var randomPort = Random.Shared.Next(30000, 40000);
        _serverContainer = new ContainerBuilder()
            .WithImage(_serverImage)
            .WithPortBinding(randomPort, 8080)
            .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
            .WithEnvironment("ConnectionStrings__DefaultConnection", connectionString)
            .WithEnvironment("Database__Provider", "Sqlite")
            .WithBindMount(_sqliteDbPath, _sqliteDbPath)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8080).ForPath("/health")))
            .Build();

        await _serverContainer.StartAsync();

        BaseUrl = $"http://localhost:{randomPort}";
    }

    public async Task DisposeAsync()
    {
        if (_serverContainer is not null)
        {
            await _serverContainer.StopAsync();
            await _serverContainer.DisposeAsync();
        }

        if (_serverImage is not null)
        {
            await _serverImage.DisposeAsync();
        }

        if (!string.IsNullOrEmpty(_sqliteDbPath) && File.Exists(_sqliteDbPath))
        {
            File.Delete(_sqliteDbPath);
        }
    }
}
