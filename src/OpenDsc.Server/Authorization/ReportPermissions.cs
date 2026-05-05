// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Frozen;

namespace OpenDsc.Server.Authorization;

/// <summary>
/// Reporting permissions.
/// </summary>
public static class ReportPermissions
{
    public const string Read = "reports.read";
    public const string ReadAll = "reports.read-all";

    public static readonly FrozenSet<string> All = new[]
    {
        Read,
        ReadAll,
    }.ToFrozenSet(StringComparer.Ordinal);
}
