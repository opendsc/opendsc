// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;
using Microsoft.Extensions.Options;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Reports;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using NuGet.Versioning;

namespace OpenDsc.Server.Services;

public sealed class NodeService : INodeReader, INodeTagManager, INodeConfigurationManager, INodeRegistrationManager, INodeLcmManager, INodeManager
{
    private readonly ServerDbContext _db;
    private readonly IParameterMergeService _parameterMergeService;
    private readonly IOptions<ServerConfig> _serverConfig;
    private readonly IWebHostEnvironment _env;

    public NodeService(
        ServerDbContext db,
        IParameterMergeService parameterMergeService,
        IOptions<ServerConfig> serverConfig,
        IWebHostEnvironment env)
    {
        _db = db;
        _parameterMergeService = parameterMergeService;
        _serverConfig = serverConfig;
        _env = env;
    }

    public async Task<IReadOnlyList<NodeSummary>> GetNodesAsync(
        NodeFilterRequest? filter = null,
        CancellationToken cancellationToken = default)
    {
        var settings = await _db.ServerSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var staleness = settings?.StalenessMultiplier ?? 2.0;
        var now = DateTimeOffset.UtcNow;

        var query = _db.Nodes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter?.FqdnContains))
        {
            query = query.Where(n => n.Fqdn.Contains(filter.FqdnContains));
        }

        if (!string.IsNullOrWhiteSpace(filter?.ConfigurationContains))
        {
            query = query.Where(n => n.ConfigurationName != null && n.ConfigurationName.Contains(filter.ConfigurationContains));
        }

        if (!string.IsNullOrWhiteSpace(filter?.Status) && Enum.TryParse<NodeStatus>(filter.Status, true, out var status))
        {
            query = query.Where(n => n.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filter?.LcmStatus) && Enum.TryParse<OpenDsc.Contracts.Lcm.LcmStatus>(filter.LcmStatus, true, out var lcmStatus))
        {
            query = query.Where(n => n.LcmStatus == lcmStatus);
        }

        var nodes = await query
            .OrderBy(n => n.Fqdn)
            .ToListAsync(cancellationToken);

        if (filter?.Limit is int limit && limit > 0)
        {
            nodes = nodes.Take(limit).ToList();
        }

        return nodes.Select(n => ToNodeSummary(n, staleness, now)).ToList();
    }

    public async Task<NodeDetails?> GetNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var settings = await _db.ServerSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var staleness = settings?.StalenessMultiplier ?? 2.0;
        var now = DateTimeOffset.UtcNow;

        var node = await _db.Nodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);

        if (node is null)
        {
            return null;
        }

        return new NodeDetails
        {
            Summary = ToNodeSummary(node, staleness, now),
            Id = node.Id,
            Fqdn = node.Fqdn,
            ConfigurationName = node.ConfigurationName,
            Status = node.Status.ToString(),
            LcmStatus = node.LcmStatus.ToString(),
            IsStale = node.LastCheckIn.HasValue
                && node.ConfigurationModeInterval.HasValue
                && (now - node.LastCheckIn.Value) > node.ConfigurationModeInterval.Value * staleness,
            LastCheckIn = node.LastCheckIn,
            CreatedAt = node.CreatedAt,
            ConfigurationSource = node.ConfigurationSource,
            ConfigurationMode = node.ConfigurationMode,
            ConfigurationModeInterval = node.ConfigurationModeInterval,
            ReportCompliance = node.ReportCompliance,
            DesiredConfigurationMode = node.DesiredConfigurationMode,
            DesiredConfigurationModeInterval = node.DesiredConfigurationModeInterval,
            DesiredReportCompliance = node.DesiredReportCompliance,
            CertificateThumbprint = node.CertificateThumbprint,
            CertificateSubject = node.CertificateSubject,
            CertificateNotAfter = node.CertificateNotAfter
        };
    }

    public async Task<NodeConfigurationManifest?> GetNodeConfigurationManifestAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _db.NodeConfigurations
            .AsNoTracking()
            .Include(nc => nc.Configuration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Files)
            .Include(nc => nc.CompositeConfiguration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Items)
            .ThenInclude(i => i.ChildConfiguration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(nc => nc.NodeId == nodeId, cancellationToken);

        if (assignment is null)
        {
            return null;
        }

        var content = await BuildConfigurationContentAsync(nodeId, assignment, cancellationToken);
        if (content is null)
        {
            return null;
        }

        return new NodeConfigurationManifest
        {
            Content = content,
            EntryPoint = assignment.CompositeConfigurationId.HasValue
                ? assignment.CompositeConfiguration?.EntryPoint ?? "main.dsc.yaml"
                : assignment.Configuration?.Versions.FirstOrDefault()?.EntryPoint ?? "main.dsc.yaml"
        };
    }

    public async Task<NodeConfigurationBundle?> GetNodeConfigurationBundleAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var nodeConfig = await _db.NodeConfigurations
            .Include(nc => nc.Configuration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Files)
            .Include(nc => nc.CompositeConfiguration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Items)
            .ThenInclude(i => i.ChildConfiguration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Files)
            .AsSplitQuery()
            .FirstOrDefaultAsync(nc => nc.NodeId == nodeId, cancellationToken);

        if (nodeConfig is null)
        {
            return null;
        }

        var dataDir = _serverConfig.Value.ConfigurationsDirectory;

        if (nodeConfig.CompositeConfigurationId.HasValue)
        {
            return await BuildCompositeBundleAsync(nodeId, nodeConfig, dataDir, cancellationToken);
        }

        var activeVersion = VersionResolver.ResolveVersion(
            nodeConfig.Configuration!.Versions, v => v.Version, nodeConfig.MajorVersion, nodeConfig.PrereleaseChannel);

        if (activeVersion is null)
        {
            return null;
        }

        var versionDir = Path.Combine(dataDir, nodeConfig.Configuration.Name, $"v{activeVersion.Version}");

        var bundleStream = new MemoryStream();
        using (var archive = new ZipArchive(bundleStream, ZipArchiveMode.Create, true))
        {
            foreach (var file in activeVersion.Files)
            {
                var filePath = Path.Combine(versionDir, file.RelativePath);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                var entry = archive.CreateEntry(file.RelativePath);
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(filePath);
                await fileStream.CopyToAsync(entryStream, cancellationToken);
            }

            if (nodeConfig.Configuration!.UseServerManagedParameters)
            {
                var mergedParameters = await _parameterMergeService.MergeParametersAsync(nodeId, nodeConfig.ConfigurationId!.Value, cancellationToken);
                if (!string.IsNullOrWhiteSpace(mergedParameters))
                {
                    var paramEntry = archive.CreateEntry("parameters.yaml");
                    using var paramStream = paramEntry.Open();
                    using var writer = new StreamWriter(paramStream);
                    await writer.WriteAsync(mergedParameters);
                }
            }
        }

        return new NodeConfigurationBundle
        {
            Content = bundleStream.ToArray(),
            FileName = $"{nodeConfig.Configuration.Name}-v{activeVersion.Version}.zip",
            ContentType = "application/zip"
        };
    }

    public async Task<NodeAssignmentSummary?> GetNodeAssignmentAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _db.NodeConfigurations.AsNoTracking()
            .Include(nc => nc.Configuration)
            .Include(nc => nc.CompositeConfiguration)
            .FirstOrDefaultAsync(nc => nc.NodeId == nodeId, cancellationToken);

        if (assignment is null)
        {
            return null;
        }

        return new NodeAssignmentSummary
        {
            NodeId = nodeId,
            ConfigurationName = assignment.Configuration?.Name ?? assignment.CompositeConfiguration?.Name,
            IsComposite = assignment.CompositeConfigurationId.HasValue,
            MajorVersion = assignment.MajorVersion,
            PrereleaseChannel = assignment.PrereleaseChannel,
            AssignedAt = assignment.AssignedAt
        };
    }

    public async Task<IReadOnlyList<ConfigurationOption>> GetAvailableConfigurationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Configurations
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ConfigurationOption { Id = c.Id, Name = c.Name })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigurationOption>> GetAvailableCompositeConfigurationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.CompositeConfigurations
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ConfigurationOption { Id = c.Id, Name = c.Name })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigurationAssignmentOption>> GetAssignableConfigurationsAsync(
        CancellationToken cancellationToken = default)
    {
        var configurations = await _db.Configurations
            .AsNoTracking()
            .Include(c => c.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return configurations
            .Select(c => new ConfigurationAssignmentOption
            {
                Id = c.Id,
                Name = c.Name,
                MajorVersions = c.Versions
                    .Select(v => SemanticVersion.TryParse(v.Version, out var sv) ? sv.Major : -1)
                    .Where(v => v >= 0)
                    .Distinct()
                    .OrderBy(v => v)
                    .ToList()
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ConfigurationAssignmentOption>> GetAssignableCompositeConfigurationsAsync(
        CancellationToken cancellationToken = default)
    {
        var compositeConfigurations = await _db.CompositeConfigurations
            .AsNoTracking()
            .Include(c => c.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return compositeConfigurations
            .Select(c => new ConfigurationAssignmentOption
            {
                Id = c.Id,
                Name = c.Name,
                MajorVersions = c.Versions
                    .Select(v => SemanticVersion.TryParse(v.Version, out var sv) ? sv.Major : -1)
                    .Where(v => v >= 0)
                    .Distinct()
                    .OrderBy(v => v)
                    .ToList()
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ScopeTypeSummary>> GetScopeTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var scopeTypes = await _db.ScopeTypes
            .AsNoTracking()
            .Where(st => st.Name != "Node" && st.Name != "Default")
            .OrderByDescending(st => st.Precedence)
            .ToListAsync(cancellationToken);

        return scopeTypes
            .Select(st => new ScopeTypeSummary
            {
                Id = st.Id,
                Name = st.Name,
                Precedence = st.Precedence,
                ValueMode = Enum.TryParse<ScopeValueMode>(st.ValueMode.ToString(), out var mode)
                    ? mode
                    : ScopeValueMode.Unrestricted
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ScopeValueSummary>> GetScopeValuesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.ScopeValues
            .AsNoTracking()
            .Include(sv => sv.ScopeType)
            .OrderBy(sv => sv.Value)
            .Select(sv => new ScopeValueSummary
            {
                Id = sv.Id,
                ScopeTypeId = sv.ScopeTypeId,
                ScopeTypeName = sv.ScopeType.Name,
                Value = sv.Value,
                Precedence = sv.ScopeType.Precedence
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReportSummary>> GetNodeReportsAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Nodes.AnyAsync(n => n.Id == nodeId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException("Node not found.");
        }

        var reports = await _db.Reports
            .AsNoTracking()
            .Where(r => r.NodeId == nodeId)
            .Select(r => new ReportSummary
            {
                Id = r.Id,
                NodeId = r.NodeId,
                NodeFqdn = string.Empty,
                Timestamp = r.Timestamp,
                Operation = r.Operation,
                InDesiredState = r.InDesiredState,
                HadErrors = r.HadErrors
            })
            .ToListAsync(cancellationToken);

        return reports
            .OrderByDescending(r => r.Timestamp)
            .ToList();
    }

    public async Task<IReadOnlyList<NodeStatusEventSummary>> GetNodeStatusEventsAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Nodes.AnyAsync(n => n.Id == nodeId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException("Node not found.");
        }

        var events = await _db.NodeStatusEvents
            .AsNoTracking()
            .Where(e => e.NodeId == nodeId)
            .Select(e => new NodeStatusEventSummary
            {
                Id = e.Id,
                NodeId = e.NodeId,
                NodeFqdn = e.Node.Fqdn,
                LcmStatus = e.LcmStatus.HasValue ? e.LcmStatus.Value.ToString() : null,
                Timestamp = e.Timestamp
            })
            .ToListAsync(cancellationToken);

        return events
            .OrderByDescending(e => e.Timestamp)
            .ToList();
    }

    public async Task<IReadOnlyList<NodeScopeValueSummary>> GetNodeScopeValuesAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Nodes.AnyAsync(n => n.Id == nodeId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException("Node not found.");
        }

        return await _db.NodeTags
            .AsNoTracking()
            .Include(nt => nt.ScopeValue)
            .ThenInclude(sv => sv.ScopeType)
            .Where(nt => nt.NodeId == nodeId)
            .OrderBy(nt => nt.ScopeValue.ScopeType.Precedence)
            .Select(nt => new NodeScopeValueSummary
            {
                ScopeTypeId = nt.ScopeValue.ScopeTypeId,
                ScopeTypeName = nt.ScopeValue.ScopeType.Name,
                ScopeValueId = nt.ScopeValueId,
                ScopeValue = nt.ScopeValue.Value,
                Precedence = nt.ScopeValue.ScopeType.Precedence
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NodeTagSummary>> GetNodeTagsAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Nodes.AnyAsync(n => n.Id == nodeId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException("Node not found.");
        }

        return await _db.NodeTags
            .AsNoTracking()
            .Include(nt => nt.ScopeValue)
            .ThenInclude(sv => sv.ScopeType)
            .Where(nt => nt.NodeId == nodeId)
            .OrderBy(nt => nt.ScopeValue.ScopeType.Precedence)
            .ThenBy(nt => nt.ScopeValue.Value)
            .Select(nt => new NodeTagSummary
            {
                NodeId = nt.NodeId,
                ScopeTypeId = nt.ScopeValue.ScopeTypeId,
                ScopeValueId = nt.ScopeValueId,
                ScopeTypeName = nt.ScopeValue.ScopeType.Name,
                ScopeValue = nt.ScopeValue.Value,
                Precedence = nt.ScopeValue.ScopeType.Precedence,
                AssignedAt = nt.AssignedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<NodeTagSummary> AddNodeTagAsync(
        Guid nodeId,
        AddNodeTagRequest request,
        CancellationToken cancellationToken = default)
    {
        var nodeExists = await _db.Nodes.AnyAsync(n => n.Id == nodeId, cancellationToken);
        if (!nodeExists)
        {
            throw new KeyNotFoundException("Node not found.");
        }

        var scopeValue = await _db.ScopeValues
            .Include(sv => sv.ScopeType)
            .FirstOrDefaultAsync(sv => sv.Id == request.ScopeValueId, cancellationToken);

        if (scopeValue is null)
        {
            throw new ArgumentException("Scope value not found.", nameof(request));
        }

        var existingTagWithSameType = await _db.NodeTags
            .Include(nt => nt.ScopeValue)
            .FirstOrDefaultAsync(nt =>
                nt.NodeId == nodeId &&
                nt.ScopeValue.ScopeTypeId == scopeValue.ScopeTypeId, cancellationToken);

        if (existingTagWithSameType is not null)
        {
            throw new InvalidOperationException($"Node already has a tag for scope type '{scopeValue.ScopeType.Name}'. Remove the existing tag first.");
        }

        var existingTag = await _db.NodeTags
            .FirstOrDefaultAsync(nt => nt.NodeId == nodeId && nt.ScopeValueId == request.ScopeValueId, cancellationToken);

        if (existingTag is not null)
        {
            throw new InvalidOperationException("This scope value is already assigned to the node");
        }

        var nodeTag = new NodeTag
        {
            NodeId = nodeId,
            ScopeValueId = request.ScopeValueId,
            AssignedAt = DateTimeOffset.UtcNow
        };

        _db.NodeTags.Add(nodeTag);
        await _db.SaveChangesAsync(cancellationToken);

        return new NodeTagSummary
        {
            NodeId = nodeTag.NodeId,
            ScopeTypeId = scopeValue.ScopeTypeId,
            ScopeValueId = nodeTag.ScopeValueId,
            ScopeTypeName = scopeValue.ScopeType.Name,
            ScopeValue = scopeValue.Value,
            Precedence = scopeValue.ScopeType.Precedence,
            AssignedAt = nodeTag.AssignedAt
        };
    }

    public async Task RemoveNodeTagAsync(
        Guid nodeId,
        RemoveNodeTagRequest request,
        CancellationToken cancellationToken = default)
    {
        var nodeTag = await _db.NodeTags
            .FirstOrDefaultAsync(nt => nt.NodeId == nodeId && nt.ScopeValueId == request.ScopeValueId, cancellationToken);

        if (nodeTag is null)
        {
            throw new KeyNotFoundException("Node tag not found.");
        }

        _db.NodeTags.Remove(nodeTag);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetNodeScopeValueAsync(
        Guid nodeId,
        SetNodeScopeValueRequest request,
        CancellationToken cancellationToken = default)
    {
        var node = await _db.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken)
            ?? throw new KeyNotFoundException("Node not found.");

        var scopeType = await _db.ScopeTypes.FirstOrDefaultAsync(st => st.Id == request.ScopeTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("Scope type not found.");

        if (string.IsNullOrWhiteSpace(request.ScopeValue))
        {
            throw new ArgumentException("Scope value is required.", nameof(request));
        }

        var scopeValue = await _db.ScopeValues
            .FirstOrDefaultAsync(sv => sv.ScopeTypeId == request.ScopeTypeId && sv.Value == request.ScopeValue, cancellationToken);

        if (scopeValue is null)
        {
            scopeValue = new ScopeValue
            {
                Id = Guid.NewGuid(),
                ScopeTypeId = request.ScopeTypeId,
                Value = request.ScopeValue,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.ScopeValues.Add(scopeValue);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var existingTypeTag = await _db.NodeTags
            .Include(nt => nt.ScopeValue)
            .FirstOrDefaultAsync(nt => nt.NodeId == node.Id && nt.ScopeValue.ScopeTypeId == scopeType.Id, cancellationToken);

        if (existingTypeTag is not null)
        {
            existingTypeTag.ScopeValueId = scopeValue.Id;
            existingTypeTag.AssignedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            _db.NodeTags.Add(new NodeTag
            {
                NodeId = node.Id,
                ScopeValueId = scopeValue.Id,
                AssignedAt = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RegisterNodeResponse> RegisterNodeAsync(
        RegisterNodeRequest request,
        string? certificateThumbprint,
        string? certificateSubject,
        DateTimeOffset? certificateNotAfter,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Fqdn))
        {
            throw new ArgumentException("FQDN is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.RegistrationKey))
        {
            throw new ArgumentException("Registration key is required.", nameof(request));
        }

        var registrationKey = await _db.RegistrationKeys.FirstOrDefaultAsync(k => k.Key == request.RegistrationKey, cancellationToken)
            ?? throw new ArgumentException($"Invalid registration key: '{request.RegistrationKey}'.", nameof(request));

        if (registrationKey.IsRevoked)
        {
            throw new InvalidOperationException("Registration key has been revoked.");
        }

        if (DateTimeOffset.UtcNow > registrationKey.ExpiresAt)
        {
            throw new InvalidOperationException("Registration key has expired.");
        }

        if (registrationKey.MaxUses.HasValue && registrationKey.CurrentUses >= registrationKey.MaxUses.Value)
        {
            throw new InvalidOperationException("Registration key has reached its maximum uses.");
        }

        if (string.IsNullOrWhiteSpace(certificateThumbprint))
        {
            throw new ArgumentException("Certificate thumbprint is required.", nameof(certificateThumbprint));
        }

        if (string.IsNullOrWhiteSpace(certificateSubject))
        {
            throw new ArgumentException("Certificate subject is required.", nameof(certificateSubject));
        }

        if (!certificateNotAfter.HasValue)
        {
            throw new ArgumentException("Certificate expiration is required.", nameof(certificateNotAfter));
        }

        var thumbprintConflict = await _db.Nodes
            .FirstOrDefaultAsync(n => n.CertificateThumbprint == certificateThumbprint && n.Fqdn != request.Fqdn, cancellationToken);

        if (thumbprintConflict is not null)
        {
            throw new InvalidOperationException($"Certificate thumbprint is already registered to another node: {thumbprintConflict.Fqdn}");
        }

        var existingNode = await _db.Nodes.FirstOrDefaultAsync(n => n.Fqdn == request.Fqdn, cancellationToken);
        if (existingNode is not null)
        {
            existingNode.CertificateThumbprint = certificateThumbprint;
            existingNode.CertificateSubject = certificateSubject;
            existingNode.CertificateNotAfter = certificateNotAfter.Value;
            existingNode.LastCheckIn = DateTimeOffset.UtcNow;
            existingNode.ConfigurationSource = request.ConfigurationSource ?? ConfigurationSource.Pull;
            existingNode.ConfigurationMode = request.ConfigurationMode;
            existingNode.ConfigurationModeInterval = request.ConfigurationModeInterval;
            existingNode.ReportCompliance = request.ReportCompliance;
            registrationKey.CurrentUses++;
            await _db.SaveChangesAsync(cancellationToken);
            return new RegisterNodeResponse { NodeId = existingNode.Id };
        }

        var node = new Node
        {
            Id = Guid.NewGuid(),
            Fqdn = request.Fqdn,
            CertificateThumbprint = certificateThumbprint,
            CertificateSubject = certificateSubject,
            CertificateNotAfter = certificateNotAfter.Value,
            Status = NodeStatus.Unknown,
            CreatedAt = DateTimeOffset.UtcNow,
            LastCheckIn = DateTimeOffset.UtcNow,
            ConfigurationSource = request.ConfigurationSource ?? ConfigurationSource.Pull,
            ConfigurationMode = request.ConfigurationMode,
            ConfigurationModeInterval = request.ConfigurationModeInterval,
            ReportCompliance = request.ReportCompliance
        };

        _db.Nodes.Add(node);
        registrationKey.CurrentUses++;
        await _db.SaveChangesAsync(cancellationToken);
        return new RegisterNodeResponse { NodeId = node.Id };
    }

    public async Task DeleteNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        var node = await _db.Nodes.FindAsync([nodeId], cancellationToken);
        if (node is null)
        {
            throw new KeyNotFoundException("Node not found.");
        }

        _db.Nodes.Remove(node);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignConfigurationAsync(Guid nodeId, AssignConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var node = await _db.Nodes.FindAsync([nodeId], cancellationToken)
            ?? throw new KeyNotFoundException("Node not found.");

        if (request.IsComposite)
        {
            var composite = await _db.CompositeConfigurations
                .Include(c => c.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
                .FirstOrDefaultAsync(c => c.Name == request.ConfigurationName, cancellationToken)
                ?? throw new KeyNotFoundException("Composite configuration not found.");

            var eligibleCompositeVersion = VersionResolver.ResolveVersion(composite.Versions, v => v.Version, request.MajorVersion, request.PrereleaseChannel)
                ?? throw new InvalidOperationException("No published version satisfies the specified major version and prerelease channel constraints.");

            var nodeConfig = await _db.NodeConfigurations.FindAsync([nodeId], cancellationToken);
            if (nodeConfig is null)
            {
                nodeConfig = new NodeConfiguration
                {
                    NodeId = nodeId,
                    CompositeConfigurationId = composite.Id,
                    MajorVersion = request.MajorVersion,
                    PrereleaseChannel = request.PrereleaseChannel,
                    AssignedAt = DateTimeOffset.UtcNow
                };
                _db.NodeConfigurations.Add(nodeConfig);
            }
            else
            {
                nodeConfig.ConfigurationId = null;
                nodeConfig.ActiveVersion = null;
                nodeConfig.CompositeConfigurationId = composite.Id;
                nodeConfig.ActiveCompositeVersion = null;
                nodeConfig.MajorVersion = request.MajorVersion;
                nodeConfig.PrereleaseChannel = request.PrereleaseChannel;
            }

            node.ConfigurationName = request.ConfigurationName;
        }
        else
        {
            var config = await _db.Configurations
                .Include(c => c.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
                .FirstOrDefaultAsync(c => c.Name == request.ConfigurationName, cancellationToken)
                ?? throw new KeyNotFoundException("Configuration not found.");

            var eligibleVersion = VersionResolver.ResolveVersion(config.Versions, v => v.Version, request.MajorVersion, request.PrereleaseChannel)
                ?? throw new InvalidOperationException("No published version satisfies the specified major version and prerelease channel constraints.");

            var nodeConfig = await _db.NodeConfigurations.FindAsync([nodeId], cancellationToken);
            if (nodeConfig is null)
            {
                nodeConfig = new NodeConfiguration
                {
                    NodeId = nodeId,
                    ConfigurationId = config.Id,
                    MajorVersion = request.MajorVersion,
                    PrereleaseChannel = request.PrereleaseChannel,
                    AssignedAt = DateTimeOffset.UtcNow
                };
                _db.NodeConfigurations.Add(nodeConfig);
            }
            else
            {
                nodeConfig.CompositeConfigurationId = null;
                nodeConfig.ActiveCompositeVersion = null;
                nodeConfig.ConfigurationId = config.Id;
                nodeConfig.ActiveVersion = null;
                nodeConfig.MajorVersion = request.MajorVersion;
                nodeConfig.PrereleaseChannel = request.PrereleaseChannel;
            }

            node.ConfigurationName = request.ConfigurationName;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveConfigurationAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        var node = await _db.Nodes.FindAsync([nodeId], cancellationToken)
            ?? throw new KeyNotFoundException("Node not found.");

        var nodeConfig = await _db.NodeConfigurations.FindAsync([nodeId], cancellationToken);
        if (nodeConfig is not null)
        {
            _db.NodeConfigurations.Remove(nodeConfig);
        }

        node.ConfigurationName = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ConfigurationChecksumResponse?> GetConfigurationChecksumAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        var node = await _db.Nodes.FindAsync([nodeId], cancellationToken);
        if (node is null)
        {
            return null;
        }

        node.LastCheckIn = DateTimeOffset.UtcNow;

        var nodeConfig = await _db.NodeConfigurations
            .Include(nc => nc.Configuration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Files)
            .Include(nc => nc.CompositeConfiguration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Items)
            .ThenInclude(i => i.ChildConfiguration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(nc => nc.NodeId == nodeId, cancellationToken);

        if (nodeConfig is null)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }

        string checksum;
        string entryPoint;
        string? parametersFile = null;

        if (nodeConfig.CompositeConfigurationId.HasValue)
        {
            var activeCompositeVersion = VersionResolver.ResolveVersion(
                nodeConfig.CompositeConfiguration!.Versions, v => v.Version, nodeConfig.MajorVersion, nodeConfig.PrereleaseChannel);

            if (activeCompositeVersion is null)
            {
                await _db.SaveChangesAsync(cancellationToken);
                return null;
            }

            var checksumParts = new List<string> { $"composite:{activeCompositeVersion.Version}" };

            foreach (var item in activeCompositeVersion.Items.OrderBy(i => i.Order))
            {
                var childVersion = !string.IsNullOrWhiteSpace(item.ActiveVersion)
                    ? item.ChildConfiguration!.Versions.FirstOrDefault(v => v.Version == item.ActiveVersion)
                    : VersionResolver.ResolveVersion(item.ChildConfiguration!.Versions, v => v.Version, null, nodeConfig.PrereleaseChannel);

                if (childVersion is null)
                {
                    continue;
                }

                checksumParts.Add($"child:{item.ChildConfiguration!.Name}:{childVersion.Version}:{childVersion.EntryPoint}");

                foreach (var file in childVersion.Files.OrderBy(f => f.RelativePath))
                {
                    checksumParts.Add($"{item.ChildConfiguration.Name}/{file.RelativePath}:{file.Checksum}");
                }

                if (item.ChildConfiguration.UseServerManagedParameters)
                {
                    var mergedParams = await _parameterMergeService.MergeParametersAsync(nodeId, item.ChildConfigurationId, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(mergedParams))
                    {
                        var paramHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(mergedParams))).ToLowerInvariant();
                        checksumParts.Add($"params:{item.ChildConfiguration.Name}:{paramHash}");
                    }
                }
            }

            var combined = string.Join("|", checksumParts);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
            checksum = Convert.ToHexString(hash).ToLowerInvariant();
            entryPoint = nodeConfig.CompositeConfiguration!.EntryPoint;
        }
        else
        {
            var activeVersion = VersionResolver.ResolveVersion(
                nodeConfig.Configuration!.Versions, v => v.Version, nodeConfig.MajorVersion, nodeConfig.PrereleaseChannel);

            if (activeVersion is null)
            {
                await _db.SaveChangesAsync(cancellationToken);
                return null;
            }

            var checksumParts = new List<string>
            {
                $"version:{activeVersion.Version}",
                $"entrypoint:{activeVersion.EntryPoint}"
            };

            foreach (var file in activeVersion.Files.OrderBy(f => f.RelativePath))
            {
                checksumParts.Add($"{file.RelativePath}:{file.Checksum}");
            }

            if (nodeConfig.Configuration!.UseServerManagedParameters)
            {
                var mergedParams = await _parameterMergeService.MergeParametersAsync(nodeId, nodeConfig.ConfigurationId!.Value, cancellationToken);
                if (!string.IsNullOrWhiteSpace(mergedParams))
                {
                    var paramHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(mergedParams))).ToLowerInvariant();
                    checksumParts.Add($"params:{paramHash}");
                    parametersFile = "parameters.yaml";
                }
            }

            var combined = string.Join("|", checksumParts);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
            checksum = Convert.ToHexString(hash).ToLowerInvariant();
            entryPoint = activeVersion.EntryPoint;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new ConfigurationChecksumResponse { Checksum = checksum, EntryPoint = entryPoint, ParametersFile = parametersFile };
    }

    public async Task<bool> CheckConfigurationChangedAsync(Guid nodeId, string etag, CancellationToken cancellationToken = default)
    {
        var checksum = await GetConfigurationChecksumAsync(nodeId, cancellationToken);
        return checksum is null || !string.Equals(checksum.Checksum, etag, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<RotateCertificateResponse> RotateCertificateAsync(Guid nodeId, RotateCertificateRequest request, CancellationToken cancellationToken = default)
    {
        var node = await _db.Nodes.FindAsync([nodeId], cancellationToken)
            ?? throw new KeyNotFoundException("Node not found.");

        node.CertificateThumbprint = request.CertificateThumbprint;
        node.CertificateSubject = request.CertificateSubject;
        node.CertificateNotAfter = request.CertificateNotAfter;
        node.LastCheckIn = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new RotateCertificateResponse { Message = "Certificate updated successfully" };
    }

    public async Task UpdateLcmStatusAsync(Guid nodeId, UpdateLcmStatusRequest request, CancellationToken cancellationToken = default)
    {
        var node = await _db.Nodes.FindAsync([nodeId], cancellationToken)
            ?? throw new KeyNotFoundException("Node not found.");

        node.LcmStatus = request.LcmStatus;
        node.LastCheckIn = DateTimeOffset.UtcNow;
        _db.NodeStatusEvents.Add(new NodeStatusEvent
        {
            NodeId = nodeId,
            LcmStatus = request.LcmStatus,
            Timestamp = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<NodeLcmConfigResponse?> GetNodeLcmConfigAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        var node = await _db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken)
            ?? throw new KeyNotFoundException("Node not found.");

        var serverSettings = await _db.ServerSettings.FindAsync([1], cancellationToken);
        return new NodeLcmConfigResponse
        {
            ConfigurationMode = node.DesiredConfigurationMode ?? serverSettings?.DefaultConfigurationMode,
            ConfigurationModeInterval = node.DesiredConfigurationModeInterval ?? serverSettings?.DefaultConfigurationModeInterval,
            ReportCompliance = node.DesiredReportCompliance ?? serverSettings?.DefaultReportCompliance,
            CertificateRotationInterval = serverSettings?.CertificateRotationInterval
        };
    }

    public async Task<NodeLcmConfigResponse?> UpdateNodeLcmConfigAsync(Guid nodeId, UpdateNodeLcmConfigRequest request, CancellationToken cancellationToken = default)
    {
        var node = await _db.Nodes.FindAsync([nodeId], cancellationToken)
            ?? throw new KeyNotFoundException("Node not found.");

        node.DesiredConfigurationMode = request.ConfigurationMode;
        node.DesiredConfigurationModeInterval = request.ConfigurationModeInterval;
        node.DesiredReportCompliance = request.ReportCompliance;
        await _db.SaveChangesAsync(cancellationToken);

        return new NodeLcmConfigResponse
        {
            ConfigurationMode = node.DesiredConfigurationMode,
            ConfigurationModeInterval = node.DesiredConfigurationModeInterval,
            ReportCompliance = node.DesiredReportCompliance
        };
    }

    public async Task ReportNodeLcmConfigAsync(Guid nodeId, ReportNodeLcmConfigRequest request, CancellationToken cancellationToken = default)
    {
        var node = await _db.Nodes.FindAsync([nodeId], cancellationToken)
            ?? throw new KeyNotFoundException("Node not found.");

        node.ConfigurationMode = request.ConfigurationMode;
        node.ConfigurationModeInterval = request.ConfigurationModeInterval;
        node.ReportCompliance = request.ReportCompliance;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<RegistrationSettingsSummary> GetRegistrationSettingsAsync(CancellationToken cancellationToken = default)
        => _db.RegistrationKeys
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new RegistrationSettingsSummary
            {
                RegistrationEnabled = true,
                ActiveKeyCount = g.Count()
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ContinueWith(t => t.Result ?? new RegistrationSettingsSummary { RegistrationEnabled = true, ActiveKeyCount = 0 }, cancellationToken);

    private static NodeSummary ToNodeSummary(Node node, double stalenessMultiplier, DateTimeOffset now)
    {
        return new NodeSummary
        {
            Id = node.Id,
            Fqdn = node.Fqdn,
            ConfigurationName = node.ConfigurationName,
            Status = node.Status.ToString(),
            LcmStatus = node.LcmStatus.ToString(),
            IsStale = node.LastCheckIn.HasValue
                && node.ConfigurationModeInterval.HasValue
                && (now - node.LastCheckIn.Value) > node.ConfigurationModeInterval.Value * stalenessMultiplier,
            LastCheckIn = node.LastCheckIn,
            CreatedAt = node.CreatedAt,
            ConfigurationSource = node.ConfigurationSource,
            ConfigurationMode = node.ConfigurationMode,
            ConfigurationModeInterval = node.ConfigurationModeInterval,
            ReportCompliance = node.ReportCompliance,
            DesiredConfigurationMode = node.DesiredConfigurationMode,
            DesiredConfigurationModeInterval = node.DesiredConfigurationModeInterval,
            DesiredReportCompliance = node.DesiredReportCompliance
        };
    }

    private async Task<string?> BuildConfigurationContentAsync(Guid nodeId, NodeConfiguration nodeConfig, CancellationToken cancellationToken)
    {
        if (nodeConfig.CompositeConfigurationId.HasValue)
        {
            var activeCompositeVersion = VersionResolver.ResolveVersion(
                nodeConfig.CompositeConfiguration!.Versions, v => v.Version, nodeConfig.MajorVersion, nodeConfig.PrereleaseChannel);

            if (activeCompositeVersion is null)
            {
                return null;
            }

            return $"$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json\nresources:\n  - name: {nodeConfig.CompositeConfiguration.Name}\n    type: Microsoft.DSC/Include\n    properties:\n      configurationFile: {nodeConfig.CompositeConfiguration.EntryPoint}\n";
        }

        var activeVersion = VersionResolver.ResolveVersion(
            nodeConfig.Configuration!.Versions, v => v.Version, nodeConfig.MajorVersion, nodeConfig.PrereleaseChannel);

        if (activeVersion is null)
        {
            return null;
        }

        return $"$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json\nresources:\n  - name: {nodeConfig.Configuration.Name}\n    type: Microsoft.DSC/Include\n    properties:\n      configurationFile: {activeVersion.EntryPoint}\n";
    }

    private async Task<NodeConfigurationBundle> BuildCompositeBundleAsync(Guid nodeId, NodeConfiguration nodeConfig, string dataDir, CancellationToken cancellationToken)
    {
        var activeCompositeVersion = VersionResolver.ResolveVersion(
            nodeConfig.CompositeConfiguration!.Versions, v => v.Version, nodeConfig.MajorVersion, nodeConfig.PrereleaseChannel);

        if (activeCompositeVersion is null)
        {
            throw new InvalidOperationException("No published composite version available.");
        }

        var bundleStream = new MemoryStream();
        using (var archive = new ZipArchive(bundleStream, ZipArchiveMode.Create, true))
        {
            var includeResources = new List<string>();

            foreach (var item in activeCompositeVersion.Items)
            {
                var childVersion = !string.IsNullOrWhiteSpace(item.ActiveVersion)
                    ? item.ChildConfiguration.Versions.FirstOrDefault(v => v.Version == item.ActiveVersion)
                    : VersionResolver.ResolveVersion(item.ChildConfiguration.Versions, v => v.Version, null, nodeConfig.PrereleaseChannel);

                if (childVersion is null)
                {
                    continue;
                }

                var childVersionDir = Path.Combine(dataDir, item.ChildConfiguration.Name, $"v{childVersion.Version}");
                var childFolderName = item.ChildConfiguration.Name;

                foreach (var file in childVersion.Files)
                {
                    var sourcePath = Path.Combine(childVersionDir, file.RelativePath);
                    if (!File.Exists(sourcePath))
                    {
                        continue;
                    }

                    var entryPath = $"{childFolderName}/{file.RelativePath}";
                    var entry = archive.CreateEntry(entryPath);
                    using var entryStream = entry.Open();
                    using var fileStream = File.OpenRead(sourcePath);
                    await fileStream.CopyToAsync(entryStream, cancellationToken);
                }

                string? childParametersFile = null;
                if (item.ChildConfiguration.UseServerManagedParameters)
                {
                    var mergedParameters = await _parameterMergeService.MergeParametersAsync(nodeId, item.ChildConfigurationId, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(mergedParameters))
                    {
                        childParametersFile = $"{childFolderName}/parameters.yaml";
                        var paramEntry = archive.CreateEntry(childParametersFile);
                        using var paramStream = paramEntry.Open();
                        using var writer = new StreamWriter(paramStream);
                        await writer.WriteAsync(mergedParameters);
                    }
                }

                var childEntryPoint = childVersion.EntryPoint;
                var includeProps = $"      configurationFile: {childFolderName}/{childEntryPoint}";
                if (childParametersFile is not null)
                {
                    includeProps += $"\n      parametersFile: {childParametersFile}";
                }

                includeResources.Add($"  - name: {item.ChildConfiguration.Name}\n    type: Microsoft.DSC/Include\n    properties:\n{includeProps}");
            }

            var mainContent = $"$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json\nresources:\n{string.Join("\n", includeResources)}\n";
            var mainEntry = archive.CreateEntry(nodeConfig.CompositeConfiguration.EntryPoint);
            using (var mainStream = mainEntry.Open())
            using (var writer = new StreamWriter(mainStream))
            {
                await writer.WriteAsync(mainContent);
            }
        }

        return new NodeConfigurationBundle
        {
            Content = bundleStream.ToArray(),
            FileName = $"{nodeConfig.CompositeConfiguration.Name}-v{activeCompositeVersion.Version}.zip",
            ContentType = "application/zip"
        };
    }
}
