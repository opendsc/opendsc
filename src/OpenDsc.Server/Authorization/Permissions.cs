// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Frozen;

namespace OpenDsc.Server.Authorization;

/// <summary>
/// Aggregated set of all valid PAT scopes across all permission areas.
/// </summary>
public static class Permissions
{
    public static readonly FrozenSet<string> AllScopes =
        ServerPermissions.All
        .Union(NodePermissions.All)
        .Union(ReportPermissions.All)
        .Union(RetentionPermissions.All)
        .Union(ConfigurationPermissions.All)
        .Union(CompositeConfigurationPermissions.All)
        .Union(ParameterPermissions.All)
        .Union(ScopePermissions.All)
        .ToFrozenSet(StringComparer.Ordinal);
}
