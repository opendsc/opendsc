// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public interface IConfigurationApiClient
{
    Task<bool> CreateConfigurationAsync(string name, string? description, string entryPoint, string version, bool isDraft, IReadOnlyList<IBrowserFile> files);
    Task<bool> CreateVersionAsync(string name, string version, bool isDraft, IReadOnlyList<IBrowserFile> files);
    Task<bool> PublishVersionAsync(string name, string version);
    Task<bool> DeleteConfigurationAsync(string name);
    Task<bool> DeleteVersionAsync(string name, string version);
    Task<Stream?> DownloadFileAsync(string name, string version, string filePath);
}

public sealed class ConfigurationApiClient : IConfigurationApiClient
{
    private readonly ServerDbContext _db;
    private readonly IConfiguration _config;
    private readonly IResourceAuthorizationService _authService;
    private readonly IUserContextService _userContext;
    private readonly ILogger<ConfigurationApiClient> _logger;

    public ConfigurationApiClient(
        ServerDbContext db,
        IConfiguration config,
        IResourceAuthorizationService authService,
        IUserContextService userContext,
        ILogger<ConfigurationApiClient> logger)
    {
        _db = db;
        _config = config;
        _authService = authService;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<bool> CreateConfigurationAsync(
        string name,
        string? description,
        string entryPoint,
        string version,
        bool isDraft,
        IReadOnlyList<IBrowserFile> files)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Configuration name is required");
                return false;
            }

            if (files.Count == 0)
            {
                _logger.LogWarning("At least one file is required");
                return false;
            }

            if (await _db.Configurations.AnyAsync(c => c.Name == name))
            {
                _logger.LogWarning("Configuration '{Name}' already exists", name);
                return false;
            }

            if (!files.Any(f => f.Name == entryPoint))
            {
                _logger.LogWarning("Entry point file '{EntryPoint}' not found in uploaded files", entryPoint);
                return false;
            }

            var configuration = new Configuration
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                EntryPoint = entryPoint,
                IsServerManaged = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Configurations.Add(configuration);

            var configVersion = new ConfigurationVersion
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configuration.Id,
                Version = version,
                IsDraft = isDraft,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.ConfigurationVersions.Add(configVersion);

            var dataDir = _config["DataDirectory"] ?? "data";
            var versionDir = Path.Combine(dataDir, "configurations", name, $"v{version}");
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

            var userId = _userContext.GetCurrentUserId();
            if (userId.HasValue)
            {
                await _authService.GrantConfigurationPermissionAsync(
                    configuration.Id,
                    userId.Value,
                    PrincipalType.User,
                    ResourcePermission.Manage,
                    userId.Value);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating configuration '{Name}'", name);
            return false;
        }
    }

    public async Task<bool> CreateVersionAsync(
        string name,
        string version,
        bool isDraft,
        IReadOnlyList<IBrowserFile> files)
    {
        try
        {
            var configuration = await _db.Configurations
                .FirstOrDefaultAsync(c => c.Name == name);

            if (configuration == null)
            {
                _logger.LogWarning("Configuration '{Name}' not found", name);
                return false;
            }

            if (await _db.ConfigurationVersions.AnyAsync(v => v.ConfigurationId == configuration.Id && v.Version == version))
            {
                _logger.LogWarning("Version '{Version}' already exists for configuration '{Name}'", version, name);
                return false;
            }

            var configVersion = new ConfigurationVersion
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configuration.Id,
                Version = version,
                IsDraft = isDraft,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.ConfigurationVersions.Add(configVersion);

            var dataDir = _config["DataDirectory"] ?? "data";
            var versionDir = Path.Combine(dataDir, "configurations", name, $"v{version}");
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

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating version '{Version}' for configuration '{Name}'", version, name);
            return false;
        }
    }

    public async Task<bool> PublishVersionAsync(string name, string version)
    {
        try
        {
            var configuration = await _db.Configurations
                .Include(c => c.Versions)
                .FirstOrDefaultAsync(c => c.Name == name);

            if (configuration == null)
            {
                _logger.LogWarning("Configuration '{Name}' not found", name);
                return false;
            }

            var configVersion = configuration.Versions
                .FirstOrDefault(v => v.Version == version);

            if (configVersion == null)
            {
                _logger.LogWarning("Version '{Version}' not found for configuration '{Name}'", version, name);
                return false;
            }

            if (!configVersion.IsDraft)
            {
                _logger.LogWarning("Version '{Version}' is already published", version);
                return false;
            }

            configVersion.IsDraft = false;
            configuration.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing version '{Version}' for configuration '{Name}'", version, name);
            return false;
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
                _logger.LogWarning("Configuration '{Name}' not found", name);
                return false;
            }

            var dataDir = _config["DataDirectory"] ?? "data";
            var configDir = Path.Combine(dataDir, "configurations", name);
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
            _logger.LogError(ex, "Error deleting configuration '{Name}'", name);
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
                _logger.LogWarning("Configuration '{Name}' not found", name);
                return false;
            }

            var configVersion = configuration.Versions
                .FirstOrDefault(v => v.Version == version);

            if (configVersion == null)
            {
                _logger.LogWarning("Version '{Version}' not found for configuration '{Name}'", version, name);
                return false;
            }

            var dataDir = _config["DataDirectory"] ?? "data";
            var versionDir = Path.Combine(dataDir, "configurations", name, $"v{version}");
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
            _logger.LogError(ex, "Error deleting version '{Version}' for configuration '{Name}'", version, name);
            return false;
        }
    }

    public async Task<Stream?> DownloadFileAsync(string name, string version, string filePath)
    {
        try
        {
            var dataDir = _config["DataDirectory"] ?? "data";
            var fullPath = Path.Combine(dataDir, "configurations", name, $"v{version}", filePath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found: {FilePath}", fullPath);
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
            _logger.LogError(ex, "Error downloading file '{FilePath}' from configuration '{Name}' version '{Version}'", filePath, name, version);
            return null;
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
}
