// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Contracts.Parameters;

namespace OpenDsc.Server.Services;

public sealed partial class ParameterMergeService(ServerDbContext db, IParameterMerger merger, IOptions<ServerConfig> serverConfig, ILogger<ParameterMergeService> logger) : IParameterMergeService
{
    public async Task<string?> MergeParametersAsync(Guid nodeId, Guid configurationId, CancellationToken cancellationToken = default)
    {
        LogMergingParameters(nodeId, configurationId);
        var configuration = await db.Configurations
            .FirstOrDefaultAsync(c => c.Id == configurationId, cancellationToken);

        if (configuration is null)
        {
            return null;
        }

        var node = await db.Nodes
            .FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);

        if (node is null)
        {
            return null;
        }

        var nodeConfiguration = await db.NodeConfigurations
            .FirstOrDefaultAsync(nc => nc.NodeId == nodeId, cancellationToken);

        var prereleaseChannel = nodeConfiguration?.PrereleaseChannel;

        var configName = configuration.Name;
        var nodeFqdn = node.Fqdn;
        var dataDir = serverConfig.Value.ParametersDirectory;

        var parameterSources = new List<ParameterSource>();

        var nodeTags = await db.NodeTags
            .Include(nt => nt.ScopeValue)
            .ThenInclude(sv => sv.ScopeType)
            .Where(nt => nt.NodeId == nodeId)
            .OrderBy(nt => nt.ScopeValue.ScopeType.Precedence)
            .Select(nt => new
            {
                ScopeTypeId = nt.ScopeValue.ScopeTypeId,
                ScopeTypeName = nt.ScopeValue.ScopeType.Name,
                ScopeValue = nt.ScopeValue.Value,
                Precedence = nt.ScopeValue.ScopeType.Precedence,
                ValueMode = nt.ScopeValue.ScopeType.ValueMode,
                IsEnabled = nt.ScopeValue.ScopeType.IsEnabled
            })
            .ToListAsync(cancellationToken);

        var scopeTypes = new HashSet<Guid>(nodeTags.Select(nt => nt.ScopeTypeId));

        var defaultScopeType = await db.ScopeTypes
            .FirstOrDefaultAsync(st => st.Name == "Default", cancellationToken);

        if (defaultScopeType != null && defaultScopeType.IsEnabled && !scopeTypes.Contains(defaultScopeType.Id))
        {
            var defaultCandidates = await db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                .Where(pf =>
                    pf.ParameterSchema!.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == defaultScopeType.Id &&
                    pf.Status == ParameterVersionStatus.Published)
                .ToListAsync(cancellationToken);

            var defaultParamFile = VersionResolver.ResolveVersion(
                defaultCandidates, pf => pf.Version, majorVersion: null, prereleaseChannel);

            if (defaultParamFile != null && !defaultParamFile.IsPassthrough)
            {
                var defaultPath = Path.Combine(dataDir, configName, "Default", $"v{defaultParamFile.Version}", "parameters.yaml");

                if (File.Exists(defaultPath))
                {
                    var content = await File.ReadAllTextAsync(defaultPath, cancellationToken);
                    parameterSources.Add(new ParameterSource
                    {
                        ScopeTypeName = "Default",
                        ScopeValue = null,
                        Precedence = defaultScopeType.Precedence,
                        Content = content
                    });
                }
            }
        }

        foreach (var tag in nodeTags)
        {
            if (!tag.IsEnabled)
            {
                continue;
            }

            var tagCandidates = await db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                .Where(pf =>
                    pf.ParameterSchema!.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == tag.ScopeTypeId &&
                    pf.ScopeValue == tag.ScopeValue &&
                    pf.Status == ParameterVersionStatus.Published)
                .ToListAsync(cancellationToken);

            var paramFile = VersionResolver.ResolveVersion(
                tagCandidates, pf => pf.Version, majorVersion: null, prereleaseChannel);

            if (paramFile is null || paramFile.IsPassthrough)
            {
                continue;
            }

            var filePath = Path.Combine(dataDir, configName, tag.ScopeTypeName, tag.ScopeValue, $"v{paramFile.Version}", "parameters.yaml");

            if (!File.Exists(filePath))
            {
                continue;
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            parameterSources.Add(new ParameterSource
            {
                ScopeTypeName = tag.ScopeTypeName,
                ScopeValue = tag.ScopeValue,
                Precedence = tag.Precedence,
                Content = content
            });
        }

        var nodeScopeType = await db.ScopeTypes
            .FirstOrDefaultAsync(st => st.Name == "Node", cancellationToken);

        if (nodeScopeType != null && nodeScopeType.IsEnabled)
        {
            var nodeCandidates = await db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                .Where(pf =>
                    pf.ParameterSchema!.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == nodeScopeType.Id &&
                    pf.ScopeValue == nodeFqdn &&
                    pf.Status == ParameterVersionStatus.Published)
                .ToListAsync(cancellationToken);

            var nodeParamFile = VersionResolver.ResolveVersion(
                nodeCandidates, pf => pf.Version, majorVersion: null, prereleaseChannel);

            if (nodeParamFile != null && !nodeParamFile.IsPassthrough)
            {
                var nodePath = Path.Combine(dataDir, configName, "Node", nodeFqdn, $"v{nodeParamFile.Version}", "parameters.yaml");

                if (File.Exists(nodePath))
                {
                    var content = await File.ReadAllTextAsync(nodePath, cancellationToken);
                    parameterSources.Add(new ParameterSource
                    {
                        ScopeTypeName = "Node",
                        ScopeValue = nodeFqdn,
                        Precedence = nodeScopeType.Precedence,
                        Content = content
                    });
                }
            }
        }

        if (parameterSources.Count == 0)
        {
            LogNoParameterSourcesFound(nodeId, configurationId);
            return null;
        }

        var mergedContent = merger.Merge(parameterSources.Select(ps => ps.Content));
        LogParameterMergeComplete(nodeId, configurationId, parameterSources.Count);
        return mergedContent;
    }

    public async Task<MergeResult?> MergeParametersWithProvenanceAsync(Guid nodeId, Guid configurationId, CancellationToken cancellationToken = default)
    {
        var configuration = await db.Configurations
            .FirstOrDefaultAsync(c => c.Id == configurationId, cancellationToken);

        if (configuration is null)
        {
            return null;
        }

        var node = await db.Nodes
            .FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);

        if (node is null)
        {
            return null;
        }

        var nodeConfiguration = await db.NodeConfigurations
            .FirstOrDefaultAsync(nc => nc.NodeId == nodeId, cancellationToken);

        var prereleaseChannel = nodeConfiguration?.PrereleaseChannel;

        var configName = configuration.Name;
        var nodeFqdn = node.Fqdn;
        var dataDir = serverConfig.Value.ParametersDirectory;

        var parameterSources = new List<ParameterSource>();

        var nodeTags = await db.NodeTags
            .Include(nt => nt.ScopeValue)
            .ThenInclude(sv => sv.ScopeType)
            .Where(nt => nt.NodeId == nodeId)
            .OrderBy(nt => nt.ScopeValue.ScopeType.Precedence)
            .Select(nt => new
            {
                ScopeTypeId = nt.ScopeValue.ScopeTypeId,
                ScopeTypeName = nt.ScopeValue.ScopeType.Name,
                ScopeValue = nt.ScopeValue.Value,
                Precedence = nt.ScopeValue.ScopeType.Precedence,
                ValueMode = nt.ScopeValue.ScopeType.ValueMode,
                IsEnabled = nt.ScopeValue.ScopeType.IsEnabled
            })
            .ToListAsync(cancellationToken);

        var scopeTypes = new HashSet<Guid>(nodeTags.Select(nt => nt.ScopeTypeId));

        var defaultScopeType = await db.ScopeTypes
            .FirstOrDefaultAsync(st => st.Name == "Default", cancellationToken);

        if (defaultScopeType != null && defaultScopeType.IsEnabled && !scopeTypes.Contains(defaultScopeType.Id))
        {
            var defaultCandidates = await db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                .Where(pf =>
                    pf.ParameterSchema!.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == defaultScopeType.Id &&
                    pf.Status == ParameterVersionStatus.Published)
                .ToListAsync(cancellationToken);

            var defaultParamFile = VersionResolver.ResolveVersion(
                defaultCandidates, pf => pf.Version, majorVersion: null, prereleaseChannel);

            if (defaultParamFile != null && !defaultParamFile.IsPassthrough)
            {
                var defaultPath = Path.Combine(dataDir, configName, "Default", $"v{defaultParamFile.Version}", "parameters.yaml");

                if (File.Exists(defaultPath))
                {
                    var content = await File.ReadAllTextAsync(defaultPath, cancellationToken);
                    parameterSources.Add(new ParameterSource
                    {
                        ScopeTypeName = "Default",
                        ScopeValue = null,
                        Precedence = defaultScopeType.Precedence,
                        Content = content
                    });
                }
            }
        }

        foreach (var tag in nodeTags)
        {
            if (!tag.IsEnabled)
            {
                continue;
            }

            var tagCandidates = await db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                .Where(pf =>
                    pf.ParameterSchema!.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == tag.ScopeTypeId &&
                    pf.ScopeValue == tag.ScopeValue &&
                    pf.Status == ParameterVersionStatus.Published)
                .ToListAsync(cancellationToken);

            var paramFile = VersionResolver.ResolveVersion(
                tagCandidates, pf => pf.Version, majorVersion: null, prereleaseChannel);

            if (paramFile is null || paramFile.IsPassthrough)
            {
                continue;
            }

            var filePath = Path.Combine(dataDir, configName, tag.ScopeTypeName, tag.ScopeValue, $"v{paramFile.Version}", "parameters.yaml");

            if (!File.Exists(filePath))
            {
                continue;
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            parameterSources.Add(new ParameterSource
            {
                ScopeTypeName = tag.ScopeTypeName,
                ScopeValue = tag.ScopeValue,
                Precedence = tag.Precedence,
                Content = content
            });
        }

        var nodeScopeType = await db.ScopeTypes
            .FirstOrDefaultAsync(st => st.Name == "Node", cancellationToken);

        if (nodeScopeType != null && nodeScopeType.IsEnabled)
        {
            var nodeCandidates = await db.ParameterFiles
                .Include(pf => pf.ParameterSchema)
                .Where(pf =>
                    pf.ParameterSchema!.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == nodeScopeType.Id &&
                    pf.ScopeValue == nodeFqdn &&
                    pf.Status == ParameterVersionStatus.Published)
                .ToListAsync(cancellationToken);

            var nodeParamFile = VersionResolver.ResolveVersion(
                nodeCandidates, pf => pf.Version, majorVersion: null, prereleaseChannel);

            if (nodeParamFile != null && !nodeParamFile.IsPassthrough)
            {
                var nodePath = Path.Combine(dataDir, configName, "Node", nodeFqdn, $"v{nodeParamFile.Version}", "parameters.yaml");

                if (File.Exists(nodePath))
                {
                    var content = await File.ReadAllTextAsync(nodePath, cancellationToken);
                    parameterSources.Add(new ParameterSource
                    {
                        ScopeTypeName = "Node",
                        ScopeValue = nodeFqdn,
                        Precedence = nodeScopeType.Precedence,
                        Content = content
                    });
                }
            }
        }

        if (parameterSources.Count == 0)
        {
            return null;
        }

        return merger.MergeWithProvenance(parameterSources);
    }

    [LoggerMessage(EventId = EventIds.MergingParameters, Level = LogLevel.Debug, Message = "Merging parameters for node {NodeId}, configuration {ConfigurationId}")]
    private partial void LogMergingParameters(Guid nodeId, Guid configurationId);

    [LoggerMessage(EventId = EventIds.NoParameterSourcesFound, Level = LogLevel.Debug, Message = "No parameter sources found for node {NodeId}, configuration {ConfigurationId}")]
    private partial void LogNoParameterSourcesFound(Guid nodeId, Guid configurationId);

    [LoggerMessage(EventId = EventIds.ParameterMergeComplete, Level = LogLevel.Debug, Message = "Merged {SourceCount} parameter source(s) for node {NodeId}, configuration {ConfigurationId}")]
    private partial void LogParameterMergeComplete(Guid nodeId, Guid configurationId, int sourceCount);
}
