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
using SmoDatabase = Microsoft.SqlServer.Management.Smo.Database;
using SmoObjectPermission = Microsoft.SqlServer.Management.Smo.ObjectPermission;
using SmoSchema = Microsoft.SqlServer.Management.Smo.Schema;

namespace OpenDsc.Resource.SqlServer.ObjectPermission;

[DscResource("OpenDsc.SqlServer/ObjectPermission", "0.1.0", Description = "Manage SQL Server object permissions", Tags = ["sql", "sqlserver", "object", "permission", "security"])]
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

    public Schema Get(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);
        server.ConnectionContext.DatabaseName = instance.DatabaseName;

        try
        {
            var database = server.Databases[instance.DatabaseName];

            if (database == null)
            {
                throw new InvalidOperationException($"Database '{instance.DatabaseName}' not found on server '{instance.ServerInstance}'.");
            }

            var schemaName = instance.SchemaName ?? "dbo";
            var smoObject = GetDatabaseObject(database, instance.ObjectType, schemaName, instance.ObjectName);

            if (smoObject == null)
            {
                throw new InvalidOperationException($"Object '{schemaName}.{instance.ObjectName}' of type '{instance.ObjectType}' not found in database '{instance.DatabaseName}'.");
            }

            var permissions = smoObject.EnumObjectPermissions(instance.Principal);
            var matchingPermission = permissions.FirstOrDefault(p => HasPermission(p.PermissionType, instance.Permission));

            if (matchingPermission == null)
            {
                return new Schema
                {
                    ServerInstance = instance.ServerInstance,
                    DatabaseName = instance.DatabaseName,
                    SchemaName = schemaName,
                    ObjectType = instance.ObjectType,
                    ObjectName = instance.ObjectName,
                    Principal = instance.Principal,
                    Permission = instance.Permission,
                    Exist = false
                };
            }

            return new Schema
            {
                ServerInstance = instance.ServerInstance,
                DatabaseName = database.Name,
                SchemaName = schemaName,
                ObjectType = instance.ObjectType,
                ObjectName = instance.ObjectName,
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

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);
        server.ConnectionContext.DatabaseName = instance.DatabaseName;

        try
        {
            var database = server.Databases[instance.DatabaseName];

            if (database == null)
            {
                throw new InvalidOperationException($"Database '{instance.DatabaseName}' not found on server '{instance.ServerInstance}'.");
            }

            var schemaName = instance.SchemaName ?? "dbo";
            var smoObject = GetDatabaseObject(database, instance.ObjectType, schemaName, instance.ObjectName);

            if (smoObject == null)
            {
                throw new InvalidOperationException($"Object '{schemaName}.{instance.ObjectName}' of type '{instance.ObjectType}' not found in database '{instance.DatabaseName}'.");
            }

            var permissionSet = MapPermissionNameToPermissionSet(instance.Permission);
            var desiredState = instance.State ?? PermissionState.Grant;

            var currentPermissions = smoObject.EnumObjectPermissions(instance.Principal);
            var currentMatch = currentPermissions.FirstOrDefault(p => HasPermission(p.PermissionType, instance.Permission));

            if (currentMatch != null)
            {
                var currentState = currentMatch.PermissionState;
                if (currentState == desiredState)
                {
                    return null;
                }

                smoObject.Revoke(permissionSet, instance.Principal, false, true, string.Empty);
            }

            switch (desiredState)
            {
                case PermissionState.Grant:
                    smoObject.Grant(permissionSet, instance.Principal);
                    break;

                case PermissionState.GrantWithGrant:
                    smoObject.Grant(permissionSet, instance.Principal, true);
                    break;

                case PermissionState.Deny:
                    smoObject.Deny(permissionSet, instance.Principal);
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

    public void Delete(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);
        server.ConnectionContext.DatabaseName = instance.DatabaseName;

        try
        {
            var database = server.Databases[instance.DatabaseName];

            if (database == null)
            {
                return;
            }

            var schemaName = instance.SchemaName ?? "dbo";
            var smoObject = GetDatabaseObject(database, instance.ObjectType, schemaName, instance.ObjectName);

            if (smoObject == null)
            {
                return;
            }

            var permissionSet = MapPermissionNameToPermissionSet(instance.Permission);
            var permissions = smoObject.EnumObjectPermissions(instance.Principal);
            var matchingPermission = permissions.FirstOrDefault(p => HasPermission(p.PermissionType, instance.Permission));

            if (matchingPermission != null)
            {
                smoObject.Revoke(permissionSet, instance.Principal);
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

    public IEnumerable<Schema> Export(Schema? filter)
    {
        var serverInstance = filter?.ServerInstance ?? Environment.GetEnvironmentVariable("SQLSERVER_INSTANCE") ?? ".";
        var username = filter?.ConnectUsername ?? Environment.GetEnvironmentVariable("SQLSERVER_USERNAME");
        var password = filter?.ConnectPassword ?? Environment.GetEnvironmentVariable("SQLSERVER_PASSWORD");
        var databaseName = filter?.DatabaseName;
        var schemaNameFilter = filter?.SchemaName;
        var objectTypeFilter = filter?.ObjectType;
        var objectNameFilter = filter?.ObjectName;
        var principal = filter?.Principal;
        var permissionFilter = filter?.Permission;

        var server = SqlConnectionHelper.CreateConnection(serverInstance, username, password);

        try
        {
            var databases = string.IsNullOrEmpty(databaseName)
                ? server.Databases.Cast<SmoDatabase>().Where(d => !d.IsSystemObject)
                : server.Databases.Cast<SmoDatabase>().Where(d => string.Equals(d.Name, databaseName, StringComparison.OrdinalIgnoreCase));

            foreach (var database in databases)
            {
                var objectTypes = new[] { ObjectType.Table, ObjectType.View, ObjectType.StoredProcedure, ObjectType.UserDefinedFunction, ObjectType.Schema, ObjectType.Sequence, ObjectType.Synonym };

                if (objectTypeFilter != null)
                {
                    objectTypes = new[] { objectTypeFilter.Value };
                }

                foreach (var objectType in objectTypes)
                {
                    var objectInfos = GetObjectInfosByType(database, objectType);

                    foreach (var objectInfo in objectInfos)
                    {
                        if (!string.IsNullOrWhiteSpace(objectNameFilter) &&
                            !string.Equals(objectInfo.Name, objectNameFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(schemaNameFilter) &&
                            !string.Equals(objectInfo.Schema, schemaNameFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var permissions = string.IsNullOrWhiteSpace(principal)
                            ? objectInfo.Object.EnumObjectPermissions()
                            : objectInfo.Object.EnumObjectPermissions(principal);

                        foreach (ObjectPermissionInfo permissionInfo in permissions)
                        {
                            foreach (var permissionName in GetPermissionNames(permissionInfo.PermissionType))
                            {
                                if (!string.IsNullOrWhiteSpace(permissionFilter) &&
                                    !string.Equals(permissionName, permissionFilter, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                yield return new Schema
                                {
                                    ServerInstance = serverInstance,
                                    DatabaseName = database.Name,
                                    SchemaName = objectInfo.Schema,
                                    ObjectType = objectType,
                                    ObjectName = objectInfo.Name,
                                    Principal = permissionInfo.Grantee,
                                    Permission = permissionName,
                                    State = permissionInfo.PermissionState,
                                    Grantor = permissionInfo.Grantor
                                };
                            }
                        }
                    }
                }
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

    private sealed record ObjectInfo(string Name, string Schema, IObjectPermission Object);

    private static IEnumerable<ObjectInfo> GetObjectInfosByType(SmoDatabase database, ObjectType objectType)
    {
        database.Refresh();

        return objectType switch
        {
            ObjectType.Table => database.Tables.Cast<Table>().Select(t => new ObjectInfo(t.Name, t.Schema, t)),
            ObjectType.View => database.Views.Cast<View>().Select(v => new ObjectInfo(v.Name, v.Schema, v)),
            ObjectType.StoredProcedure => database.StoredProcedures.Cast<StoredProcedure>().Select(p => new ObjectInfo(p.Name, p.Schema, p)),
            ObjectType.UserDefinedFunction => database.UserDefinedFunctions.Cast<UserDefinedFunction>().Select(f => new ObjectInfo(f.Name, f.Schema, f)),
            ObjectType.Schema => database.Schemas.Cast<SmoSchema>().Select(s => new ObjectInfo(s.Name, s.Name, s)),
            ObjectType.Sequence => database.Sequences.Cast<Sequence>().Select(s => new ObjectInfo(s.Name, s.Schema, s)),
            ObjectType.Synonym => database.Synonyms.Cast<Synonym>().Select(s => new ObjectInfo(s.Name, s.Schema, s)),
            _ => Enumerable.Empty<ObjectInfo>()
        };
    }

    private static IObjectPermission? GetDatabaseObject(SmoDatabase database, ObjectType objectType, string schemaName, string objectName)
    {
        database.Refresh();

        return objectType switch
        {
            ObjectType.Table => database.Tables.Cast<Table>().FirstOrDefault(t => string.Equals(t.Name, objectName, StringComparison.OrdinalIgnoreCase) && string.Equals(t.Schema, schemaName, StringComparison.OrdinalIgnoreCase)),
            ObjectType.View => database.Views.Cast<View>().FirstOrDefault(v => string.Equals(v.Name, objectName, StringComparison.OrdinalIgnoreCase) && string.Equals(v.Schema, schemaName, StringComparison.OrdinalIgnoreCase)),
            ObjectType.StoredProcedure => database.StoredProcedures.Cast<StoredProcedure>().FirstOrDefault(p => string.Equals(p.Name, objectName, StringComparison.OrdinalIgnoreCase) && string.Equals(p.Schema, schemaName, StringComparison.OrdinalIgnoreCase)),
            ObjectType.UserDefinedFunction => database.UserDefinedFunctions.Cast<UserDefinedFunction>().FirstOrDefault(f => string.Equals(f.Name, objectName, StringComparison.OrdinalIgnoreCase) && string.Equals(f.Schema, schemaName, StringComparison.OrdinalIgnoreCase)),
            ObjectType.Schema => database.Schemas.Cast<SmoSchema>().FirstOrDefault(s => string.Equals(s.Name, objectName, StringComparison.OrdinalIgnoreCase)),
            ObjectType.Sequence => database.Sequences.Cast<Sequence>().FirstOrDefault(s => string.Equals(s.Name, objectName, StringComparison.OrdinalIgnoreCase) && string.Equals(s.Schema, schemaName, StringComparison.OrdinalIgnoreCase)),
            ObjectType.Synonym => database.Synonyms.Cast<Synonym>().FirstOrDefault(s => string.Equals(s.Name, objectName, StringComparison.OrdinalIgnoreCase) && string.Equals(s.Schema, schemaName, StringComparison.OrdinalIgnoreCase)),
            _ => null
        };
    }

    private static IEnumerable<string> GetPermissionNames(ObjectPermissionSet permissionSet)
    {
        return typeof(ObjectPermissionSet)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(bool) && (bool)(p.GetValue(permissionSet) ?? false))
            .Select(p => p.Name);
    }

    private static bool HasPermission(ObjectPermissionSet permissionSet, string permission)
    {
        var property = typeof(ObjectPermissionSet).GetProperty(permission, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property == null || property.PropertyType != typeof(bool))
        {
            return false;
        }

        return (bool)(property.GetValue(permissionSet) ?? false);
    }

    private static SmoObjectPermission MapPermissionNameToSmoPermission(string permission)
    {
        var property = typeof(SmoObjectPermission).GetProperty(permission, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
            ?? throw new ArgumentException($"Unknown permission: {permission}", nameof(permission));

        return (SmoObjectPermission)(property.GetValue(null)
            ?? throw new ArgumentException($"Permission property returned null: {permission}", nameof(permission)));
    }

    private static ObjectPermissionSet MapPermissionNameToPermissionSet(string permission)
    {
        return new ObjectPermissionSet(MapPermissionNameToSmoPermission(permission));
    }
}
