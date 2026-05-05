// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Frozen;

namespace OpenDsc.Server.Authorization;

/// <summary>
/// Server administration permissions.
/// </summary>
public static class ServerPermissions
{
    public const string Settings_Read = "server.settings.read";
    public const string Settings_Write = "server.settings.write";
    public const string Users_Manage = "users.manage";
    public const string Groups_Manage = "groups.manage";
    public const string Roles_Manage = "roles.manage";
    public const string RegistrationKeys_Manage = "registration-keys.manage";

    public static readonly FrozenSet<string> All = new[]
    {
        Settings_Read,
        Settings_Write,
        Users_Manage,
        Groups_Manage,
        Roles_Manage,
        RegistrationKeys_Manage,
    }.ToFrozenSet(StringComparer.Ordinal);
}
