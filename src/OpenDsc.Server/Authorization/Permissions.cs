// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Authorization;

/// <summary>
/// Global permissions for server-wide operations.
/// </summary>
public static class Permissions
{
    // Server Administration
    public const string ServerSettings_Read = "server.settings.read";
    public const string ServerSettings_Write = "server.settings.write";
    public const string Users_Manage = "users.manage";
    public const string Groups_Manage = "groups.manage";
    public const string Roles_Manage = "roles.manage";
    public const string RegistrationKeys_Manage = "registration-keys.manage";

    // Node Management (global only)
    public const string Nodes_Read = "nodes.read";
    public const string Nodes_Write = "nodes.write";
    public const string Nodes_Delete = "nodes.delete";
    public const string Nodes_AssignConfiguration = "nodes.assign-configuration";

    // Reporting (global only)
    public const string Reports_Read = "reports.read";
    public const string Reports_ReadAll = "reports.read-all";

    // Retention Policies
    public const string Retention_Manage = "retention.manage";

    // Admin Override Permissions (bypass resource ACLs)
    public const string Configurations_AdminOverride = "configurations.admin-override";
    public const string CompositeConfigurations_AdminOverride = "composite-configurations.admin-override";
    public const string Parameters_AdminOverride = "parameters.admin-override";
    public const string Scopes_AdminOverride = "scopes.admin-override";
}
