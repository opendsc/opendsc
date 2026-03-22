// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NuGet.Versioning;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public interface IParameterService
{
    Task<(bool Success, string? ErrorMessage)> CreateOrUpdateParameterAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version,
        string? content,
        bool isPassthrough = false);

    Task<List<ParameterFileDto>> GetParameterVersionsAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue);

    Task<(bool Success, string? ErrorMessage)> PublishParameterVersionAsync(
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

public sealed partial class ParameterService : IParameterService
{
    private readonly ServerDbContext _db;
    private readonly IOptions<ServerConfig> _serverConfig;
    private readonly IResourceAuthorizationService _authService;
    private readonly IUserContextService _userContext;
    private readonly IParameterMergeService _parameterMergeService;
    private readonly IParameterValidator _validator;
    private readonly ILogger<ParameterService> _logger;

    public ParameterService(
        ServerDbContext db,
        IOptions<ServerConfig> serverConfig,
        IResourceAuthorizationService authService,
        IUserContextService userContext,
        IParameterMergeService parameterMergeService,
        IParameterValidator validator,
        ILogger<ParameterService> logger)
    {
        _db = db;
        _serverConfig = serverConfig;
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
        string? content,
        bool isPassthrough = false)
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

            if (!isPassthrough && !string.IsNullOrWhiteSpace(parameterSchema.GeneratedJsonSchema))
            {
                var validationResult = _validator.Validate(parameterSchema.GeneratedJsonSchema, content!);
                if (!validationResult.IsValid)
                {
                    var errorMessages = string.Join("; ", validationResult.Errors?.Select(e => $"{e.Path}: {e.Message}") ?? []);
                    return (false, $"Parameter validation failed: {errorMessages}");
                }
            }

            var dataDir = _serverConfig.Value.ParametersDirectory;

            if (!isPassthrough)
            {
                var filePath = !string.IsNullOrWhiteSpace(scopeValue)
                    ? Path.Combine(dataDir, configuration.Name, scopeType.Name, scopeValue, $"v{version}", "parameters.yaml")
                    : Path.Combine(dataDir, configuration.Name, scopeType.Name, $"v{version}", "parameters.yaml");

                var fileDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
                {
                    Directory.CreateDirectory(fileDir);
                }

                await File.WriteAllTextAsync(filePath, content!);
            }

            var checksum = isPassthrough ? "passthrough" : ComputeChecksum(content!);

            var existingFile = await _db.ParameterFiles
                .FirstOrDefaultAsync(pf =>
                    pf.ParameterSchemaId == parameterSchema.Id &&
                    pf.ScopeTypeId == scopeTypeId &&
                    pf.ScopeValue == scopeValue &&
                    pf.Version == version);

            if (existingFile != null)
            {
                if (existingFile.Status != ParameterVersionStatus.Draft)
                {
                    return (false, "Cannot modify a published parameter version. Published versions are immutable. Create a new version instead.");
                }

                existingFile.Checksum = checksum;
                existingFile.IsPassthrough = isPassthrough;
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
                    Status = ParameterVersionStatus.Draft,
                    IsPassthrough = isPassthrough,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _db.ParameterFiles.Add(parameterFile);
            }

            await _db.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            LogErrorCreatingUpdatingParameter(ex);
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
                Status = pf.Status,
                IsPassthrough = pf.IsPassthrough,
                MajorVersion = pf.MajorVersion,
                CreatedAt = pf.CreatedAt
            })
            .ToListAsync();

        return files.OrderByDescending(f => f.CreatedAt).ToList();
    }

    public async Task<(bool Success, string? ErrorMessage)> PublishParameterVersionAsync(
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

            parameterFile.Status = ParameterVersionStatus.Published;

            await _db.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            LogErrorPublishingParameterVersion(ex);
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

            _db.ParameterFiles.Remove(parameterFile);
            await _db.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            LogErrorDeletingParameterVersion(ex);
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

            var result = await _parameterMergeService.MergeParametersWithProvenanceAsync(nodeId, configurationId);
            if (result == null)
            {
                return null;
            }

            var provenance = result.Provenance.ToDictionary(
                kvp => kvp.Key,
                kvp => new ParameterSourceInfo
                {
                    ScopeTypeName = kvp.Value.ScopeTypeName,
                    ScopeValue = kvp.Value.ScopeValue,
                    Precedence = kvp.Value.Precedence,
                    Value = kvp.Value.Value,
                    OverriddenBy = kvp.Value.OverriddenValues?.Select(ov => new ScopeInfo
                    {
                        ScopeTypeName = ov.ScopeTypeName,
                        ScopeValue = ov.ScopeValue,
                        Precedence = ov.Precedence,
                        Value = ov.Value
                    }).ToList()
                });

            return new ParameterProvenanceDto
            {
                NodeId = nodeId,
                ConfigurationId = configurationId,
                MergedParameters = result.MergedContent,
                Provenance = provenance
            };
        }
        catch (Exception ex)
        {
            LogErrorGettingParameterProvenance(ex, nodeId);
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

            if (parameterFile.Status != ParameterVersionStatus.Draft)
            {
                return (false, "Only draft versions can be edited in place.");
            }

            if (!await _authService.CanModifyParameterAsync(userId.Value, parameterFile.ParameterSchemaId))
            {
                return (false, "Access denied");
            }

            var dataDir = _serverConfig.Value.ParametersDirectory;
            var filePath = !string.IsNullOrWhiteSpace(parameterFile.ScopeValue)
                ? Path.Combine(dataDir, parameterFile.ParameterSchema.Configuration.Name,
                    parameterFile.ScopeType.Name, parameterFile.ScopeValue, $"v{parameterFile.Version}", "parameters.yaml")
                : Path.Combine(dataDir, parameterFile.ParameterSchema.Configuration.Name,
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
            LogErrorUpdatingDraftParameter(ex, parameterId);
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

            var dataDir = _serverConfig.Value.ParametersDirectory;
            var filePath = !string.IsNullOrWhiteSpace(parameterFile.ScopeValue)
                ? Path.Combine(dataDir, parameterFile.ParameterSchema.Configuration.Name,
                    parameterFile.ScopeType.Name, parameterFile.ScopeValue, $"v{parameterFile.Version}", "parameters.yaml")
                : Path.Combine(dataDir, parameterFile.ParameterSchema.Configuration.Name,
                    parameterFile.ScopeType.Name, $"v{parameterFile.Version}", "parameters.yaml");

            if (!File.Exists(filePath))
            {
                return null;
            }

            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            LogErrorReadingParameterContent(ex, parameterId);
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

    [LoggerMessage(EventId = EventIds.ErrorCreatingUpdatingParameter, Level = LogLevel.Error, Message = "Error creating/updating parameter")]
    private partial void LogErrorCreatingUpdatingParameter(Exception ex);

    [LoggerMessage(EventId = EventIds.ErrorPublishingParameterVersion, Level = LogLevel.Error, Message = "Error publishing parameter version")]
    private partial void LogErrorPublishingParameterVersion(Exception ex);

    [LoggerMessage(EventId = EventIds.ErrorDeletingParameterVersion, Level = LogLevel.Error, Message = "Error deleting parameter version")]
    private partial void LogErrorDeletingParameterVersion(Exception ex);

    [LoggerMessage(EventId = EventIds.ErrorGettingParameterProvenance, Level = LogLevel.Error, Message = "Error getting parameter provenance for node {NodeId}")]
    private partial void LogErrorGettingParameterProvenance(Exception ex, Guid nodeId);

    [LoggerMessage(EventId = EventIds.ErrorUpdatingDraftParameter, Level = LogLevel.Error, Message = "Error updating draft parameter {ParameterId}")]
    private partial void LogErrorUpdatingDraftParameter(Exception ex, Guid parameterId);

    [LoggerMessage(EventId = EventIds.ErrorReadingParameterContent, Level = LogLevel.Error, Message = "Error reading parameter content for {ParameterId}")]
    private partial void LogErrorReadingParameterContent(Exception ex, Guid parameterId);
}

public sealed class ParameterFileDto
{
    public required Guid Id { get; init; }
    public required Guid ScopeTypeId { get; init; }
    public required Guid ConfigurationId { get; init; }
    public string? ScopeValue { get; init; }
    public required string Version { get; init; }
    public required int MajorVersion { get; init; }
    public required string Checksum { get; init; }
    public required ParameterVersionStatus Status { get; init; }
    public required bool IsPassthrough { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class ParameterProvenanceDto
{
    public required Guid NodeId { get; init; }
    public required Guid ConfigurationId { get; init; }
    public required string MergedParameters { get; init; }
    public required Dictionary<string, ParameterSourceInfo> Provenance { get; init; }
    public string? PrereleaseChannel { get; init; }
}

public sealed class ParameterSourceInfo
{
    public required string ScopeTypeName { get; init; }
    public string? ScopeValue { get; init; }
    public required int Precedence { get; init; }
    public required object? Value { get; init; }
    public List<ScopeInfo>? OverriddenBy { get; init; }
    public string? ResolvedVersion { get; init; }
    public bool IsPrerelease { get; init; }
}

public sealed class ScopeInfo
{
    public required string ScopeTypeName { get; init; }
    public string? ScopeValue { get; init; }
    public required int Precedence { get; init; }
    public required object? Value { get; init; }
}

