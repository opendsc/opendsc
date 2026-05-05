// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Frozen;

namespace OpenDsc.Server.Authorization;

/// <summary>
/// Node management permissions.
/// </summary>
public static class NodePermissions
{
    public const string Read = "nodes.read";
    public const string Write = "nodes.write";
    public const string Delete = "nodes.delete";
    public const string AssignConfiguration = "nodes.assign-configuration";

    public static readonly FrozenSet<string> All = new[]
    {
        Read,
        Write,
        Delete,
        AssignConfiguration,
    }.ToFrozenSet(StringComparer.Ordinal);
}
