// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using Microsoft.SqlServer.Management.Smo;

using PermissionState = Microsoft.SqlServer.Management.Smo.PermissionState;
using SmoServerPermission = Microsoft.SqlServer.Management.Smo.ServerPermission;

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
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

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
                State = matchingPermission.PermissionState,
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
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

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
                var currentState = currentMatch.PermissionState;
                if (currentState == desiredState)
                {
                    return null;
                }

                // Revoke with cascade to handle GrantWithGrant and any dependent grants
                server.Revoke(permissionSet, instance.Principal, false, true);
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

    public void Delete(Schema instance)
    {
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

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

    private static bool HasPermission(ServerPermissionSet permissionSet, string permission)
    {
        var property = typeof(ServerPermissionSet).GetProperty(permission, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null || property.PropertyType != typeof(bool))
        {
            return false;
        }

        return (bool)(property.GetValue(permissionSet) ?? false);
    }

    private static SmoServerPermission MapPermissionNameToSmoPermission(string permission)
    {
        var property = typeof(SmoServerPermission).GetProperty(permission, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
            ?? throw new ArgumentException($"Unknown permission: {permission}", nameof(permission));

        return (SmoServerPermission)(property.GetValue(null)
            ?? throw new ArgumentException($"Permission property returned null: {permission}", nameof(permission)));
    }

    private static ServerPermissionSet MapPermissionNameToPermissionSet(string permission)
    {
        return new ServerPermissionSet(MapPermissionNameToSmoPermission(permission));
    }
}
