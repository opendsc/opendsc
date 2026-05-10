// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NuGet.Versioning;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Parameters;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

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

    public async Task<ParameterVersionDetails> CreateAsync(
        Guid scopeTypeId,
        Guid configurationId,
        CreateParameterRequest request,
        CancellationToken cancellationToken = default)
    {
        var scopeValue = request.ScopeValue;
        var version = request.Version;
        var content = request.Content;
        var isPassthrough = request.IsPassthrough == true;

        var scopeType = await _db.ScopeTypes.FirstOrDefaultAsync(st => st.Id == scopeTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("Scope type not found.");

        var configuration = await _db.Configurations.FirstOrDefaultAsync(c => c.Id == configurationId, cancellationToken)
            ?? throw new KeyNotFoundException("Configuration not found.");

        if (scopeType.Name == "Default")
        {
            if (!string.IsNullOrWhiteSpace(scopeValue))
                throw new ArgumentException("The 'Default' scope type does not accept a scope value.");
        }
        else if (scopeType.Name == "Node")
        {
            if (string.IsNullOrWhiteSpace(scopeValue))
                throw new ArgumentException("Scope type 'Node' requires a node FQDN as the scope value.");

            var nodeExists = await _db.Nodes.AnyAsync(n => n.Fqdn == scopeValue, cancellationToken);
            if (!nodeExists)
                throw new KeyNotFoundException($"Node '{scopeValue}' is not registered.");
        }
        else if (scopeType.ValueMode == ScopeValueMode.Restricted)
        {
            if (string.IsNullOrWhiteSpace(scopeValue))
                throw new ArgumentException($"Scope type '{scopeType.Name}' is restricted and requires a scope value.");

            var scopeValueExists = await _db.ScopeValues
                .AnyAsync(sv => sv.ScopeTypeId == scopeTypeId && sv.Value == scopeValue, cancellationToken);

            if (!scopeValueExists)
                throw new KeyNotFoundException($"Scope value '{scopeValue}' does not exist for scope type '{scopeType.Name}'.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(scopeValue))
                throw new ArgumentException($"Scope type '{scopeType.Name}' requires a scope value.");
        }

        if (!SemanticVersion.TryParse(version, out var semVer))
            throw new ArgumentException($"Version '{version}' is not a valid semantic version.");

        int majorVersion = semVer.Major;

        var userId = _userContext.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var allSchemas = await _db.ParameterSchemas
            .Where(ps => ps.ConfigurationId == configurationId)
            .ToListAsync(cancellationToken);

        var parameterSchema = allSchemas.FirstOrDefault(ps =>
            !string.IsNullOrEmpty(ps.SchemaVersion) &&
            SemanticVersion.TryParse(ps.SchemaVersion, out var sv) &&
            sv.Major == majorVersion)
            ?? throw new InvalidOperationException(
                $"No parameter schema exists for major version {majorVersion}. " +
                $"Please create a configuration version {majorVersion}.0.0 with a parameter schema first.");

        if (!await _authService.CanModifyParameterAsync(userId, parameterSchema.Id))
            throw new UnauthorizedAccessException("Access denied.");

        if (!isPassthrough && !string.IsNullOrWhiteSpace(parameterSchema.GeneratedJsonSchema))
        {
            var validationResult = _validator.Validate(parameterSchema.GeneratedJsonSchema, content!);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors?.Select(e => $"{e.Path}: {e.Message}") ?? []);
                throw new ArgumentException($"Parameter validation failed: {errorMessages}");
            }
        }

        var existingFile = await _db.ParameterFiles
            .FirstOrDefaultAsync(pf =>
                pf.ParameterSchemaId == parameterSchema.Id &&
                pf.ScopeTypeId == scopeTypeId &&
                pf.ScopeValue == scopeValue &&
                pf.Version == version, cancellationToken);

        if (existingFile != null)
            throw new InvalidOperationException(
                $"Parameter version '{version}' already exists. Use UpdateParameterVersionAsync to modify a draft.");

        var dataDir = _serverConfig.Value.ParametersDirectory;

        if (!isPassthrough)
        {
            var filePath = !string.IsNullOrWhiteSpace(scopeValue)
                ? Path.Combine(dataDir, configuration.Name, scopeType.Name, scopeValue, $"v{version}", "parameters.yaml")
                : Path.Combine(dataDir, configuration.Name, scopeType.Name, $"v{version}", "parameters.yaml");

            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
                Directory.CreateDirectory(fileDir);

            await File.WriteAllTextAsync(filePath, content!, cancellationToken);
        }

        var checksum = isPassthrough ? "passthrough" : ComputeChecksum(content!);

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
        await _db.SaveChangesAsync(cancellationToken);

        return new ParameterVersionDetails
        {
            Id = parameterFile.Id,
            ScopeTypeId = scopeTypeId,
            ConfigurationId = configurationId,
            ScopeValue = parameterFile.ScopeValue,
            Version = parameterFile.Version,
            Checksum = parameterFile.Checksum,
            Status = parameterFile.Status,
            IsPassthrough = parameterFile.IsPassthrough,
            MajorVersion = parameterFile.MajorVersion,
            CreatedAt = parameterFile.CreatedAt
        };
    }

    public async Task<IReadOnlyList<ParameterVersionDetails>> GetVersionsAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        CancellationToken cancellationToken = default)
    {
        var files = await _db.ParameterFiles
            .Where(pf => pf.ParameterSchema!.ConfigurationId == configurationId &&
                         pf.ScopeTypeId == scopeTypeId &&
                         pf.ScopeValue == scopeValue)
            .Select(pf => new ParameterVersionDetails
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
            .ToListAsync(cancellationToken);

        return files.OrderByDescending(f => f.CreatedAt).ToList();
    }

    public async Task PublishAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version,
        CancellationToken cancellationToken = default)
    {
        var userId = _userContext.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var parameterFile = await _db.ParameterFiles
            .Include(pf => pf.ParameterSchema)
            .FirstOrDefaultAsync(pf =>
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeTypeId == scopeTypeId &&
                pf.ScopeValue == scopeValue &&
                pf.Version == version, cancellationToken)
            ?? throw new KeyNotFoundException("Parameter version not found.");

        if (!await _authService.CanModifyParameterAsync(userId, parameterFile.ParameterSchemaId))
            throw new UnauthorizedAccessException("Access denied.");

        parameterFile.Status = ParameterVersionStatus.Published;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version,
        CancellationToken cancellationToken = default)
    {
        var userId = _userContext.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var parameterFile = await _db.ParameterFiles
            .Include(pf => pf.ParameterSchema)
            .FirstOrDefaultAsync(pf =>
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeTypeId == scopeTypeId &&
                pf.ScopeValue == scopeValue &&
                pf.Version == version, cancellationToken)
            ?? throw new KeyNotFoundException("Parameter version not found.");

        if (!await _authService.CanManageParameterAsync(userId, parameterFile.ParameterSchemaId))
            throw new UnauthorizedAccessException("Access denied.");

        _db.ParameterFiles.Remove(parameterFile);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ParameterProvenanceDetails?> GetNodeProvenanceAsync(Guid nodeId, Guid configurationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var node = await _db.Nodes
                .FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);

            if (node == null)
            {
                return null;
            }

            var nodeConfig = await _db.NodeConfigurations
                .FirstOrDefaultAsync(nc => nc.NodeId == nodeId, cancellationToken);

            var result = await _parameterMergeService.MergeParametersWithProvenanceAsync(nodeId, configurationId, cancellationToken);
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

            return new ParameterProvenanceDetails
            {
                NodeId = nodeId,
                ConfigurationId = configurationId,
                MergedParameters = result.MergedContent,
                Provenance = provenance,
                PrereleaseChannel = nodeConfig?.PrereleaseChannel
            };
        }
        catch (Exception ex)
        {
            LogErrorGettingParameterProvenance(ex, nodeId);
            return null;
        }
    }

    public async Task<ParameterResolutionDetails?> GetNodeResolutionAsync(Guid nodeId, Guid? configurationId = null, CancellationToken cancellationToken = default)
    {
        var node = await _db.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);
        if (node == null)
        {
            return null;
        }

        var nodeConfigRecord = await _db.NodeConfigurations
            .Include(nc => nc.Configuration)
            .FirstOrDefaultAsync(nc => nc.NodeId == nodeId, cancellationToken);

        if (!configurationId.HasValue)
        {
            if (nodeConfigRecord is null)
            {
                return null;
            }

            configurationId = nodeConfigRecord.ConfigurationId;
        }

        var prereleaseChannel = nodeConfigRecord?.PrereleaseChannel;

        var configuration = await _db.Configurations.FindAsync([configurationId!.Value], cancellationToken);
        if (configuration is null)
        {
            return null;
        }

        var scopes = new List<ScopeResolutionDetails>();

        var nodeTags = await _db.NodeTags
            .Include(nt => nt.ScopeValue)
            .ThenInclude(sv => sv.ScopeType)
            .Where(nt => nt.NodeId == nodeId)
            .OrderBy(nt => nt.ScopeValue.ScopeType.Precedence)
            .ToListAsync(cancellationToken);

        var taggedScopeTypeIds = new HashSet<Guid>(nodeTags.Select(nt => nt.ScopeValue.ScopeTypeId));

        var defaultScopeType = await _db.ScopeTypes.FirstOrDefaultAsync(st => st.Name == "Default", cancellationToken);
        if (defaultScopeType != null && !taggedScopeTypeIds.Contains(defaultScopeType.Id))
        {
            var defaultCandidates = await _db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                .Where(pf => pf.ParameterSchema!.ConfigurationId == configurationId && pf.ScopeTypeId == defaultScopeType.Id && pf.Status == ParameterVersionStatus.Published)
                .ToListAsync(cancellationToken);

            var resolved = VersionResolver.ResolveVersion(defaultCandidates, pf => pf.Version, majorVersion: null, prereleaseChannel);
            var version = resolved?.Version;
            var isPrerelease = version is not null && SemanticVersion.TryParse(version, out var sv) && sv.IsPrerelease;

            scopes.Add(new ScopeResolutionDetails
            {
                ScopeTypeName = "Default",
                ScopeValue = null,
                ResolvedVersion = version,
                IsPrerelease = isPrerelease,
                NoPublishedVersion = defaultCandidates.Count == 0
            });
        }

        foreach (var tag in nodeTags)
        {
            var tagCandidates = await _db.ParameterFiles
                .Where(pf => pf.ParameterSchema!.ConfigurationId == configurationId && pf.ScopeTypeId == tag.ScopeValue.ScopeTypeId && pf.ScopeValue == tag.ScopeValue.Value && pf.Status == ParameterVersionStatus.Published)
                .ToListAsync(cancellationToken);

            var resolved = VersionResolver.ResolveVersion(tagCandidates, pf => pf.Version, majorVersion: null, prereleaseChannel);
            var version = resolved?.Version;
            var isPrerelease = version is not null && SemanticVersion.TryParse(version, out var sv) && sv.IsPrerelease;

            scopes.Add(new ScopeResolutionDetails
            {
                ScopeTypeName = tag.ScopeValue.ScopeType.Name,
                ScopeValue = tag.ScopeValue.Value,
                ResolvedVersion = version,
                IsPrerelease = isPrerelease,
                NoPublishedVersion = tagCandidates.Count == 0
            });
        }

        var nodeScopeType = await _db.ScopeTypes.FirstOrDefaultAsync(st => st.Name == "Node", cancellationToken);
        if (nodeScopeType != null)
        {
            var nodeCandidates = await _db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                .Where(pf => pf.ParameterSchema!.ConfigurationId == configurationId && pf.ScopeTypeId == nodeScopeType.Id && pf.ScopeValue == node.Fqdn && pf.Status == ParameterVersionStatus.Published)
                .ToListAsync(cancellationToken);

            var resolved = VersionResolver.ResolveVersion(nodeCandidates, pf => pf.Version, majorVersion: null, prereleaseChannel);
            var version = resolved?.Version;
            var isPrerelease = version is not null && SemanticVersion.TryParse(version, out var sv) && sv.IsPrerelease;

            scopes.Add(new ScopeResolutionDetails
            {
                ScopeTypeName = "Node",
                ScopeValue = node.Fqdn,
                ResolvedVersion = version,
                IsPrerelease = isPrerelease,
                NoPublishedVersion = nodeCandidates.Count == 0
            });
        }

        return new ParameterResolutionDetails
        {
            NodeId = nodeId,
            ConfigurationId = configurationId.Value,
            PrereleaseChannel = prereleaseChannel,
            Scopes = scopes
        };
    }

    public async Task UpdateAsync(Guid parameterId, UpdateParameterRequest request, CancellationToken cancellationToken = default)
    {
        var content = request.Content;

        var userId = _userContext.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var parameterFile = await _db.ParameterFiles
            .Include(pf => pf.ParameterSchema)
                .ThenInclude(ps => ps.Configuration)
            .Include(pf => pf.ScopeType)
            .FirstOrDefaultAsync(pf => pf.Id == parameterId, cancellationToken)
            ?? throw new KeyNotFoundException("Parameter version not found.");

        if (parameterFile.Status != ParameterVersionStatus.Draft)
            throw new InvalidOperationException("Only draft versions can be edited in place.");

        if (!await _authService.CanModifyParameterAsync(userId, parameterFile.ParameterSchemaId))
            throw new UnauthorizedAccessException("Access denied.");

        var dataDir = _serverConfig.Value.ParametersDirectory;
        var filePath = !string.IsNullOrWhiteSpace(parameterFile.ScopeValue)
            ? Path.Combine(dataDir, parameterFile.ParameterSchema.Configuration.Name,
                parameterFile.ScopeType.Name, parameterFile.ScopeValue, $"v{parameterFile.Version}", "parameters.yaml")
            : Path.Combine(dataDir, parameterFile.ParameterSchema.Configuration.Name,
                parameterFile.ScopeType.Name, $"v{parameterFile.Version}", "parameters.yaml");

        var fileDir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
            Directory.CreateDirectory(fileDir);

        await File.WriteAllTextAsync(filePath, content, cancellationToken);

        parameterFile.Checksum = ComputeChecksum(content);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetContentAsync(Guid parameterId, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameterFile = await _db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                    .ThenInclude(ps => ps.Configuration)
                .Include(pf => pf.ScopeType)
                .FirstOrDefaultAsync(pf => pf.Id == parameterId, cancellationToken);

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

            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            LogErrorReadingParameterContent(ex, parameterId);
            return null;
        }
    }

    public async Task<IReadOnlyList<int>> GetAvailableMajorVersionsAsync(Guid configurationId, CancellationToken cancellationToken = default)
    {
        var schemaVersions = await _db.ParameterSchemas
            .Where(ps => ps.ConfigurationId == configurationId && ps.SchemaVersion != null)
            .Select(ps => ps.SchemaVersion!)
            .ToListAsync(cancellationToken);

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


