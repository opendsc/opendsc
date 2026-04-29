// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using Xunit;
using Xunit.Sdk;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class ParameterServiceTests : IDisposable
{
    private readonly ServerDbContext _db;
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly Mock<IResourceAuthorizationService> _mockAuthService;
    private readonly Mock<IParameterMergeService> _mockMergeService;
    private readonly Mock<IParameterValidator> _mockValidator;
    private readonly IOptions<ServerConfig> _serverConfig;
    private readonly ParameterService _service;
    private readonly Guid _testUserId;
    private readonly string _tempDir;

    public ParameterServiceTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ServerDbContext(options);
        _testUserId = Guid.NewGuid();

        _mockUserContext = new Mock<IUserContextService>();
        _mockUserContext.Setup(x => x.GetCurrentUserId()).Returns(_testUserId);

        _mockAuthService = new Mock<IResourceAuthorizationService>();
        _mockAuthService.Setup(x => x.CanModifyParameterAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);
        _mockAuthService.Setup(x => x.CanManageParameterAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        _mockMergeService = new Mock<IParameterMergeService>();
        _mockValidator = new Mock<IParameterValidator>();

        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        var config = new ServerConfig { ParametersDirectory = _tempDir };
        _serverConfig = Options.Create(config);

        _service = new ParameterService(
            _db,
            _serverConfig,
            _mockAuthService.Object,
            _mockUserContext.Object,
            _mockMergeService.Object,
            _mockValidator.Object,
            new NullLogger<ParameterService>()
        );
    }

    #region CreateOrUpdateParameterAsync Tests

    [Fact]
    public async Task CreateOrUpdateParameterAsync_WithValidInput_CreatesNewParameter()
    {
        // Arrange
        var (scopeTypeId, configId, paramSchemaId) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters:\n  key: value";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ValidationResult { IsValid = true });

        // Act
        var result = await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        var savedFile = await _db.ParameterFiles
            .FirstOrDefaultAsync(pf => pf.Version == version, TestContext.Current.CancellationToken);
        savedFile.Should().NotBeNull();
        savedFile!.Status.Should().Be(ParameterVersionStatus.Draft);
        savedFile.IsPassthrough.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrUpdateParameterAsync_WithMissingConfiguration_ReturnsFail()
    {
        // Arrange
        var scopeTypeId = await SetupScopeTypeAsync("Default", ScopeValueMode.Unrestricted);
        var invalidConfigId = Guid.NewGuid();
        const string version = "1.0.0";
        const string content = "parameters: {}";

        // Act
        var result = await _service.CreateOrUpdateParameterAsync(scopeTypeId, invalidConfigId, null, version, content);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Configuration not found");
    }

    [Fact]
    public async Task CreateOrUpdateParameterAsync_WithMissingScopeType_ReturnsFail()
    {
        // Arrange
        var invalidScopeTypeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        const string version = "1.0.0";
        const string content = "parameters: {}";

        // Act
        var result = await _service.CreateOrUpdateParameterAsync(invalidScopeTypeId, configId, null, version, content);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Scope type not found");
    }

    [Fact]
    public async Task CreateOrUpdateParameterAsync_WithInvalidVersion_ReturnsFail()
    {
        // Arrange
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string invalidVersion = "not-a-version";
        const string content = "parameters: {}";

        // Act
        var result = await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, invalidVersion, content);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not a valid semantic version");
    }

    [Fact]
    public async Task CreateOrUpdateParameterAsync_WithDefaultScopeAndScopeValue_ReturnsFail()
    {
        // Arrange
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters: {}";

        // Act
        var result = await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, "some-value", version, content);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Default");
    }

    [Fact]
    public async Task CreateOrUpdateParameterAsync_WithMissingParameterSchema_ReturnsFail()
    {
        // Arrange
        var scopeTypeId = await SetupScopeTypeAsync("Default", ScopeValueMode.Unrestricted);
        var configId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "TestConfig" };
        _db.Configurations.Add(config);
        _db.SaveChanges();

        const string version = "1.0.0";
        const string content = "parameters: {}";

        // Act
        var result = await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No parameter schema exists");
    }

    [Fact]
    public async Task CreateOrUpdateParameterAsync_WithUnauthenticatedUser_ReturnsFail()
    {
        // Arrange
        _mockUserContext.Setup(x => x.GetCurrentUserId()).Returns((Guid?)null);
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters: {}";

        // Act
        var result = await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not authenticated");
    }

    [Fact]
    public async Task CreateOrUpdateParameterAsync_WithNoAccess_ReturnsFail()
    {
        // Arrange
        _mockAuthService.Setup(x => x.CanModifyParameterAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters: {}";

        // Act
        var result = await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Access denied");
    }

    [Fact]
    public async Task CreateOrUpdateParameterAsync_WithValidationFailure_ReturnsFail()
    {
        // Arrange
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "invalid: yaml: content:";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(ValidationResult.Failure(new ValidationError { Path = "root", Message = "Invalid schema", Code = "INVALID" }));

        // Act
        var result = await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Parameter validation failed");
    }

    [Fact]
    public async Task CreateOrUpdateParameterAsync_WithPassthrough_SkipsValidation()
    {
        // Arrange
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters: {}";

        // Act
        var result = await _service.CreateOrUpdateParameterAsync(
            scopeTypeId, configId, null, version, content, isPassthrough: true);

        // Assert
        result.Success.Should().BeTrue();
        _mockValidator.Verify(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrUpdateParameterAsync_WithExistingDraftVersion_Updates()
    {
        // Arrange
        var (scopeTypeId, configId, paramSchemaId) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string originalContent = "parameters:\n  key: original";
        const string updatedContent = "parameters:\n  key: updated";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ValidationResult { IsValid = true });

        // Create initial version
        await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, originalContent);

        // Act - Update the draft
        var result = await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, updatedContent);

        // Assert
        result.Success.Should().BeTrue();
        var files = await _db.ParameterFiles.Where(pf => pf.Version == version).ToListAsync(TestContext.Current.CancellationToken);
        files.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateOrUpdateParameterAsync_WithPublishedVersion_CannotUpdate()
    {
        // Arrange
        var (scopeTypeId, configId, paramSchemaId) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters: {}";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ValidationResult { IsValid = true });

        // Create and publish version
        await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);
        await _service.PublishParameterVersionAsync(scopeTypeId, configId, null, version);

        // Act - Try to update published version
        var result = await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, "new: content");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cannot modify a published parameter version");
    }

    #endregion

    #region GetParameterVersionsAsync Tests

    [Fact]
    public async Task GetParameterVersionsAsync_WithMultipleVersions_ReturnsOrderedByCreatedAt()
    {
        // Arrange
        var (scopeTypeId, configId, paramSchemaId) = await SetupParameterEntitiesAsync();

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(ValidationResult.Success());

        // Manually create parameter files with controlled timestamps
        var now = DateTimeOffset.UtcNow;
        var paramFiles = new[]
        {
            new ParameterFile
            {
                Id = Guid.NewGuid(),
                ParameterSchemaId = paramSchemaId,
                ScopeTypeId = scopeTypeId,
                ScopeValue = null,
                Version = "1.0.0",
                Status = ParameterVersionStatus.Published,
                Checksum = "checksum1",
                IsPassthrough = false,
                MajorVersion = 1,
                CreatedAt = now.AddSeconds(-2)
            },
            new ParameterFile
            {
                Id = Guid.NewGuid(),
                ParameterSchemaId = paramSchemaId,
                ScopeTypeId = scopeTypeId,
                ScopeValue = null,
                Version = "1.1.0",
                Status = ParameterVersionStatus.Published,
                Checksum = "checksum2",
                IsPassthrough = false,
                MajorVersion = 1,
                CreatedAt = now.AddSeconds(-1)
            },
            new ParameterFile
            {
                Id = Guid.NewGuid(),
                ParameterSchemaId = paramSchemaId,
                ScopeTypeId = scopeTypeId,
                ScopeValue = null,
                Version = "1.2.0",
                Status = ParameterVersionStatus.Published,
                Checksum = "checksum3",
                IsPassthrough = false,
                MajorVersion = 1,
                CreatedAt = now
            }
        };
        _db.ParameterFiles.AddRange(paramFiles);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _service.GetParameterVersionsAsync(scopeTypeId, configId, null);

        // Assert
        result.Should().HaveCount(3);
        result[0].Version.Should().Be("1.2.0"); // Most recent first
        result[1].Version.Should().Be("1.1.0");
        result[2].Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task GetParameterVersionsAsync_WithNoVersions_ReturnsEmpty()
    {
        // Arrange
        var scopeTypeId = await SetupScopeTypeAsync("Default", ScopeValueMode.Unrestricted);
        var configId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "TestConfig" };
        _db.Configurations.Add(config);
        _db.SaveChanges();

        // Act
        var result = await _service.GetParameterVersionsAsync(scopeTypeId, configId, null);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region PublishParameterVersionAsync Tests

    [Fact]
    public async Task PublishParameterVersionAsync_WithValidDraft_PublishesSuccessfully()
    {
        // Arrange
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters: {}";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ValidationResult { IsValid = true });

        await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);

        // Act
        var result = await _service.PublishParameterVersionAsync(scopeTypeId, configId, null, version);

        // Assert
        result.Success.Should().BeTrue();
        var file = await _db.ParameterFiles.FirstOrDefaultAsync(pf => pf.Version == version, TestContext.Current.CancellationToken);
        file!.Status.Should().Be(ParameterVersionStatus.Published);
    }

    [Fact]
    public async Task PublishParameterVersionAsync_WithMissingVersion_ReturnsFail()
    {
        // Arrange
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";

        // Act
        var result = await _service.PublishParameterVersionAsync(scopeTypeId, configId, null, version);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task PublishParameterVersionAsync_WithUnauthenticatedUser_ReturnsFail()
    {
        // Arrange
        _mockUserContext.Setup(x => x.GetCurrentUserId()).Returns((Guid?)null);
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";

        // Act
        var result = await _service.PublishParameterVersionAsync(scopeTypeId, configId, null, version);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not authenticated");
    }

    [Fact]
    public async Task PublishParameterVersionAsync_WithNoAccess_ReturnsFail()
    {
        // Arrange
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters: {}";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(ValidationResult.Success());

        await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);

        // Now deny access for publish
        _mockAuthService.Setup(x => x.CanModifyParameterAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.PublishParameterVersionAsync(scopeTypeId, configId, null, version);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Access denied");
    }

    #endregion

    #region DeleteParameterVersionAsync Tests

    [Fact]
    public async Task DeleteParameterVersionAsync_WithValidVersion_DeletesSuccessfully()
    {
        // Arrange
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters: {}";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ValidationResult { IsValid = true });

        _mockAuthService.Setup(x => x.CanManageParameterAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);

        // Act
        var result = await _service.DeleteParameterVersionAsync(scopeTypeId, configId, null, version);

        // Assert
        result.Success.Should().BeTrue();
        var file = await _db.ParameterFiles.FirstOrDefaultAsync(pf => pf.Version == version, TestContext.Current.CancellationToken);
        file.Should().BeNull();
    }

    [Fact]
    public async Task DeleteParameterVersionAsync_WithMissingVersion_ReturnsFail()
    {
        // Arrange
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";

        // Act
        var result = await _service.DeleteParameterVersionAsync(scopeTypeId, configId, null, version);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteParameterVersionAsync_WithUnauthenticatedUser_ReturnsFail()
    {
        // Arrange
        _mockUserContext.Setup(x => x.GetCurrentUserId()).Returns((Guid?)null);
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();

        // Act
        var result = await _service.DeleteParameterVersionAsync(scopeTypeId, configId, null, "1.0.0");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not authenticated");
    }

    #endregion

    #region GetAvailableMajorVersionsAsync Tests

    [Fact]
    public async Task GetAvailableMajorVersionsAsync_WithMultipleVersions_ReturnsSortedUniqueMajorVersions()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "TestConfig" };
        _db.Configurations.Add(config);
        _db.SaveChanges();

        var schemas = new[]
        {
            new ParameterSchema { Id = Guid.NewGuid(), ConfigurationId = configId, SchemaVersion = "1.0.0" },
            new ParameterSchema { Id = Guid.NewGuid(), ConfigurationId = configId, SchemaVersion = "1.5.0" },
            new ParameterSchema { Id = Guid.NewGuid(), ConfigurationId = configId, SchemaVersion = "2.0.0" },
            new ParameterSchema { Id = Guid.NewGuid(), ConfigurationId = configId, SchemaVersion = "2.1.0" }
        };
        _db.ParameterSchemas.AddRange(schemas);
        _db.SaveChanges();

        // Act
        var result = await _service.GetAvailableMajorVersionsAsync(configId);

        // Assert
        result.Should().Equal(1, 2);
    }

    [Fact]
    public async Task GetAvailableMajorVersionsAsync_WithNoSchemas_ReturnsEmpty()
    {
        // Arrange
        var configId = Guid.NewGuid();

        // Act
        var result = await _service.GetAvailableMajorVersionsAsync(configId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetNodeParameterProvenanceAsync Tests

    [Fact]
    public async Task GetNodeParameterProvenanceAsync_WithValidNode_ReturnsProvenance()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var node = new Node { Id = nodeId, Fqdn = "test.node.local" };
        _db.Nodes.Add(node);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mergeResult = new MergeResult
        {
            MergedContent = "merged: content",
            Provenance = new Dictionary<string, ParameterProvenance>
            {
                ["key1"] = new ParameterProvenance
                {
                    ScopeTypeName = "Default",
                    ScopeValue = null,
                    Precedence = 0,
                    Value = "value1"
                }
            }
        };
        _mockMergeService.Setup(x => x.MergeParametersWithProvenanceAsync(nodeId, configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergeResult);

        // Act
        var result = await _service.GetNodeParameterProvenanceAsync(nodeId, configId);

        // Assert
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(nodeId);
        result.ConfigurationId.Should().Be(configId);
        result.MergedParameters.Should().Be("merged: content");
        result.Provenance.Should().ContainKey("key1");
    }

    [Fact]
    public async Task GetNodeParameterProvenanceAsync_WithMissingNode_ReturnsNull()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configId = Guid.NewGuid();

        // Act
        var result = await _service.GetNodeParameterProvenanceAsync(nodeId, configId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNodeParameterProvenanceAsync_WithMergeReturningNull_ReturnsNull()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var node = new Node { Id = nodeId, Fqdn = "test.node.local" };
        _db.Nodes.Add(node);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockMergeService.Setup(x => x.MergeParametersWithProvenanceAsync(nodeId, configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MergeResult?)null);

        // Act
        var result = await _service.GetNodeParameterProvenanceAsync(nodeId, configId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNodeParameterProvenanceAsync_WithProvenanceHavingOverrides_MapsOverriddenValues()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var node = new Node { Id = nodeId, Fqdn = "test.node.local" };
        _db.Nodes.Add(node);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mergeResult = new MergeResult
        {
            MergedContent = "merged: content",
            Provenance = new Dictionary<string, ParameterProvenance>
            {
                ["key1"] = new ParameterProvenance
                {
                    ScopeTypeName = "Default",
                    ScopeValue = null,
                    Precedence = 0,
                    Value = "value1",
                    OverriddenValues = new List<ScopeValueInfo>
                    {
                        new() { ScopeTypeName = "Node", ScopeValue = "node1", Precedence = 1, Value = "value2" }
                    }
                }
            }
        };
        _mockMergeService.Setup(x => x.MergeParametersWithProvenanceAsync(nodeId, configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergeResult);

        // Act
        var result = await _service.GetNodeParameterProvenanceAsync(nodeId, configId);

        // Assert
        result.Should().NotBeNull();
        result!.Provenance["key1"].OverriddenBy.Should().NotBeNull();
        result.Provenance["key1"].OverriddenBy!.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetNodeParameterProvenanceAsync_WithException_ReturnsNull()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var node = new Node { Id = nodeId, Fqdn = "test.node.local" };
        _db.Nodes.Add(node);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mockMergeService.Setup(x => x.MergeParametersWithProvenanceAsync(nodeId, configId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        // Act
        var result = await _service.GetNodeParameterProvenanceAsync(nodeId, configId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateParameterDraftAsync Tests

    [Fact]
    public async Task UpdateParameterDraftAsync_WithValidDraft_UpdatesSuccessfully()
    {
        // Arrange
        var (scopeTypeId, configId, paramSchemaId) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string originalContent = "parameters:\n  key: value";
        const string updatedContent = "parameters:\n  key: newvalue";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ValidationResult { IsValid = true });

        // Create initial draft
        await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, originalContent);
        var paramFile = await _db.ParameterFiles.FirstOrDefaultAsync(pf => pf.Version == version, TestContext.Current.CancellationToken);
        paramFile.Should().NotBeNull();

        // Act
        var result = await _service.UpdateParameterDraftAsync(paramFile!.Id, updatedContent);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        var updatedFile = await _db.ParameterFiles.FirstOrDefaultAsync(pf => pf.Id == paramFile.Id, TestContext.Current.CancellationToken);
        updatedFile.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateParameterDraftAsync_WithMissingParameter_ReturnsFail()
    {
        // Arrange
        var parameterId = Guid.NewGuid();
        const string content = "parameters: {}";

        // Act
        var result = await _service.UpdateParameterDraftAsync(parameterId, content);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Parameter version not found");
    }

    [Fact]
    public async Task UpdateParameterDraftAsync_WithPublishedVersion_ReturnsFail()
    {
        // Arrange
        var (scopeTypeId, configId, paramSchemaId) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters:\n  key: value";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ValidationResult { IsValid = true });

        await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);
        var paramFile = await _db.ParameterFiles.FirstOrDefaultAsync(pf => pf.Version == version, TestContext.Current.CancellationToken);

        // Publish the version
        await _service.PublishParameterVersionAsync(scopeTypeId, configId, null, version);

        // Act
        var result = await _service.UpdateParameterDraftAsync(paramFile!.Id, "new content");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Only draft versions can be edited");
    }

    [Fact]
    public async Task UpdateParameterDraftAsync_WithUnauthenticatedUser_ReturnsFail()
    {
        // Arrange
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters:\n  key: value";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ValidationResult { IsValid = true });

        // Create parameter with authenticated user
        await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);
        var paramFile = await _db.ParameterFiles.FirstOrDefaultAsync(pf => pf.Version == version, TestContext.Current.CancellationToken);
        paramFile.Should().NotBeNull();

        // Now set up for unauthenticated update
        _mockUserContext.Setup(x => x.GetCurrentUserId()).Returns((Guid?)null);

        // Act
        var result = await _service.UpdateParameterDraftAsync(paramFile!.Id, "new content");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not authenticated");
    }

    [Fact]
    public async Task UpdateParameterDraftAsync_WithNoAccess_ReturnsFail()
    {
        // Arrange
        var (scopeTypeId, configId, paramSchemaId) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters:\n  key: value";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ValidationResult { IsValid = true });

        // Create parameter with auth allowed
        await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);
        var paramFile = await _db.ParameterFiles.FirstOrDefaultAsync(pf => pf.Version == version, TestContext.Current.CancellationToken);
        paramFile.Should().NotBeNull();

        // Now deny access for the update
        _mockAuthService.Setup(x => x.CanModifyParameterAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateParameterDraftAsync(paramFile!.Id, "new content");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Access denied");
    }

    #endregion

    #region GetParameterContentAsync Tests

    [Fact]
    public async Task GetParameterContentAsync_WithValidParameter_ReturnsContent()
    {
        // Arrange
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters:\n  key: value";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ValidationResult { IsValid = true });

        await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);
        var paramFile = await _db.ParameterFiles.FirstOrDefaultAsync(pf => pf.Version == version, TestContext.Current.CancellationToken);

        // Act
        var result = await _service.GetParameterContentAsync(paramFile!.Id);

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public async Task GetParameterContentAsync_WithMissingParameter_ReturnsNull()
    {
        // Arrange
        var parameterId = Guid.NewGuid();

        // Act
        var result = await _service.GetParameterContentAsync(parameterId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetParameterContentAsync_WithMissingFile_ReturnsNull()
    {
        // Arrange
        var (scopeTypeId, configId, _) = await SetupParameterEntitiesAsync();
        const string version = "1.0.0";
        const string content = "parameters:\n  key: value";

        _mockValidator.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new ValidationResult { IsValid = true });

        await _service.CreateOrUpdateParameterAsync(scopeTypeId, configId, null, version, content);
        var paramFile = await _db.ParameterFiles.FirstOrDefaultAsync(pf => pf.Version == version, TestContext.Current.CancellationToken);

        // Delete the physical file
        var dataDir = _serverConfig.Value.ParametersDirectory;
        var filePath = Path.Combine(dataDir, paramFile!.ParameterSchema.Configuration.Name,
            paramFile.ScopeType.Name, $"v{paramFile.Version}", "parameters.yaml");
        var fileDir = Path.GetDirectoryName(filePath);
        if (Directory.Exists(fileDir))
        {
            Directory.Delete(fileDir, true);
        }

        // Act
        var result = await _service.GetParameterContentAsync(paramFile.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetParameterContentAsync_WithException_ReturnsNull()
    {
        // Arrange
        var (scopeTypeId, configId, paramSchemaId) = await SetupParameterEntitiesAsync();

        // Create a parameter file in the database without creating the physical file
        var scopeType = await _db.ScopeTypes.FirstAsync(TestContext.Current.CancellationToken);
        var config = await _db.Configurations.FirstAsync(TestContext.Current.CancellationToken);
        var paramFile = new ParameterFile
        {
            Id = Guid.NewGuid(),
            ParameterSchemaId = paramSchemaId,
            ScopeTypeId = scopeType.Id,
            ScopeValue = null,
            Version = "1.0.0",
            Status = ParameterVersionStatus.Published,
            Checksum = "abc123",
            IsPassthrough = false,
            MajorVersion = 1
        };
        _db.ParameterFiles.Add(paramFile);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Set the directory to an invalid path that will cause an exception
        var invalidConfig = new ServerConfig { ParametersDirectory = "\x00invalid" };
        var invalidOptions = Options.Create(invalidConfig);
        var service = new ParameterService(
            _db,
            invalidOptions,
            _mockAuthService.Object,
            _mockUserContext.Object,
            _mockMergeService.Object,
            _mockValidator.Object,
            new NullLogger<ParameterService>()
        );

        // Act
        var result = await service.GetParameterContentAsync(paramFile.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private async Task<(Guid ScopeTypeId, Guid ConfigId, Guid ParameterSchemaId)> SetupParameterEntitiesAsync()
    {
        var scopeTypeId = await SetupScopeTypeAsync("Default", ScopeValueMode.Unrestricted);
        var configId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "TestConfig" };
        _db.Configurations.Add(config);

        var paramSchemaId = Guid.NewGuid();
        var paramSchema = new ParameterSchema
        {
            Id = paramSchemaId,
            ConfigurationId = configId,
            SchemaVersion = "1.0.0",
            GeneratedJsonSchema = "{}"
        };
        _db.ParameterSchemas.Add(paramSchema);
        _db.SaveChanges();

        return (scopeTypeId, configId, paramSchemaId);
    }

    private async Task<Guid> SetupScopeTypeAsync(string name, ScopeValueMode valueMode)
    {
        var scopeTypeId = Guid.NewGuid();
        var scopeType = new ScopeType
        {
            Id = scopeTypeId,
            Name = name,
            ValueMode = valueMode
        };
        _db.ScopeTypes.Add(scopeType);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return scopeTypeId;
    }

    #endregion

    public void Dispose()
    {
        _db?.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
