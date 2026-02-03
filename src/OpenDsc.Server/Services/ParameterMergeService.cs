// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Parameters;
using OpenDsc.Server.Data;

namespace OpenDsc.Server.Services;

public sealed class ParameterMergeService(ServerDbContext db, IParameterMerger merger, IConfiguration config) : IParameterMergeService
{
    public async Task<string?> MergeParametersAsync(Guid nodeId, Guid configurationId, CancellationToken cancellationToken = default)
    {
        var nodeConfig = await db.NodeConfigurations
            .Include(nc => nc.Configuration)
            .FirstOrDefaultAsync(nc => nc.NodeId == nodeId && nc.ConfigurationId == configurationId, cancellationToken);

        if (nodeConfig is null || !nodeConfig.UseServerManagedParameters)
        {
            return null;
        }

        var scopeAssignments = await db.NodeScopeAssignments
            .Include(nsa => nsa.Scope)
            .Where(nsa => nsa.NodeId == nodeId)
            .OrderBy(nsa => nsa.Scope.Precedence)
            .Select(nsa => nsa.Scope)
            .ToListAsync(cancellationToken);

        if (scopeAssignments.Count == 0)
        {
            return null;
        }

        var parameterVersions = new List<(int Precedence, string Content)>();

        foreach (var scope in scopeAssignments)
        {
            var paramVersion = await db.ParameterVersions
                .FirstOrDefaultAsync(pv =>
                    pv.ScopeId == scope.Id &&
                    pv.ConfigurationId == configurationId &&
                    pv.IsActive,
                    cancellationToken);

            if (paramVersion is null)
            {
                continue;
            }

            var dataDir = config["DataDirectory"] ?? "data";
            var parameterFile = Path.Combine(dataDir, "parameters", scope.Name, nodeConfig.Configuration.Name, $"v{paramVersion.Version}", "parameters.yaml");

            if (!File.Exists(parameterFile))
            {
                continue;
            }

            var content = await File.ReadAllTextAsync(parameterFile, cancellationToken);
            parameterVersions.Add((scope.Precedence, content));
        }

        if (parameterVersions.Count == 0)
        {
            return null;
        }

        var mergedContent = merger.Merge([.. parameterVersions.Select(pv => pv.Content)]);
        return mergedContent;
    }
}
