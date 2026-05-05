// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Frozen;

namespace OpenDsc.Server.Authorization;

/// <summary>
/// Retention policy permissions.
/// </summary>
public static class RetentionPermissions
{
    public const string Manage = "retention.manage";

    public static readonly FrozenSet<string> All = new[]
    {
        Manage,
    }.ToFrozenSet(StringComparer.Ordinal);
}
