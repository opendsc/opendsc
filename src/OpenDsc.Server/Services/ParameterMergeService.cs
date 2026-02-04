// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Data;

namespace OpenDsc.Server.Services;

public sealed class ParameterMergeService(ServerDbContext db, IParameterMerger merger, IConfiguration config) : IParameterMergeService
{
    public async Task<string?> MergeParametersAsync(Guid nodeId, Guid configurationId, CancellationToken cancellationToken = default)
    {
        var nodeConfig = await db.NodeConfigurations
            .Include(nc => nc.Configuration)
            .Include(nc => nc.Node)
            .FirstOrDefaultAsync(nc => nc.NodeId == nodeId && nc.ConfigurationId == configurationId, cancellationToken);

        if (nodeConfig is null || !nodeConfig.UseServerManagedParameters)
        {
            return null;
        }

        var configName = nodeConfig.Configuration.Name;
        var nodeFqdn = nodeConfig.Node.Fqdn;
        var dataDir = config["DataDirectory"] ?? "data";

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
                AllowsValues = nt.ScopeValue.ScopeType.AllowsValues
            })
            .ToListAsync(cancellationToken);

        var scopeTypes = new HashSet<Guid>(nodeTags.Select(nt => nt.ScopeTypeId));

        var defaultScopeType = await db.ScopeTypes
            .FirstOrDefaultAsync(st => st.Name == "Default", cancellationToken);

        if (defaultScopeType != null && !scopeTypes.Contains(defaultScopeType.Id))
        {
            var defaultParamFile = await db.ParameterFiles
                .FirstOrDefaultAsync(pf =>
                    pf.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == defaultScopeType.Id &&
                    pf.IsActive,
                    cancellationToken);

            if (defaultParamFile != null)
            {
                var defaultPath = Path.Combine(dataDir, "parameters", configName, "Default", "parameters.yaml");

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
            var paramFile = await db.ParameterFiles
                .FirstOrDefaultAsync(pf =>
                    pf.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == tag.ScopeTypeId &&
                    pf.ScopeValue == tag.ScopeValue &&
                    pf.IsActive,
                    cancellationToken);

            if (paramFile is null)
            {
                continue;
            }

            var filePath = Path.Combine(dataDir, "parameters", configName, tag.ScopeTypeName, tag.ScopeValue, "parameters.yaml");

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

        if (nodeScopeType != null)
        {
            var nodeParamFile = await db.ParameterFiles
                .FirstOrDefaultAsync(pf =>
                    pf.ConfigurationId == configurationId &&
                    pf.ScopeTypeId == nodeScopeType.Id &&
                    pf.ScopeValue == nodeFqdn &&
                    pf.IsActive,
                    cancellationToken);

            if (nodeParamFile != null)
            {
                var nodePath = Path.Combine(dataDir, "parameters", configName, "Node", nodeFqdn, "parameters.yaml");

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

        var mergedContent = merger.Merge(parameterSources.Select(ps => ps.Content));
        return mergedContent;
    }
}
