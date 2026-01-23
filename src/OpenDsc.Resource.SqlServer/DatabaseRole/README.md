# OpenDsc.SqlServer/DatabaseRole

## Synopsis

Manage SQL Server database roles.

## Description

The `OpenDsc.SqlServer/DatabaseRole` resource enables you to manage SQL Server
database roles. You can create, update, retrieve, and delete custom database
roles, as well as manage membership in both custom and fixed database roles.

This resource supports configuring role ownership and managing role membership
with support for both additive and purge modes.

## Requirements

- SQL Server instance accessible from the machine running DSC
- Appropriate SQL Server permissions to manage database roles (typically
  db_owner or db_securityadmin role membership in the target database)
- Windows authentication or SQL Server authentication for connecting

## Capabilities

The resource has the following capabilities:

- `get` - Retrieve the current state of a database role
- `set` - Create or update a database role
- `test` - Test if a database role is in the desired state
- `delete` - Remove a database role (custom roles only)
- `export` - List all database roles

## Fixed Database Roles

SQL Server includes the following fixed database roles that cannot be created,
deleted, or renamed. You can only manage membership in these roles:

| Role                | Description                                                      |
|---------------------|------------------------------------------------------------------|
| `db_owner`          | Members can perform all configuration and maintenance activities |
| `db_securityadmin`  | Members can modify role membership and manage permissions        |
| `db_accessadmin`    | Members can add or remove access for Windows logins and groups   |
| `db_backupoperator` | Members can back up the database                                 |
| `db_ddladmin`       | Members can run any DDL command                                  |
| `db_datawriter`     | Members can add, delete, or change data in all user tables       |
| `db_datareader`     | Members can read all data from all user tables                   |
| `db_denydatawriter` | Members cannot add, modify, or delete data in user tables        |
| `db_denydatareader` | Members cannot read any data in user tables                      |
| `public`            | Every database user belongs to the public role                   |

## Properties

### Required Properties

- **serverInstance** (string) - The name of the SQL Server instance to connect
  to. Use `.` or `(local)` for the default local instance, or
  `servername\instancename` for named instances.
- **databaseName** (string) - The name of the database containing the role.
- **name** (string) - The name of the database role.

### Optional Properties

- **owner** (string) - The owner of the database role. Can be a database user
  or another role. Not applicable for fixed database roles.
- **members** (string[]) - The members of the database role. Members can be
  database users or other database roles.
- **_purge** (boolean) - When `true`, removes members not in the `members`
  list. When `false` (default), only adds members without removing others.
  Write-only.
- **_exist** (boolean) - Indicates whether the database role should exist.
  Default: `true`. Not applicable for fixed database roles.

### Read-Only Properties

- **createDate** (datetime) - The creation date of the role.
- **dateLastModified** (datetime) - The date the role was last modified.
- **isFixedRole** (boolean) - Whether this is a fixed database role.

## Examples

### Get Database Role

Retrieve the current state of a database role:

```yaml
serverInstance: .
databaseName: MyDatabase
name: MyCustomRole
```

```powershell
dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input '{"serverInstance":".","databaseName":"MyDatabase","name":"MyCustomRole"}'
```

### Create Custom Database Role

Create a new custom database role:

```yaml
serverInstance: .
databaseName: MyDatabase
name: AppReadWriteRole
owner: dbo
```

```powershell
dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input '{"serverInstance":".","databaseName":"MyDatabase","name":"AppReadWriteRole","owner":"dbo"}'
```

### Create Role with Members

Create a role and add members:

```yaml
serverInstance: .
databaseName: MyDatabase
name: AppUsers
owner: dbo
members:
  - AppUser1
  - AppUser2
  - AppServiceAccount
```

### Add Members to Fixed Role

Add users to a fixed database role (additive mode - default):

```yaml
serverInstance: .
databaseName: MyDatabase
name: db_datareader
members:
  - ReportUser
  - AnalyticsUser
```

```powershell
dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input '{"serverInstance":".","databaseName":"MyDatabase","name":"db_datareader","members":["ReportUser","AnalyticsUser"]}'
```

### Add Members Without Removing Existing

Add additional members without removing existing ones (default behavior):

```yaml
serverInstance: .
databaseName: MyDatabase
name: AppUsers
members:
  - NewUser1
  - NewUser2
```

### Replace All Members (Purge Mode)

Set exact membership, removing any members not in the list:

```yaml
serverInstance: .
databaseName: MyDatabase
name: AppUsers
members:
  - User1
  - User2
_purge: true
```

```powershell
dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input '{"serverInstance":".","databaseName":"MyDatabase","name":"AppUsers","members":["User1","User2"],"_purge":true}'
```

### Remove All Members

Remove all members from a role:

```yaml
serverInstance: .
databaseName: MyDatabase
name: AppUsers
members: []
_purge: true
```

### Change Role Owner

Change the owner of a custom database role:

```yaml
serverInstance: .
databaseName: MyDatabase
name: AppUsers
owner: AppAdmin
```

### Delete Custom Role

Remove a custom database role:

```yaml
serverInstance: .
databaseName: MyDatabase
name: AppUsers
_exist: false
```

```powershell
dsc resource delete -r OpenDsc.SqlServer/DatabaseRole --input '{"serverInstance":".","databaseName":"MyDatabase","name":"AppUsers"}'
```

### Export All Database Roles

List all database roles in a database:

```powershell
# Set environment variable for database name
$env:SQLSERVER_DATABASE = "MyDatabase"
dsc resource export -r OpenDsc.SqlServer/DatabaseRole
```

## Exit Codes

The resource returns the following exit codes:

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Invalid argument
- **4** - Unauthorized access
- **5** - Invalid operation
