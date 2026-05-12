// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using NuGet.Versioning;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Parameters;
using ParametersFileMigrationStatus = OpenDsc.Contracts.Parameters.ParameterFileMigrationStatus;
using OpenDsc.Contracts.Permissions;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

using ParametersPublishResult = OpenDsc.Contracts.Parameters.PublishResult;
using ParametersValidationResult = OpenDsc.Contracts.Parameters.ValidationResult;

namespace OpenDsc.Server.Services;

public sealed partial class ParameterService : IParameterService
{
    private readonly ServerDbContext _db;
    private readonly IOptions<ServerConfig> _serverConfig;
    private readonly IResourceAuthorizationService _authService;
    private readonly IUserContextService _userContext;
    private readonly IParameterMergeService _parameterMergeService;
    private readonly IParameterValidator _validator;
    private readonly IParameterSchemaBuilder _schemaBuilder;
    private readonly IParameterCompatibilityService _compatibilityService;
    private readonly ILogger<ParameterService> _logger;

    public ParameterService(
        ServerDbContext db,
        IOptions<ServerConfig> serverConfig,
        IResourceAuthorizationService authService,
        IUserContextService userContext,
        IParameterMergeService parameterMergeService,
        IParameterValidator validator,
        IParameterSchemaBuilder schemaBuilder,
        IParameterCompatibilityService compatibilityService,
        ILogger<ParameterService> logger)
    {
        _db = db;
        _serverConfig = serverConfig;
        _authService = authService;
        _userContext = userContext;
        _parameterMergeService = parameterMergeService;
        _validator = validator;
        _schemaBuilder = schemaBuilder;
        _compatibilityService = compatibilityService;
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

    public async Task<IReadOnlyList<MajorVersionSummary>> GetMajorVersionSummariesAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue = null,
        CancellationToken cancellationToken = default)
    {
        var parameterSchema = await _db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configurationId, cancellationToken)
            ?? throw new KeyNotFoundException("Parameter schema not found.");

        var userId = _userContext.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        if (!await _authService.CanReadParameterAsync(userId, parameterSchema.Id))
            throw new UnauthorizedAccessException("Access denied.");

        var allFiles = await _db.ParameterFiles
            .Where(pf =>
                pf.ScopeTypeId == scopeTypeId &&
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeValue == scopeValue)
            .Select(pf => new { pf.MajorVersion, pf.Version, pf.CreatedAt, pf.Status, pf.NeedsMigration })
            .ToListAsync(cancellationToken);

        return allFiles
            .GroupBy(pf => pf.MajorVersion)
            .Select(g => new MajorVersionSummary
            {
                MajorVersion = g.Key,
                VersionCount = g.Count(),
                HasActive = g.Any(pf => pf.Status == ParameterVersionStatus.Published),
                LatestVersion = g.OrderByDescending(pf => pf.CreatedAt).First().Version,
                HasMigrationNeeded = g.Any(pf => pf.NeedsMigration)
            })
            .OrderByDescending(m => m.MajorVersion)
            .ToList();
    }

    public async Task<ParameterVersionDetails?> GetActiveParameterForMajorAsync(
        Guid scopeTypeId,
        Guid configurationId,
        int majorVersion,
        string? scopeValue = null,
        CancellationToken cancellationToken = default)
    {
        var parameterSchema = await _db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configurationId, cancellationToken)
            ?? throw new KeyNotFoundException("Parameter schema not found.");

        var userId = _userContext.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        if (!await _authService.CanReadParameterAsync(userId, parameterSchema.Id))
            throw new UnauthorizedAccessException("Access denied.");

        var file = await _db.ParameterFiles
            .FirstOrDefaultAsync(pf =>
                pf.ScopeTypeId == scopeTypeId &&
                pf.ParameterSchema!.ConfigurationId == configurationId &&
                pf.ScopeValue == scopeValue &&
                pf.MajorVersion == majorVersion &&
                pf.Status == ParameterVersionStatus.Published, cancellationToken);

        if (file is null)
            return null;

        return new ParameterVersionDetails
        {
            Id = file.Id,
            ScopeTypeId = file.ScopeTypeId,
            ConfigurationId = configurationId,
            ScopeValue = file.ScopeValue,
            Version = file.Version,
            MajorVersion = file.MajorVersion,
            Checksum = file.Checksum,
            Status = file.Status,
            IsPassthrough = file.IsPassthrough,
            CreatedAt = file.CreatedAt
        };
    }

    public async Task<List<PermissionEntry>?> GetPermissionsAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _db.Configurations
            .FirstOrDefaultAsync(c => c.Id == configurationId, cancellationToken);

        if (configuration is null)
            return null;

        var parameterSchema = await _db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id, cancellationToken);

        if (parameterSchema is null)
            return null;

        var userId = _userContext.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        if (!await _authService.CanManageParameterAsync(userId, parameterSchema.Id) &&
            !await _authService.CanManageConfigurationAsync(userId, configuration.Id))
            throw new UnauthorizedAccessException("Access denied.");

        var acl = await _authService.GetParameterAclAsync(parameterSchema.Id);
        return await BuildPermissionEntriesAsync(
            acl.Select(p => (p.PrincipalType, p.PrincipalId, p.PermissionLevel, p.GrantedAt, p.GrantedByUserId)),
            cancellationToken);
    }

    public async Task GrantPermissionAsync(
        Guid configurationId,
        GrantPermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _db.Configurations
            .FirstOrDefaultAsync(c => c.Id == configurationId, cancellationToken)
            ?? throw new KeyNotFoundException("Configuration not found.");

        var parameterSchema = await _db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Parameter schema not found.");

        var userId = _userContext.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        if (!await _authService.CanManageParameterAsync(userId, parameterSchema.Id) &&
            !await _authService.CanManageConfigurationAsync(userId, configuration.Id))
            throw new UnauthorizedAccessException("Access denied.");

        if (!Enum.TryParse<PrincipalType>(request.PrincipalType, ignoreCase: true, out var principalType))
            throw new ArgumentException($"Invalid principal type '{request.PrincipalType}'. Must be 'User' or 'Group'.");

        if (!Enum.TryParse<ResourcePermission>(request.Level, ignoreCase: true, out var level))
            throw new ArgumentException($"Invalid permission level '{request.Level}'. Must be 'Read', 'Modify', or 'Manage'.");

        if (principalType == PrincipalType.User && !await _db.Users.AnyAsync(u => u.Id == request.PrincipalId, cancellationToken))
            throw new KeyNotFoundException("User not found.");

        if (principalType == PrincipalType.Group && !await _db.Groups.AnyAsync(g => g.Id == request.PrincipalId, cancellationToken))
            throw new KeyNotFoundException("Group not found.");

        await _authService.GrantParameterPermissionAsync(parameterSchema.Id, request.PrincipalId, principalType, level, userId);
    }

    public async Task RevokePermissionAsync(
        Guid configurationId,
        RevokePermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _db.Configurations
            .FirstOrDefaultAsync(c => c.Id == configurationId, cancellationToken)
            ?? throw new KeyNotFoundException("Configuration not found.");

        var parameterSchema = await _db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Parameter schema not found.");

        var userId = _userContext.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        if (!await _authService.CanManageParameterAsync(userId, parameterSchema.Id) &&
            !await _authService.CanManageConfigurationAsync(userId, configuration.Id))
            throw new UnauthorizedAccessException("Access denied.");

        if (!Enum.TryParse<PrincipalType>(request.PrincipalType, ignoreCase: true, out var principalType))
            throw new ArgumentException($"Invalid principal type '{request.PrincipalType}'. Must be 'User' or 'Group'.");

        await _authService.RevokeParameterPermissionAsync(parameterSchema.Id, request.PrincipalId, principalType);
    }

    public async Task<ParametersPublishResult> UploadSchemaAsync(
        Guid configurationId,
        string version,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _db.Configurations
            .FirstOrDefaultAsync(c => c.Id == configurationId, cancellationToken)
            ?? throw new KeyNotFoundException("Configuration not found.");

        var userId = _userContext.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        if (!await _authService.CanModifyConfigurationAsync(userId, configuration.Id))
            throw new UnauthorizedAccessException("Access denied.");

        if (!SemanticVersion.TryParse(version, out var semVer))
            throw new ArgumentException($"Version '{version}' is not a valid semantic version.");

        using var reader = new StreamReader(content);
        var schemaContent = await reader.ReadToEndAsync(cancellationToken);

        var existingForVersion = await _db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id && ps.SchemaVersion == version, cancellationToken);

        if (existingForVersion is not null)
            throw new InvalidOperationException($"Parameter schema version '{version}' already exists.");

        var parametersBlock = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaContent);
        if (parametersBlock is null || !parametersBlock.TryGetValue("parameters", out var paramsObj))
            throw new ArgumentException("Parameter schema must contain a 'parameters' object.");

        var paramsJson = JsonSerializer.Serialize(paramsObj);
        var paramDefinitions = JsonSerializer.Deserialize<Dictionary<string, ParameterDefinition>>(paramsJson)
            ?? throw new ArgumentException("Failed to parse parameter definitions.");

        var jsonSchemaObj = _schemaBuilder.BuildJsonSchema(paramDefinitions);
        var jsonSchema = _schemaBuilder.SerializeSchema(jsonSchemaObj);

        var previousSchema = await _db.ParameterSchemas
            .Where(ps => ps.ConfigurationId == configuration.Id)
            .OrderByDescending(ps => ps.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousSchema is not null && !string.IsNullOrWhiteSpace(previousSchema.GeneratedJsonSchema))
        {
            if (string.IsNullOrWhiteSpace(previousSchema.SchemaVersion))
                throw new InvalidOperationException("Previous schema version is missing.");

            var compatibilityReport = _compatibilityService.CompareSchemas(
                previousSchema.GeneratedJsonSchema,
                jsonSchema,
                previousSchema.SchemaVersion,
                version);

            var previousSemVer = SemanticVersion.Parse(previousSchema.SchemaVersion);
            var isMajorVersionBump = semVer.Major > previousSemVer.Major;

            if (compatibilityReport.HasBreakingChanges && !isMajorVersionBump)
            {
                var parameterFiles = await _db.ParameterFiles
                    .Include(pf => pf.ScopeType)
                    .Where(pf => pf.ParameterSchemaId == previousSchema.Id)
                    .ToListAsync(cancellationToken);

                var migrationRequirements = parameterFiles.Select(pf => new ParametersFileMigrationStatus
                {
                    ScopeTypeName = pf.ScopeType?.Name ?? string.Empty,
                    ScopeValue = pf.ScopeValue ?? "Global",
                    Version = pf.Version,
                    MajorVersion = pf.MajorVersion,
                    NeedsMigration = true,
                    Errors = []
                }).ToList();

                return new ParametersPublishResult
                {
                    Success = false,
                    CompatibilityReport = new Contracts.Parameters.CompatibilityReport
                    {
                        HasBreakingChanges = compatibilityReport.HasBreakingChanges,
                        BreakingChanges = compatibilityReport.BreakingChanges?.Select(c => new ParameterChange
                        {
                            ParameterName = c.ParameterName,
                            ChangeType = c.ChangeType,
                            Details = c.Description ?? string.Empty
                        }).ToList(),
                        NonBreakingChanges = compatibilityReport.NonBreakingChanges?.Select(c => new ParameterChange
                        {
                            ParameterName = c.ParameterName,
                            ChangeType = c.ChangeType,
                            Details = c.Description ?? string.Empty
                        }).ToList()
                    },
                    MigrationRequirements = migrationRequirements
                };
            }
        }

        var newSchema = new ParameterSchema
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configuration.Id,
            SchemaVersion = version,
            GeneratedJsonSchema = jsonSchema,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.ParameterSchemas.Add(newSchema);
        await _db.SaveChangesAsync(cancellationToken);

        return new ParametersPublishResult { Success = true };
    }

    public async Task<ParametersValidationResult> ValidateAsync(
        Guid configurationId,
        string version,
        string parameterContent,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _db.Configurations
            .FirstOrDefaultAsync(c => c.Id == configurationId, cancellationToken)
            ?? throw new KeyNotFoundException("Configuration not found.");

        var userId = _userContext.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        if (!await _authService.CanReadConfigurationAsync(userId, configuration.Id))
            throw new UnauthorizedAccessException("Access denied.");

        var parameterSchema = await _db.ParameterSchemas
            .FirstOrDefaultAsync(ps => ps.ConfigurationId == configuration.Id && ps.SchemaVersion == version, cancellationToken)
            ?? throw new KeyNotFoundException("Parameter schema not found.");

        if (string.IsNullOrWhiteSpace(parameterSchema.GeneratedJsonSchema))
            throw new InvalidOperationException("No JSON schema available for this configuration version.");

        var serverResult = _validator.Validate(parameterSchema.GeneratedJsonSchema, parameterContent);
        return new ParametersValidationResult
        {
            IsValid = serverResult.IsValid,
            Errors = serverResult.Errors?.Select(e => new OpenDsc.Contracts.Parameters.ValidationError
            {
                Path = e.Path,
                Message = e.Message,
                Code = e.Code
            }).ToList()
        };
    }

    public async Task<IReadOnlyList<ParameterSchemaDetails>> GetSchemasAsync(
        CancellationToken cancellationToken = default)
    {
        var schemas = await _db.ParameterSchemas
            .Include(ps => ps.Configuration)
            .Include(ps => ps.ParameterFiles)
            .Select(ps => new ParameterSchemaDetails
            {
                Id = ps.Id,
                ConfigurationId = ps.ConfigurationId,
                ConfigurationName = ps.Configuration.Name,
                SchemaVersion = ps.SchemaVersion,
                GeneratedJsonSchema = ps.GeneratedJsonSchema,
                ParameterFileCount = ps.ParameterFiles.Count,
                CreatedAt = ps.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return schemas
            .OrderBy(ps => ps.ConfigurationName)
            .ThenBy(ps => ps.CreatedAt)
            .ToList();
    }

    public async Task<ParameterSchemaDetails?> GetSchemaAsync(
        Guid configurationId,
        int? majorVersion = null,
        CancellationToken cancellationToken = default)
    {
        var schemas = await _db.ParameterSchemas
            .Include(ps => ps.Configuration)
            .Include(ps => ps.ParameterFiles)
            .Where(ps => ps.ConfigurationId == configurationId)
            .ToListAsync(cancellationToken);

        ParameterSchema? schema;
        if (majorVersion.HasValue)
        {
            schema = schemas.FirstOrDefault(ps =>
                !string.IsNullOrEmpty(ps.SchemaVersion) &&
                SemanticVersion.TryParse(ps.SchemaVersion, out var sv) &&
                sv.Major == majorVersion.Value);
        }
        else
        {
            schema = schemas.FirstOrDefault();
        }

        if (schema is null)
            return null;

        return new ParameterSchemaDetails
        {
            Id = schema.Id,
            ConfigurationId = schema.ConfigurationId,
            ConfigurationName = schema.Configuration?.Name ?? string.Empty,
            SchemaVersion = schema.SchemaVersion,
            GeneratedJsonSchema = schema.GeneratedJsonSchema,
            ParameterFileCount = schema.ParameterFiles?.Count ?? 0,
            CreatedAt = schema.CreatedAt
        };
    }

    public async Task<IReadOnlyList<ParameterFileDetails>> GetSchemaFilesAsync(
        Guid schemaId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ParameterFiles
            .Include(pf => pf.ScopeType)
            .Where(pf => pf.ParameterSchemaId == schemaId)
            .OrderBy(pf => pf.ScopeType.Precedence)
            .ThenBy(pf => pf.ScopeValue)
            .ThenBy(pf => pf.Version)
            .Select(pf => new ParameterFileDetails
            {
                Id = pf.Id,
                ParameterSchemaId = pf.ParameterSchemaId,
                ScopeTypeName = pf.ScopeType.Name,
                ScopeValue = pf.ScopeValue,
                Version = pf.Version,
                Status = pf.Status,
                Checksum = pf.Checksum,
                CreatedAt = pf.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<PermissionEntry>> BuildPermissionEntriesAsync(
        IEnumerable<(PrincipalType PrincipalType, Guid PrincipalId, ResourcePermission Level, DateTimeOffset GrantedAt, Guid? GrantedByUserId)> entries,
        CancellationToken cancellationToken)
    {
        var list = entries.ToList();
        var userIds = list.Where(e => e.PrincipalType == PrincipalType.User).Select(e => e.PrincipalId).ToList();
        var groupIds = list.Where(e => e.PrincipalType == PrincipalType.Group).Select(e => e.PrincipalId).ToList();

        var userNames = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

        var groupNames = await _db.Groups
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken);

        return list.Select(e => new PermissionEntry
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


