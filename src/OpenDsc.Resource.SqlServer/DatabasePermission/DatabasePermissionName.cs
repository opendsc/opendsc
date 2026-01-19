// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.DatabasePermission;

[Description("Specifies the database permission type.")]
public enum DatabasePermissionName
{
    [Description("Alter the database.")]
    Alter,

    [Description("Alter any application role.")]
    AlterAnyApplicationRole,

    [Description("Alter any assembly.")]
    AlterAnyAssembly,

    [Description("Alter any asymmetric key.")]
    AlterAnyAsymmetricKey,

    [Description("Alter any certificate.")]
    AlterAnyCertificate,

    [Description("Alter any contract.")]
    AlterAnyContract,

    [Description("Alter any database audit.")]
    AlterAnyDatabaseAudit,

    [Description("Alter any database DDL trigger.")]
    AlterAnyDatabaseDdlTrigger,

    [Description("Alter any database event notification.")]
    AlterAnyDatabaseEventNotification,

    [Description("Alter any database scoped configuration.")]
    AlterAnyDatabaseScopedConfiguration,

    [Description("Alter any dataspace.")]
    AlterAnyDataspace,

    [Description("Alter any external data source.")]
    AlterAnyExternalDataSource,

    [Description("Alter any external file format.")]
    AlterAnyExternalFileFormat,

    [Description("Alter any fulltext catalog.")]
    AlterAnyFulltextCatalog,

    [Description("Alter any mask.")]
    AlterAnyMask,

    [Description("Alter any message type.")]
    AlterAnyMessageType,

    [Description("Alter any remote service binding.")]
    AlterAnyRemoteServiceBinding,

    [Description("Alter any role.")]
    AlterAnyRole,

    [Description("Alter any route.")]
    AlterAnyRoute,

    [Description("Alter any schema.")]
    AlterAnySchema,

    [Description("Alter any security policy.")]
    AlterAnySecurityPolicy,

    [Description("Alter any sensitivity classification.")]
    AlterAnySensitivityClassification,

    [Description("Alter any service.")]
    AlterAnyService,

    [Description("Alter any symmetric key.")]
    AlterAnySymmetricKey,

    [Description("Alter any user.")]
    AlterAnyUser,

    [Description("Authenticate to the database.")]
    Authenticate,

    [Description("Backup the database.")]
    BackupDatabase,

    [Description("Backup the transaction log.")]
    BackupLog,

    [Description("Issue a checkpoint.")]
    Checkpoint,

    [Description("Connect to the database.")]
    Connect,

    [Description("Connect for replication.")]
    ConnectReplication,

    [Description("Full control over the database.")]
    Control,

    [Description("Create aggregate functions.")]
    CreateAggregate,

    [Description("Create assemblies.")]
    CreateAssembly,

    [Description("Create asymmetric keys.")]
    CreateAsymmetricKey,

    [Description("Create certificates.")]
    CreateCertificate,

    [Description("Create contracts.")]
    CreateContract,

    [Description("Create databases.")]
    CreateDatabase,

    [Description("Create database DDL event notifications.")]
    CreateDatabaseDdlEventNotification,

    [Description("Create defaults.")]
    CreateDefault,

    [Description("Create fulltext catalogs.")]
    CreateFulltextCatalog,

    [Description("Create functions.")]
    CreateFunction,

    [Description("Create message types.")]
    CreateMessageType,

    [Description("Create stored procedures.")]
    CreateProcedure,

    [Description("Create queues.")]
    CreateQueue,

    [Description("Create remote service bindings.")]
    CreateRemoteServiceBinding,

    [Description("Create roles.")]
    CreateRole,

    [Description("Create routes.")]
    CreateRoute,

    [Description("Create rules.")]
    CreateRule,

    [Description("Create schemas.")]
    CreateSchema,

    [Description("Create services.")]
    CreateService,

    [Description("Create symmetric keys.")]
    CreateSymmetricKey,

    [Description("Create synonyms.")]
    CreateSynonym,

    [Description("Create tables.")]
    CreateTable,

    [Description("Create types.")]
    CreateType,

    [Description("Create users.")]
    CreateUser,

    [Description("Create views.")]
    CreateView,

    [Description("Create XML schema collections.")]
    CreateXmlSchemaCollection,

    [Description("Delete data.")]
    Delete,

    [Description("Execute stored procedures and functions.")]
    Execute,

    [Description("Execute any external script.")]
    ExecuteAnyExternalScript,

    [Description("Insert data.")]
    Insert,

    [Description("Kill database connections.")]
    KillDatabaseConnection,

    [Description("Reference objects.")]
    References,

    [Description("Select data.")]
    Select,

    [Description("View showplan.")]
    Showplan,

    [Description("Subscribe to query notifications.")]
    SubscribeQueryNotifications,

    [Description("Take ownership.")]
    TakeOwnership,

    [Description("Unmask dynamic data.")]
    Unmask,

    [Description("Update data.")]
    Update,

    [Description("View any column encryption key definition.")]
    ViewAnyColumnEncryptionKeyDefinition,

    [Description("View any column master key definition.")]
    ViewAnyColumnMasterKeyDefinition,

    [Description("View any sensitivity classification.")]
    ViewAnySensitivityClassification,

    [Description("View database state.")]
    ViewDatabaseState,

    [Description("View definition.")]
    ViewDefinition
}
