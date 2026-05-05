// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Frozen;

namespace OpenDsc.Server.Authorization;

/// <summary>
/// Composite configuration admin-override permissions.
/// </summary>
public static class CompositeConfigurationPermissions
{
    public const string AdminOverride = "composite-configurations.admin-override";

    public static readonly FrozenSet<string> All = new[]
    {
        AdminOverride,
    }.ToFrozenSet(StringComparer.Ordinal);
}
