// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.ServerPermission;

[Description("SQL Server server-level permission names.")]
public enum ServerPermissionName
{
    [Description("Administer bulk operations.")]
    AdministerBulkOperations,

    [Description("Alter any availability group.")]
    AlterAnyAvailabilityGroup,

    [Description("Alter any connection.")]
    AlterAnyConnection,

    [Description("Alter any credential.")]
    AlterAnyCredential,

    [Description("Alter any database.")]
    AlterAnyDatabase,

    [Description("Alter any endpoint.")]
    AlterAnyEndpoint,

    [Description("Alter any event notification.")]
    AlterAnyEventNotification,

    [Description("Alter any event session.")]
    AlterAnyEventSession,

    [Description("Alter any event session add event.")]
    AlterAnyEventSessionAddEvent,

    [Description("Alter any event session add target.")]
    AlterAnyEventSessionAddTarget,

    [Description("Alter any event session disable.")]
    AlterAnyEventSessionDisable,

    [Description("Alter any event session drop event.")]
    AlterAnyEventSessionDropEvent,

    [Description("Alter any event session drop target.")]
    AlterAnyEventSessionDropTarget,

    [Description("Alter any event session enable.")]
    AlterAnyEventSessionEnable,

    [Description("Alter any event session option.")]
    AlterAnyEventSessionOption,

    [Description("Alter any linked server.")]
    AlterAnyLinkedServer,

    [Description("Alter any login.")]
    AlterAnyLogin,

    [Description("Alter any server audit.")]
    AlterAnyServerAudit,

    [Description("Alter any server role.")]
    AlterAnyServerRole,

    [Description("Alter resources.")]
    AlterResources,

    [Description("Alter server state.")]
    AlterServerState,

    [Description("Alter settings.")]
    AlterSettings,

    [Description("Alter trace.")]
    AlterTrace,

    [Description("Authenticate server.")]
    AuthenticateServer,

    [Description("Connect to any database.")]
    ConnectAnyDatabase,

    [Description("Connect to SQL Server.")]
    ConnectSql,

    [Description("Control server.")]
    ControlServer,

    [Description("Create any database.")]
    CreateAnyDatabase,

    [Description("Create any event session.")]
    CreateAnyEventSession,

    [Description("Create availability group.")]
    CreateAvailabilityGroup,

    [Description("Create DDL event notification.")]
    CreateDdlEventNotification,

    [Description("Create endpoint.")]
    CreateEndpoint,

    [Description("Create login.")]
    CreateLogin,

    [Description("Create server role.")]
    CreateServerRole,

    [Description("Create trace event notification.")]
    CreateTraceEventNotification,

    [Description("Drop any event session.")]
    DropAnyEventSession,

    [Description("External access assembly.")]
    ExternalAccessAssembly,

    [Description("Impersonate any login.")]
    ImpersonateAnyLogin,

    [Description("Select all user securables.")]
    SelectAllUserSecurables,

    [Description("Shutdown the server.")]
    Shutdown,

    [Description("Unsafe assembly.")]
    UnsafeAssembly,

    [Description("View any cryptographically secured definition.")]
    ViewAnyCryptographicallySecuredDefinition,

    [Description("View any database.")]
    ViewAnyDatabase,

    [Description("View any definition.")]
    ViewAnyDefinition,

    [Description("View any error log.")]
    ViewAnyErrorLog,

    [Description("View any performance definition.")]
    ViewAnyPerformanceDefinition,

    [Description("View any security definition.")]
    ViewAnySecurityDefinition,

    [Description("View server performance state.")]
    ViewServerPerformanceState,

    [Description("View server security audit.")]
    ViewServerSecurityAudit,

    [Description("View server security state.")]
    ViewServerSecurityState,

    [Description("View server state.")]
    ViewServerState
}
