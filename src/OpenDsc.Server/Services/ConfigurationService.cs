// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NuGet.Versioning;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Contracts;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenDsc.Server.Services;

public interface IConfigurationService
{
    Task CreateConfigurationAsync(string name, string? description, string entryPoint, string version, bool isDraft, bool useServerManagedParameters, IReadOnlyList<IBrowserFile> files);
    Task CreateVersionAsync(string name, string version, bool isDraft, IReadOnlyList<IBrowserFile> files, string? entryPoint = null);
    Task CreateVersionFromExistingAsync(string name, string sourceVersion, string newVersion, bool isDraft);
    Task AddFilesToVersionAsync(string name, string version, IReadOnlyList<IBrowserFile> files);
    Task<PublishResult> PublishVersionAsync(string name, string version);
    Task DeleteConfigurationAsync(string name);
    Task DeleteVersionAsync(string name, string version);
    Task DeleteFileAsync(string name, string version, string filePath);
    Task ChangeVersionEntryPointAsync(string name, string version, string entryPoint);
    Task<Stream?> DownloadFileAsync(string name, string version, string filePath);
    Task SaveFileAsync(string name, string version, string filePath, string content);
    Task<List<ConfigurationSummaryDto>> GetConfigurationsAsync();
    Task<ConfigurationDetailsDto?> GetConfigurationAsync(string name);
    Task<List<ConfigurationVersionDto>?> GetVersionsAsync(string name);
    Task<bool> IsConfigurationAssignedAsync(string configName);
    Task<VersionUsageInfo> IsVersionInUseAsync(string configName, string version);
    Task<ConfigurationSettingsDto?> GetConfigurationSettingsAsync(string configName);
    Task<ConfigurationSettingsDto> UpdateConfigurationSettingsAsync(string configName, bool? requireSemVer, ParameterValidationMode? paramValidation);
    Task DeleteConfigurationSettingsAsync(string configName);
    Task<ConfigurationRetentionDto?> GetRetentionSettingsAsync(string configName);
    Task SaveRetentionSettingsAsync(string configName, bool? enabled, int? keepVersions, int? keepDays, bool? keepReleaseVersions);
    Task ResetRetentionSettingsAsync(string configName);
    Task<ConfigurationDetailsDto> UpdateConfigurationAsync(string name, string? description, bool? useServerManagedParameters);
    Task<List<PermissionEntryDto>?> GetPermissionsAsync(string configName);
    Task GrantPermissionAsync(string configName, Guid principalId, string principalType, string level);
    Task RevokePermissionAsync(string configName, Guid principalId, string principalType);
    Task<Guid?> GetParameterSchemaIdAsync(string configName);
    Task<List<string>> GetConfigurationVersionListAsync(string configName);
}

public sealed class ConfigurationSummaryDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool UseServerManagedParameters { get; init; }
    public required int VersionCount { get; init; }
    public string? LatestVersion { get; init; }
    public required bool HasPublishedVersion { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class ConfigurationDetailsDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool UseServerManagedParameters { get; init; }
    public string? LatestVersion { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class ConfigurationFileDto
{
    public required string RelativePath { get; init; }
    public string? ContentType { get; init; }
}

public sealed class ConfigurationVersionDto
{
    public required string Version { get; init; }
    public required string EntryPoint { get; init; }
    public ConfigurationVersionStatus Status { get; set; }
    public string? PrereleaseChannel { get; init; }
    public required int FileCount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public List<ConfigurationFileDto> Files { get; init; } = [];
}

public sealed class CreateConfigurationDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? EntryPoint { get; init; }
    public bool UseServerManagedParameters { get; init; } = true;
    public string? Version { get; init; }
}

public sealed class UpdateConfigurationDto
{
    public string? Description { get; init; }
    public bool? UseServerManagedParameters { get; init; }
}

public sealed class CreateConfigurationVersionDto
{
    public required string Version { get; init; }
    public string? EntryPoint { get; init; }
    public string? PrereleaseChannel { get; init; }
}

public sealed class ConfigurationSettingsDto
{
    public required bool IsOverridden { get; init; }
    public required bool RequireSemVer { get; init; }
    public required ParameterValidationMode ParameterValidationMode { get; init; }
}

public sealed class ConfigurationRetentionDto
{
    public bool IsOverridden { get; init; }
    public bool? Enabled { get; init; }
    public int? KeepVersions { get; init; }
    public int? KeepDays { get; init; }
    public bool? KeepReleaseVersions { get; init; }
}

public sealed class VersionUsageInfo
{
    public bool IsInUse { get; init; }
    public IReadOnlyList<string> Details { get; init; } = [];
}

public sealed class PublishResult
{
    public bool Success { get; init; }
    public CompatibilityReport? CompatibilityReport { get; init; }
    public string? ErrorMessage { get; init; }
    public ConfigurationVersionStatus? UpdatedStatus { get; init; }
    public string? UpdatedVersion { get; init; }
    public string? UpdatedPrereleaseChannel { get; init; }
}

public sealed partial class ConfigurationService : IConfigurationService
{
    private readonly ServerDbContext _db;
    private readonly IOptions<ServerConfig> _serverConfig;
    private readonly IResourceAuthorizationService _authService;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IParameterSchemaBuilder _schemaBuilder;
    private readonly IParameterCompatibilityService _compatibilityService;
    private readonly IParameterSchemaService _parameterSchemaService;
    private readonly IParameterValidator _parameterValidator;

    public ConfigurationService(
        ServerDbContext db,
        IOptions<ServerConfig> serverConfig,
        IResourceAuthorizationService authService,
        IUserContextService userContext,
        ILogger<ConfigurationService> logger,
        IParameterSchemaBuilder schemaBuilder,
        IParameterCompatibilityService compatibilityService,
        IParameterSchemaService parameterSchemaService,
        IParameterValidator parameterValidator)
    {
        _db = db;
        _serverConfig = serverConfig;
        _authService = authService;
        _userContext = userContext;
        _logger = logger;
        _schemaBuilder = schemaBuilder;
        _compatibilityService = compatibilityService;
        _parameterSchemaService = parameterSchemaService;
        _parameterValidator = parameterValidator;
    }

    public async Task CreateConfigurationAsync(
        string name,
        string? description,
        string entryPoint,
        string version,
        bool isDraft,
        bool useServerManagedParameters,
        IReadOnlyList<IBrowserFile> files)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            LogConfigurationNameRequired();
            throw new ArgumentException("Configuration name is required.", nameof(name));
        }

        if (files.Count == 0)
        {
            LogFilesRequired();
            throw new ArgumentException("At least one file is required.", nameof(files));
        }

        if (await _db.Configurations.AnyAsync(c => c.Name == name))
        {
            LogConfigurationAlreadyExists(name);
            throw new InvalidOperationException($"Configuration '{name}' already exists.");
        }

        if (!files.Any(f => f.Name == entryPoint))
        {
            LogEntryPointNotFoundInUploadedFiles(entryPoint);
            throw new ArgumentException($"Entry point '{entryPoint}' was not found in the uploaded files.", nameof(entryPoint));
        }

        var configuration = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            UseServerManagedParameters = useServerManagedParameters,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Configurations.Add(configuration);

        var configVersion = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configuration.Id,
            Version = version,
            EntryPoint = entryPoint,
            Status = isDraft ? ConfigurationVersionStatus.Draft : ConfigurationVersionStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ConfigurationVersions.Add(configVersion);

        var dataDir = _serverConfig.Value.ConfigurationsDirectory;
        var versionDir = Path.Combine(dataDir, name, $"v{version}");
        Directory.CreateDirectory(versionDir);

        foreach (var file in files)
        {
            var relativePath = file.Name;
            var filePath = Path.Combine(versionDir, relativePath);
            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            await using (var stream = File.Create(filePath))
            {
                await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(stream);
            }

            var checksum = await ComputeFileChecksumAsync(filePath);
            var contentType = GetContentType(file);

            var configFile = new ConfigurationFile
            {
                Id = Guid.NewGuid(),
                VersionId = configVersion.Id,
                RelativePath = relativePath,
                ContentType = contentType,
                Checksum = checksum,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.ConfigurationFiles.Add(configFile);
        }

        await _db.SaveChangesAsync();

        // Extract and generate parameter schema from entry point file
        var entryPointPath = Path.Combine(versionDir, configVersion.EntryPoint);
        if (File.Exists(entryPointPath))
        {
            var entryPointContent = await File.ReadAllTextAsync(entryPointPath);
            var parametersBlock = ExtractParametersFromYaml(entryPointContent);

            if (parametersBlock != null && parametersBlock.Count > 0)
            {
                var paramDefinitions = ConvertToParameterDefinitions(parametersBlock);
                var jsonSchemaObj = _schemaBuilder.BuildJsonSchema(paramDefinitions);
                var jsonSchema = _schemaBuilder.SerializeSchema(jsonSchemaObj);

                var paramSchema = new ParameterSchema
                {
                    Id = Guid.NewGuid(),
                    ConfigurationId = configuration.Id,
                    SchemaVersion = version,
                    GeneratedJsonSchema = jsonSchema,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _db.ParameterSchemas.Add(paramSchema);
                configVersion.ParameterSchemaId = paramSchema.Id;
                await _db.SaveChangesAsync();
            }
        }

        var userId = _userContext.GetCurrentUserId();
        if (userId is Guid creatorId &&
            !await _authService.HasGlobalPermissionAsync(creatorId, ConfigurationPermissions.AdminOverride))
        {
            await _authService.GrantConfigurationPermissionAsync(
                configuration.Id,
                creatorId,
                PrincipalType.User,
                ResourcePermission.Manage,
                creatorId);
        }

    }

    public async Task CreateVersionAsync(
        string name,
        string version,
        bool isDraft,
        IReadOnlyList<IBrowserFile> files,
        string? entryPoint = null)
    {
        var configuration = await _db.Configurations
            .FirstOrDefaultAsync(c => c.Name == name)
            ?? throw new KeyNotFoundException($"Configuration '{name}' not found.");

        if (await _db.ConfigurationVersions.AnyAsync(v => v.ConfigurationId == configuration.Id && v.Version == version))
        {
            LogVersionAlreadyExists(version, name);
            throw new InvalidOperationException($"Version '{version}' already exists for configuration '{name}'.");
        }

        // Resolve entry point: prefer explicit value, fall back to latest version's entry point
        var latestVersionEntryPoint = (await _db.ConfigurationVersions
            .Where(v => v.ConfigurationId == configuration.Id)
            .Select(v => new { v.EntryPoint, v.CreatedAt })
            .ToListAsync())
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => v.EntryPoint)
            .FirstOrDefault();

        var resolvedEntryPoint = entryPoint ?? latestVersionEntryPoint ?? "main.dsc.yaml";

        var configVersion = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configuration.Id,
            Version = version,
            EntryPoint = resolvedEntryPoint,
            Status = isDraft ? ConfigurationVersionStatus.Draft : ConfigurationVersionStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ConfigurationVersions.Add(configVersion);

        var dataDir = _serverConfig.Value.ConfigurationsDirectory;
        var versionDir = Path.Combine(dataDir, name, $"v{version}");
        Directory.CreateDirectory(versionDir);

        foreach (var file in files)
        {
            var relativePath = file.Name;
            var filePath = Path.Combine(versionDir, relativePath);
            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            await using (var stream = File.Create(filePath))
            {
                await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(stream);
            }

            var checksum = await ComputeFileChecksumAsync(filePath);
            var contentType = GetContentType(file);

            var configFile = new ConfigurationFile
            {
                Id = Guid.NewGuid(),
                VersionId = configVersion.Id,
                RelativePath = relativePath,
                ContentType = contentType,
                Checksum = checksum,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.ConfigurationFiles.Add(configFile);
        }

        configuration.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        // Extract and generate parameter schema from entry point file
        var entryPointPath = Path.Combine(versionDir, configVersion.EntryPoint);
        if (File.Exists(entryPointPath))
        {
            var entryPointContent = await File.ReadAllTextAsync(entryPointPath);
            var parametersBlock = ExtractParametersFromYaml(entryPointContent);

            if (parametersBlock != null && parametersBlock.Count > 0)
            {
                var paramDefinitions = ConvertToParameterDefinitions(parametersBlock);
                var jsonSchemaObj = _schemaBuilder.BuildJsonSchema(paramDefinitions);
                var jsonSchema = _schemaBuilder.SerializeSchema(jsonSchemaObj);

                var existingSchema = await _db.ParameterSchemas
                    .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id && ps.SchemaVersion == version);

                if (existingSchema == null)
                {
                    var paramSchema = new ParameterSchema
                    {
                        Id = Guid.NewGuid(),
                        ConfigurationId = configuration.Id,
                        SchemaVersion = version,
                        GeneratedJsonSchema = jsonSchema,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    _db.ParameterSchemas.Add(paramSchema);
                    configVersion.ParameterSchemaId = paramSchema.Id;
                }
                else
                {
                    configVersion.ParameterSchemaId = existingSchema.Id;
                    existingSchema.UpdatedAt = DateTimeOffset.UtcNow;
                }

                await _db.SaveChangesAsync();
            }
        }

    }

    public async Task CreateVersionFromExistingAsync(string name, string sourceVersion, string newVersion, bool isDraft)
    {
        var configuration = await _db.Configurations
            .Include(c => c.Versions)
            .ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(c => c.Name == name)
            ?? throw new KeyNotFoundException($"Configuration '{name}' not found.");

        var sourceConfigVersion = configuration.Versions
            .FirstOrDefault(v => v.Version == sourceVersion)
            ?? throw new KeyNotFoundException($"Source version '{sourceVersion}' not found for configuration '{name}'.");

        if (await _db.ConfigurationVersions.AnyAsync(v => v.ConfigurationId == configuration.Id && v.Version == newVersion))
        {
            LogVersionAlreadyExists(newVersion, name);
            throw new InvalidOperationException($"Version '{newVersion}' already exists for configuration '{name}'.");
        }

        var configVersion = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configuration.Id,
            Version = newVersion,
            EntryPoint = sourceConfigVersion.EntryPoint,
            Status = isDraft ? ConfigurationVersionStatus.Draft : ConfigurationVersionStatus.Published,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ConfigurationVersions.Add(configVersion);

        var dataDir = _serverConfig.Value.ConfigurationsDirectory;
        var sourceVersionDir = Path.Combine(dataDir, name, $"v{sourceVersion}");
        var newVersionDir = Path.Combine(dataDir, name, $"v{newVersion}");

        if (!Directory.Exists(sourceVersionDir))
        {
            LogSourceVersionDirectoryNotFound(sourceVersionDir);
            throw new InvalidOperationException($"Source version directory '{sourceVersionDir}' does not exist.");
        }

        Directory.CreateDirectory(newVersionDir);

        foreach (var sourceFile in sourceConfigVersion.Files)
        {
            var sourceFilePath = Path.Combine(sourceVersionDir, sourceFile.RelativePath);
            var newFilePath = Path.Combine(newVersionDir, sourceFile.RelativePath);

            if (!File.Exists(sourceFilePath))
            {
                LogSourceFileNotFound(sourceFilePath);
                continue;
            }

            var fileDir = Path.GetDirectoryName(newFilePath);
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            File.Copy(sourceFilePath, newFilePath, overwrite: false);

            var checksum = await ComputeFileChecksumAsync(newFilePath);

            var configFile = new ConfigurationFile
            {
                Id = Guid.NewGuid(),
                VersionId = configVersion.Id,
                RelativePath = sourceFile.RelativePath,
                ContentType = sourceFile.ContentType,
                Checksum = checksum,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.ConfigurationFiles.Add(configFile);
        }

        configuration.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        // Extract and generate parameter schema from copied entry point file
        var entryPointPath = Path.Combine(newVersionDir, configVersion.EntryPoint);
        if (File.Exists(entryPointPath))
        {
            var entryPointContent = await File.ReadAllTextAsync(entryPointPath);
            var parametersBlock = ExtractParametersFromYaml(entryPointContent);

            if (parametersBlock != null && parametersBlock.Count > 0)
            {
                var paramDefinitions = ConvertToParameterDefinitions(parametersBlock);
                var jsonSchemaObj = _schemaBuilder.BuildJsonSchema(paramDefinitions);
                var jsonSchema = _schemaBuilder.SerializeSchema(jsonSchemaObj);

                var existingSchema = await _db.ParameterSchemas
                    .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id && ps.SchemaVersion == newVersion);

                if (existingSchema == null)
                {
                    var paramSchema = new ParameterSchema
                    {
                        Id = Guid.NewGuid(),
                        ConfigurationId = configuration.Id,
                        SchemaVersion = newVersion,
                        GeneratedJsonSchema = jsonSchema,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    _db.ParameterSchemas.Add(paramSchema);
                    configVersion.ParameterSchemaId = paramSchema.Id;
                }
                else
                {
                    configVersion.ParameterSchemaId = existingSchema.Id;
                    existingSchema.UpdatedAt = DateTimeOffset.UtcNow;
                }

                await _db.SaveChangesAsync();
            }
        }

    }

    public async Task AddFilesToVersionAsync(string name, string version, IReadOnlyList<IBrowserFile> files)
    {
        var configuration = await _db.Configurations
            .Include(c => c.Versions)
            .ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(c => c.Name == name)
            ?? throw new KeyNotFoundException($"Configuration '{name}' not found.");

        var configVersion = configuration.Versions
            .FirstOrDefault(v => v.Version == version)
            ?? throw new KeyNotFoundException($"Version '{version}' not found for configuration '{name}'.");

        if (configVersion.Status != ConfigurationVersionStatus.Draft)
        {
            LogCannotAddFilesToPublishedVersion(version);
            throw new InvalidOperationException($"Cannot add files to published version '{version}'.");
        }

        var dataDir = _serverConfig.Value.ConfigurationsDirectory;
        var versionDir = Path.Combine(dataDir, name, $"v{version}");

        foreach (var file in files)
        {
            var relativePath = file.Name;

            if (configVersion.Files.Any(f => f.RelativePath == relativePath))
            {
                LogFileAlreadyExistsInVersion(relativePath, version);
                continue;
            }

            var filePath = Path.Combine(versionDir, relativePath);
            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            await using (var stream = File.Create(filePath))
            {
                await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(stream);
            }

            var checksum = await ComputeFileChecksumAsync(filePath);
            var contentType = GetContentType(file);

            var configFile = new ConfigurationFile
            {
                Id = Guid.NewGuid(),
                VersionId = configVersion.Id,
                RelativePath = relativePath,
                ContentType = contentType,
                Checksum = checksum,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.ConfigurationFiles.Add(configFile);
        }

        configuration.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<PublishResult> PublishVersionAsync(string name, string version)
    {
        var configuration = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == name)
            ?? throw new KeyNotFoundException($"Configuration '{name}' not found.");

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanModifyConfigurationAsync(userId.Value, configuration.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to configuration '{name}'.");
        }

        var configVersion = await _db.ConfigurationVersions
            .Include(v => v.Files)
            .FirstOrDefaultAsync(v => v.ConfigurationId == configuration.Id && v.Version == version)
            ?? throw new KeyNotFoundException($"Version '{version}' not found for configuration '{name}'.");

        if (configVersion.Status != ConfigurationVersionStatus.Draft)
        {
            throw new InvalidOperationException("Version is already published.");
        }

        if (configVersion.Files.Count == 0)
        {
            throw new InvalidOperationException("Cannot publish version with no files.");
        }

        if (!SemanticVersion.TryParse(version, out var semVer))
        {
            throw new ArgumentException($"Version '{version}' is not a valid semantic version.", nameof(version));
        }

        var newMajor = semVer.Major;
        var dataDir = _serverConfig.Value.ConfigurationsDirectory;
        var entryPointPath = Path.Combine(dataDir, name, $"v{version}", configVersion.EntryPoint);

        if (!File.Exists(entryPointPath))
        {
            throw new FileNotFoundException($"Entry point file '{configVersion.EntryPoint}' not found.", entryPointPath);
        }

        var entryPointContent = await File.ReadAllTextAsync(entryPointPath);
        var parametersJson = await _parameterSchemaService.ParseParameterBlockAsync(entryPointContent);

        CompatibilityReport? compatibilityReport = null;

        if (parametersJson != null)
        {
            await _parameterSchemaService.GenerateAndStoreSchemaAsync(configuration.Id, parametersJson, version);

            var parameterSchema = await _db.ParameterSchemas
                .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id);

            if (parameterSchema != null && !string.IsNullOrWhiteSpace(parameterSchema.SchemaVersion) &&
                parameterSchema.SchemaVersion != version)
            {
                if (!SemanticVersion.TryParse(parameterSchema.SchemaVersion, out var oldSemVer))
                {
                    throw new InvalidOperationException($"Previous schema version '{parameterSchema.SchemaVersion}' is not a valid semantic version.");
                }

                var oldMajor = oldSemVer.Major;
                compatibilityReport = _compatibilityService.CompareSchemas(
                    parameterSchema.GeneratedJsonSchema,
                    parameterSchema.GeneratedJsonSchema,
                    parameterSchema.SchemaVersion,
                    version);

                if (semVer.Major == oldSemVer.Major && compatibilityReport.HasBreakingChanges)
                {
                    var affectedFiles = await _db.ParameterFiles
                        .Include(pf => pf.ScopeType)
                        .Where(pf => pf.ParameterSchemaId == parameterSchema.Id && pf.MajorVersion == oldMajor)
                        .ToListAsync();

                    compatibilityReport = new CompatibilityReport
                    {
                        OldVersion = compatibilityReport.OldVersion,
                        NewVersion = compatibilityReport.NewVersion,
                        NewMajorVersion = compatibilityReport.NewMajorVersion,
                        HasBreakingChanges = true,
                        BreakingChanges = compatibilityReport.BreakingChanges,
                        NonBreakingChanges = compatibilityReport.NonBreakingChanges,
                        AffectedParameterFiles = affectedFiles.Select(f => new ParameterFileMigrationStatus
                        {
                            FileId = f.Id,
                            ScopeTypeName = f.ScopeType.Name,
                            ScopeValue = f.ScopeValue,
                            Version = f.Version,
                            NeedsMigration = true,
                            Errors = null
                        }).ToList()
                    };

                    return new PublishResult { Success = false, CompatibilityReport = compatibilityReport };
                }
                else if (semVer.Major > oldSemVer.Major)
                {
                    var activeParameters = await _db.ParameterFiles
                        .Include(pf => pf.ScopeType)
                        .Where(pf => pf.ParameterSchemaId == parameterSchema.Id &&
                                     pf.MajorVersion == oldMajor &&
                                     pf.Status == ParameterVersionStatus.Published)
                        .ToListAsync();

                    foreach (var activeParam in activeParameters)
                    {
                        var existsInNewMajor = await _db.ParameterFiles
                            .AnyAsync(pf => pf.ParameterSchemaId == parameterSchema.Id &&
                                            pf.MajorVersion == newMajor &&
                                            pf.ScopeTypeId == activeParam.ScopeTypeId &&
                                            pf.ScopeValue == activeParam.ScopeValue);

                        if (!existsInNewMajor)
                        {
                            var paramFilePath = !string.IsNullOrWhiteSpace(activeParam.ScopeValue)
                                ? Path.Combine(dataDir, "parameters", name, activeParam.ScopeType.Name, activeParam.ScopeValue, "parameters.yaml")
                                : Path.Combine(dataDir, "parameters", name, activeParam.ScopeType.Name, "parameters.yaml");

                            string? validationErrors = null;
                            if (File.Exists(paramFilePath) && !string.IsNullOrWhiteSpace(parameterSchema.GeneratedJsonSchema))
                            {
                                var content = await File.ReadAllTextAsync(paramFilePath);
                                var validationResult = _parameterValidator.Validate(parameterSchema.GeneratedJsonSchema, content);
                                if (!validationResult.IsValid)
                                {
                                    validationErrors = JsonSerializer.Serialize(
                                        validationResult.Errors,
                                        SourceGenerationContext.Default.ListValidationError);
                                }
                            }

                            _db.ParameterFiles.Add(new ParameterFile
                            {
                                Id = Guid.NewGuid(),
                                ParameterSchemaId = parameterSchema.Id,
                                ScopeTypeId = activeParam.ScopeTypeId,
                                ScopeValue = activeParam.ScopeValue,
                                Version = $"{newMajor}.0.0",
                                MajorVersion = newMajor,
                                Checksum = activeParam.Checksum,
                                ContentType = activeParam.ContentType,
                                Status = ParameterVersionStatus.Published,
                                NeedsMigration = validationErrors != null || compatibilityReport!.HasBreakingChanges,
                                ValidationErrors = validationErrors,
                                CreatedAt = DateTimeOffset.UtcNow
                            });
                        }
                    }

                    await _db.SaveChangesAsync();
                }
            }
        }

        configVersion.Status = ConfigurationVersionStatus.Published;
        await _db.SaveChangesAsync();

        return new PublishResult
        {
            Success = true,
            UpdatedStatus = configVersion.Status,
            UpdatedVersion = configVersion.Version,
            UpdatedPrereleaseChannel = configVersion.PrereleaseChannel
        };
    }

    public async Task<ConfigurationDetailsDto> UpdateConfigurationAsync(string name, string? description, bool? useServerManagedParameters)
    {
        var configuration = await _db.Configurations
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Name == name)
            ?? throw new KeyNotFoundException($"Configuration '{name}' not found.");

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanModifyConfigurationAsync(userId.Value, configuration.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to configuration '{name}'.");
        }

        if (useServerManagedParameters == false && configuration.UseServerManagedParameters)
        {
            var activeParamCount = await _db.ParameterFiles
                .CountAsync(pf => pf.ParameterSchema!.ConfigurationId == configuration.Id &&
                                  pf.Status == ParameterVersionStatus.Published);

            if (activeParamCount > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot disable server-managed parameters: {activeParamCount} active parameter file(s) exist. Deactivate them first.");
            }
        }

        if (description is not null)
        {
            configuration.Description = description;
        }

        if (useServerManagedParameters.HasValue)
        {
            configuration.UseServerManagedParameters = useServerManagedParameters.Value;
        }

        configuration.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var latestVersion = configuration.Versions
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => v.Version)
            .FirstOrDefault();

        return new ConfigurationDetailsDto
        {
            Id = configuration.Id,
            Name = configuration.Name,
            Description = configuration.Description,
            UseServerManagedParameters = configuration.UseServerManagedParameters,
            LatestVersion = latestVersion,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt
        };
    }

    public async Task DeleteConfigurationAsync(string name)
    {
        var configuration = await _db.Configurations
            .Include(c => c.Versions)
                .ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(c => c.Name == name)
            ?? throw new KeyNotFoundException($"Configuration '{name}' not found.");

        // Check if configuration is assigned to nodes
        var assignedNodes = await _db.NodeConfigurations
            .Where(nc => nc.ConfigurationId == configuration.Id)
            .Include(nc => nc.Node)
            .ToListAsync();

        if (assignedNodes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete configuration '{name}' because it is assigned to {assignedNodes.Count} node(s): {string.Join(", ", assignedNodes.Select(n => n.Node.Fqdn))}");
        }

        // Check if configuration is used in composite configurations
        var compositeUsage = await _db.CompositeConfigurationItems
            .Where(cci => cci.ChildConfigurationId == configuration.Id)
            .Include(cci => cci.CompositeConfigurationVersion)
                .ThenInclude(ccv => ccv.CompositeConfiguration)
            .ToListAsync();

        if (compositeUsage.Count > 0)
        {
            var compositeNames = compositeUsage
                .Select(c => $"{c.CompositeConfigurationVersion.CompositeConfiguration.Name} v{c.CompositeConfigurationVersion.Version}")
                .Distinct();
            throw new InvalidOperationException(
                $"Cannot delete configuration '{name}' because it is used in composite configuration(s): {string.Join(", ", compositeNames)}. Remove this configuration from all composite configurations first.");
        }

        var dataDir = _serverConfig.Value.ConfigurationsDirectory;
        var configDir = Path.Combine(dataDir, name);
        if (Directory.Exists(configDir))
        {
            Directory.Delete(configDir, recursive: true);
        }

        _db.Configurations.Remove(configuration);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteVersionAsync(string name, string version)
    {
        var configuration = await _db.Configurations
            .Include(c => c.Versions)
                .ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(c => c.Name == name)
            ?? throw new KeyNotFoundException($"Configuration '{name}' not found.");

        var configVersion = configuration.Versions
            .FirstOrDefault(v => v.Version == version)
            ?? throw new KeyNotFoundException($"Version '{version}' not found for configuration '{name}'.");

        // Check if version is assigned to nodes
        var assignedNodes = await _db.NodeConfigurations
            .Where(nc => nc.ConfigurationId == configuration.Id && nc.ActiveVersion == version)
            .Include(nc => nc.Node)
            .ToListAsync();

        if (assignedNodes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete version '{version}' of configuration '{name}' because it is assigned to {assignedNodes.Count} node(s): {string.Join(", ", assignedNodes.Select(n => n.Node.Fqdn))}");
        }

        // Extract major version from the version string
        var versionMajor = ExtractMajorVersion(version);

        // Check if this major version is referenced in composite configurations
        if (versionMajor.HasValue)
        {
            var compositeUsage = await _db.CompositeConfigurationItems
                .Where(cci => cci.ChildConfigurationId == configuration.Id && cci.MajorVersion == versionMajor.Value)
                .Include(cci => cci.CompositeConfigurationVersion)
                    .ThenInclude(ccv => ccv.CompositeConfiguration)
                .ToListAsync();

            if (compositeUsage.Count > 0)
            {
                // Count how many published versions exist with this major version
                var publishedVersionsWithMajor = await _db.ConfigurationVersions
                    .Where(v => v.ConfigurationId == configuration.Id &&
                           v.Status == ConfigurationVersionStatus.Published)
                    .Select(v => v.Version)
                    .ToListAsync();

                var majorVersionCount = publishedVersionsWithMajor
                    .Count(v => ExtractMajorVersion(v) == versionMajor);

                // Only prevent deletion if this is the last published version with this major
                if (majorVersionCount == 1)
                {
                    var compositeNames = compositeUsage
                        .Select(c => $"{c.CompositeConfigurationVersion.CompositeConfiguration.Name} v{c.CompositeConfigurationVersion.Version}")
                        .Distinct();
                    throw new InvalidOperationException(
                        $"Cannot delete version '{version}' of configuration '{name}' because it is the last published version with major version {versionMajor} and is used in composite configuration(s): {string.Join(", ", compositeNames)}. Update or remove this major version from all composite configurations first.");
                }
            }
        }

        var dataDir = _serverConfig.Value.ConfigurationsDirectory;
        var versionDir = Path.Combine(dataDir, name, $"v{version}");
        if (Directory.Exists(versionDir))
        {
            Directory.Delete(versionDir, recursive: true);
        }

        _db.ConfigurationVersions.Remove(configVersion);
        configuration.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static int? ExtractMajorVersion(string version)
    {
        var match = System.Text.RegularExpressions.Regex.Match(version, @"^(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var major))
        {
            return major;
        }
        return null;
    }

    public async Task DeleteFileAsync(string name, string version, string filePath)
    {
        var configuration = await _db.Configurations
            .Include(c => c.Versions)
                .ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(c => c.Name == name)
            ?? throw new KeyNotFoundException($"Configuration '{name}' not found.");

        var configVersion = configuration.Versions
            .FirstOrDefault(v => v.Version == version)
            ?? throw new KeyNotFoundException($"Version '{version}' not found for configuration '{name}'.");

        if (configVersion.Status != ConfigurationVersionStatus.Draft)
        {
            LogCannotDeleteFilesFromPublishedVersion(version);
            throw new InvalidOperationException($"Cannot delete files from published version '{version}'.");
        }

        var configFile = configVersion.Files
            .FirstOrDefault(f => f.RelativePath == filePath)
            ?? throw new KeyNotFoundException($"File '{filePath}' not found in version '{version}'.");

        var dataDir = _serverConfig.Value.ConfigurationsDirectory;
        var fullPath = Path.Combine(dataDir, name, $"v{version}", filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        _db.ConfigurationFiles.Remove(configFile);
        configuration.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task ChangeVersionEntryPointAsync(string name, string version, string entryPoint)
    {
        var configVersion = await _db.ConfigurationVersions
            .Include(v => v.Files)
            .Include(v => v.Configuration)
            .FirstOrDefaultAsync(v => v.Configuration.Name == name && v.Version == version)
            ?? throw new KeyNotFoundException($"Version '{version}' not found for configuration '{name}'.");

        if (configVersion.Status != ConfigurationVersionStatus.Draft)
        {
            LogCannotChangeEntryPointOfPublishedVersion(version);
            throw new InvalidOperationException($"Cannot change entry point of published version '{version}'.");
        }

        if (!configVersion.Files.Any(f => string.Equals(f.RelativePath, entryPoint, StringComparison.OrdinalIgnoreCase)))
        {
            LogEntryPointFileNotFoundInVersion(entryPoint, version);
            throw new ArgumentException($"Entry point '{entryPoint}' was not found in version '{version}'.", nameof(entryPoint));
        }

        configVersion.EntryPoint = entryPoint;
        await _db.SaveChangesAsync();
    }

    public async Task<Stream?> DownloadFileAsync(string name, string version, string filePath)
    {
        try
        {
            var dataDir = _serverConfig.Value.ConfigurationsDirectory;
            var fullPath = Path.Combine(dataDir, name, $"v{version}", filePath);

            if (!File.Exists(fullPath))
            {
                LogFileNotFound(fullPath);
                return null;
            }

            var memory = new MemoryStream();
            await using (var fileStream = File.OpenRead(fullPath))
            {
                await fileStream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return memory;
        }
        catch (Exception ex)
        {
            LogErrorDownloadingFile(ex, filePath, name, version);
            return null;
        }
    }

    public async Task SaveFileAsync(string name, string version, string filePath, string content)
    {
        var configVersion = await _db.ConfigurationVersions
            .Include(v => v.Files)
            .Include(v => v.Configuration)
            .FirstOrDefaultAsync(v => v.Configuration.Name == name && v.Version == version)
            ?? throw new KeyNotFoundException($"Version '{version}' not found for configuration '{name}'.");

        var configFile = configVersion.Files.FirstOrDefault(f => f.RelativePath == filePath)
            ?? throw new KeyNotFoundException($"File '{filePath}' not found in version '{version}'.");

        var dataDir = _serverConfig.Value.ConfigurationsDirectory;
        var versionDir = Path.Combine(dataDir, name, $"v{version}");
        var fullPath = Path.Combine(versionDir, filePath);

        await File.WriteAllTextAsync(fullPath, content);

        configFile.Checksum = await ComputeFileChecksumAsync(fullPath);
        await _db.SaveChangesAsync();

        if (configVersion.EntryPoint == filePath)
        {
            var parametersBlock = ExtractParametersFromYaml(content);
            if (parametersBlock != null && parametersBlock.Count > 0)
            {
                var paramDefinitions = ConvertToParameterDefinitions(parametersBlock);
                var jsonSchemaObj = _schemaBuilder.BuildJsonSchema(paramDefinitions);
                var jsonSchema = _schemaBuilder.SerializeSchema(jsonSchemaObj);

                var existingSchema = await _db.ParameterSchemas
                    .FirstOrDefaultAsync(ps => ps.ConfigurationId == configVersion.ConfigurationId && ps.SchemaVersion == version);

                if (existingSchema == null)
                {
                    var paramSchema = new ParameterSchema
                    {
                        Id = Guid.NewGuid(),
                        ConfigurationId = configVersion.ConfigurationId,
                        SchemaVersion = version,
                        GeneratedJsonSchema = jsonSchema,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    _db.ParameterSchemas.Add(paramSchema);
                    configVersion.ParameterSchemaId = paramSchema.Id;
                }
                else
                {
                    existingSchema.GeneratedJsonSchema = jsonSchema;
                    existingSchema.UpdatedAt = DateTimeOffset.UtcNow;
                    configVersion.ParameterSchemaId = existingSchema.Id;
                }

                await _db.SaveChangesAsync();
            }
        }
    }

    public async Task<List<ConfigurationSummaryDto>> GetConfigurationsAsync()
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == null)
        {
            return [];
        }

        var readableIds = await _authService.GetReadableConfigurationIdsAsync(userId.Value);

        var configs = await _db.Configurations
            .Where(c => readableIds.Contains(c.Id))
            .Include(c => c.Versions)
            .ToListAsync();

        return configs.Select(c => new ConfigurationSummaryDto
        {
            Name = c.Name,
            Description = c.Description,
            UseServerManagedParameters = c.UseServerManagedParameters,
            VersionCount = c.Versions.Count,
            LatestVersion = c.Versions
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => v.Version)
                .FirstOrDefault(),
            HasPublishedVersion = c.Versions.Any(v => v.Status == ConfigurationVersionStatus.Published),
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<ConfigurationDetailsDto?> GetConfigurationAsync(string name)
    {
        var config = await _db.Configurations
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (config is null)
        {
            return null;
        }

        var latestVersion = config.Versions
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => v.Version)
            .FirstOrDefault();

        return new ConfigurationDetailsDto
        {
            Id = config.Id,
            Name = config.Name,
            Description = config.Description,
            UseServerManagedParameters = config.UseServerManagedParameters,
            LatestVersion = latestVersion,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }

    public async Task<List<ConfigurationVersionDto>?> GetVersionsAsync(string name)
    {
        var config = await _db.Configurations
            .Include(c => c.Versions)
                .ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(c => c.Name == name);

        if (config is null)
        {
            return null;
        }

        return config.Versions
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new ConfigurationVersionDto
            {
                Version = v.Version,
                EntryPoint = v.EntryPoint,
                Status = v.Status,
                PrereleaseChannel = v.PrereleaseChannel,
                FileCount = v.Files.Count,
                CreatedAt = v.CreatedAt,
                CreatedBy = v.CreatedBy,
                Files = v.Files.Select(f => new ConfigurationFileDto { RelativePath = f.RelativePath, ContentType = f.ContentType }).ToList()
            })
            .ToList();
    }

    public async Task<bool> IsConfigurationAssignedAsync(string configName)
    {
        var config = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (config is null) return false;
        return await _db.NodeConfigurations.AnyAsync(nc => nc.ConfigurationId == config.Id)
            || await _db.CompositeConfigurationItems.AnyAsync(i => i.ChildConfigurationId == config.Id);
    }

    public async Task<VersionUsageInfo> IsVersionInUseAsync(string configName, string version)
    {
        var config = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (config is null) return new VersionUsageInfo { IsInUse = false };
        var configId = config.Id;

        var nodesUsing = await _db.NodeConfigurations
            .Where(nc => nc.ConfigurationId == configId && nc.ActiveVersion == version)
            .Include(nc => nc.Node)
            .ToListAsync();

        // Extract major version from the version string
        var versionMajor = ExtractMajorVersion(version);

        var compositesUsing = new List<CompositeConfigurationItem>();
        if (versionMajor.HasValue)
        {
            // Check if this major version is used in composites
            var compositesByMajor = await _db.CompositeConfigurationItems
                .Where(cci => cci.ChildConfigurationId == configId && cci.MajorVersion == versionMajor.Value)
                .Include(cci => cci.CompositeConfigurationVersion)
                    .ThenInclude(ccv => ccv.CompositeConfiguration)
                .ToListAsync();

            if (compositesByMajor.Any())
            {
                // Count how many published versions exist with this major version
                var publishedVersionsWithMajor = await _db.ConfigurationVersions
                    .Where(v => v.ConfigurationId == configId &&
                           v.Status == ConfigurationVersionStatus.Published)
                    .Select(v => v.Version)
                    .ToListAsync();

                var majorVersionCount = publishedVersionsWithMajor
                    .Count(v => ExtractMajorVersion(v) == versionMajor);

                // Only mark as in-use if this is the last published version with this major
                if (majorVersionCount == 1)
                {
                    compositesUsing = compositesByMajor;
                }
            }
        }

        if (!nodesUsing.Any() && !compositesUsing.Any())
        {
            return new VersionUsageInfo { IsInUse = false };
        }

        var details = new List<string>();

        if (nodesUsing.Any())
        {
            details.Add($"- Nodes: {string.Join(", ", nodesUsing.Select(n => n.Node.Fqdn))}");
        }

        if (compositesUsing.Any())
        {
            var compositeNames = compositesUsing
                .Select(c => $"{c.CompositeConfigurationVersion.CompositeConfiguration.Name} v{c.CompositeConfigurationVersion.Version}")
                .Distinct();
            details.Add($"- Composite Configurations: {string.Join(", ", compositeNames)}");
        }

        return new VersionUsageInfo { IsInUse = true, Details = details };
    }

    public async Task<ConfigurationSettingsDto?> GetConfigurationSettingsAsync(string configName)
    {
        var configuration = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (configuration is null)
        {
            return null;
        }

        var settings = await _db.Set<ConfigurationSettings>()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == configuration.Id);

        if (settings is null)
        {
            var globalSettings = await _db.Set<ValidationSettings>().FirstOrDefaultAsync()
                                 ?? new ValidationSettings();

            return new ConfigurationSettingsDto
            {
                IsOverridden = false,
                RequireSemVer = globalSettings.EnforceSemverCompliance,
                ParameterValidationMode = globalSettings.DefaultParameterValidation
            };
        }

        return new ConfigurationSettingsDto
        {
            IsOverridden = true,
            RequireSemVer = settings.EnforceSemverCompliance ?? true,
            ParameterValidationMode = settings.ParameterValidation ?? ParameterValidationMode.Strict
        };
    }

    public async Task<ConfigurationSettingsDto> UpdateConfigurationSettingsAsync(
        string configName,
        bool? requireSemVer,
        ParameterValidationMode? paramValidation)
    {
        var configuration = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == configName)
            ?? throw new KeyNotFoundException($"Configuration '{configName}' not found.");

        var globalSettings = await _db.Set<ValidationSettings>().FirstOrDefaultAsync()
                             ?? new ValidationSettings();

        if (!globalSettings.AllowSemverComplianceOverride || !globalSettings.AllowParameterValidationOverride)
        {
            throw new InvalidOperationException("Configuration-level overrides are not allowed by global settings.");
        }

        var settings = await _db.Set<ConfigurationSettings>()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == configuration.Id);

        if (settings is null)
        {
            settings = new ConfigurationSettings
            {
                ConfigurationId = configuration.Id,
                EnforceSemverCompliance = requireSemVer ?? globalSettings.EnforceSemverCompliance,
                ParameterValidation = paramValidation ?? globalSettings.DefaultParameterValidation,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.Add(settings);
        }
        else
        {
            if (requireSemVer.HasValue)
            {
                settings.EnforceSemverCompliance = requireSemVer.Value;
            }

            if (paramValidation.HasValue)
            {
                settings.ParameterValidation = paramValidation.Value;
            }

            settings.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();

        return new ConfigurationSettingsDto
        {
            IsOverridden = true,
            RequireSemVer = settings.EnforceSemverCompliance ?? true,
            ParameterValidationMode = settings.ParameterValidation ?? ParameterValidationMode.Strict
        };
    }

    public async Task DeleteConfigurationSettingsAsync(string configName)
    {
        var configuration = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (configuration is null)
        {
            return;
        }

        var settings = await _db.Set<ConfigurationSettings>()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == configuration.Id);

        if (settings is not null)
        {
            _db.Remove(settings);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<ConfigurationRetentionDto?> GetRetentionSettingsAsync(string configName)
    {
        var config = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (config is null) return null;

        var settings = await _db.Set<ConfigurationSettings>()
            .AsNoTracking()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == config.Id);

        if (settings is null
            || (settings.RetentionEnabled is null
                && settings.RetentionKeepVersions is null
                && settings.RetentionKeepDays is null
                && settings.RetentionKeepReleaseVersions is null))
        {
            return new ConfigurationRetentionDto { IsOverridden = false };
        }

        return new ConfigurationRetentionDto
        {
            IsOverridden = true,
            Enabled = settings.RetentionEnabled,
            KeepVersions = settings.RetentionKeepVersions,
            KeepDays = settings.RetentionKeepDays,
            KeepReleaseVersions = settings.RetentionKeepReleaseVersions
        };
    }

    public async Task SaveRetentionSettingsAsync(string configName, bool? enabled, int? keepVersions, int? keepDays, bool? keepReleaseVersions)
    {
        var config = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (config is null) return;

        var settings = await _db.Set<ConfigurationSettings>()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == config.Id);

        if (settings is null)
        {
            settings = new ConfigurationSettings
            {
                ConfigurationId = config.Id,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.Add(settings);
        }

        if (enabled.HasValue) settings.RetentionEnabled = enabled.Value;
        if (keepVersions.HasValue) settings.RetentionKeepVersions = keepVersions.Value;
        if (keepDays.HasValue) settings.RetentionKeepDays = keepDays.Value;
        if (keepReleaseVersions.HasValue) settings.RetentionKeepReleaseVersions = keepReleaseVersions.Value;
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task ResetRetentionSettingsAsync(string configName)
    {
        var config = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (config is null) return;

        var settings = await _db.Set<ConfigurationSettings>()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == config.Id);

        if (settings is not null)
        {
            settings.RetentionEnabled = null;
            settings.RetentionKeepVersions = null;
            settings.RetentionKeepDays = null;
            settings.RetentionKeepReleaseVersions = null;
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    private static async Task<string> ComputeFileChecksumAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes);
    }

    private static string GetContentType(IBrowserFile file)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            return file.ContentType;
        }

        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        return extension switch
        {
            ".yaml" or ".yml" => "application/x-yaml",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
    }

    public async Task<Guid?> GetParameterSchemaIdAsync(string configName)
    {
        var config = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (config is null)
        {
            return null;
        }

        var schema = await _db.ParameterSchemas
            .AsNoTracking()
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == config.Id);

        return schema?.Id;
    }

    public async Task<List<string>> GetConfigurationVersionListAsync(string configName)
    {
        var config = await _db.Configurations
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Name == configName);

        if (config is null)
        {
            return [];
        }

        return config.Versions
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => v.Version)
            .ToList();
    }

    public async Task<List<PermissionEntryDto>?> GetPermissionsAsync(string configName)
    {
        var config = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (config is null)
        {
            return null;
        }

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanManageConfigurationAsync(userId.Value, config.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to configuration '{configName}'.");
        }

        var acl = await _authService.GetConfigurationAclAsync(config.Id);
        return await BuildPermissionEntriesAsync(acl.Select(p => (p.PrincipalType, p.PrincipalId, p.PermissionLevel, p.GrantedAt, p.GrantedByUserId)));
    }

    public async Task GrantPermissionAsync(string configName, Guid principalId, string principalType, string level)
    {
        var config = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == configName)
            ?? throw new KeyNotFoundException($"Configuration '{configName}' not found.");

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanManageConfigurationAsync(userId.Value, config.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to configuration '{configName}'.");
        }

        if (!Enum.TryParse<PrincipalType>(principalType, ignoreCase: true, out var parsedPrincipalType))
        {
            throw new ArgumentException($"Invalid principal type '{principalType}'. Must be 'User' or 'Group'.", nameof(principalType));
        }

        if (!Enum.TryParse<ResourcePermission>(level, ignoreCase: true, out var parsedLevel))
        {
            throw new ArgumentException($"Invalid permission level '{level}'. Must be 'Read', 'Modify', or 'Manage'.", nameof(level));
        }

        if (parsedPrincipalType == PrincipalType.User && !await _db.Users.AnyAsync(u => u.Id == principalId))
        {
            throw new KeyNotFoundException($"User '{principalId}' not found.");
        }

        if (parsedPrincipalType == PrincipalType.Group && !await _db.Groups.AnyAsync(g => g.Id == principalId))
        {
            throw new KeyNotFoundException($"Group '{principalId}' not found.");
        }

        await _authService.GrantConfigurationPermissionAsync(config.Id, principalId, parsedPrincipalType, parsedLevel, userId.Value);
    }

    public async Task RevokePermissionAsync(string configName, Guid principalId, string principalType)
    {
        var config = await _db.Configurations.FirstOrDefaultAsync(c => c.Name == configName)
            ?? throw new KeyNotFoundException($"Configuration '{configName}' not found.");

        var userId = _userContext.GetCurrentUserId();
        if (userId == null || !await _authService.CanManageConfigurationAsync(userId.Value, config.Id))
        {
            throw new UnauthorizedAccessException($"Access denied to configuration '{configName}'.");
        }

        if (!Enum.TryParse<PrincipalType>(principalType, ignoreCase: true, out var parsedPrincipalType))
        {
            throw new ArgumentException($"Invalid principal type '{principalType}'. Must be 'User' or 'Group'.", nameof(principalType));
        }

        await _authService.RevokeConfigurationPermissionAsync(config.Id, principalId, parsedPrincipalType);
    }

    private async Task<List<PermissionEntryDto>> BuildPermissionEntriesAsync(
        IEnumerable<(PrincipalType PrincipalType, Guid PrincipalId, ResourcePermission Level, DateTimeOffset GrantedAt, Guid? GrantedByUserId)> entries)
    {
        var list = entries.ToList();
        var userIds = list.Where(e => e.PrincipalType == PrincipalType.User).Select(e => e.PrincipalId).ToList();
        var groupIds = list.Where(e => e.PrincipalType == PrincipalType.Group).Select(e => e.PrincipalId).ToList();

        var userNames = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username);

        var groupNames = await _db.Groups
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Name);

        return list.Select(e => new PermissionEntryDto
        {
            PrincipalType = e.PrincipalType.ToString(),
            PrincipalId = e.PrincipalId,
            PrincipalName = e.PrincipalType == PrincipalType.User
                ? userNames.GetValueOrDefault(e.PrincipalId, "Unknown")
                : groupNames.GetValueOrDefault(e.PrincipalId, "Unknown"),
            Level = e.Level.ToString(),
            GrantedAt = e.GrantedAt,
            GrantedByUserId = e.GrantedByUserId
        }).ToList();
    }

    private static Dictionary<string, object>? ExtractParametersFromYaml(string yamlContent)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var document = deserializer.Deserialize<Dictionary<object, object>>(yamlContent);
            if (document?.TryGetValue("parameters", out var parametersObj) == true)
            {
                if (parametersObj is Dictionary<object, object> paramDict)
                {
                    return paramDict.ToDictionary(
                        kvp => kvp.Key.ToString() ?? string.Empty,
                        kvp => kvp.Value);
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static Dictionary<string, ParameterDefinition> ConvertToParameterDefinitions(Dictionary<string, object> parametersBlock)
    {
        var result = new Dictionary<string, ParameterDefinition>();

        foreach (var param in parametersBlock)
        {
            if (param.Value is Dictionary<object, object> paramObj)
            {
                var def = new ParameterDefinition
                {
                    Type = paramObj.ContainsKey("type") ? paramObj["type"]?.ToString() ?? "string" : "string",
                    Description = paramObj.ContainsKey("description") ? paramObj["description"]?.ToString() : null,
                    DefaultValue = paramObj.ContainsKey("defaultValue") ? paramObj["defaultValue"] : null,
                    AllowedValues = paramObj.ContainsKey("allowedValues") && paramObj["allowedValues"] is List<object> list ? list.ToArray() : null,
                    MinLength = paramObj.ContainsKey("minLength") ? paramObj["minLength"] as int? : null,
                    MaxLength = paramObj.ContainsKey("maxLength") ? paramObj["maxLength"] as int? : null,
                    MinValue = paramObj.ContainsKey("minValue") ? paramObj["minValue"] as int? : null,
                    MaxValue = paramObj.ContainsKey("maxValue") ? paramObj["maxValue"] as int? : null
                };
                result[param.Key] = def;
            }
        }

        return result;
    }

    [LoggerMessage(EventId = EventIds.ConfigurationNameRequired, Level = LogLevel.Warning, Message = "Configuration name is required")]
    private partial void LogConfigurationNameRequired();

    [LoggerMessage(EventId = EventIds.FilesRequired, Level = LogLevel.Warning, Message = "At least one file is required")]
    private partial void LogFilesRequired();

    [LoggerMessage(EventId = EventIds.ConfigurationAlreadyExists, Level = LogLevel.Warning, Message = "Configuration '{Name}' already exists")]
    private partial void LogConfigurationAlreadyExists(string name);

    [LoggerMessage(EventId = EventIds.EntryPointNotFoundInUploadedFiles, Level = LogLevel.Warning, Message = "Entry point file '{EntryPoint}' not found in uploaded files")]
    private partial void LogEntryPointNotFoundInUploadedFiles(string entryPoint);

    [LoggerMessage(EventId = EventIds.ErrorCreatingConfiguration, Level = LogLevel.Error, Message = "Error creating configuration '{Name}'")]
    private partial void LogErrorCreatingConfiguration(Exception ex, string name);

    [LoggerMessage(EventId = EventIds.ConfigurationNotFound, Level = LogLevel.Warning, Message = "Configuration '{Name}' not found")]
    private partial void LogConfigurationNotFound(string name);

    [LoggerMessage(EventId = EventIds.VersionAlreadyExists, Level = LogLevel.Warning, Message = "Version '{Version}' already exists for configuration '{Name}'")]
    private partial void LogVersionAlreadyExists(string version, string name);

    [LoggerMessage(EventId = EventIds.ErrorCreatingVersion, Level = LogLevel.Error, Message = "Error creating version '{Version}' for configuration '{Name}'")]
    private partial void LogErrorCreatingVersion(Exception ex, string version, string name);

    [LoggerMessage(EventId = EventIds.SourceVersionNotFound, Level = LogLevel.Warning, Message = "Source version '{Version}' not found for configuration '{Name}'")]
    private partial void LogSourceVersionNotFound(string version, string name);

    [LoggerMessage(EventId = EventIds.ErrorCreatingVersionFromExisting, Level = LogLevel.Error, Message = "Error creating version '{NewVersion}' from existing version '{SourceVersion}' for configuration '{Name}'")]
    private partial void LogErrorCreatingVersionFromExisting(Exception ex, string newVersion, string sourceVersion, string name);

    [LoggerMessage(EventId = EventIds.SourceVersionDirectoryNotFound, Level = LogLevel.Warning, Message = "Source version directory not found: {SourceDir}")]
    private partial void LogSourceVersionDirectoryNotFound(string sourceDir);

    [LoggerMessage(EventId = EventIds.SourceFileNotFound, Level = LogLevel.Warning, Message = "Source file not found: {FilePath}")]
    private partial void LogSourceFileNotFound(string filePath);

    [LoggerMessage(EventId = EventIds.CannotAddFilesToPublishedVersion, Level = LogLevel.Warning, Message = "Cannot add files to published version '{Version}'")]
    private partial void LogCannotAddFilesToPublishedVersion(string version);

    [LoggerMessage(EventId = EventIds.FileAlreadyExistsInVersion, Level = LogLevel.Warning, Message = "File '{FilePath}' already exists in version '{Version}'")]
    private partial void LogFileAlreadyExistsInVersion(string filePath, string version);

    [LoggerMessage(EventId = EventIds.ErrorAddingFilesToVersion, Level = LogLevel.Error, Message = "Error adding files to version '{Version}' for configuration '{Name}'")]
    private partial void LogErrorAddingFilesToVersion(Exception ex, string version, string name);

    [LoggerMessage(EventId = EventIds.ErrorDeletingConfiguration, Level = LogLevel.Error, Message = "Error deleting configuration '{Name}'")]
    private partial void LogErrorDeletingConfiguration(Exception ex, string name);

    [LoggerMessage(EventId = EventIds.VersionNotFound, Level = LogLevel.Warning, Message = "Version '{Version}' not found for configuration '{Name}'")]
    private partial void LogVersionNotFound(string version, string name);

    [LoggerMessage(EventId = EventIds.ErrorDeletingVersion, Level = LogLevel.Error, Message = "Error deleting version '{Version}' for configuration '{Name}'")]
    private partial void LogErrorDeletingVersion(Exception ex, string version, string name);

    [LoggerMessage(EventId = EventIds.CannotDeleteFilesFromPublishedVersion, Level = LogLevel.Warning, Message = "Cannot delete files from published version '{Version}'")]
    private partial void LogCannotDeleteFilesFromPublishedVersion(string version);

    [LoggerMessage(EventId = EventIds.FileNotFoundInVersion, Level = LogLevel.Warning, Message = "File '{FilePath}' not found in version '{Version}'")]
    private partial void LogFileNotFoundInVersion(string filePath, string version);

    [LoggerMessage(EventId = EventIds.ErrorDeletingFileFromVersion, Level = LogLevel.Error, Message = "Error deleting file '{FilePath}' from version '{Version}' for configuration '{Name}'")]
    private partial void LogErrorDeletingFileFromVersion(Exception ex, string filePath, string version, string name);

    [LoggerMessage(EventId = EventIds.CannotChangeEntryPointOfPublishedVersion, Level = LogLevel.Warning, Message = "Cannot change entry point of published version '{Version}'")]
    private partial void LogCannotChangeEntryPointOfPublishedVersion(string version);

    [LoggerMessage(EventId = EventIds.EntryPointFileNotFoundInVersion, Level = LogLevel.Warning, Message = "Entry point file '{EntryPoint}' not found in version '{Version}'")]
    private partial void LogEntryPointFileNotFoundInVersion(string entryPoint, string version);

    [LoggerMessage(EventId = EventIds.ErrorChangingEntryPoint, Level = LogLevel.Error, Message = "Error changing entry point for version '{Version}' of configuration '{Name}'")]
    private partial void LogErrorChangingEntryPoint(Exception ex, string version, string name);

    [LoggerMessage(EventId = EventIds.FileNotFound, Level = LogLevel.Warning, Message = "File not found: {FilePath}")]
    private partial void LogFileNotFound(string filePath);

    [LoggerMessage(EventId = EventIds.ErrorDownloadingFile, Level = LogLevel.Error, Message = "Error downloading file '{FilePath}' from configuration '{Name}' version '{Version}'")]
    private partial void LogErrorDownloadingFile(Exception ex, string filePath, string name, string version);

    [LoggerMessage(EventId = EventIds.ErrorSavingFile, Level = LogLevel.Error, Message = "Error saving file '{FilePath}' in version '{Version}' for configuration '{Name}'")]
    private partial void LogErrorSavingFile(Exception ex, string filePath, string version, string name);
}
