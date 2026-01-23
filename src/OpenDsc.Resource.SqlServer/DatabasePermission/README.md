# OpenDsc.SqlServer/DatabasePermission

## Synopsis

Manage SQL Server database permissions.

## Description

The `OpenDsc.SqlServer/DatabasePermission` resource enables you to manage
permissions on SQL Server databases. You can grant, deny, or revoke permissions
for database principals (users or roles).

This resource supports all standard database-level permissions including
SELECT, INSERT, UPDATE, DELETE, EXECUTE, ALTER, CONTROL, and many more.

## Requirements

- SQL Server instance accessible from the machine running DSC
- Appropriate SQL Server permissions to manage database permissions (typically
  requires CONTROL permission on the database or membership in db_owner role)
- Windows authentication is used for connecting to SQL Server

## Capabilities

The resource has the following capabilities:

- `get` - Retrieve the current state of a database permission
- `set` - Grant or deny a database permission
- `test` - Test if a permission is in the desired state
- `delete` - Revoke a database permission
- `export` - List all permissions for principals in a database

## Properties

### Required Properties

- **serverInstance** (string) - The name of the SQL Server instance to connect
  to. Use `.` or `(local)` for the default local instance, or
  `servername\instancename` for named instances.
- **databaseName** (string) - The name of the database where the permission
  is managed.
- **principal** (string) - The name of the principal (user or role) to grant
  or deny permissions to.
- **permission** (enum) - The permission to grant or deny. See Available
  Permissions section below.

### Optional Properties

- **state** (enum) - The state of the permission. Valid values:
  - `Grant` - Grants the permission (default)
  - `GrantWithGrant` - Grants the permission with the ability to grant it
    to others
  - `Deny` - Denies the permission
- **_exist** (boolean) - Indicates whether the permission should exist.
  Default: `true`. Set to `false` to revoke the permission.

### Read-Only Properties

- **grantor** (string) - The principal who granted the permission.
- **_inDesiredState** (boolean) - Indicates whether the permission is in the
  desired state.

## Available Permissions

The following database permissions are supported:

- **Alter** - Ability to alter the database or its objects
- **AlterAnyApplicationRole** - Ability to alter any application role
- **AlterAnyAssembly** - Ability to alter any assembly
- **AlterAnyCertificate** - Ability to alter any certificate
- **AlterAnyDatabaseDdlTrigger** - Ability to alter any DDL trigger
- **AlterAnyRole** - Ability to alter any database role
- **AlterAnySchema** - Ability to alter any schema
- **AlterAnyUser** - Ability to alter any database user
- **Authenticate** - Ability to authenticate to the database
- **BackupDatabase** - Ability to backup the database
- **BackupLog** - Ability to backup the transaction log
- **Checkpoint** - Ability to checkpoint the database
- **Connect** - Ability to connect to the database
- **Control** - Full control over the database
- **CreateAggregate** - Ability to create aggregates
- **CreateAssembly** - Ability to create assemblies
- **CreateFunction** - Ability to create functions
- **CreateProcedure** - Ability to create stored procedures
- **CreateRole** - Ability to create database roles
- **CreateSchema** - Ability to create schemas
- **CreateTable** - Ability to create tables
- **CreateType** - Ability to create user-defined types
- **CreateView** - Ability to create views
- **Delete** - Ability to delete data
- **Execute** - Ability to execute stored procedures and functions
- **Insert** - Ability to insert data
- **References** - Ability to create references
- **Select** - Ability to select data
- **Update** - Ability to update data
- **ViewDatabaseState** - Ability to view database state
- **ViewDefinition** - Ability to view object definitions

## Examples

### Get Permission State

Retrieve the current state of a database permission:

```yaml
serverInstance: .
databaseName: MyAppDb
principal: AppUser
permission: Select
```

```powershell
dsc resource get -r OpenDsc.SqlServer/DatabasePermission --input '{"serverInstance":".","databaseName":"MyAppDb","principal":"AppUser","permission":"Select"}'
```

### Grant Select Permission

Grant SELECT permission to a user:

```yaml
serverInstance: .
databaseName: MyAppDb
principal: AppUser
permission: Select
state: Grant
```

```powershell
dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input '{"serverInstance":".","databaseName":"MyAppDb","principal":"AppUser","permission":"Select","state":"Grant"}'
```

### Grant Permission with Grant Option

Grant a permission that the user can grant to others:

```yaml
serverInstance: .
databaseName: MyAppDb
principal: DbaRole
permission: ViewDefinition
state: GrantWithGrant
```

### Deny Permission

Deny a permission to prevent access:

```yaml
serverInstance: .
databaseName: MyAppDb
principal: RestrictedUser
permission: Delete
state: Deny
```

### Grant Multiple Permissions

Grant common data access permissions (requires multiple resource instances):

```yaml
# First resource
serverInstance: .
databaseName: MyAppDb
principal: AppUser
permission: Select

# Second resource
serverInstance: .
databaseName: MyAppDb
principal: AppUser
permission: Insert

# Third resource
serverInstance: .
databaseName: MyAppDb
principal: AppUser
permission: Update
```

### Revoke Permission

Remove a previously granted or denied permission:

```yaml
serverInstance: .
databaseName: MyAppDb
principal: AppUser
permission: Delete
_exist: false
```

```powershell
dsc resource delete -r OpenDsc.SqlServer/DatabasePermission --input '{"serverInstance":".","databaseName":"MyAppDb","principal":"AppUser","permission":"Delete"}'
```

### Test Permission State

Test if a permission is in the desired state:

```powershell
dsc resource test -r OpenDsc.SqlServer/DatabasePermission --input '{"serverInstance":".","databaseName":"MyAppDb","principal":"AppUser","permission":"Select","state":"Grant"}'
```

### Export All Permissions

List all permissions in a database:

```powershell
dsc resource export -r OpenDsc.SqlServer/DatabasePermission --input '{"serverInstance":".","databaseName":"MyAppDb"}'
```

## Exit Codes

The resource returns the following exit codes:

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Invalid argument
- **4** - Unauthorized access
- **5** - Invalid operation
