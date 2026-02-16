// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;

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

    Task<string?> GetParameterContentAsync(Guid parameterId);
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

            if (scopeType.ValueMode == ScopeValueMode.Restricted && string.IsNullOrWhiteSpace(scopeValue))
            {
                return (false, $"Scope type '{scopeType.Name}' is restricted and requires a scope value");
            }

            if (scopeType.ValueMode == ScopeValueMode.Restricted && !string.IsNullOrWhiteSpace(scopeValue))
            {
                var scopeValueExists = await _db.ScopeValues
                    .AnyAsync(sv => sv.ScopeTypeId == scopeTypeId && sv.Value == scopeValue);

                if (!scopeValueExists)
                {
                    return (false, $"Scope value '{scopeValue}' does not exist for scope type '{scopeType.Name}'");
                }
            }

            var userId = _userContext.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return (false, "User not authenticated");
            }

            var parameterSchema = await _db.ParameterSchemas
                .FirstOrDefaultAsync(ps => ps.ConfigurationId == configurationId);

            if (parameterSchema != null)
            {
                if (!await _authService.CanModifyParameterAsync(userId.Value, parameterSchema.Id))
                {
                    return (false, "Access denied");
                }

                // Validate against schema if available
                if (!string.IsNullOrWhiteSpace(parameterSchema.GeneratedJsonSchema))
                {
                    var validationResult = _validator.Validate(parameterSchema.GeneratedJsonSchema, content);
                    if (!validationResult.IsValid)
                    {
                        var errorMessages = string.Join("; ", validationResult.Errors?.Select(e => $"{e.Path}: {e.Message}") ?? []);
                        return (false, $"Parameter validation failed: {errorMessages}");
                    }
                }
            }
            else
            {
                if (!await _authService.CanModifyConfigurationAsync(userId.Value, configurationId))
                {
                    return (false, "Access denied");
                }

                parameterSchema = new ParameterSchema
                {
                    Id = Guid.NewGuid(),
                    ConfigurationId = configurationId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                _db.ParameterSchemas.Add(parameterSchema);
                await _db.SaveChangesAsync();

                await _authService.GrantParameterPermissionAsync(
                    parameterSchema.Id,
                    userId.Value,
                    PrincipalType.User,
                    ResourcePermission.Manage,
                    userId.Value);
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
        var parameterSchema = await _db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configurationId);

        if (parameterSchema == null)
        {
            return [];
        }

        var files = await _db.ParameterFiles
            .Where(pf => pf.ParameterSchemaId == parameterSchema.Id &&
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

            var parameterSchema = await _db.ParameterSchemas
                .FirstOrDefaultAsync(ps => ps.ConfigurationId == configurationId);

            if (parameterSchema == null)
            {
                return (false, "Parameter schema not found");
            }

            if (!await _authService.CanModifyParameterAsync(userId.Value, parameterSchema.Id))
            {
                return (false, "Access denied");
            }

            var parameterFile = await _db.ParameterFiles
                .FirstOrDefaultAsync(pf =>
                    pf.ParameterSchemaId == parameterSchema.Id &&
                    pf.ScopeTypeId == scopeTypeId &&
                    pf.ScopeValue == scopeValue &&
                    pf.Version == version);

            if (parameterFile == null)
            {
                return (false, "Parameter version not found");
            }

            var activeFiles = await _db.ParameterFiles
                .Where(pf =>
                    pf.ParameterSchemaId == parameterSchema.Id &&
                    pf.ScopeTypeId == scopeTypeId &&
                    pf.ScopeValue == scopeValue &&
                    pf.IsActive)
                .ToListAsync();

            foreach (var file in activeFiles)
            {
                file.IsActive = false;
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

            var parameterSchema = await _db.ParameterSchemas
                .FirstOrDefaultAsync(ps => ps.ConfigurationId == configurationId);

            if (parameterSchema == null)
            {
                return (false, "Parameter schema not found");
            }

            if (!await _authService.CanManageParameterAsync(userId.Value, parameterSchema.Id))
            {
                return (false, "Access denied");
            }

            var parameterFile = await _db.ParameterFiles
                .FirstOrDefaultAsync(pf =>
                    pf.ParameterSchemaId == parameterSchema.Id &&
                    pf.ScopeTypeId == scopeTypeId &&
                    pf.ScopeValue == scopeValue &&
                    pf.Version == version);

            if (parameterFile == null)
            {
                return (false, "Parameter version not found");
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

    private static string ComputeChecksum(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

