// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;

using NuGet.Versioning;

using OpenDsc.Server.Data;
using OpenDsc.Server.Endpoints;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public interface IParameterApiClient
{
    Task<(bool Success, string? ErrorMessage)> CreateOrUpdateParameterAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version,
        string content,
        bool isDraft);

    Task<List<ParameterFileDto>> GetParameterVersionsAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue);

    Task<(bool Success, string? ErrorMessage)> ActivateParameterVersionAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version);

    Task<(bool Success, string? ErrorMessage)> DeleteParameterVersionAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version);

    Task<ParameterProvenanceDto?> GetNodeParameterProvenanceAsync(Guid nodeId, Guid configurationId);

    Task<(bool Success, string? ErrorMessage)> UpdateParameterDraftAsync(Guid parameterId, string content);

    Task<string?> GetParameterContentAsync(Guid parameterId);

    Task<List<int>> GetAvailableMajorVersionsAsync(Guid configurationId);
}

public sealed class ParameterApiClient : IParameterApiClient
{
    private readonly ServerDbContext _db;
    private readonly IConfiguration _config;
    private readonly IResourceAuthorizationService _authService;
    private readonly IUserContextService _userContext;
    private readonly IParameterMergeService _parameterMergeService;
    private readonly IParameterValidator _validator;
    private readonly ILogger<ParameterApiClient> _logger;

    public ParameterApiClient(
        ServerDbContext db,
        IConfiguration config,
        IResourceAuthorizationService authService,
        IUserContextService userContext,
        IParameterMergeService parameterMergeService,
        IParameterValidator validator,
        ILogger<ParameterApiClient> logger)
    {
        _db = db;
        _config = config;
        _authService = authService;
        _userContext = userContext;
        _parameterMergeService = parameterMergeService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateOrUpdateParameterAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version,
        string content,
        bool isDraft)
    {
        try
        {
            var scopeType = await _db.ScopeTypes.FirstOrDefaultAsync(st => st.Id == scopeTypeId);
            if (scopeType == null)
            {
                return (false, "Scope type not found");
            }

            var configuration = await _db.Configurations.FirstOrDefaultAsync(c => c.Id == configurationId);
            if (configuration == null)
            {
                return (false, "Configuration not found");
            }

            if (scopeType.Name == "Default")
            {
                if (!string.IsNullOrWhiteSpace(scopeValue))
                {
                    return (false, "The 'Default' scope type does not accept a scope value.");
                }
            }
            else if (scopeType.Name == "Node")
            {
                if (string.IsNullOrWhiteSpace(scopeValue))
                {
                    return (false, "Scope type 'Node' requires a node FQDN as the scope value.");
                }

                var nodeExists = await _db.Nodes.AnyAsync(n => n.Fqdn == scopeValue);
                if (!nodeExists)
                {
                    return (false, $"Node '{scopeValue}' is not registered.");
                }
            }
            else if (scopeType.ValueMode == ScopeValueMode.Restricted)
            {
                if (string.IsNullOrWhiteSpace(scopeValue))
                {
                    return (false, $"Scope type '{scopeType.Name}' is restricted and requires a scope value.");
                }

                var scopeValueExists = await _db.ScopeValues
                    .AnyAsync(sv => sv.ScopeTypeId == scopeTypeId && sv.Value == scopeValue);

                if (!scopeValueExists)
                {
                    return (false, $"Scope value '{scopeValue}' does not exist for scope type '{scopeType.Name}'.");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(scopeValue))
                {
                    return (false, $"Scope type '{scopeType.Name}' requires a scope value.");
                }
            }

            if (!SemanticVersion.TryParse(version, out var semVer))
            {
                return (false, $"Version '{version}' is not a valid semantic version");
            }

            int majorVersion = semVer.Major;

            var userId = _userContext.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return (false, "User not authenticated");
            }

            var allSchemas = await _db.ParameterSchemas
                .Where(ps => ps.ConfigurationId == configurationId)
                .ToListAsync();

            var parameterSchema = allSchemas.FirstOrDefault(ps =>
                !string.IsNullOrEmpty(ps.SchemaVersion) &&
                SemanticVersion.TryParse(ps.SchemaVersion, out var sv) &&
                sv.Major == majorVersion);

            if (parameterSchema == null)
            {
                return (false, $"No parameter schema exists for major version {majorVersion}. " +
                               $"Please create a configuration version {majorVersion}.0.0 with a parameter schema first.");
            }

            if (!await _authService.CanModifyParameterAsync(userId.Value, parameterSchema.Id))
            {
                return (false, "Access denied");
            }

            if (!string.IsNullOrWhiteSpace(parameterSchema.GeneratedJsonSchema))
            {
                var validationResult = _validator.Validate(parameterSchema.GeneratedJsonSchema, content);
                if (!validationResult.IsValid)
                {
                    var errorMessages = string.Join("; ", validationResult.Errors?.Select(e => $"{e.Path}: {e.Message}") ?? []);
                    return (false, $"Parameter validation failed: {errorMessages}");
                }
            }

            var dataDir = _config["DataDirectory"] ?? "data";
            var filePath = !string.IsNullOrWhiteSpace(scopeValue)
                ? Path.Combine(dataDir, "parameters", configuration.Name, scopeType.Name, scopeValue, $"v{version}", "parameters.yaml")
                : Path.Combine(dataDir, "parameters", configuration.Name, scopeType.Name, $"v{version}", "parameters.yaml");

            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            await File.WriteAllTextAsync(filePath, content);

            var checksum = ComputeChecksum(content);

            var existingFile = await _db.ParameterFiles
                .FirstOrDefaultAsync(pf =>
                    pf.ParameterSchemaId == parameterSchema.Id &&
                    pf.ScopeTypeId == scopeTypeId &&
                    pf.ScopeValue == scopeValue &&
                    pf.Version == version);

            if (existingFile != null)
            {
                if (!existingFile.IsDraft)
                {
                    return (false, "Cannot modify a published parameter version. Published versions are immutable. Create a new version instead.");
                }

                // Only allow editing draft versions
                existingFile.Checksum = checksum;
                existingFile.IsDraft = isDraft;
            }
            else
            {
                var parameterFile = new ParameterFile
                {
                    Id = Guid.NewGuid(),
                    ScopeTypeId = scopeTypeId,
                    ParameterSchemaId = parameterSchema.Id,
                    ScopeValue = scopeValue,
                    Version = version,
                    MajorVersion = majorVersion,
                    ContentType = "application/x-yaml",
                    Checksum = checksum,
                    IsActive = false,
                    IsDraft = isDraft,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _db.ParameterFiles.Add(parameterFile);
            }

            await _db.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating parameter");
            return (false, ex.Message);
        }
    }

    public async Task<List<ParameterFileDto>> GetParameterVersionsAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue)
    {
        var files = await _db.ParameterFiles
            .Where(pf => pf.ParameterSchema!.ConfigurationId == configurationId &&
                         pf.ScopeTypeId == scopeTypeId &&
                         pf.ScopeValue == scopeValue)
            .Select(pf => new ParameterFileDto
            {
                Id = pf.Id,
                ScopeTypeId = scopeTypeId,
                ConfigurationId = configurationId,
                ScopeValue = pf.ScopeValue,
                Version = pf.Version,
                Checksum = pf.Checksum,
                IsActive = pf.IsActive,
                IsDraft = pf.IsDraft,
                IsArchived = pf.IsArchived,
                MajorVersion = pf.MajorVersion,
                CreatedAt = pf.CreatedAt
            })
            .ToListAsync();

        return files.OrderByDescending(f => f.CreatedAt).ToList();
    }

    public async Task<(bool Success, string? ErrorMessage)> ActivateParameterVersionAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return (false, "User not authenticated");
            }

            var parameterFile = await _db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                .FirstOrDefaultAsync(pf =>
                    pf.ParameterSchema!.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == scopeTypeId &&
                    pf.ScopeValue == scopeValue &&
                    pf.Version == version);

            if (parameterFile == null)
            {
                return (false, "Parameter version not found");
            }

            if (!await _authService.CanModifyParameterAsync(userId.Value, parameterFile.ParameterSchemaId))
            {
                return (false, "Access denied");
            }

            var activeFiles = await _db.ParameterFiles
                .Where(pf =>
                    pf.ParameterSchema!.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == scopeTypeId &&
                    pf.ScopeValue == scopeValue &&
                    pf.MajorVersion == parameterFile.MajorVersion &&
                    pf.IsActive)
                .ToListAsync();

            foreach (var file in activeFiles)
            {
                file.IsActive = false;
                file.IsArchived = true;
            }

            parameterFile.IsActive = true;
            parameterFile.IsDraft = false;

            await _db.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating parameter version");
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteParameterVersionAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return (false, "User not authenticated");
            }

            var parameterFile = await _db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                .FirstOrDefaultAsync(pf =>
                    pf.ParameterSchema!.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == scopeTypeId &&
                    pf.ScopeValue == scopeValue &&
                    pf.Version == version);

            if (parameterFile == null)
            {
                return (false, "Parameter version not found");
            }

            if (!await _authService.CanManageParameterAsync(userId.Value, parameterFile.ParameterSchemaId))
            {
                return (false, "Access denied");
            }

            if (parameterFile.IsActive)
            {
                return (false, "Cannot delete active parameter version");
            }

            _db.ParameterFiles.Remove(parameterFile);
            await _db.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting parameter version");
            return (false, ex.Message);
        }
    }

    public async Task<ParameterProvenanceDto?> GetNodeParameterProvenanceAsync(Guid nodeId, Guid configurationId)
    {
        try
        {
            var node = await _db.Nodes
                .FirstOrDefaultAsync(n => n.Id == nodeId);

            if (node == null)
            {
                return null;
            }

            var result = await _parameterMergeService.MergeParametersAsync(nodeId, configurationId);
            if (result == null)
            {
                return null;
            }

            return new ParameterProvenanceDto
            {
                NodeId = nodeId,
                ConfigurationId = configurationId,
                MergedParameters = result,
                Provenance = new Dictionary<string, ParameterSourceInfo>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parameter provenance for node {NodeId}", nodeId);
            return null;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateParameterDraftAsync(Guid parameterId, string content)
    {
        try
        {
            var userId = _userContext.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return (false, "User not authenticated");
            }

            var parameterFile = await _db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                    .ThenInclude(ps => ps.Configuration)
                .Include(pf => pf.ScopeType)
                .FirstOrDefaultAsync(pf => pf.Id == parameterId);

            if (parameterFile == null)
            {
                return (false, "Parameter version not found");
            }

            if (!parameterFile.IsDraft)
            {
                return (false, "Only draft versions can be edited in place.");
            }

            if (!await _authService.CanModifyParameterAsync(userId.Value, parameterFile.ParameterSchemaId))
            {
                return (false, "Access denied");
            }

            var dataDir = _config["DataDirectory"] ?? "data";
            var filePath = !string.IsNullOrWhiteSpace(parameterFile.ScopeValue)
                ? Path.Combine(dataDir, "parameters", parameterFile.ParameterSchema.Configuration.Name,
                    parameterFile.ScopeType.Name, parameterFile.ScopeValue, $"v{parameterFile.Version}", "parameters.yaml")
                : Path.Combine(dataDir, "parameters", parameterFile.ParameterSchema.Configuration.Name,
                    parameterFile.ScopeType.Name, $"v{parameterFile.Version}", "parameters.yaml");

            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            await File.WriteAllTextAsync(filePath, content);

            parameterFile.Checksum = ComputeChecksum(content);
            await _db.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating draft parameter {ParameterId}", parameterId);
            return (false, ex.Message);
        }
    }

    public async Task<string?> GetParameterContentAsync(Guid parameterId)
    {
        try
        {
            var parameterFile = await _db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                    .ThenInclude(ps => ps.Configuration)
                .Include(pf => pf.ScopeType)
                .FirstOrDefaultAsync(pf => pf.Id == parameterId);

            if (parameterFile == null)
            {
                return null;
            }

            var dataDir = _config["DataDirectory"] ?? "data";
            var filePath = !string.IsNullOrWhiteSpace(parameterFile.ScopeValue)
                ? Path.Combine(dataDir, "parameters", parameterFile.ParameterSchema.Configuration.Name,
                    parameterFile.ScopeType.Name, parameterFile.ScopeValue, $"v{parameterFile.Version}", "parameters.yaml")
                : Path.Combine(dataDir, "parameters", parameterFile.ParameterSchema.Configuration.Name,
                    parameterFile.ScopeType.Name, $"v{parameterFile.Version}", "parameters.yaml");

            if (!File.Exists(filePath))
            {
                return null;
            }

            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading parameter content for {ParameterId}", parameterId);
            return null;
        }
    }

    public async Task<List<int>> GetAvailableMajorVersionsAsync(Guid configurationId)
    {
        var schemaVersions = await _db.ParameterSchemas
            .Where(ps => ps.ConfigurationId == configurationId && ps.SchemaVersion != null)
            .Select(ps => ps.SchemaVersion!)
            .ToListAsync();

        return schemaVersions
            .Where(v => SemanticVersion.TryParse(v, out _))
            .Select(v => SemanticVersion.Parse(v).Major)
            .Distinct()
            .OrderBy(m => m)
            .ToList();
    }

    private static string ComputeChecksum(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

