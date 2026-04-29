// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

#pragma warning disable xUnit1051

using System.Text.Json;

using AwesomeAssertions;

using Json.Schema;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class ParameterSchemaServiceTests : IDisposable
{
    private readonly ServerDbContext _dbContext;
    private readonly Mock<IParameterSchemaBuilder> _schemaBuilderMock;
    private readonly ParameterSchemaService _service;

    public ParameterSchemaServiceTests()
    {
        _dbContext = new ServerDbContext(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options);

        _schemaBuilderMock = new Mock<IParameterSchemaBuilder>();

        _service = new ParameterSchemaService(_dbContext, _schemaBuilderMock.Object, NullLogger<ParameterSchemaService>.Instance);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region ParseParameterBlockAsync Tests

    [Fact]
    public async Task ParseParameterBlockAsync_WithNullInput_ReturnsNull()
    {
        var result = await _service.ParseParameterBlockAsync(null!);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ParseParameterBlockAsync_WithEmptyString_ReturnsNull()
    {
        var result = await _service.ParseParameterBlockAsync("");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ParseParameterBlockAsync_WithWhitespaceOnly_ReturnsNull()
    {
        var result = await _service.ParseParameterBlockAsync("   \n\t  ");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ParseParameterBlockAsync_WithValidYamlWithParameters_ReturnsJsonParameters()
    {
        var yaml = "parameters:\n  env: prod\n  count: 3";

        var result = await _service.ParseParameterBlockAsync(yaml);

        result.Should().NotBeNull();

        using var doc = JsonDocument.Parse(result!);
        var root = doc.RootElement;

        root.TryGetProperty("env", out _).Should().BeTrue();
        root.TryGetProperty("count", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ParseParameterBlockAsync_WithYamlWithoutParameters_ReturnsNull()
    {
        var yaml = @"
description: 'No parameters here'
version: '1.0.0'
";

        var result = await _service.ParseParameterBlockAsync(yaml);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ParseParameterBlockAsync_WithInvalidYaml_ReturnsNull()
    {
        var invalidYaml = @"
this is: not: valid: yaml: syntax:::
  - broken
   - indentation
";

        var result = await _service.ParseParameterBlockAsync(invalidYaml);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ParseParameterBlockAsync_WithComplexParameterStructures_ReturnsValidJson()
    {
        var yaml = "parameters:\n  str: test\n  num: 42\n  flag: true";

        var result = await _service.ParseParameterBlockAsync(yaml);

        result.Should().NotBeNull();

        using var doc = JsonDocument.Parse(result!);
        var root = doc.RootElement;

        root.TryGetProperty("str", out _).Should().BeTrue();
        root.TryGetProperty("num", out _).Should().BeTrue();
        root.TryGetProperty("flag", out _).Should().BeTrue();
    }

    #endregion

    #region DetectSchemaChanges Tests

    [Fact]
    public void DetectSchemaChanges_WithBothSchemasNull_ReturnsIdentical()
    {
        var result = _service.DetectSchemaChanges(null, null);

        result.IsIdentical.Should().BeTrue();
        result.HasBreakingChanges.Should().BeFalse();
        result.HasAdditiveChanges.Should().BeFalse();
        result.RemovedParameters.Should().BeEmpty();
        result.AddedParameters.Should().BeEmpty();
    }

    [Fact]
    public void DetectSchemaChanges_WithBothSchemasEmpty_ReturnsIdentical()
    {
        var result = _service.DetectSchemaChanges("", "  ");

        result.IsIdentical.Should().BeTrue();
        result.HasBreakingChanges.Should().BeFalse();
        result.HasAdditiveChanges.Should().BeFalse();
    }

    [Fact]
    public void DetectSchemaChanges_WithOnlyOldSchemaNull_ReturnsAdditive()
    {
        var newSchema = @"{ ""param1"": {}, ""param2"": {} }";

        var result = _service.DetectSchemaChanges(null, newSchema);

        result.HasAdditiveChanges.Should().BeTrue();
        result.HasBreakingChanges.Should().BeFalse();
        result.IsIdentical.Should().BeFalse();
        result.AddedParameters.Should().HaveCount(2).And.Contain(["param1", "param2"]);
    }

    [Fact]
    public void DetectSchemaChanges_WithOnlyNewSchemaNull_ReturnsBreaking()
    {
        var oldSchema = @"{ ""param1"": {}, ""param2"": {} }";

        var result = _service.DetectSchemaChanges(oldSchema, null);

        result.HasBreakingChanges.Should().BeTrue();
        result.HasAdditiveChanges.Should().BeFalse();
        result.IsIdentical.Should().BeFalse();
        result.RemovedParameters.Should().HaveCount(2).And.Contain(["param1", "param2"]);
    }

    [Fact]
    public void DetectSchemaChanges_WithParametersRemoved_ReturnsBreaking()
    {
        var oldSchema = @"{ ""param1"": {}, ""param2"": {}, ""param3"": {} }";
        var newSchema = @"{ ""param1"": {}, ""param2"": {} }";

        var result = _service.DetectSchemaChanges(oldSchema, newSchema);

        result.HasBreakingChanges.Should().BeTrue();
        result.HasAdditiveChanges.Should().BeFalse();
        result.IsIdentical.Should().BeFalse();
        result.RemovedParameters.Should().HaveCount(1).And.Contain("param3");
    }

    [Fact]
    public void DetectSchemaChanges_WithParametersAdded_ReturnsAdditive()
    {
        var oldSchema = @"{ ""param1"": {}, ""param2"": {} }";
        var newSchema = @"{ ""param1"": {}, ""param2"": {}, ""param3"": {}, ""param4"": {} }";

        var result = _service.DetectSchemaChanges(oldSchema, newSchema);

        result.HasAdditiveChanges.Should().BeTrue();
        result.HasBreakingChanges.Should().BeFalse();
        result.IsIdentical.Should().BeFalse();
        result.AddedParameters.Should().HaveCount(2).And.Contain(["param3", "param4"]);
    }

    [Fact]
    public void DetectSchemaChanges_WithBothAddedAndRemoved_ReturnsBoth()
    {
        var oldSchema = @"{ ""param1"": {}, ""param2"": {}, ""oldParam"": {} }";
        var newSchema = @"{ ""param1"": {}, ""param2"": {}, ""newParam"": {} }";

        var result = _service.DetectSchemaChanges(oldSchema, newSchema);

        result.HasBreakingChanges.Should().BeTrue();
        result.HasAdditiveChanges.Should().BeTrue();
        result.IsIdentical.Should().BeFalse();
        result.RemovedParameters.Should().Contain("oldParam");
        result.AddedParameters.Should().Contain("newParam");
    }

    [Fact]
    public void DetectSchemaChanges_WithIdenticalSchemas_ReturnsIdentical()
    {
        var schema = @"{ ""param1"": {}, ""param2"": {}, ""param3"": {} }";

        var result = _service.DetectSchemaChanges(schema, schema);

        result.IsIdentical.Should().BeTrue();
        result.HasBreakingChanges.Should().BeFalse();
        result.HasAdditiveChanges.Should().BeFalse();
        result.RemovedParameters.Should().BeEmpty();
        result.AddedParameters.Should().BeEmpty();
    }

    [Fact]
    public void DetectSchemaChanges_WithCaseInsensitiveComparison_TreatsAsIdentical()
    {
        var oldSchema = @"{ ""Param1"": {}, ""PARAM2"": {} }";
        var newSchema = @"{ ""param1"": {}, ""param2"": {} }";

        var result = _service.DetectSchemaChanges(oldSchema, newSchema);

        result.IsIdentical.Should().BeTrue();
        result.HasBreakingChanges.Should().BeFalse();
        result.HasAdditiveChanges.Should().BeFalse();
    }

    [Fact]
    public void DetectSchemaChanges_WithSpecialCharactersInParameterNames_DetectsChanges()
    {
        var oldSchema = @"{ ""param-1"": {}, ""param_2"": {} }";
        var newSchema = @"{ ""param-1"": {}, ""param_2"": {}, ""param.3"": {} }";

        var result = _service.DetectSchemaChanges(oldSchema, newSchema);

        result.HasAdditiveChanges.Should().BeTrue();
        result.AddedParameters.Should().Contain("param.3");
    }

    #endregion

    #region ValidateSemVerCompliance Tests

    [Fact]
    public void ValidateSemVerCompliance_WithInvalidNewVersion_ReturnsFalse()
    {
        var result = _service.ValidateSemVerCompliance("not-a-version", null, new SchemaChanges(false, false, true, [], [], []));

        result.IsValid.Should().BeFalse();
        result.Violations.Should().NotBeEmpty();
        result.Violations[0].Should().Contain("not a valid semantic version");
    }

    [Fact]
    public void ValidateSemVerCompliance_WithFirstVersion_ReturnsTrue()
    {
        var result = _service.ValidateSemVerCompliance("1.0.0", null, new SchemaChanges(false, false, true, [], [], []));

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSemVerCompliance_WithInvalidOldVersion_ReturnsFalse()
    {
        var result = _service.ValidateSemVerCompliance("2.0.0", "bad-version", new SchemaChanges(false, false, false, [], [], []));

        result.IsValid.Should().BeFalse();
        result.Violations.Should().NotBeEmpty();
        result.Violations[0].Should().Contain("Previous version");
    }

    [Fact]
    public void ValidateSemVerCompliance_WithBreakingChangesAndMajorBump_ReturnsTrue()
    {
        var changes = new SchemaChanges(true, false, false, ["param1"], [], []);

        var result = _service.ValidateSemVerCompliance("2.0.0", "1.5.3", changes);

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSemVerCompliance_WithBreakingChangesWithoutMajorBump_ReturnsFalse()
    {
        var changes = new SchemaChanges(true, false, false, ["param1"], [], []);

        var result = _service.ValidateSemVerCompliance("1.5.4", "1.5.3", changes);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().NotBeEmpty();
        result.ExpectedVersionComponent.Should().Be("MAJOR");
        result.Violations[0].Should().Contain("Breaking changes");
    }

    [Fact]
    public void ValidateSemVerCompliance_WithAdditiveChangesAndMinorBump_ReturnsTrue()
    {
        var changes = new SchemaChanges(false, true, false, [], [], ["param2"]);

        var result = _service.ValidateSemVerCompliance("1.1.0", "1.0.5", changes);

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSemVerCompliance_WithAdditiveChangesAndMajorBump_ReturnsTrue()
    {
        var changes = new SchemaChanges(false, true, false, [], [], ["param2"]);

        var result = _service.ValidateSemVerCompliance("2.0.0", "1.9.9", changes);

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSemVerCompliance_WithAdditiveChangesWithoutVersionBump_ReturnsFalse()
    {
        var changes = new SchemaChanges(false, true, false, [], [], ["param2"]);

        var result = _service.ValidateSemVerCompliance("1.0.5", "1.0.4", changes);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().NotBeEmpty();
        result.ExpectedVersionComponent.Should().Be("MINOR");
        result.Violations[0].Should().Contain("New parameters added");
    }

    [Fact]
    public void ValidateSemVerCompliance_WithIdenticalSchemaAndPatchBump_ReturnsTrue()
    {
        var changes = new SchemaChanges(false, false, true, [], [], []);

        var result = _service.ValidateSemVerCompliance("1.0.1", "1.0.0", changes);

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSemVerCompliance_WithIdenticalSchemaAndMinorBump_ReturnsTrue()
    {
        var changes = new SchemaChanges(false, false, true, [], [], []);

        var result = _service.ValidateSemVerCompliance("1.1.0", "1.0.0", changes);

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSemVerCompliance_WithIdenticalSchemaAndNoBump_ReturnsFalse()
    {
        var changes = new SchemaChanges(false, false, true, [], [], []);

        var result = _service.ValidateSemVerCompliance("1.0.0", "1.0.0", changes);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().NotBeEmpty();
        result.ExpectedVersionComponent.Should().Be("PATCH");
        result.Violations[0].Should().Contain("No parameter schema changes");
    }

    [Fact]
    public void ValidateSemVerCompliance_WithPrereleaseVersion_ValidatesCorrectly()
    {
        var changes = new SchemaChanges(false, true, false, [], [], ["param2"]);

        var result = _service.ValidateSemVerCompliance("1.1.0-rc1", "1.0.0", changes);

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSemVerCompliance_WithBuildMetadata_ValidatesCorrectly()
    {
        var changes = new SchemaChanges(false, false, true, [], [], []);

        var result = _service.ValidateSemVerCompliance("1.0.1+build.123", "1.0.0+build.122", changes);

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSemVerCompliance_WithMultipleBoundaryConditions_VerifiesCorrectly()
    {
        var changes = new SchemaChanges(false, false, true, [], [], []);

        // 0.1.0 to 0.2.0 is valid MINOR bump in pre-1.0.0 versions
        var result = _service.ValidateSemVerCompliance("0.2.0", "0.1.0", changes);

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    #endregion

    #region GenerateAndStoreSchemaAsync Tests

    [Fact]
    public async Task GenerateAndStoreSchemaAsync_WithValidParameters_CreatesNewSchema()
    {
        var configId = Guid.NewGuid();
        var parametersJson = @"{ ""param1"": { ""type"": ""String"", ""description"": ""First parameter"" }, ""param2"": { ""type"": ""Int32"" } }";
        var mockSchema = new JsonSchemaBuilder().Build();

        _schemaBuilderMock
            .Setup(x => x.BuildJsonSchema(It.IsAny<Dictionary<string, ParameterDefinition>>()))
            .Returns(mockSchema);

        _schemaBuilderMock
            .Setup(x => x.SerializeSchema(mockSchema))
            .Returns("{\"$schema\": \"http://json-schema.org/draft-07/schema#\"}");

        var result = await _service.GenerateAndStoreSchemaAsync(configId, parametersJson, "1.0.0");

        result.Should().NotBeNull();
        result.ConfigurationId.Should().Be(configId);
        result.SchemaVersion.Should().Be("1.0.0");
        result.GeneratedJsonSchema.Should().Contain("json-schema.org");

        var storedSchema = await _dbContext.ParameterSchemas.FindAsync(result.Id);
        storedSchema.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateAndStoreSchemaAsync_WithExistingSchema_UpdatesExisting()
    {
        var configId = Guid.NewGuid();
        var existingSchema = new ParameterSchema
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configId,
            GeneratedJsonSchema = "{\"old\": \"schema\"}",
            SchemaVersion = "1.0.0",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _dbContext.ParameterSchemas.Add(existingSchema);
        await _dbContext.SaveChangesAsync();

        var parametersJson = @"{ ""param1"": { ""type"": ""String"" } }";
        var mockSchema = new JsonSchemaBuilder().Build();

        _schemaBuilderMock
            .Setup(x => x.BuildJsonSchema(It.IsAny<Dictionary<string, ParameterDefinition>>()))
            .Returns(mockSchema);

        _schemaBuilderMock
            .Setup(x => x.SerializeSchema(mockSchema))
            .Returns("{\"updated\": \"schema\"}");

        var result = await _service.GenerateAndStoreSchemaAsync(configId, parametersJson, "2.0.0");

        result.Id.Should().Be(existingSchema.Id);
        result.SchemaVersion.Should().Be("2.0.0");
        result.GeneratedJsonSchema.Should().Contain("updated");

        var stored = await _dbContext.ParameterSchemas.FindAsync(existingSchema.Id);
        stored!.SchemaVersion.Should().Be("2.0.0");
    }

    [Fact]
    public async Task GenerateAndStoreSchemaAsync_WithInvalidSemanticVersion_ThrowsException()
    {
        var configId = Guid.NewGuid();
        var parametersJson = @"{ ""param1"": { ""Type"": ""String"" } }";

        var action = () => _service.GenerateAndStoreSchemaAsync(configId, parametersJson, "not-a-version");

        await action.Should().ThrowAsync<ArgumentException>().Where(x => x.ParamName == "version");
    }

    [Fact]
    public async Task GenerateAndStoreSchemaAsync_WithInvalidParametersJson_ThrowsException()
    {
        var configId = Guid.NewGuid();
        var invalidJson = "{ invalid json }";

        var action = () => _service.GenerateAndStoreSchemaAsync(configId, invalidJson, "1.0.0");

        await action.Should().ThrowAsync<ArgumentException>().Where(x => x.ParamName == "parametersJson");
    }

    [Fact]
    public async Task GenerateAndStoreSchemaAsync_WithComplexParameterTypes_HandlesCorrectly()
    {
        var configId = Guid.NewGuid();
        var parametersJson = @"{
            ""stringParam"": { ""type"": ""String"", ""description"": ""Test string"" },
            ""intParam"": { ""type"": ""Int32"", ""defaultValue"": 42 },
            ""boolParam"": { ""type"": ""Boolean"" },
            ""choiceParam"": { ""type"": ""String"", ""allowedValues"": [""opt1"", ""opt2""] }
        }";

        var mockSchema = new JsonSchemaBuilder().Build();

        _schemaBuilderMock
            .Setup(x => x.BuildJsonSchema(It.IsAny<Dictionary<string, ParameterDefinition>>()))
            .Returns(mockSchema);

        _schemaBuilderMock
            .Setup(x => x.SerializeSchema(mockSchema))
            .Returns("{\"type\": \"object\", \"properties\": {}}");

        var result = await _service.GenerateAndStoreSchemaAsync(configId, parametersJson, "1.0.0");

        result.Should().NotBeNull();
        result.ConfigurationId.Should().Be(configId);
    }

    #endregion

    #region FindOrCreateSchemaAsync Tests

    [Fact]
    public async Task FindOrCreateSchemaAsync_WithNullSchemaDefinition_ReturnsNull()
    {
        var result = await _service.FindOrCreateSchemaAsync(Guid.NewGuid(), null);
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindOrCreateSchemaAsync_WithEmptySchemaDefinition_ReturnsNull()
    {
        var result = await _service.FindOrCreateSchemaAsync(Guid.NewGuid(), "");
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindOrCreateSchemaAsync_WithWhitespaceSchemaDefinition_ReturnsNull()
    {
        var result = await _service.FindOrCreateSchemaAsync(Guid.NewGuid(), "   \n\t  ");
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindOrCreateSchemaAsync_WithExistingSchema_ReturnsExisting()
    {
        var configId = Guid.NewGuid();
        var existingSchema = new ParameterSchema
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configId,
            GeneratedJsonSchema = "{\"existing\": \"schema\"}",
            SchemaVersion = "1.0.0",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.ParameterSchemas.Add(existingSchema);
        await _dbContext.SaveChangesAsync();

        var schemaDefinition = @"{ ""param1"": {} }";
        var result = await _service.FindOrCreateSchemaAsync(configId, schemaDefinition);

        result.Should().NotBeNull();
        result!.Id.Should().Be(existingSchema.Id);
        result.ConfigurationId.Should().Be(configId);
    }

    [Fact]
    public async Task FindOrCreateSchemaAsync_WithNonExistentConfiguration_ReturnsNull()
    {
        var configId = Guid.NewGuid();
        var schemaDefinition = @"{ ""param1"": {} }";

        var result = await _service.FindOrCreateSchemaAsync(configId, schemaDefinition);

        result.Should().BeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Integration_ParseParametersAndGenerateSchema_WorksTogether()
    {
        var yaml = "parameters:\n  env: prod\n  count: 3";

        var parametersJson = await _service.ParseParameterBlockAsync(yaml);
        parametersJson.Should().NotBeNull();

        var configId = Guid.NewGuid();
        var mockSchema = new JsonSchemaBuilder().Build();

        _schemaBuilderMock
            .Setup(x => x.BuildJsonSchema(It.IsAny<Dictionary<string, ParameterDefinition>>()))
            .Returns(mockSchema);

        _schemaBuilderMock
            .Setup(x => x.SerializeSchema(mockSchema))
            .Returns("{\"$schema\": \"http://json-schema.org/draft-07/schema#\"}");

        var schema = await _service.GenerateAndStoreSchemaAsync(configId, parametersJson!, "1.0.0");

        schema.Should().NotBeNull();
        schema.ConfigurationId.Should().Be(configId);
    }

    [Fact]
    public void Integration_DetectChangesAndValidateSemVer_WorkTogether()
    {
        var oldSchema = @"{ ""param1"": {}, ""param2"": {} }";
        var newSchema = @"{ ""param1"": {}, ""param2"": {}, ""param3"": {} }";

        var changes = _service.DetectSchemaChanges(oldSchema, newSchema);
        changes.HasAdditiveChanges.Should().BeTrue();

        var result = _service.ValidateSemVerCompliance("1.1.0", "1.0.0", changes);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Integration_ComplexWorkflow_AllMethodsWorkCorrectly()
    {
        // Detect changes
        var oldSchema = @"{ ""Environment"": {}, ""NodeCount"": {}, ""OldParam"": {} }";
        var newSchema = @"{ ""Environment"": {}, ""NodeCount"": {}, ""NewParam"": {}, ""AnotherNew"": {} }";

        var changes = _service.DetectSchemaChanges(oldSchema, newSchema);

        changes.HasBreakingChanges.Should().BeTrue();
        changes.HasAdditiveChanges.Should().BeTrue();
        changes.RemovedParameters.Should().Contain("OldParam");
        changes.AddedParameters.Should().HaveCount(2);

        // Validate version bumps for various scenarios
        var majorBumpResult = _service.ValidateSemVerCompliance("2.0.0", "1.0.0", changes);
        majorBumpResult.IsValid.Should().BeTrue();

        var invalidBumpResult = _service.ValidateSemVerCompliance("1.0.1", "1.0.0", changes);
        invalidBumpResult.IsValid.Should().BeFalse();
        invalidBumpResult.ExpectedVersionComponent.Should().Be("MAJOR");
    }

    #endregion
}
