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
    public const string SettingsRead = "server.settings.read";
    public const string SettingsWrite = "server.settings.write";
    public const string UsersManage = "users.manage";
    public const string GroupsManage = "groups.manage";
    public const string RolesManage = "roles.manage";
    public const string RegistrationKeysManage = "registration-keys.manage";

    public static readonly FrozenSet<string> All = new[]
    {
        SettingsRead,
        SettingsWrite,
        UsersManage,
        GroupsManage,
        RolesManage,
        RegistrationKeysManage,
    }.ToFrozenSet(StringComparer.Ordinal);
}
