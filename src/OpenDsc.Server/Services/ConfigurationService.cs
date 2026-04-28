// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenDsc.Server.Services;

public interface IConfigurationService
{
    Task<bool> CreateConfigurationAsync(string name, string? description, string entryPoint, string version, bool isDraft, bool useServerManagedParameters, IReadOnlyList<IBrowserFile> files);
    Task<bool> CreateVersionAsync(string name, string version, bool isDraft, IReadOnlyList<IBrowserFile> files, string? entryPoint = null);
    Task<bool> CreateVersionFromExistingAsync(string name, string sourceVersion, string newVersion, bool isDraft);
    Task<bool> AddFilesToVersionAsync(string name, string version, IReadOnlyList<IBrowserFile> files);
    Task<PublishResult> PublishVersionAsync(string name, string version);
    Task<bool> DeleteConfigurationAsync(string name);
    Task<bool> DeleteVersionAsync(string name, string version);
    Task<bool> DeleteFileAsync(string name, string version, string filePath);
    Task<bool> ChangeVersionEntryPointAsync(string name, string version, string entryPoint);
    Task<Stream?> DownloadFileAsync(string name, string version, string filePath);
    Task<bool> SaveFileAsync(string name, string version, string filePath, string content);
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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IParameterSchemaBuilder _schemaBuilder;
    private readonly IParameterCompatibilityService _compatibilityService;

    public ConfigurationService(
        ServerDbContext db,
        IOptions<ServerConfig> serverConfig,
        IResourceAuthorizationService authService,
        IUserContextService userContext,
        ILogger<ConfigurationService> logger,
        IHttpContextAccessor httpContextAccessor,
        IParameterSchemaBuilder schemaBuilder,
        IParameterCompatibilityService compatibilityService)
    {
        _db = db;
        _serverConfig = serverConfig;
        _authService = authService;
        _userContext = userContext;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _schemaBuilder = schemaBuilder;
        _compatibilityService = compatibilityService;
    }

    public async Task<bool> CreateConfigurationAsync(
        string name,
        string? description,
        string entryPoint,
        string version,
        bool isDraft,
        bool useServerManagedParameters,
        IReadOnlyList<IBrowserFile> files)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                LogConfigurationNameRequired();
                return false;
            }

            if (files.Count == 0)
            {
                LogFilesRequired();
                return false;
            }

            if (await _db.Configurations.AnyAsync(c => c.Name == name))
            {
                LogConfigurationAlreadyExists(name);
                return false;
            }

            if (!files.Any(f => f.Name == entryPoint))
            {
                LogEntryPointNotFoundInUploadedFiles(entryPoint);
                return false;
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
                !await _authService.HasGlobalPermissionAsync(creatorId, Permissions.Configurations_AdminOverride))
            {
                await _authService.GrantConfigurationPermissionAsync(
                    configuration.Id,
                    creatorId,
                    PrincipalType.User,
                    ResourcePermission.Manage,
                    creatorId);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogErrorCreatingConfiguration(ex, name);
            return false;
        }
    }

    public async Task<bool> CreateVersionAsync(
        string name,
        string version,
        bool isDraft,
        IReadOnlyList<IBrowserFile> files,
        string? entryPoint = null)
    {
        try
        {
            var configuration = await _db.Configurations
                .FirstOrDefaultAsync(c => c.Name == name);

            if (configuration == null)
            {
                LogConfigurationNotFound(name);
                return false;
            }

            if (await _db.ConfigurationVersions.AnyAsync(v => v.ConfigurationId == configuration.Id && v.Version == version))
            {
                LogVersionAlreadyExists(version, name);
                return false;
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

            return true;
        }
        catch (Exception ex)
        {
            LogErrorCreatingVersion(ex, version, name);
            return false;
        }
    }

    public async Task<bool> CreateVersionFromExistingAsync(string name, string sourceVersion, string newVersion, bool isDraft)
    {
        try
        {
            var configuration = await _db.Configurations
                .Include(c => c.Versions)
                .ThenInclude(v => v.Files)
                .FirstOrDefaultAsync(c => c.Name == name);

            if (configuration == null)
            {
                LogConfigurationNotFound(name);
                return false;
            }

            var sourceConfigVersion = configuration.Versions
                .FirstOrDefault(v => v.Version == sourceVersion);

            if (sourceConfigVersion == null)
            {
                LogSourceVersionNotFound(sourceVersion, name);
                return false;
            }

            if (await _db.ConfigurationVersions.AnyAsync(v => v.ConfigurationId == configuration.Id && v.Version == newVersion))
            {
                LogVersionAlreadyExists(newVersion, name);
                return false;
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
                return false;
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

            return true;
        }
        catch (Exception ex)
        {
            LogErrorCreatingVersionFromExisting(ex, newVersion, sourceVersion, name);
            return false;
        }
    }

    public async Task<bool> AddFilesToVersionAsync(string name, string version, IReadOnlyList<IBrowserFile> files)
    {
        try
        {
            var configuration = await _db.Configurations
                .Include(c => c.Versions)
                .ThenInclude(v => v.Files)
                .FirstOrDefaultAsync(c => c.Name == name);

            if (configuration == null)
            {
                LogConfigurationNotFound(name);
                return false;
            }

            var configVersion = configuration.Versions
                .FirstOrDefault(v => v.Version == version);

            if (configVersion == null)
            {
                LogVersionNotFound(version, name);
                return false;
            }

            if (configVersion.Status != ConfigurationVersionStatus.Draft)
            {
                LogCannotAddFilesToPublishedVersion(version);
                return false;
            }

            var dataDir = _serverConfig.Value.ConfigurationsDirectory;
            var versionDir = Path.Combine(dataDir, name, $"v{version}");

            foreach (var file in files)
            {
                var relativePath = file.Name;

                // Check if file already exists in this version
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

            return true;
        }
        catch (Exception ex)
        {
            LogErrorAddingFilesToVersion(ex, version, name);
            return false;
        }
    }

    public async Task<PublishResult> PublishVersionAsync(string name, string version)
    {
        try
        {
            // Get the current request's base URL
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return new PublishResult { Success = false, ErrorMessage = "Unable to determine server URL" };
            }

            var request = httpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

            // Forward authentication cookie from current request
            if (request.Headers.Cookie.Count > 0)
            {
                httpClient.DefaultRequestHeaders.Add("Cookie", request.Headers.Cookie.ToString());
            }

            var response = await httpClient.PutAsync($"/api/v1/configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}/publish", null);

            if (response.IsSuccessStatusCode)
            {
                // Parse the returned ConfigurationVersionDto to get updated state
                var content = await response.Content.ReadAsStringAsync();
                try
                {
                    var versionData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    ConfigurationVersionStatus? updatedStatus = null;
                    string? updatedVersion = null;
                    string? updatedChannel = null;

                    if (versionData != null)
                    {
                        if (versionData.TryGetValue("status", out var statusObj))
                        {
                            if (statusObj is JsonElement statusElem)
                            {
                                if (statusElem.ValueKind == JsonValueKind.String)
                                {
                                    if (Enum.TryParse<ConfigurationVersionStatus>(statusElem.GetString(), true, out var parsedStatus))
                                        updatedStatus = parsedStatus;
                                }
                                else if (statusElem.ValueKind == JsonValueKind.Number)
                                {
                                    updatedStatus = (ConfigurationVersionStatus)statusElem.GetInt32();
                                }
                            }
                            else if (statusObj is ConfigurationVersionStatus statusEnum)
                            {
                                updatedStatus = statusEnum;
                            }
                        }

                        if (versionData.TryGetValue("version", out var versionObj) && versionObj is string versionStr)
                        {
                            updatedVersion = versionStr;
                        }

                        if (versionData.TryGetValue("prereleaseChannel", out var channelObj) && channelObj is string channelStr)
                        {
                            updatedChannel = channelStr;
                        }
                    }

                    return new PublishResult
                    {
                        Success = true,
                        UpdatedStatus = updatedStatus,
                        UpdatedVersion = updatedVersion,
                        UpdatedPrereleaseChannel = updatedChannel
                    };
                }
                catch (Exception ex)
                {
                    LogFailedToParsePublishResponse(ex);
                    return new PublishResult { Success = true };
                }
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // Breaking changes detected - parse CompatibilityReport from response
                var content = await response.Content.ReadAsStringAsync();
                var compatibilityReport = System.Text.Json.JsonSerializer.Deserialize<CompatibilityReport>(
                    content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return new PublishResult
                {
                    Success = false,
                    CompatibilityReport = compatibilityReport
                };
            }

            // Other error
            var errorContent = await response.Content.ReadAsStringAsync();
            LogFailedToPublishVersion(version, name, errorContent);
            return new PublishResult
            {
                Success = false,
                ErrorMessage = errorContent
            };
        }
        catch (Exception ex)
        {
            LogErrorPublishingVersion(ex, version, name);
            return new PublishResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> DeleteConfigurationAsync(string name)
    {
        try
        {
            var configuration = await _db.Configurations
                .Include(c => c.Versions)
                    .ThenInclude(v => v.Files)
                .FirstOrDefaultAsync(c => c.Name == name);

            if (configuration == null)
            {
                LogConfigurationNotFound(name);
                return false;
            }

            var dataDir = _serverConfig.Value.ConfigurationsDirectory;
            var configDir = Path.Combine(dataDir, name);
            if (Directory.Exists(configDir))
            {
                Directory.Delete(configDir, recursive: true);
            }

            _db.Configurations.Remove(configuration);
            await _db.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            LogErrorDeletingConfiguration(ex, name);
            return false;
        }
    }

    public async Task<bool> DeleteVersionAsync(string name, string version)
    {
        try
        {
            var configuration = await _db.Configurations
                .Include(c => c.Versions)
                    .ThenInclude(v => v.Files)
                .FirstOrDefaultAsync(c => c.Name == name);

            if (configuration == null)
            {
                LogConfigurationNotFound(name);
                return false;
            }

            var configVersion = configuration.Versions
                .FirstOrDefault(v => v.Version == version);

            if (configVersion == null)
            {
                LogVersionNotFound(version, name);
                return false;
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

            return true;
        }
        catch (Exception ex)
        {
            LogErrorDeletingVersion(ex, version, name);
            return false;
        }
    }

    public async Task<bool> DeleteFileAsync(string name, string version, string filePath)
    {
        try
        {
            var configuration = await _db.Configurations
                .Include(c => c.Versions)
                    .ThenInclude(v => v.Files)
                .FirstOrDefaultAsync(c => c.Name == name);

            if (configuration == null)
            {
                LogConfigurationNotFound(name);
                return false;
            }

            var configVersion = configuration.Versions
                .FirstOrDefault(v => v.Version == version);

            if (configVersion == null)
            {
                LogVersionNotFound(version, name);
                return false;
            }

            if (configVersion.Status != ConfigurationVersionStatus.Draft)
            {
                LogCannotDeleteFilesFromPublishedVersion(version);
                return false;
            }

            var configFile = configVersion.Files
                .FirstOrDefault(f => f.RelativePath == filePath);

            if (configFile == null)
            {
                LogFileNotFoundInVersion(filePath, version);
                return false;
            }

            var dataDir = _serverConfig.Value.ConfigurationsDirectory;
            var fullPath = Path.Combine(dataDir, name, $"v{version}", filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            _db.ConfigurationFiles.Remove(configFile);
            configuration.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            LogErrorDeletingFileFromVersion(ex, filePath, version, name);
            return false;
        }
    }

    public async Task<bool> ChangeVersionEntryPointAsync(string name, string version, string entryPoint)
    {
        try
        {
            var configVersion = await _db.ConfigurationVersions
                .Include(v => v.Files)
                .Include(v => v.Configuration)
                .FirstOrDefaultAsync(v => v.Configuration.Name == name && v.Version == version);

            if (configVersion == null)
            {
                LogVersionNotFound(version, name);
                return false;
            }

            if (configVersion.Status != ConfigurationVersionStatus.Draft)
            {
                LogCannotChangeEntryPointOfPublishedVersion(version);
                return false;
            }

            if (!configVersion.Files.Any(f => string.Equals(f.RelativePath, entryPoint, StringComparison.OrdinalIgnoreCase)))
            {
                LogEntryPointFileNotFoundInVersion(entryPoint, version);
                return false;
            }

            configVersion.EntryPoint = entryPoint;
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            LogErrorChangingEntryPoint(ex, version, name);
            return false;
        }
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

    public async Task<bool> SaveFileAsync(string name, string version, string filePath, string content)
    {
        try
        {
            var configVersion = await _db.ConfigurationVersions
                .Include(v => v.Files)
                .Include(v => v.Configuration)
                .FirstOrDefaultAsync(v => v.Configuration.Name == name && v.Version == version);

            if (configVersion == null)
            {
                LogVersionNotFound(version, name);
                return false;
            }

            var configFile = configVersion.Files.FirstOrDefault(f => f.RelativePath == filePath);
            if (configFile == null)
            {
                LogFileNotFoundInVersion(filePath, version);
                return false;
            }

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

            return true;
        }
        catch (Exception ex)
        {
            LogErrorSavingFile(ex, filePath, version, name);
            return false;
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

    [LoggerMessage(EventId = EventIds.FailedToParsePublishResponse, Level = LogLevel.Warning, Message = "Failed to parse publish response, but publish succeeded")]
    private partial void LogFailedToParsePublishResponse(Exception ex);

    [LoggerMessage(EventId = EventIds.FailedToPublishVersion, Level = LogLevel.Error, Message = "Failed to publish version {Version} for {Name}: {Error}")]
    private partial void LogFailedToPublishVersion(string version, string name, string error);

    [LoggerMessage(EventId = EventIds.ErrorPublishingVersion, Level = LogLevel.Error, Message = "Error publishing version {Version} for configuration {Name}")]
    private partial void LogErrorPublishingVersion(Exception ex, string version, string name);

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
