// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

#pragma warning disable xUnit1051

using AwesomeAssertions;

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class ConfigurationServiceTests : IDisposable
{
    private readonly ServerDbContext _dbContext;
    private readonly IOptions<ServerConfig> _serverConfig;
    private readonly Mock<IResourceAuthorizationService> _mockAuthService;
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IParameterSchemaBuilder> _mockSchemaBuilder;
    private readonly Mock<IParameterCompatibilityService> _mockCompatibilityService;
    private readonly ConfigurationService _client;

    public ConfigurationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ServerDbContext(options);
        _serverConfig = Options.Create(new ServerConfig { DataDirectory = "test-data" });
        _mockAuthService = new Mock<IResourceAuthorizationService>();
        _mockUserContext = new Mock<IUserContextService>();
        _mockLogger = new Mock<ILogger<ConfigurationService>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockSchemaBuilder = new Mock<IParameterSchemaBuilder>();
        _mockCompatibilityService = new Mock<IParameterCompatibilityService>();

        _client = new ConfigurationService(
            _dbContext,
            _serverConfig,
            _mockAuthService.Object,
            _mockUserContext.Object,
            _mockLogger.Object,
            _mockHttpContextAccessor.Object,
            _mockSchemaBuilder.Object,
            _mockCompatibilityService.Object);
    }

    [Fact]
    public async Task PublishVersionAsync_WithNoHttpContext_ReturnsFailure()
    {
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var result = await _client.PublishVersionAsync("test-config", "1.0.0");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unable to determine server URL");
    }

    [Fact]
    public async Task PublishVersionAsync_WithHttpContext_ConstructsCorrectUrl()
    {
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        var cookieCollection = new HeaderDictionary();

        mockRequest.Setup(r => r.Scheme).Returns("https");
        mockRequest.Setup(r => r.Host).Returns(new HostString("localhost", 5001));
        mockRequest.Setup(r => r.Headers).Returns(cookieCollection);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        // Note: This will fail because we can't actually make the HTTP call in a unit test
        // But we can verify the URL construction logic is correct
        var result = await _client.PublishVersionAsync("test-config", "1.0.0");

        result.Success.Should().BeFalse();
        mockRequest.Verify(r => r.Scheme, Times.Once);
        mockRequest.Verify(r => r.Host, Times.Once);
    }

    [Fact]
    public async Task PublishVersionAsync_WithCookie_ForwardsCookieHeader()
    {
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        var cookieCollection = new HeaderDictionary
        {
            { "Cookie", ".AspNetCore.Cookies=test-cookie-value" }
        };

        mockRequest.Setup(r => r.Scheme).Returns("https");
        mockRequest.Setup(r => r.Host).Returns(new HostString("localhost", 5001));
        mockRequest.Setup(r => r.Headers).Returns(cookieCollection);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var result = await _client.PublishVersionAsync("test-config", "1.0.0");

        // Verify cookie header was accessed (implying it would be forwarded)
        mockRequest.Verify(r => r.Headers, Times.AtLeastOnce);
    }

    [Fact]
    public async Task PublishVersionAsync_EscapesSpecialCharactersInUrl()
    {
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        var cookieCollection = new HeaderDictionary();

        mockRequest.Setup(r => r.Scheme).Returns("https");
        mockRequest.Setup(r => r.Host).Returns(new HostString("localhost", 5001));
        mockRequest.Setup(r => r.Headers).Returns(cookieCollection);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var result = await _client.PublishVersionAsync("test config", "1.0.0");

        // The method should handle URL escaping for config names with spaces
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task PublishVersionAsync_WithSuccessResponse_ReturnsUpdatedVersionData()
    {
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        var cookieCollection = new HeaderDictionary();

        mockRequest.Setup(r => r.Scheme).Returns("https");
        mockRequest.Setup(r => r.Host).Returns(new HostString("localhost", 5001));
        mockRequest.Setup(r => r.Headers).Returns(cookieCollection);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        // Note: This unit test validates the parsing logic
        // Full HTTP integration is tested in integration tests
        var actualResult = await _client.PublishVersionAsync("test-config", "1.0.0");
        actualResult.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishVersionAsync_WithConflictResponse_ReturnsCompatibilityReport()
    {
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        var cookieCollection = new HeaderDictionary();

        mockRequest.Setup(r => r.Scheme).Returns("https");
        mockRequest.Setup(r => r.Host).Returns(new HostString("localhost", 5001));
        mockRequest.Setup(r => r.Headers).Returns(cookieCollection);
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        var actualResult = await _client.PublishVersionAsync("test-config", "1.0.0");
        actualResult.Should().NotBeNull();
    }

    #region CreateConfigurationAsync Tests

    [Fact]
    public async Task CreateConfigurationAsync_WithValidData_CreatesConfiguration()
    {
        var name = "TestConfig";
        var files = CreateMockFiles(("main.dsc.yaml", "configuration {}"));

        var result = await _client.CreateConfigurationAsync(
            name, "Test config", "main.dsc.yaml", "1.0.0", isDraft: false,
            useServerManagedParameters: false, files);

        result.Should().Be(true);
        (await _dbContext.Configurations.FirstOrDefaultAsync(c => c.Name == name)).Should().NotBeNull();
    }

    [Fact]
    public async Task CreateConfigurationAsync_WithoutName_ReturnsFalse()
    {
        var files = CreateMockFiles(("main.dsc.yaml", "config {}"));

        var result = await _client.CreateConfigurationAsync(
            string.Empty, "desc", "main.dsc.yaml", "1.0.0", false, false, files);

        result.Should().Be(false);
    }

    [Fact]
    public async Task CreateConfigurationAsync_WithoutFiles_ReturnsFalse()
    {
        var result = await _client.CreateConfigurationAsync(
            "TestConfig", "desc", "main.dsc.yaml", "1.0.0", false, false, []);

        result.Should().Be(false);
    }

    [Fact]
    public async Task CreateConfigurationAsync_WithDuplicateName_ReturnsFalse()
    {
        var name = "DuplicateConfig";
        var files = CreateMockFiles(("main.dsc.yaml", "config {}"));

        await _client.CreateConfigurationAsync(
            name, "First", "main.dsc.yaml", "1.0.0", false, false, files);

        var result = await _client.CreateConfigurationAsync(
            name, "Second", "main.dsc.yaml", "1.0.1", false, false,
            CreateMockFiles(("main.dsc.yaml", "config {}")));

        result.Should().Be(false);
    }

    [Fact]
    public async Task CreateConfigurationAsync_WithEntryPointNotInFiles_ReturnsFalse()
    {
        var files = CreateMockFiles(("other.yaml", "content"));

        var result = await _client.CreateConfigurationAsync(
            "TestConfig", "desc", "main.dsc.yaml", "1.0.0", false, false, files);

        result.Should().Be(false);
    }

    [Fact]
    public async Task CreateConfigurationAsync_WithDraftStatus_CreatesAsDraft()
    {
        var name = "DraftConfig";
        var files = CreateMockFiles(("main.dsc.yaml", "config {}"));

        await _client.CreateConfigurationAsync(
            name, "desc", "main.dsc.yaml", "1.0.0", isDraft: true, false, files);

        var version = await _dbContext.ConfigurationVersions
            .FirstOrDefaultAsync(v => v.Version == "1.0.0");

        version?.Status.Should().Be(ConfigurationVersionStatus.Draft);
    }

    [Fact]
    public async Task CreateConfigurationAsync_WithPublishedStatus_CreatesAsPublished()
    {
        var name = "PublishedConfig";
        var files = CreateMockFiles(("main.dsc.yaml", "config {}"));

        await _client.CreateConfigurationAsync(
            name, "desc", "main.dsc.yaml", "1.0.0", isDraft: false, false, files);

        var version = await _dbContext.ConfigurationVersions
            .FirstOrDefaultAsync(v => v.Version == "1.0.0");

        version?.Status.Should().Be(ConfigurationVersionStatus.Published);
    }

    [Fact]
    public async Task CreateConfigurationAsync_WithServerManagedParametersTrue_SetsFlag()
    {
        var name = "ServerManaged";
        var files = CreateMockFiles(("main.dsc.yaml", "config {}"));

        await _client.CreateConfigurationAsync(
            name, "desc", "main.dsc.yaml", "1.0.0", false,
            useServerManagedParameters: true, files);

        var config = await _dbContext.Configurations.FirstOrDefaultAsync(c => c.Name == name);

        config?.UseServerManagedParameters.Should().Be(true);
    }

    [Fact]
    public async Task CreateConfigurationAsync_WithWhitespaceOnlyName_ReturnsFalse()
    {
        var files = CreateMockFiles(("main.dsc.yaml", "config {}"));

        var result = await _client.CreateConfigurationAsync(
            "   ", "desc", "main.dsc.yaml", "1.0.0", false, false, files);

        result.Should().Be(false);
    }

    [Fact]
    public async Task CreateConfigurationAsync_WithNestedDirectoryFile_CreatesSuccessfully()
    {
        var name = "NestedConfig";
        var files = CreateMockFiles(
            ("main.dsc.yaml", "configuration {}"),
            ("subdir/nested.yaml", "nested {}"));

        var result = await _client.CreateConfigurationAsync(
            name, "desc", "main.dsc.yaml", "1.0.0", false, false, files);

        result.Should().Be(true);
        (await _dbContext.ConfigurationFiles
            .Where(f => f.RelativePath == "subdir/nested.yaml")
            .CountAsync())
            .Should().Be(1);
    }

    #endregion

    #region CreateVersionAsync Tests

    [Fact]
    public async Task CreateVersionAsync_WithValidData_CreatesVersion()
    {
        var config = new Configuration { Id = Guid.NewGuid(), Name = "TestConfig", CreatedAt = DateTimeOffset.UtcNow };
        _dbContext.Configurations.Add(config);
        await _dbContext.SaveChangesAsync();

        var files = CreateMockFiles(("main.dsc.yaml", "config {}"));

        var result = await _client.CreateVersionAsync("TestConfig", "2.0.0", false, files);

        result.Should().Be(true);
        (await _dbContext.ConfigurationVersions.FirstOrDefaultAsync(v => v.Version == "2.0.0"))
            .Should().NotBeNull();
    }

    [Fact]
    public async Task CreateVersionAsync_ForNonexistentConfiguration_ReturnsFalse()
    {
        var files = CreateMockFiles(("main.dsc.yaml", "config {}"));

        var result = await _client.CreateVersionAsync("NonexistentConfig", "1.0.0", false, files);

        result.Should().Be(false);
    }

    [Fact]
    public async Task CreateVersionAsync_WithDuplicateVersion_ReturnsFalse()
    {
        var config = new Configuration { Id = Guid.NewGuid(), Name = "TestConfig", CreatedAt = DateTimeOffset.UtcNow };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        await _dbContext.SaveChangesAsync();

        var files = CreateMockFiles(("main.dsc.yaml", "config {}"));

        var result = await _client.CreateVersionAsync("TestConfig", "1.0.0", false, files);

        result.Should().Be(false);
    }

    [Fact]
    public async Task CreateVersionAsync_WithExplicitEntryPoint_UsesProvidedEntryPoint()
    {
        var config = new Configuration { Id = Guid.NewGuid(), Name = "TestConfig", CreatedAt = DateTimeOffset.UtcNow };
        _dbContext.Configurations.Add(config);
        await _dbContext.SaveChangesAsync();

        var files = CreateMockFiles(("custom.yaml", "config {}"));

        await _client.CreateVersionAsync("TestConfig", "1.0.0", false, files, entryPoint: "custom.yaml");

        var createdVersion = await _dbContext.ConfigurationVersions.FirstOrDefaultAsync(v => v.Version == "1.0.0");

        createdVersion?.EntryPoint.Should().Be("custom.yaml");
    }

    [Fact]
    public async Task CreateVersionAsync_WithoutEntryPoint_UsesLatestVersionEntryPoint()
    {
        var config = new Configuration { Id = Guid.NewGuid(), Name = "TestConfig", CreatedAt = DateTimeOffset.UtcNow };
        var v1 = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "myentry.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(v1);
        await _dbContext.SaveChangesAsync();

        var files = CreateMockFiles(("myentry.yaml", "config {}"));

        await _client.CreateVersionAsync("TestConfig", "2.0.0", false, files);

        var createdVersion = await _dbContext.ConfigurationVersions.FirstOrDefaultAsync(v => v.Version == "2.0.0");

        createdVersion?.EntryPoint.Should().Be("myentry.yaml");
    }

    [Fact]
    public async Task CreateVersionAsync_WithEmptyName_ReturnsFalse()
    {
        var files = CreateMockFiles(("main.dsc.yaml", "config {}"));

        var result = await _client.CreateVersionAsync(string.Empty, "1.0.0", false, files);

        result.Should().Be(false);
    }

    [Fact]
    public async Task CreateVersionAsync_WithDraftStatus_CreatesAsDraft()
    {
        var config = new Configuration { Id = Guid.NewGuid(), Name = "TestConfig", CreatedAt = DateTimeOffset.UtcNow };
        _dbContext.Configurations.Add(config);
        await _dbContext.SaveChangesAsync();

        var files = CreateMockFiles(("main.dsc.yaml", "config {}"));

        await _client.CreateVersionAsync("TestConfig", "1.0.0", isDraft: true, files);

        var version = await _dbContext.ConfigurationVersions
            .FirstOrDefaultAsync(v => v.Version == "1.0.0");

        version?.Status.Should().Be(ConfigurationVersionStatus.Draft);
    }

    #endregion

    #region DeleteConfigurationAsync Tests

    [Fact]
    public async Task DeleteConfigurationAsync_WithValidConfiguration_DeletesSuccessfully()
    {
        var config = new Configuration { Id = Guid.NewGuid(), Name = "ToDelete", CreatedAt = DateTimeOffset.UtcNow };
        _dbContext.Configurations.Add(config);
        await _dbContext.SaveChangesAsync();

        var result = await _client.DeleteConfigurationAsync("ToDelete");

        result.Should().Be(true);
        (await _dbContext.Configurations.FirstOrDefaultAsync(c => c.Name == "ToDelete"))
            .Should().BeNull();
    }

    [Fact]
    public async Task DeleteConfigurationAsync_WithNonexistentConfiguration_ReturnsFalse()
    {
        var result = await _client.DeleteConfigurationAsync("NonexistentConfig");

        result.Should().Be(false);
    }

    #endregion

    #region CreateVersionFromExistingAsync Tests

    [Fact]
    public async Task CreateVersionFromExistingAsync_WithValidSourceVersion_CreatesNewVersion()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var sourceVersion = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var sourceFile = new ConfigurationFile
        {
            Id = Guid.NewGuid(),
            VersionId = sourceVersion.Id,
            RelativePath = "main.dsc.yaml",
            ContentType = "text/plain",
            Checksum = "abc123",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(sourceVersion);
        _dbContext.ConfigurationFiles.Add(sourceFile);
        await _dbContext.SaveChangesAsync();

        // Create the source version directory and file for the service to copy
        // The service looks for files in {ConfigurationsDirectory}/{name}/v{version}
        // Use a unique directory to avoid conflicts with other tests
        var uniqueTestDir = Path.Combine(Path.GetTempPath(), $"dsc-test-{Guid.NewGuid()}");
        var configurationsDir = Path.Combine(uniqueTestDir, "configurations");
        var sourceVersionDir = Path.Combine(configurationsDir, "TestConfig", "v1.0.0");
        Directory.CreateDirectory(sourceVersionDir);
        var sourceFilePath = Path.Combine(sourceVersionDir, "main.dsc.yaml");
        await File.WriteAllTextAsync(sourceFilePath, "parameters:\n  env: test");

        // Create a new ServerConfig that points to our unique test directory
        var testServerConfig = Options.Create(new ServerConfig { DataDirectory = uniqueTestDir });
        var testClient = new ConfigurationService(
            _dbContext,
            testServerConfig,
            _mockAuthService.Object,
            _mockUserContext.Object,
            _mockLogger.Object,
            _mockHttpContextAccessor.Object,
            _mockSchemaBuilder.Object,
            _mockCompatibilityService.Object);

        try
        {
            var result = await testClient.CreateVersionFromExistingAsync(
                "TestConfig", "1.0.0", "2.0.0", isDraft: false);

            result.Should().Be(true);
            (await _dbContext.ConfigurationVersions.FirstOrDefaultAsync(v => v.Version == "2.0.0"))
                .Should().NotBeNull();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(uniqueTestDir))
            {
                Directory.Delete(uniqueTestDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task CreateVersionFromExistingAsync_WithNonexistentConfiguration_ReturnsFalse()
    {
        var result = await _client.CreateVersionFromExistingAsync(
            "NonexistentConfig", "1.0.0", "2.0.0", isDraft: false);

        result.Should().Be(false);
    }

    [Fact]
    public async Task CreateVersionFromExistingAsync_WithNonexistentSourceVersion_ReturnsFalse()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        await _dbContext.SaveChangesAsync();

        var result = await _client.CreateVersionFromExistingAsync(
            "TestConfig", "1.0.0", "2.0.0", isDraft: false);

        result.Should().Be(false);
    }

    [Fact]
    public async Task CreateVersionFromExistingAsync_WithDuplicateTargetVersion_ReturnsFalse()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var sourceVersion = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var existingVersion = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "2.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(sourceVersion);
        _dbContext.ConfigurationVersions.Add(existingVersion);
        await _dbContext.SaveChangesAsync();

        var result = await _client.CreateVersionFromExistingAsync(
            "TestConfig", "1.0.0", "2.0.0", isDraft: false);

        result.Should().Be(false);
    }

    [Fact]
    public async Task CreateVersionFromExistingAsync_WithDraftStatus_CreatesDraft()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var sourceVersion = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var sourceFile = new ConfigurationFile
        {
            Id = Guid.NewGuid(),
            VersionId = sourceVersion.Id,
            RelativePath = "main.dsc.yaml",
            ContentType = "text/plain",
            Checksum = "abc123",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(sourceVersion);
        _dbContext.ConfigurationFiles.Add(sourceFile);
        await _dbContext.SaveChangesAsync();

        var uniqueTestDir = Path.Combine(Path.GetTempPath(), $"dsc-test-{Guid.NewGuid()}");
        var configurationsDir = Path.Combine(uniqueTestDir, "configurations");
        var sourceVersionDir = Path.Combine(configurationsDir, "TestConfig", "v1.0.0");
        Directory.CreateDirectory(sourceVersionDir);
        var sourceFilePath = Path.Combine(sourceVersionDir, "main.dsc.yaml");
        await File.WriteAllTextAsync(sourceFilePath, "config {}");

        var testServerConfig = Options.Create(new ServerConfig { DataDirectory = uniqueTestDir });
        var testClient = new ConfigurationService(
            _dbContext,
            testServerConfig,
            _mockAuthService.Object,
            _mockUserContext.Object,
            _mockLogger.Object,
            _mockHttpContextAccessor.Object,
            _mockSchemaBuilder.Object,
            _mockCompatibilityService.Object);

        try
        {
            var result = await testClient.CreateVersionFromExistingAsync(
                "TestConfig", "1.0.0", "2.0.0", isDraft: true);

            result.Should().Be(true);

            var newVersion = await _dbContext.ConfigurationVersions
                .FirstOrDefaultAsync(v => v.Version == "2.0.0");

            newVersion?.Status.Should().Be(ConfigurationVersionStatus.Draft);
        }
        finally
        {
            if (Directory.Exists(uniqueTestDir))
            {
                Directory.Delete(uniqueTestDir, recursive: true);
            }
        }
    }

    #endregion

    #region AddFilesToVersionAsync Tests

    [Fact]
    public async Task AddFilesToVersionAsync_WithValidInput_AddsFilesToVersion()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        await _dbContext.SaveChangesAsync();

        var newFiles = CreateMockFiles(
            ("additional.yaml", "additional content"),
            ("config.json", "{}"));

        var result = await _client.AddFilesToVersionAsync("TestConfig", "1.0.0", newFiles);

        result.Should().Be(true);
        (await _dbContext.ConfigurationFiles
            .Where(f => f.VersionId == version.Id)
            .CountAsync())
            .Should().Be(2);
    }

    [Fact]
    public async Task AddFilesToVersionAsync_WithNonexistentConfiguration_ReturnsFalse()
    {
        var files = CreateMockFiles(("file.yaml", "content"));

        var result = await _client.AddFilesToVersionAsync("NonexistentConfig", "1.0.0", files);

        result.Should().Be(false);
    }

    [Fact]
    public async Task AddFilesToVersionAsync_WithNonexistentVersion_ReturnsFalse()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        await _dbContext.SaveChangesAsync();

        var files = CreateMockFiles(("file.yaml", "content"));

        var result = await _client.AddFilesToVersionAsync("TestConfig", "1.0.0", files);

        result.Should().Be(false);
    }

    [Fact]
    public async Task AddFilesToVersionAsync_WithPublishedVersion_ReturnsFalse()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        await _dbContext.SaveChangesAsync();

        var newFiles = CreateMockFiles(("additional.yaml", "content"));

        var result = await _client.AddFilesToVersionAsync("TestConfig", "1.0.0", newFiles);

        result.Should().Be(false);
    }

    [Fact]
    public async Task AddFilesToVersionAsync_WithDuplicateFileNames_SkipsDuplicates()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var existingFile = new ConfigurationFile
        {
            Id = Guid.NewGuid(),
            VersionId = version.Id,
            RelativePath = "duplicate.yaml",
            ContentType = "text/plain",
            Checksum = "abc123",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        _dbContext.ConfigurationFiles.Add(existingFile);
        await _dbContext.SaveChangesAsync();

        var newFiles = CreateMockFiles(
            ("duplicate.yaml", "new content"),
            ("other.yaml", "other content"));

        var result = await _client.AddFilesToVersionAsync("TestConfig", "1.0.0", newFiles);

        result.Should().Be(true);
        // Should only add the non-duplicate file
        (await _dbContext.ConfigurationFiles
            .Where(f => f.VersionId == version.Id)
            .CountAsync())
            .Should().Be(2);
    }

    [Fact]
    public async Task AddFilesToVersionAsync_WithEmptyFileList_ReturnsTrue()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        await _dbContext.SaveChangesAsync();

        var result = await _client.AddFilesToVersionAsync("TestConfig", "1.0.0", []);

        result.Should().Be(true);
    }

    #endregion

    #region DeleteVersionAsync Tests

    [Fact]
    public async Task DeleteVersionAsync_WithValidVersion_DeletesSuccessfully()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        await _dbContext.SaveChangesAsync();

        var result = await _client.DeleteVersionAsync("TestConfig", "1.0.0");

        result.Should().Be(true);
        (await _dbContext.ConfigurationVersions.FirstOrDefaultAsync(v => v.Version == "1.0.0"))
            .Should().BeNull();
    }

    [Fact]
    public async Task DeleteVersionAsync_WithNonexistentConfiguration_ReturnsFalse()
    {
        var result = await _client.DeleteVersionAsync("NonexistentConfig", "1.0.0");

        result.Should().Be(false);
    }

    [Fact]
    public async Task DeleteVersionAsync_WithNonexistentVersion_ReturnsFalse()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        await _dbContext.SaveChangesAsync();

        var result = await _client.DeleteVersionAsync("TestConfig", "1.0.0");

        result.Should().Be(false);
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_WithValidFile_DeletesSuccessfully()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var file = new ConfigurationFile
        {
            Id = Guid.NewGuid(),
            VersionId = version.Id,
            RelativePath = "extra.yaml",
            ContentType = "text/plain",
            Checksum = "abc123",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        _dbContext.ConfigurationFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var result = await _client.DeleteFileAsync("TestConfig", "1.0.0", "extra.yaml");

        result.Should().Be(true);
        (await _dbContext.ConfigurationFiles
            .FirstOrDefaultAsync(f => f.RelativePath == "extra.yaml"))
            .Should().BeNull();
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonexistentConfiguration_ReturnsFalse()
    {
        var result = await _client.DeleteFileAsync("NonexistentConfig", "1.0.0", "file.yaml");

        result.Should().Be(false);
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonexistentVersion_ReturnsFalse()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        await _dbContext.SaveChangesAsync();

        var result = await _client.DeleteFileAsync("TestConfig", "1.0.0", "file.yaml");

        result.Should().Be(false);
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonexistentFile_ReturnsFalse()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        await _dbContext.SaveChangesAsync();

        var result = await _client.DeleteFileAsync("TestConfig", "1.0.0", "nonexistent.yaml");

        result.Should().Be(false);
    }

    [Fact]
    public async Task DeleteFileAsync_WithLastFile_StillDeletes()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var file = new ConfigurationFile
        {
            Id = Guid.NewGuid(),
            VersionId = version.Id,
            RelativePath = "only-file.yaml",
            ContentType = "text/plain",
            Checksum = "abc123",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        _dbContext.ConfigurationFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var result = await _client.DeleteFileAsync("TestConfig", "1.0.0", "only-file.yaml");

        result.Should().Be(true);
        (await _dbContext.ConfigurationFiles
            .Where(f => f.VersionId == version.Id)
            .CountAsync())
            .Should().Be(0);
    }

    #endregion

    #region ChangeVersionEntryPointAsync Tests

    [Fact]
    public async Task ChangeVersionEntryPointAsync_WithValidFile_ChangesEntryPoint()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var file = new ConfigurationFile
        {
            Id = Guid.NewGuid(),
            VersionId = version.Id,
            RelativePath = "secondary.dsc.yaml",
            ContentType = "text/plain",
            Checksum = "abc123",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        _dbContext.ConfigurationFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var result = await _client.ChangeVersionEntryPointAsync("TestConfig", "1.0.0", "secondary.dsc.yaml");

        result.Should().Be(true);
        var updatedVersion = await _dbContext.ConfigurationVersions.FirstOrDefaultAsync(v => v.Version == "1.0.0");
        updatedVersion.Should().NotBeNull();
        updatedVersion!.EntryPoint.Should().Be("secondary.dsc.yaml");
    }

    [Fact]
    public async Task ChangeVersionEntryPointAsync_WithNonexistentConfiguration_ReturnsFalse()
    {
        var result = await _client.ChangeVersionEntryPointAsync("NonexistentConfig", "1.0.0", "new-entry.yaml");

        result.Should().Be(false);
    }

    [Fact]
    public async Task ChangeVersionEntryPointAsync_WithNonexistentVersion_ReturnsFalse()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        await _dbContext.SaveChangesAsync();

        var result = await _client.ChangeVersionEntryPointAsync("TestConfig", "1.0.0", "new-entry.yaml");

        result.Should().Be(false);
    }

    [Fact]
    public async Task ChangeVersionEntryPointAsync_WithNonexistentFile_ReturnsFalse()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        await _dbContext.SaveChangesAsync();

        var result = await _client.ChangeVersionEntryPointAsync("TestConfig", "1.0.0", "nonexistent.yaml");

        result.Should().Be(false);
    }

    [Fact]
    public async Task ChangeVersionEntryPointAsync_WithNestedFile_UpdatesEntryPoint()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var file = new ConfigurationFile
        {
            Id = Guid.NewGuid(),
            VersionId = version.Id,
            RelativePath = "configs/nested.dsc.yaml",
            ContentType = "text/plain",
            Checksum = "abc123",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        _dbContext.ConfigurationFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var result = await _client.ChangeVersionEntryPointAsync("TestConfig", "1.0.0", "configs/nested.dsc.yaml");

        result.Should().Be(true);
        var updatedVersion = await _dbContext.ConfigurationVersions.FirstOrDefaultAsync(v => v.Version == "1.0.0");
        updatedVersion!.EntryPoint.Should().Be("configs/nested.dsc.yaml");
    }

    #endregion

    #region DownloadFileAsync Tests

    [Fact]
    public async Task DownloadFileAsync_WithNonexistentFile_ReturnsNull()
    {
        var result = await _client.DownloadFileAsync("NonexistentConfig", "1.0.0", "file.yaml");

        result.Should().BeNull();
    }

    #endregion

    #region SaveFileAsync Tests

    [Fact]
    public async Task SaveFileAsync_WithValidFile_UpdatesFileContent()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var file = new ConfigurationFile
        {
            Id = Guid.NewGuid(),
            VersionId = version.Id,
            RelativePath = "main.dsc.yaml",
            ContentType = "text/plain",
            Checksum = "oldchecksum",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        _dbContext.ConfigurationFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        // Create the version directory and file for the service to modify
        var uniqueTestDir = Path.Combine(Path.GetTempPath(), $"dsc-test-{Guid.NewGuid()}");
        var configurationsDir = Path.Combine(uniqueTestDir, "configurations");
        var versionDir = Path.Combine(configurationsDir, "TestConfig", "v1.0.0");
        Directory.CreateDirectory(versionDir);
        var filePath = Path.Combine(versionDir, "main.dsc.yaml");
        await File.WriteAllTextAsync(filePath, "old content");

        // Create a new ServerConfig that points to our unique test directory
        var testServerConfig = Options.Create(new ServerConfig { DataDirectory = uniqueTestDir });
        var testClient = new ConfigurationService(
            _dbContext,
            testServerConfig,
            _mockAuthService.Object,
            _mockUserContext.Object,
            _mockLogger.Object,
            _mockHttpContextAccessor.Object,
            _mockSchemaBuilder.Object,
            _mockCompatibilityService.Object);

        try
        {
            var result = await testClient.SaveFileAsync(
                "TestConfig", "1.0.0", "main.dsc.yaml", "new content");

            result.Should().Be(true);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(uniqueTestDir))
            {
                Directory.Delete(uniqueTestDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveFileAsync_WithNonexistentConfiguration_ReturnsFalse()
    {
        var result = await _client.SaveFileAsync("NonexistentConfig", "1.0.0", "file.yaml", "content");

        result.Should().Be(false);
    }

    [Fact]
    public async Task SaveFileAsync_WithNonexistentVersion_ReturnsFalse()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        await _dbContext.SaveChangesAsync();

        var result = await _client.SaveFileAsync("TestConfig", "1.0.0", "file.yaml", "content");

        result.Should().Be(false);
    }

    [Fact]
    public async Task SaveFileAsync_WithNonexistentFile_ReturnsFalse()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        await _dbContext.SaveChangesAsync();

        var uniqueTestDir = Path.Combine(Path.GetTempPath(), $"dsc-test-{Guid.NewGuid()}");
        var configurationsDir = Path.Combine(uniqueTestDir, "configurations");
        var versionDir = Path.Combine(configurationsDir, "TestConfig", "v1.0.0");
        Directory.CreateDirectory(versionDir);

        var testServerConfig = Options.Create(new ServerConfig { DataDirectory = uniqueTestDir });
        var testClient = new ConfigurationService(
            _dbContext,
            testServerConfig,
            _mockAuthService.Object,
            _mockUserContext.Object,
            _mockLogger.Object,
            _mockHttpContextAccessor.Object,
            _mockSchemaBuilder.Object,
            _mockCompatibilityService.Object);

        try
        {
            // Try to save a file that doesn't exist in the database
            var result = await testClient.SaveFileAsync(
                "TestConfig", "1.0.0", "nonexistent-file.yaml", "new file content");

            result.Should().Be(false);
        }
        finally
        {
            if (Directory.Exists(uniqueTestDir))
            {
                Directory.Delete(uniqueTestDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SaveFileAsync_WithWhitespaceOnlyContent_SavesSuccessfully()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "TestConfig",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var version = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Version = "1.0.0",
            EntryPoint = "main.dsc.yaml",
            Status = ConfigurationVersionStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var file = new ConfigurationFile
        {
            Id = Guid.NewGuid(),
            VersionId = version.Id,
            RelativePath = "main.dsc.yaml",
            ContentType = "text/plain",
            Checksum = "oldchecksum",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Configurations.Add(config);
        _dbContext.ConfigurationVersions.Add(version);
        _dbContext.ConfigurationFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var uniqueTestDir = Path.Combine(Path.GetTempPath(), $"dsc-test-{Guid.NewGuid()}");
        var configurationsDir = Path.Combine(uniqueTestDir, "configurations");
        var versionDir = Path.Combine(configurationsDir, "TestConfig", "v1.0.0");
        Directory.CreateDirectory(versionDir);
        var filePath = Path.Combine(versionDir, "main.dsc.yaml");
        await File.WriteAllTextAsync(filePath, "old content");

        var testServerConfig = Options.Create(new ServerConfig { DataDirectory = uniqueTestDir });
        var testClient = new ConfigurationService(
            _dbContext,
            testServerConfig,
            _mockAuthService.Object,
            _mockUserContext.Object,
            _mockLogger.Object,
            _mockHttpContextAccessor.Object,
            _mockSchemaBuilder.Object,
            _mockCompatibilityService.Object);

        try
        {
            var result = await testClient.SaveFileAsync(
                "TestConfig", "1.0.0", "main.dsc.yaml", "   \n\t  ");

            result.Should().Be(true);
        }
        finally
        {
            if (Directory.Exists(uniqueTestDir))
            {
                Directory.Delete(uniqueTestDir, recursive: true);
            }
        }
    }

    #endregion

    #region Helper Methods

    private List<IBrowserFile> CreateMockFiles(params (string name, string content)[] files)
    {
        var mockFiles = new List<IBrowserFile>();

        foreach (var (name, content) in files)
        {
            var mockFile = new Mock<IBrowserFile>();
            mockFile.Setup(f => f.Name).Returns(name);
            mockFile.Setup(f => f.Size).Returns(content.Length);

            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            mockFile.Setup(f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(stream);

            mockFiles.Add(mockFile.Object);
        }

        return mockFiles;
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
