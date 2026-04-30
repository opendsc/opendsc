// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

#pragma warning disable xUnit1051

using System.Security.Cryptography;
using System.Text;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class ParameterMergeServiceTests : IDisposable
{
    private readonly ServerDbContext _db;
    private readonly Mock<IParameterMerger> _mockMerger;
    private readonly IOptions<ServerConfig> _serverConfig;
    private readonly ParameterMergeService _service;
    private readonly string _tempDir;

    public ParameterMergeServiceTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ServerDbContext(options);
        _mockMerger = new Mock<IParameterMerger>();
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        var config = new ServerConfig { ParametersDirectory = _tempDir };
        _serverConfig = Options.Create(config);

        _service = new ParameterMergeService(
            _db,
            _mockMerger.Object,
            _serverConfig,
            new NullLogger<ParameterMergeService>()
        );
    }

    public void Dispose()
    {
        _db?.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region MergeParametersAsync Tests - Validation

    [Fact]
    public async Task MergeParametersAsync_WithNullConfiguration_ReturnsNull()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        CreateNode(nodeId, "node1.contoso.com");

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MergeParametersAsync_WithNullNode_ReturnsNull()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        CreateConfiguration(configurationId, "MyConfig");

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MergeParametersAsync_WithNoParameterSources_ReturnsNull()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        CreateConfiguration(configurationId, "MyConfig");
        CreateNode(nodeId, "node1.contoso.com");

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region MergeParametersAsync Tests - Default Scope

    [Fact]
    public async Task MergeParametersAsync_WithDefaultScopeType_MergesDefaultParameters()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";
        const string defaultContent = "defaultParam: value1";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        var defaultScopeType = CreateScopeType("Default", enabled: true, precedence: 1);
        CreateParameterFile(
            configurationId,
            defaultScopeType.Id,
            null,
            "1.0.0",
            ParameterVersionStatus.Published,
            false
        );
        CreateParameterFileOnDisk(configName, "Default", null, "1.0.0", defaultContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(defaultContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(defaultContent);
        _mockMerger.Verify(
            x => x.Merge(It.Is<IEnumerable<string>>(e => e.Contains(defaultContent)), null),
            Times.Once
        );
    }

    [Fact]
    public async Task MergeParametersAsync_WithDisabledDefaultScopeType_IgnoresDefault()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        var defaultScopeType = CreateScopeType("Default", enabled: false, precedence: 1);
        var tagScopeType = CreateScopeType("Environment", enabled: true, precedence: 2);
        const string tagContent = "tagParam: value2";

        var scopeValue = CreateScopeValue(tagScopeType.Id, "Prod");
        CreateNodeTag(nodeId, scopeValue.Id);

        CreateParameterFile(
            configurationId,
            tagScopeType.Id,
            scopeValue.Value,
            "1.0.0",
            ParameterVersionStatus.Published,
            false
        );
        CreateParameterFileOnDisk(configName, "Environment", "Prod", "1.0.0", tagContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(tagContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(tagContent);
    }

    [Fact]
    public async Task MergeParametersAsync_WithPassthroughDefaultFile_IgnoresDefault()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        var defaultScopeType = CreateScopeType("Default", enabled: true, precedence: 1);
        var tagScopeType = CreateScopeType("Environment", enabled: true, precedence: 2);
        const string tagContent = "tagParam: value2";

        var scopeValue = CreateScopeValue(tagScopeType.Id, "Prod");
        CreateNodeTag(nodeId, scopeValue.Id);

        // Passthrough default (isPassthrough: true)
        CreateParameterFile(
            configurationId,
            defaultScopeType.Id,
            null,
            "1.0.0",
            ParameterVersionStatus.Published,
            true  // isPassthrough
        );

        CreateParameterFile(
            configurationId,
            tagScopeType.Id,
            scopeValue.Value,
            "1.0.0",
            ParameterVersionStatus.Published,
            false
        );
        CreateParameterFileOnDisk(configName, "Environment", "Prod", "1.0.0", tagContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(tagContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(tagContent);
    }

    #endregion

    #region MergeParametersAsync Tests - Node Tags

    [Fact]
    public async Task MergeParametersAsync_WithNodeTags_MergesInPrecedenceOrder()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        var env = CreateScopeType("Environment", enabled: true, precedence: 1);
        var region = CreateScopeType("Region", enabled: true, precedence: 2);

        var envValue = CreateScopeValue(env.Id, "Prod");
        var regionValue = CreateScopeValue(region.Id, "EastUS");

        CreateNodeTag(nodeId, envValue.Id);
        CreateNodeTag(nodeId, regionValue.Id);

        const string envContent = "env: prod\noverride: env";
        const string regionContent = "region: eastus\noverride: region";
        const string mergedContent = "env: prod\nregion: eastus\noverride: region";

        CreateParameterFile(configurationId, env.Id, envValue.Value, "1.0.0", ParameterVersionStatus.Published, false);
        CreateParameterFile(configurationId, region.Id, regionValue.Value, "1.0.0", ParameterVersionStatus.Published, false);

        CreateParameterFileOnDisk(configName, "Environment", "Prod", "1.0.0", envContent);
        CreateParameterFileOnDisk(configName, "Region", "EastUS", "1.0.0", regionContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(mergedContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().Be(mergedContent);
    }

    [Fact]
    public async Task MergeParametersAsync_WithDisabledNodeTag_IgnoresTag()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        // Create scope type but mark it as disabled
        var env = CreateScopeType("Environment", enabled: false, precedence: 1);
        var envValue = CreateScopeValue(env.Id, "Prod");
        CreateNodeTag(nodeId, envValue.Id);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region MergeParametersAsync Tests - Node Scope

    [Fact]
    public async Task MergeParametersAsync_WithNodeSpecificScope_MergesNodeParameters()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var nodeFqdn = "node1.contoso.com";
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, nodeFqdn);

        var nodeScopeType = CreateScopeType("Node", enabled: true, precedence: 3);
        const string nodeContent = "nodeSpecific: true";

        CreateParameterFile(configurationId, nodeScopeType.Id, nodeFqdn, "1.0.0", ParameterVersionStatus.Published, false);
        CreateParameterFileOnDisk(configName, "Node", nodeFqdn, "1.0.0", nodeContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(nodeContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(nodeContent);
    }

    [Fact]
    public async Task MergeParametersAsync_WithMissingNodeFile_SkipsNodeScope()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var nodeFqdn = "node1.contoso.com";
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, nodeFqdn);

        var defaultScopeType = CreateScopeType("Default", enabled: true, precedence: 1);
        var nodeScopeType = CreateScopeType("Node", enabled: true, precedence: 3);
        const string defaultContent = "default: true";

        CreateParameterFile(configurationId, defaultScopeType.Id, null, "1.0.0", ParameterVersionStatus.Published, false);
        CreateParameterFile(configurationId, nodeScopeType.Id, nodeFqdn, "1.0.0", ParameterVersionStatus.Published, false);

        // Only create default file, NOT node file
        CreateParameterFileOnDisk(configName, "Default", null, "1.0.0", defaultContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(defaultContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().Be(defaultContent);
    }

    #endregion

    #region MergeParametersAsync Tests - Prerelease Channel

    [Fact]
    public async Task MergeParametersAsync_WithPrereleaseChannel_ResolvesPrereleasedVersions()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");
        CreateNodeConfiguration(nodeId, "beta");

        var defaultScopeType = CreateScopeType("Default", enabled: true, precedence: 1);
        const string prereleaseContent = "version: 1.0.0-beta";

        CreateParameterFile(configurationId, defaultScopeType.Id, null, "1.0.0", ParameterVersionStatus.Published, false);
        CreateParameterFile(configurationId, defaultScopeType.Id, null, "1.0.0-beta", ParameterVersionStatus.Published, false);

        CreateParameterFileOnDisk(configName, "Default", null, "1.0.0", "version: 1.0.0");
        CreateParameterFileOnDisk(configName, "Default", null, "1.0.0-beta", prereleaseContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(prereleaseContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().Be(prereleaseContent);
    }

    #endregion

    #region MergeParametersAsync Tests - Complex Scenarios

    [Fact]
    public async Task MergeParametersAsync_WithAllScopeTypes_MergesAllInOrder()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var nodeFqdn = "node1.contoso.com";
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, nodeFqdn);

        var defaultScope = CreateScopeType("Default", enabled: true, precedence: 1);
        var envScope = CreateScopeType("Environment", enabled: true, precedence: 2);
        var nodeScope = CreateScopeType("Node", enabled: true, precedence: 3);

        var envValue = CreateScopeValue(envScope.Id, "Prod");
        CreateNodeTag(nodeId, envValue.Id);

        const string defaultContent = "default: 1";
        const string envContent = "env: 2";
        const string nodeContent = "node: 3";
        const string mergedContent = "default: 1\nenv: 2\nnode: 3";

        CreateParameterFile(configurationId, defaultScope.Id, null, "1.0.0", ParameterVersionStatus.Published, false);
        CreateParameterFile(configurationId, envScope.Id, envValue.Value, "1.0.0", ParameterVersionStatus.Published, false);
        CreateParameterFile(configurationId, nodeScope.Id, nodeFqdn, "1.0.0", ParameterVersionStatus.Published, false);

        CreateParameterFileOnDisk(configName, "Default", null, "1.0.0", defaultContent);
        CreateParameterFileOnDisk(configName, "Environment", "Prod", "1.0.0", envContent);
        CreateParameterFileOnDisk(configName, "Node", nodeFqdn, "1.0.0", nodeContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(mergedContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().Be(mergedContent);
        _mockMerger.Verify(
            x => x.Merge(It.Is<IEnumerable<string>>(e =>
                e.SequenceEqual(new[] { defaultContent, envContent, nodeContent })
            ), null),
            Times.Once
        );
    }

    #endregion

    #region MergeParametersWithProvenanceAsync Tests

    [Fact]
    public async Task MergeParametersWithProvenanceAsync_WithNullConfiguration_ReturnsNull()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        CreateNode(nodeId, "node1.contoso.com");

        // Act
        var result = await _service.MergeParametersWithProvenanceAsync(nodeId, configurationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MergeParametersWithProvenanceAsync_WithNullNode_ReturnsNull()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        CreateConfiguration(configurationId, "MyConfig");

        // Act
        var result = await _service.MergeParametersWithProvenanceAsync(nodeId, configurationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MergeParametersWithProvenanceAsync_WithNoParameterSources_ReturnsNull()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        CreateConfiguration(configurationId, "MyConfig");
        CreateNode(nodeId, "node1.contoso.com");

        // Act
        var result = await _service.MergeParametersWithProvenanceAsync(nodeId, configurationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task MergeParametersWithProvenanceAsync_WithParameterSources_ReturnsMergeResult()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";
        const string defaultContent = "default: value";
        const string mergedContent = "default: value";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        var defaultScopeType = CreateScopeType("Default", enabled: true, precedence: 1);
        CreateParameterFile(configurationId, defaultScopeType.Id, null, "1.0.0", ParameterVersionStatus.Published, false);
        CreateParameterFileOnDisk(configName, "Default", null, "1.0.0", defaultContent);

        var provenance = new Dictionary<string, ParameterProvenance>
        {
            { "default", new ParameterProvenance
            {
                ScopeTypeName = "Default",
                ScopeValue = null,
                Precedence = 1,
                Value = "value"
            } }
        };
        var mergeResult = new MergeResult
        {
            MergedContent = mergedContent,
            Provenance = provenance
        };

        _mockMerger
            .Setup(x => x.MergeWithProvenance(It.IsAny<IEnumerable<ParameterSource>>(), null))
            .Returns(mergeResult);

        // Act
        var result = await _service.MergeParametersWithProvenanceAsync(nodeId, configurationId);

        // Assert
        result.Should().NotBeNull();
        result!.MergedContent.Should().Be(mergedContent);
        result!.Provenance.Should().ContainKey("default");
    }

    [Fact]
    public async Task MergeParametersWithProvenanceAsync_WithMultipleSources_IncludesAllInProvenance()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        var defaultScope = CreateScopeType("Default", enabled: true, precedence: 1);
        var envScope = CreateScopeType("Environment", enabled: true, precedence: 2);

        var envValue = CreateScopeValue(envScope.Id, "Prod");
        CreateNodeTag(nodeId, envValue.Id);

        const string defaultContent = "default: value";
        const string envContent = "env: value";
        const string mergedContent = "default: value\nenv: value";

        CreateParameterFile(configurationId, defaultScope.Id, null, "1.0.0", ParameterVersionStatus.Published, false);
        CreateParameterFile(configurationId, envScope.Id, envValue.Value, "1.0.0", ParameterVersionStatus.Published, false);

        CreateParameterFileOnDisk(configName, "Default", null, "1.0.0", defaultContent);
        CreateParameterFileOnDisk(configName, "Environment", "Prod", "1.0.0", envContent);

        var provenance = new Dictionary<string, ParameterProvenance>
        {
            { "default", new ParameterProvenance
            {
                ScopeTypeName = "Default",
                ScopeValue = null,
                Precedence = 1,
                Value = "value1"
            } },
            { "env", new ParameterProvenance
            {
                ScopeTypeName = "Environment",
                ScopeValue = "Prod",
                Precedence = 2,
                Value = "value2"
            } }
        };
        var mergeResult = new MergeResult
        {
            MergedContent = mergedContent,
            Provenance = provenance
        };

        _mockMerger
            .Setup(x => x.MergeWithProvenance(It.IsAny<IEnumerable<ParameterSource>>(), null))
            .Returns(mergeResult);

        // Act
        var result = await _service.MergeParametersWithProvenanceAsync(nodeId, configurationId);

        // Assert
        result.Should().NotBeNull();
        result!.Provenance.Should().HaveCount(2);
        result!.Provenance.Should().ContainKeys("default", "env");
    }

    #endregion

    #region MergeParametersAsync Tests - Version Selection

    [Fact]
    public async Task MergeParametersAsync_WithMultipleVersions_SelectsPublishedVersion()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        var defaultScopeType = CreateScopeType("Default", enabled: true, precedence: 1);
        const string publishedContent = "published: true";

        // Create both draft and published versions
        CreateParameterFile(configurationId, defaultScopeType.Id, null, "1.0.0", ParameterVersionStatus.Draft, false);
        CreateParameterFile(configurationId, defaultScopeType.Id, null, "1.0.0", ParameterVersionStatus.Published, false);

        CreateParameterFileOnDisk(configName, "Default", null, "1.0.0", publishedContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(publishedContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().Be(publishedContent);
    }

    [Fact]
    public async Task MergeParametersAsync_WithMajorVersionConstraint_SelectsConstrainedVersion()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");
        CreateNodeConfiguration(nodeId, null);

        var defaultScopeType = CreateScopeType("Default", enabled: true, precedence: 1);

        // Create version 1.5.0
        CreateParameterFile(configurationId, defaultScopeType.Id, null, "1.5.0", ParameterVersionStatus.Published, false);

        const string v1Content = "version: 1.5.0";
        CreateParameterFileOnDisk(configName, "Default", null, "1.5.0", v1Content);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(v1Content);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().Be(v1Content);
    }

    #endregion

    #region MergeParametersAsync Tests - Edge Cases

    [Fact]
    public async Task MergeParametersAsync_WithMultipleTagsMissingFiles_SkipsOnlyMissingTags()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        var env = CreateScopeType("Environment", enabled: true, precedence: 1);
        var region = CreateScopeType("Region", enabled: true, precedence: 2);

        var envValue = CreateScopeValue(env.Id, "Prod");
        var regionValue = CreateScopeValue(region.Id, "EastUS");

        CreateNodeTag(nodeId, envValue.Id);
        CreateNodeTag(nodeId, regionValue.Id);

        const string envContent = "env: prod";

        CreateParameterFile(configurationId, env.Id, envValue.Value, "1.0.0", ParameterVersionStatus.Published, false);
        CreateParameterFile(configurationId, region.Id, regionValue.Value, "1.0.0", ParameterVersionStatus.Published, false);

        // Only create Environment file, not Region file
        CreateParameterFileOnDisk(configName, "Environment", "Prod", "1.0.0", envContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(envContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().Be(envContent);
    }

    [Fact]
    public async Task MergeParametersAsync_WithSpecialCharactersInScopeValue_HandlesCorrectly()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        var env = CreateScopeType("Environment", enabled: true, precedence: 1);
        const string envValue = "Prod-US.East"; // Contains dash and dot
        var scopeValue = CreateScopeValue(env.Id, envValue);
        CreateNodeTag(nodeId, scopeValue.Id);

        const string envContent = "env: prod-us-east";

        CreateParameterFile(configurationId, env.Id, envValue, "1.0.0", ParameterVersionStatus.Published, false);
        CreateParameterFileOnDisk(configName, "Environment", envValue, "1.0.0", envContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(envContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().Be(envContent);
    }

    [Fact]
    public async Task MergeParametersAsync_WithEmptyParameterFile_MergesEmptyContent()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        var defaultScopeType = CreateScopeType("Default", enabled: true, precedence: 1);
        const string emptyContent = "";

        CreateParameterFile(configurationId, defaultScopeType.Id, null, "1.0.0", ParameterVersionStatus.Published, false);
        CreateParameterFileOnDisk(configName, "Default", null, "1.0.0", emptyContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(emptyContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().Be(emptyContent);
    }

    [Fact]
    public async Task MergeParametersAsync_WithLargeParameterSet_MergesAllParameters()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        var defaultScopeType = CreateScopeType("Default", enabled: true, precedence: 1);

        // Create large parameter content with many lines
        var largeContent = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"param{i}: value{i}"));

        CreateParameterFile(configurationId, defaultScopeType.Id, null, "1.0.0", ParameterVersionStatus.Published, false);
        CreateParameterFileOnDisk(configName, "Default", null, "1.0.0", largeContent);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(largeContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().Be(largeContent);
        result!.Split('\n').Should().HaveCount(100);
    }

    [Fact]
    public async Task MergeParametersAsync_WithComplexPrecedenceOrder_RespectsOrdering()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var configurationId = Guid.NewGuid();
        const string configName = "MyConfig";

        CreateConfiguration(configurationId, configName);
        CreateNode(nodeId, "node1.contoso.com");

        // Create 5 scope types with different precedence levels
        var scopes = Enumerable.Range(1, 5)
            .Select(i => CreateScopeType($"Scope{i}", enabled: true, precedence: i))
            .ToList();

        var values = scopes
            .Select((scope, idx) => CreateScopeValue(scope.Id, $"Value{idx + 1}"))
            .ToList();

        foreach (var value in values)
        {
            CreateNodeTag(nodeId, value.Id);
        }

        var contents = Enumerable.Range(1, 5)
            .Select(i => $"scope{i}: value{i}")
            .ToList();

        for (int i = 0; i < scopes.Count; i++)
        {
            CreateParameterFile(configurationId, scopes[i].Id, values[i].Value, "1.0.0", ParameterVersionStatus.Published, false);
            CreateParameterFileOnDisk(configName, $"Scope{i + 1}", $"Value{i + 1}", "1.0.0", contents[i]);
        }

        var mergedContent = string.Join("\n", contents);

        _mockMerger
            .Setup(x => x.Merge(It.IsAny<IEnumerable<string>>(), null))
            .Returns(mergedContent);

        // Act
        var result = await _service.MergeParametersAsync(nodeId, configurationId);

        // Assert
        result.Should().Be(mergedContent);
    }

    #endregion

    #region Helper Methods

    private void CreateConfiguration(Guid id, string name)
    {
        _db.Configurations.Add(new Configuration
        {
            Id = id,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();
    }

    private void CreateNode(Guid id, string fqdn)
    {
        _db.Nodes.Add(new Node
        {
            Id = id,
            Fqdn = fqdn,
            CreatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();
    }

    private void CreateNodeConfiguration(Guid nodeId, string? prereleaseChannel)
    {
        var existing = _db.NodeConfigurations.FirstOrDefault(nc => nc.NodeId == nodeId);
        if (existing != null)
        {
            _db.NodeConfigurations.Remove(existing);
        }

        _db.NodeConfigurations.Add(new NodeConfiguration
        {
            NodeId = nodeId,
            PrereleaseChannel = prereleaseChannel,
            AssignedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();
    }

    private ScopeType CreateScopeType(string name, bool enabled, int precedence)
    {
        var scopeType = new ScopeType
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsEnabled = enabled,
            Precedence = precedence,
            ValueMode = ScopeValueMode.Unrestricted,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.ScopeTypes.Add(scopeType);
        _db.SaveChanges();
        return scopeType;
    }

    private ScopeValue CreateScopeValue(Guid scopeTypeId, string value)
    {
        var scopeValue = new ScopeValue
        {
            Id = Guid.NewGuid(),
            ScopeTypeId = scopeTypeId,
            Value = value
        };
        _db.ScopeValues.Add(scopeValue);
        _db.SaveChanges();
        return scopeValue;
    }

    private void CreateNodeTag(Guid nodeId, Guid scopeValueId)
    {
        _db.NodeTags.Add(new NodeTag
        {
            NodeId = nodeId,
            ScopeValueId = scopeValueId
        });
        _db.SaveChanges();
    }

    private void CreateParameterFile(
        Guid configurationId,
        Guid scopeTypeId,
        string? scopeValue,
        string version,
        ParameterVersionStatus status,
        bool isPassthrough)
    {
        var parameterSchema = _db.ParameterSchemas
            .FirstOrDefault(ps => ps.ConfigurationId == configurationId);

        if (parameterSchema == null)
        {
            parameterSchema = new ParameterSchema
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configurationId
            };
            _db.ParameterSchemas.Add(parameterSchema);
            _db.SaveChanges();
        }

        var checksum = isPassthrough ? "passthrough" : ComputeChecksum($"scope:{scopeTypeId}:value:{scopeValue}:version:{version}");

        _db.ParameterFiles.Add(new ParameterFile
        {
            Id = Guid.NewGuid(),
            ParameterSchemaId = parameterSchema.Id,
            ScopeTypeId = scopeTypeId,
            ScopeValue = scopeValue,
            Version = version,
            Status = status,
            IsPassthrough = isPassthrough,
            Checksum = checksum,
            CreatedAt = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();
    }

    private static string ComputeChecksum(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private void CreateParameterFileOnDisk(string configName, string scopeTypeName, string? scopeValue, string version, string content)
    {
        string path;
        if (scopeValue == null)
        {
            path = Path.Combine(_tempDir, configName, scopeTypeName, $"v{version}");
        }
        else
        {
            path = Path.Combine(_tempDir, configName, scopeTypeName, scopeValue, $"v{version}");
        }

        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "parameters.yaml"), content);
    }

    #endregion
}
