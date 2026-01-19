// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using Microsoft.SqlServer.Management.Smo;

using SmoDatabase = Microsoft.SqlServer.Management.Smo.Database;
using SmoDatabasePermission = Microsoft.SqlServer.Management.Smo.DatabasePermission;
using SmoPermissionState = Microsoft.SqlServer.Management.Smo.PermissionState;

namespace OpenDsc.Resource.SqlServer.DatabasePermission;

[DscResource("OpenDsc.SqlServer/DatabasePermission", "0.1.0", Description = "Manage SQL Server database permissions", Tags = ["sql", "sqlserver", "database", "permission", "security"])]
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
            var database = server.Databases.Cast<SmoDatabase>()
                .FirstOrDefault(d => string.Equals(d.Name, instance.DatabaseName, StringComparison.OrdinalIgnoreCase));

            if (database == null)
            {
                throw new InvalidOperationException($"Database '{instance.DatabaseName}' not found on server '{instance.ServerInstance}'.");
            }

            // Get all permissions for this principal
            var permissions = database.EnumDatabasePermissions(instance.Principal);

            // Filter to database-level permissions and find the matching one
            var matchingPermission = permissions
                .Where(p => p.ObjectClass == ObjectClass.Database)
                .FirstOrDefault(p => HasPermission(p.PermissionType, instance.Permission));

            if (matchingPermission == null)
            {
                return new Schema
                {
                    ServerInstance = instance.ServerInstance,
                    DatabaseName = instance.DatabaseName,
                    Principal = instance.Principal,
                    Permission = instance.Permission,
                    Exist = false
                };
            }

            return new Schema
            {
                ServerInstance = instance.ServerInstance,
                DatabaseName = database.Name,
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
            var database = server.Databases.Cast<SmoDatabase>()
                .FirstOrDefault(d => string.Equals(d.Name, instance.DatabaseName, StringComparison.OrdinalIgnoreCase));

            if (database == null)
            {
                throw new InvalidOperationException($"Database '{instance.DatabaseName}' not found on server '{instance.ServerInstance}'.");
            }

            var permissionSet = MapPermissionNameToPermissionSet(instance.Permission);
            var desiredState = instance.State ?? PermissionState.Grant;

            var currentPermissions = database.EnumDatabasePermissions(instance.Principal);
            var currentMatch = currentPermissions
                .Where(p => p.ObjectClass == ObjectClass.Database)
                .FirstOrDefault(p => HasPermission(p.PermissionType, instance.Permission));

            if (currentMatch != null)
            {
                var currentState = MapSmoPermissionState(currentMatch.PermissionState);
                if (currentState == desiredState)
                {
                    return null;
                }

                database.Revoke(permissionSet, instance.Principal, false, false);
            }

            switch (desiredState)
            {
                case PermissionState.Grant:
                    database.Grant(permissionSet, instance.Principal);
                    break;

                case PermissionState.GrantWithGrant:
                    database.Grant(permissionSet, instance.Principal, true);
                    break;

                case PermissionState.Deny:
                    database.Deny(permissionSet, instance.Principal);
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
            var database = server.Databases.Cast<SmoDatabase>()
                .FirstOrDefault(d => string.Equals(d.Name, instance.DatabaseName, StringComparison.OrdinalIgnoreCase));

            if (database == null)
            {
                return;
            }

            var permissionSet = MapPermissionNameToPermissionSet(instance.Permission);
            var permissions = database.EnumDatabasePermissions(instance.Principal);

            var matchingPermission = permissions
                .Where(p => p.ObjectClass == ObjectClass.Database)
                .FirstOrDefault(p => HasPermission(p.PermissionType, instance.Permission));

            if (matchingPermission != null)
            {
                database.Revoke(permissionSet, instance.Principal, false, false);
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

    private static bool HasPermission(DatabasePermissionSet permissionSet, DatabasePermissionName permission)
    {
        return permission switch
        {
            DatabasePermissionName.Alter => permissionSet.Alter,
            DatabasePermissionName.AlterAnyApplicationRole => permissionSet.AlterAnyApplicationRole,
            DatabasePermissionName.AlterAnyAssembly => permissionSet.AlterAnyAssembly,
            DatabasePermissionName.AlterAnyAsymmetricKey => permissionSet.AlterAnyAsymmetricKey,
            DatabasePermissionName.AlterAnyCertificate => permissionSet.AlterAnyCertificate,
            DatabasePermissionName.AlterAnyContract => permissionSet.AlterAnyContract,
            DatabasePermissionName.AlterAnyDatabaseAudit => permissionSet.AlterAnyDatabaseAudit,
            DatabasePermissionName.AlterAnyDatabaseDdlTrigger => permissionSet.AlterAnyDatabaseDdlTrigger,
            DatabasePermissionName.AlterAnyDatabaseEventNotification => permissionSet.AlterAnyDatabaseEventNotification,
            DatabasePermissionName.AlterAnyDatabaseScopedConfiguration => permissionSet.AlterAnyDatabaseScopedConfiguration,
            DatabasePermissionName.AlterAnyDataspace => permissionSet.AlterAnyDataspace,
            DatabasePermissionName.AlterAnyExternalDataSource => permissionSet.AlterAnyExternalDataSource,
            DatabasePermissionName.AlterAnyExternalFileFormat => permissionSet.AlterAnyExternalFileFormat,
            DatabasePermissionName.AlterAnyFulltextCatalog => permissionSet.AlterAnyFulltextCatalog,
            DatabasePermissionName.AlterAnyMask => permissionSet.AlterAnyMask,
            DatabasePermissionName.AlterAnyMessageType => permissionSet.AlterAnyMessageType,
            DatabasePermissionName.AlterAnyRemoteServiceBinding => permissionSet.AlterAnyRemoteServiceBinding,
            DatabasePermissionName.AlterAnyRole => permissionSet.AlterAnyRole,
            DatabasePermissionName.AlterAnyRoute => permissionSet.AlterAnyRoute,
            DatabasePermissionName.AlterAnySchema => permissionSet.AlterAnySchema,
            DatabasePermissionName.AlterAnySecurityPolicy => permissionSet.AlterAnySecurityPolicy,
            DatabasePermissionName.AlterAnySensitivityClassification => permissionSet.AlterAnySensitivityClassification,
            DatabasePermissionName.AlterAnyService => permissionSet.AlterAnyService,
            DatabasePermissionName.AlterAnySymmetricKey => permissionSet.AlterAnySymmetricKey,
            DatabasePermissionName.AlterAnyUser => permissionSet.AlterAnyUser,
            DatabasePermissionName.Authenticate => permissionSet.Authenticate,
            DatabasePermissionName.BackupDatabase => permissionSet.BackupDatabase,
            DatabasePermissionName.BackupLog => permissionSet.BackupLog,
            DatabasePermissionName.Checkpoint => permissionSet.Checkpoint,
            DatabasePermissionName.Connect => permissionSet.Connect,
            DatabasePermissionName.ConnectReplication => permissionSet.ConnectReplication,
            DatabasePermissionName.Control => permissionSet.Control,
            DatabasePermissionName.CreateAggregate => permissionSet.CreateAggregate,
            DatabasePermissionName.CreateAssembly => permissionSet.CreateAssembly,
            DatabasePermissionName.CreateAsymmetricKey => permissionSet.CreateAsymmetricKey,
            DatabasePermissionName.CreateCertificate => permissionSet.CreateCertificate,
            DatabasePermissionName.CreateContract => permissionSet.CreateContract,
            DatabasePermissionName.CreateDatabase => permissionSet.CreateDatabase,
            DatabasePermissionName.CreateDatabaseDdlEventNotification => permissionSet.CreateDatabaseDdlEventNotification,
            DatabasePermissionName.CreateDefault => permissionSet.CreateDefault,
            DatabasePermissionName.CreateFulltextCatalog => permissionSet.CreateFulltextCatalog,
            DatabasePermissionName.CreateFunction => permissionSet.CreateFunction,
            DatabasePermissionName.CreateMessageType => permissionSet.CreateMessageType,
            DatabasePermissionName.CreateProcedure => permissionSet.CreateProcedure,
            DatabasePermissionName.CreateQueue => permissionSet.CreateQueue,
            DatabasePermissionName.CreateRemoteServiceBinding => permissionSet.CreateRemoteServiceBinding,
            DatabasePermissionName.CreateRole => permissionSet.CreateRole,
            DatabasePermissionName.CreateRoute => permissionSet.CreateRoute,
            DatabasePermissionName.CreateRule => permissionSet.CreateRule,
            DatabasePermissionName.CreateSchema => permissionSet.CreateSchema,
            DatabasePermissionName.CreateService => permissionSet.CreateService,
            DatabasePermissionName.CreateSymmetricKey => permissionSet.CreateSymmetricKey,
            DatabasePermissionName.CreateSynonym => permissionSet.CreateSynonym,
            DatabasePermissionName.CreateTable => permissionSet.CreateTable,
            DatabasePermissionName.CreateType => permissionSet.CreateType,
            DatabasePermissionName.CreateView => permissionSet.CreateView,
            DatabasePermissionName.CreateXmlSchemaCollection => permissionSet.CreateXmlSchemaCollection,
            DatabasePermissionName.Delete => permissionSet.Delete,
            DatabasePermissionName.Execute => permissionSet.Execute,
            DatabasePermissionName.ExecuteAnyExternalScript => permissionSet.ExecuteAnyExternalScript,
            DatabasePermissionName.Insert => permissionSet.Insert,
            DatabasePermissionName.KillDatabaseConnection => permissionSet.KillDatabaseConnection,
            DatabasePermissionName.References => permissionSet.References,
            DatabasePermissionName.Select => permissionSet.Select,
            DatabasePermissionName.Showplan => permissionSet.Showplan,
            DatabasePermissionName.SubscribeQueryNotifications => permissionSet.SubscribeQueryNotifications,
            DatabasePermissionName.TakeOwnership => permissionSet.TakeOwnership,
            DatabasePermissionName.Unmask => permissionSet.Unmask,
            DatabasePermissionName.Update => permissionSet.Update,
            DatabasePermissionName.ViewAnyColumnEncryptionKeyDefinition => permissionSet.ViewAnyColumnEncryptionKeyDefinition,
            DatabasePermissionName.ViewAnyColumnMasterKeyDefinition => permissionSet.ViewAnyColumnMasterKeyDefinition,
            DatabasePermissionName.ViewAnySensitivityClassification => permissionSet.ViewAnySensitivityClassification,
            DatabasePermissionName.ViewDatabaseState => permissionSet.ViewDatabaseState,
            DatabasePermissionName.ViewDefinition => permissionSet.ViewDefinition,
            _ => false
        };
    }

    private static SmoDatabasePermission MapPermissionNameToSmoPermission(DatabasePermissionName permission)
    {
        return permission switch
        {
            DatabasePermissionName.Alter => SmoDatabasePermission.Alter,
            DatabasePermissionName.AlterAnyApplicationRole => SmoDatabasePermission.AlterAnyApplicationRole,
            DatabasePermissionName.AlterAnyAssembly => SmoDatabasePermission.AlterAnyAssembly,
            DatabasePermissionName.AlterAnyAsymmetricKey => SmoDatabasePermission.AlterAnyAsymmetricKey,
            DatabasePermissionName.AlterAnyCertificate => SmoDatabasePermission.AlterAnyCertificate,
            DatabasePermissionName.AlterAnyContract => SmoDatabasePermission.AlterAnyContract,
            DatabasePermissionName.AlterAnyDatabaseAudit => SmoDatabasePermission.AlterAnyDatabaseAudit,
            DatabasePermissionName.AlterAnyDatabaseDdlTrigger => SmoDatabasePermission.AlterAnyDatabaseDdlTrigger,
            DatabasePermissionName.AlterAnyDatabaseEventNotification => SmoDatabasePermission.AlterAnyDatabaseEventNotification,
            DatabasePermissionName.AlterAnyDatabaseScopedConfiguration => SmoDatabasePermission.AlterAnyDatabaseScopedConfiguration,
            DatabasePermissionName.AlterAnyDataspace => SmoDatabasePermission.AlterAnyDataspace,
            DatabasePermissionName.AlterAnyExternalDataSource => SmoDatabasePermission.AlterAnyExternalDataSource,
            DatabasePermissionName.AlterAnyExternalFileFormat => SmoDatabasePermission.AlterAnyExternalFileFormat,
            DatabasePermissionName.AlterAnyFulltextCatalog => SmoDatabasePermission.AlterAnyFulltextCatalog,
            DatabasePermissionName.AlterAnyMask => SmoDatabasePermission.AlterAnyMask,
            DatabasePermissionName.AlterAnyMessageType => SmoDatabasePermission.AlterAnyMessageType,
            DatabasePermissionName.AlterAnyRemoteServiceBinding => SmoDatabasePermission.AlterAnyRemoteServiceBinding,
            DatabasePermissionName.AlterAnyRole => SmoDatabasePermission.AlterAnyRole,
            DatabasePermissionName.AlterAnyRoute => SmoDatabasePermission.AlterAnyRoute,
            DatabasePermissionName.AlterAnySchema => SmoDatabasePermission.AlterAnySchema,
            DatabasePermissionName.AlterAnySecurityPolicy => SmoDatabasePermission.AlterAnySecurityPolicy,
            DatabasePermissionName.AlterAnySensitivityClassification => SmoDatabasePermission.AlterAnySensitivityClassification,
            DatabasePermissionName.AlterAnyService => SmoDatabasePermission.AlterAnyService,
            DatabasePermissionName.AlterAnySymmetricKey => SmoDatabasePermission.AlterAnySymmetricKey,
            DatabasePermissionName.AlterAnyUser => SmoDatabasePermission.AlterAnyUser,
            DatabasePermissionName.Authenticate => SmoDatabasePermission.Authenticate,
            DatabasePermissionName.BackupDatabase => SmoDatabasePermission.BackupDatabase,
            DatabasePermissionName.BackupLog => SmoDatabasePermission.BackupLog,
            DatabasePermissionName.Checkpoint => SmoDatabasePermission.Checkpoint,
            DatabasePermissionName.Connect => SmoDatabasePermission.Connect,
            DatabasePermissionName.ConnectReplication => SmoDatabasePermission.ConnectReplication,
            DatabasePermissionName.Control => SmoDatabasePermission.Control,
            DatabasePermissionName.CreateAggregate => SmoDatabasePermission.CreateAggregate,
            DatabasePermissionName.CreateAssembly => SmoDatabasePermission.CreateAssembly,
            DatabasePermissionName.CreateAsymmetricKey => SmoDatabasePermission.CreateAsymmetricKey,
            DatabasePermissionName.CreateCertificate => SmoDatabasePermission.CreateCertificate,
            DatabasePermissionName.CreateContract => SmoDatabasePermission.CreateContract,
            DatabasePermissionName.CreateDatabase => SmoDatabasePermission.CreateDatabase,
            DatabasePermissionName.CreateDatabaseDdlEventNotification => SmoDatabasePermission.CreateDatabaseDdlEventNotification,
            DatabasePermissionName.CreateDefault => SmoDatabasePermission.CreateDefault,
            DatabasePermissionName.CreateFulltextCatalog => SmoDatabasePermission.CreateFulltextCatalog,
            DatabasePermissionName.CreateFunction => SmoDatabasePermission.CreateFunction,
            DatabasePermissionName.CreateMessageType => SmoDatabasePermission.CreateMessageType,
            DatabasePermissionName.CreateProcedure => SmoDatabasePermission.CreateProcedure,
            DatabasePermissionName.CreateQueue => SmoDatabasePermission.CreateQueue,
            DatabasePermissionName.CreateRemoteServiceBinding => SmoDatabasePermission.CreateRemoteServiceBinding,
            DatabasePermissionName.CreateRole => SmoDatabasePermission.CreateRole,
            DatabasePermissionName.CreateRoute => SmoDatabasePermission.CreateRoute,
            DatabasePermissionName.CreateRule => SmoDatabasePermission.CreateRule,
            DatabasePermissionName.CreateSchema => SmoDatabasePermission.CreateSchema,
            DatabasePermissionName.CreateService => SmoDatabasePermission.CreateService,
            DatabasePermissionName.CreateSymmetricKey => SmoDatabasePermission.CreateSymmetricKey,
            DatabasePermissionName.CreateSynonym => SmoDatabasePermission.CreateSynonym,
            DatabasePermissionName.CreateTable => SmoDatabasePermission.CreateTable,
            DatabasePermissionName.CreateType => SmoDatabasePermission.CreateType,
            DatabasePermissionName.CreateView => SmoDatabasePermission.CreateView,
            DatabasePermissionName.CreateXmlSchemaCollection => SmoDatabasePermission.CreateXmlSchemaCollection,
            DatabasePermissionName.Delete => SmoDatabasePermission.Delete,
            DatabasePermissionName.Execute => SmoDatabasePermission.Execute,
            DatabasePermissionName.ExecuteAnyExternalScript => SmoDatabasePermission.ExecuteAnyExternalScript,
            DatabasePermissionName.Insert => SmoDatabasePermission.Insert,
            DatabasePermissionName.KillDatabaseConnection => SmoDatabasePermission.KillDatabaseConnection,
            DatabasePermissionName.References => SmoDatabasePermission.References,
            DatabasePermissionName.Select => SmoDatabasePermission.Select,
            DatabasePermissionName.Showplan => SmoDatabasePermission.Showplan,
            DatabasePermissionName.SubscribeQueryNotifications => SmoDatabasePermission.SubscribeQueryNotifications,
            DatabasePermissionName.TakeOwnership => SmoDatabasePermission.TakeOwnership,
            DatabasePermissionName.Unmask => SmoDatabasePermission.Unmask,
            DatabasePermissionName.Update => SmoDatabasePermission.Update,
            DatabasePermissionName.ViewAnyColumnEncryptionKeyDefinition => SmoDatabasePermission.ViewAnyColumnEncryptionKeyDefinition,
            DatabasePermissionName.ViewAnyColumnMasterKeyDefinition => SmoDatabasePermission.ViewAnyColumnMasterKeyDefinition,
            DatabasePermissionName.ViewAnySensitivityClassification => SmoDatabasePermission.ViewAnySensitivityClassification,
            DatabasePermissionName.ViewDatabaseState => SmoDatabasePermission.ViewDatabaseState,
            DatabasePermissionName.ViewDefinition => SmoDatabasePermission.ViewDefinition,
            _ => throw new ArgumentException($"Unknown permission: {permission}", nameof(permission))
        };
    }

    private static DatabasePermissionSet MapPermissionNameToPermissionSet(DatabasePermissionName permission)
    {
        return new DatabasePermissionSet(MapPermissionNameToSmoPermission(permission));
    }

    private static PermissionState MapSmoPermissionState(SmoPermissionState state)
    {
        return state switch
        {
            SmoPermissionState.Grant => PermissionState.Grant,
            SmoPermissionState.GrantWithGrant => PermissionState.GrantWithGrant,
            SmoPermissionState.Deny => PermissionState.Deny,
            _ => PermissionState.Grant
        };
    }
}
