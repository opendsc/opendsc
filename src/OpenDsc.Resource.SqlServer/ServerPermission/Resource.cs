// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using Microsoft.SqlServer.Management.Smo;

using SmoServerPermission = Microsoft.SqlServer.Management.Smo.ServerPermission;
using SmoPermissionState = Microsoft.SqlServer.Management.Smo.PermissionState;

namespace OpenDsc.Resource.SqlServer.ServerPermission;

[DscResource("OpenDsc.SqlServer/ServerPermission", "0.1.0", Description = "Manage SQL Server server-level permissions", Tags = ["sql", "sqlserver", "server", "permission", "security"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(4, Exception = typeof(UnauthorizedAccessException), Description = "Unauthorized access")]
[ExitCode(5, Exception = typeof(InvalidOperationException), Description = "Invalid operation")]
public sealed class Resource(JsonSerializerContext context)
    : DscResource<Schema>(context),
      IGettable<Schema>,
      ISettable<Schema>,
      ITestable<Schema>,
      IDeletable<Schema>,
      IExportable<Schema>
{
    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        return JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema instance)
    {
        var server = new Server(instance.ServerInstance);
        server.ConnectionContext.ConnectTimeout = 30;

        try
        {
            // Get all permissions for this principal
            var permissions = server.EnumServerPermissions(instance.Principal);

            // Find the matching permission
            var matchingPermission = permissions
                .Cast<ServerPermissionInfo>()
                .FirstOrDefault(p => HasPermission(p.PermissionType, instance.Permission));

            if (matchingPermission == null)
            {
                return new Schema
                {
                    ServerInstance = instance.ServerInstance,
                    Principal = instance.Principal,
                    Permission = instance.Permission,
                    Exist = false
                };
            }

            return new Schema
            {
                ServerInstance = instance.ServerInstance,
                Principal = matchingPermission.Grantee,
                Permission = instance.Permission,
                State = MapSmoPermissionState(matchingPermission.PermissionState),
                Grantor = matchingPermission.Grantor
            };
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        var server = new Server(instance.ServerInstance);
        server.ConnectionContext.ConnectTimeout = 30;

        try
        {
            var permissionSet = MapPermissionNameToPermissionSet(instance.Permission);
            var desiredState = instance.State ?? PermissionState.Grant;

            var currentPermissions = server.EnumServerPermissions(instance.Principal);
            var currentMatch = currentPermissions
                .Cast<ServerPermissionInfo>()
                .FirstOrDefault(p => HasPermission(p.PermissionType, instance.Permission));

            if (currentMatch != null)
            {
                var currentState = MapSmoPermissionState(currentMatch.PermissionState);
                if (currentState == desiredState)
                {
                    return null;
                }

                server.Revoke(permissionSet, instance.Principal, false, false);
            }

            switch (desiredState)
            {
                case PermissionState.Grant:
                    server.Grant(permissionSet, instance.Principal);
                    break;

                case PermissionState.GrantWithGrant:
                    server.Grant(permissionSet, instance.Principal, true);
                    break;

                case PermissionState.Deny:
                    server.Deny(permissionSet, instance.Principal);
                    break;
            }

            return null;
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    public TestResult<Schema> Test(Schema instance)
    {
        var actualState = Get(instance);
        bool inDesiredState = true;

        if (instance.Exist == false)
        {
            inDesiredState = actualState.Exist == false;
        }
        else if (actualState.Exist == false)
        {
            inDesiredState = false;
        }
        else
        {
            var desiredPermissionState = instance.State ?? PermissionState.Grant;
            if (actualState.State != desiredPermissionState)
            {
                inDesiredState = false;
            }
        }

        actualState.InDesiredState = inDesiredState;

        return new TestResult<Schema>(actualState);
    }

    public void Delete(Schema instance)
    {
        var server = new Server(instance.ServerInstance);
        server.ConnectionContext.ConnectTimeout = 30;

        try
        {
            var permissionSet = MapPermissionNameToPermissionSet(instance.Permission);
            var permissions = server.EnumServerPermissions(instance.Principal);

            var matchingPermission = permissions
                .Cast<ServerPermissionInfo>()
                .FirstOrDefault(p => HasPermission(p.PermissionType, instance.Permission));

            if (matchingPermission != null)
            {
                server.Revoke(permissionSet, instance.Principal, false, false);
            }
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    public IEnumerable<Schema> Export()
    {
        yield break;
    }

    private static bool HasPermission(ServerPermissionSet permissionSet, ServerPermissionName permission)
    {
        return permission switch
        {
            ServerPermissionName.AdministerBulkOperations => permissionSet.AdministerBulkOperations,
            ServerPermissionName.AlterAnyAvailabilityGroup => permissionSet.AlterAnyAvailabilityGroup,
            ServerPermissionName.AlterAnyConnection => permissionSet.AlterAnyConnection,
            ServerPermissionName.AlterAnyCredential => permissionSet.AlterAnyCredential,
            ServerPermissionName.AlterAnyDatabase => permissionSet.AlterAnyDatabase,
            ServerPermissionName.AlterAnyEndpoint => permissionSet.AlterAnyEndpoint,
            ServerPermissionName.AlterAnyEventNotification => permissionSet.AlterAnyEventNotification,
            ServerPermissionName.AlterAnyEventSession => permissionSet.AlterAnyEventSession,
            ServerPermissionName.AlterAnyEventSessionAddEvent => permissionSet.AlterAnyEventSessionAddEvent,
            ServerPermissionName.AlterAnyEventSessionAddTarget => permissionSet.AlterAnyEventSessionAddTarget,
            ServerPermissionName.AlterAnyEventSessionDisable => permissionSet.AlterAnyEventSessionDisable,
            ServerPermissionName.AlterAnyEventSessionDropEvent => permissionSet.AlterAnyEventSessionDropEvent,
            ServerPermissionName.AlterAnyEventSessionDropTarget => permissionSet.AlterAnyEventSessionDropTarget,
            ServerPermissionName.AlterAnyEventSessionEnable => permissionSet.AlterAnyEventSessionEnable,
            ServerPermissionName.AlterAnyEventSessionOption => permissionSet.AlterAnyEventSessionOption,
            ServerPermissionName.AlterAnyLinkedServer => permissionSet.AlterAnyLinkedServer,
            ServerPermissionName.AlterAnyLogin => permissionSet.AlterAnyLogin,
            ServerPermissionName.AlterAnyServerAudit => permissionSet.AlterAnyServerAudit,
            ServerPermissionName.AlterAnyServerRole => permissionSet.AlterAnyServerRole,
            ServerPermissionName.AlterResources => permissionSet.AlterResources,
            ServerPermissionName.AlterServerState => permissionSet.AlterServerState,
            ServerPermissionName.AlterSettings => permissionSet.AlterSettings,
            ServerPermissionName.AlterTrace => permissionSet.AlterTrace,
            ServerPermissionName.AuthenticateServer => permissionSet.AuthenticateServer,
            ServerPermissionName.ConnectAnyDatabase => permissionSet.ConnectAnyDatabase,
            ServerPermissionName.ConnectSql => permissionSet.ConnectSql,
            ServerPermissionName.ControlServer => permissionSet.ControlServer,
            ServerPermissionName.CreateAnyDatabase => permissionSet.CreateAnyDatabase,
            ServerPermissionName.CreateAnyEventSession => permissionSet.CreateAnyEventSession,
            ServerPermissionName.CreateAvailabilityGroup => permissionSet.CreateAvailabilityGroup,
            ServerPermissionName.CreateDdlEventNotification => permissionSet.CreateDdlEventNotification,
            ServerPermissionName.CreateEndpoint => permissionSet.CreateEndpoint,
            ServerPermissionName.CreateLogin => permissionSet.CreateLogin,
            ServerPermissionName.CreateServerRole => permissionSet.CreateServerRole,
            ServerPermissionName.CreateTraceEventNotification => permissionSet.CreateTraceEventNotification,
            ServerPermissionName.DropAnyEventSession => permissionSet.DropAnyEventSession,
            ServerPermissionName.ExternalAccessAssembly => permissionSet.ExternalAccessAssembly,
            ServerPermissionName.ImpersonateAnyLogin => permissionSet.ImpersonateAnyLogin,
            ServerPermissionName.SelectAllUserSecurables => permissionSet.SelectAllUserSecurables,
            ServerPermissionName.Shutdown => permissionSet.Shutdown,
            ServerPermissionName.UnsafeAssembly => permissionSet.UnsafeAssembly,
            ServerPermissionName.ViewAnyCryptographicallySecuredDefinition => permissionSet.ViewAnyCryptographicallySecuredDefinition,
            ServerPermissionName.ViewAnyDatabase => permissionSet.ViewAnyDatabase,
            ServerPermissionName.ViewAnyDefinition => permissionSet.ViewAnyDefinition,
            ServerPermissionName.ViewAnyErrorLog => permissionSet.ViewAnyErrorLog,
            ServerPermissionName.ViewAnyPerformanceDefinition => permissionSet.ViewAnyPerformanceDefinition,
            ServerPermissionName.ViewAnySecurityDefinition => permissionSet.ViewAnySecurityDefinition,
            ServerPermissionName.ViewServerPerformanceState => permissionSet.ViewServerPerformanceState,
            ServerPermissionName.ViewServerSecurityAudit => permissionSet.ViewServerSecurityAudit,
            ServerPermissionName.ViewServerSecurityState => permissionSet.ViewServerSecurityState,
            ServerPermissionName.ViewServerState => permissionSet.ViewServerState,
            _ => false
        };
    }

    private static SmoServerPermission MapPermissionNameToSmoPermission(ServerPermissionName permission)
    {
        return permission switch
        {
            ServerPermissionName.AdministerBulkOperations => SmoServerPermission.AdministerBulkOperations,
            ServerPermissionName.AlterAnyAvailabilityGroup => SmoServerPermission.AlterAnyAvailabilityGroup,
            ServerPermissionName.AlterAnyConnection => SmoServerPermission.AlterAnyConnection,
            ServerPermissionName.AlterAnyCredential => SmoServerPermission.AlterAnyCredential,
            ServerPermissionName.AlterAnyDatabase => SmoServerPermission.AlterAnyDatabase,
            ServerPermissionName.AlterAnyEndpoint => SmoServerPermission.AlterAnyEndpoint,
            ServerPermissionName.AlterAnyEventNotification => SmoServerPermission.AlterAnyEventNotification,
            ServerPermissionName.AlterAnyEventSession => SmoServerPermission.AlterAnyEventSession,
            ServerPermissionName.AlterAnyEventSessionAddEvent => SmoServerPermission.AlterAnyEventSessionAddEvent,
            ServerPermissionName.AlterAnyEventSessionAddTarget => SmoServerPermission.AlterAnyEventSessionAddTarget,
            ServerPermissionName.AlterAnyEventSessionDisable => SmoServerPermission.AlterAnyEventSessionDisable,
            ServerPermissionName.AlterAnyEventSessionDropEvent => SmoServerPermission.AlterAnyEventSessionDropEvent,
            ServerPermissionName.AlterAnyEventSessionDropTarget => SmoServerPermission.AlterAnyEventSessionDropTarget,
            ServerPermissionName.AlterAnyEventSessionEnable => SmoServerPermission.AlterAnyEventSessionEnable,
            ServerPermissionName.AlterAnyEventSessionOption => SmoServerPermission.AlterAnyEventSessionOption,
            ServerPermissionName.AlterAnyLinkedServer => SmoServerPermission.AlterAnyLinkedServer,
            ServerPermissionName.AlterAnyLogin => SmoServerPermission.AlterAnyLogin,
            ServerPermissionName.AlterAnyServerAudit => SmoServerPermission.AlterAnyServerAudit,
            ServerPermissionName.AlterAnyServerRole => SmoServerPermission.AlterAnyServerRole,
            ServerPermissionName.AlterResources => SmoServerPermission.AlterResources,
            ServerPermissionName.AlterServerState => SmoServerPermission.AlterServerState,
            ServerPermissionName.AlterSettings => SmoServerPermission.AlterSettings,
            ServerPermissionName.AlterTrace => SmoServerPermission.AlterTrace,
            ServerPermissionName.AuthenticateServer => SmoServerPermission.AuthenticateServer,
            ServerPermissionName.ConnectAnyDatabase => SmoServerPermission.ConnectAnyDatabase,
            ServerPermissionName.ConnectSql => SmoServerPermission.ConnectSql,
            ServerPermissionName.ControlServer => SmoServerPermission.ControlServer,
            ServerPermissionName.CreateAnyDatabase => SmoServerPermission.CreateAnyDatabase,
            ServerPermissionName.CreateAnyEventSession => SmoServerPermission.CreateAnyEventSession,
            ServerPermissionName.CreateAvailabilityGroup => SmoServerPermission.CreateAvailabilityGroup,
            ServerPermissionName.CreateDdlEventNotification => SmoServerPermission.CreateDdlEventNotification,
            ServerPermissionName.CreateEndpoint => SmoServerPermission.CreateEndpoint,
            ServerPermissionName.CreateLogin => SmoServerPermission.CreateLogin,
            ServerPermissionName.CreateServerRole => SmoServerPermission.CreateServerRole,
            ServerPermissionName.CreateTraceEventNotification => SmoServerPermission.CreateTraceEventNotification,
            ServerPermissionName.DropAnyEventSession => SmoServerPermission.DropAnyEventSession,
            ServerPermissionName.ExternalAccessAssembly => SmoServerPermission.ExternalAccessAssembly,
            ServerPermissionName.ImpersonateAnyLogin => SmoServerPermission.ImpersonateAnyLogin,
            ServerPermissionName.SelectAllUserSecurables => SmoServerPermission.SelectAllUserSecurables,
            ServerPermissionName.Shutdown => SmoServerPermission.Shutdown,
            ServerPermissionName.UnsafeAssembly => SmoServerPermission.UnsafeAssembly,
            ServerPermissionName.ViewAnyCryptographicallySecuredDefinition => SmoServerPermission.ViewAnyCryptographicallySecuredDefinition,
            ServerPermissionName.ViewAnyDatabase => SmoServerPermission.ViewAnyDatabase,
            ServerPermissionName.ViewAnyDefinition => SmoServerPermission.ViewAnyDefinition,
            ServerPermissionName.ViewAnyErrorLog => SmoServerPermission.ViewAnyErrorLog,
            ServerPermissionName.ViewAnyPerformanceDefinition => SmoServerPermission.ViewAnyPerformanceDefinition,
            ServerPermissionName.ViewAnySecurityDefinition => SmoServerPermission.ViewAnySecurityDefinition,
            ServerPermissionName.ViewServerPerformanceState => SmoServerPermission.ViewServerPerformanceState,
            ServerPermissionName.ViewServerSecurityAudit => SmoServerPermission.ViewServerSecurityAudit,
            ServerPermissionName.ViewServerSecurityState => SmoServerPermission.ViewServerSecurityState,
            ServerPermissionName.ViewServerState => SmoServerPermission.ViewServerState,
            _ => throw new ArgumentException($"Unknown permission: {permission}", nameof(permission))
        };
    }

    private static ServerPermissionSet MapPermissionNameToPermissionSet(ServerPermissionName permission)
    {
        return new ServerPermissionSet(MapPermissionNameToSmoPermission(permission));
    }

    private static PermissionState MapSmoPermissionState(SmoPermissionState state)
    {
        return state switch
        {
            SmoPermissionState.Grant => PermissionState.Grant,
            SmoPermissionState.GrantWithGrant => PermissionState.GrantWithGrant,
            SmoPermissionState.Deny => PermissionState.Deny,
            _ => throw new ArgumentException($"Unknown permission state: {state}", nameof(state))
        };
    }
}
