# OpenDsc.SqlServer/Database

## Synopsis

Manage SQL Server databases.

## Description

The `OpenDsc.SqlServer/Database` resource enables you to manage SQL Server
databases including creation, configuration, and deletion. You can configure
database options such as recovery model, compatibility level, collation,
and various database settings.

This resource supports creating databases with custom file locations and sizes,
as well as managing database access modes and state options.

## Requirements

- SQL Server instance accessible from the machine running DSC
- Appropriate SQL Server permissions to manage databases (typically sysadmin
  or dbcreator role membership)
- Windows authentication is used for connecting to SQL Server

## Capabilities

The resource has the following capabilities:

- `get` - Retrieve the current state of a database
- `set` - Create or update a database
- `test` - Test if a database is in the desired state
- `delete` - Remove a database
- `export` - List all databases on the server

## Properties

### Required Properties

- **serverInstance** (string) - The name of the SQL Server instance to connect
  to. Use `.` or `(local)` for the default local instance, or
  `servername\instancename` for named instances.
- **name** (string) - The name of the database.

### Optional Properties (Write-Only - Creation Only)

- **primaryFilePath** (string) - The path to the primary data file (.mdf).
- **logFilePath** (string) - The path to the log file (.ldf).
- **primaryFileSize** (integer) - The initial size of the primary data file
  in MB.
- **logFileSize** (integer) - The initial size of the log file in MB.
- **primaryFileGrowth** (integer) - The file growth amount in MB for the
  primary data file.
- **logFileGrowth** (integer) - The file growth amount in MB for the log file.

### Optional Properties (Configurable)

- **collation** (string) - The database collation. If not specified, the server
  default collation is used.
- **compatibilityLevel** (enum) - The compatibility level of the database.
  Valid values: `Version80`, `Version90`, `Version100`, `Version110`,
  `Version120`, `Version130`, `Version140`, `Version150`, `Version160`.
- **recoveryModel** (enum) - The recovery model of the database. Valid values:
  `Simple`, `Full`, `BulkLogged`.
- **owner** (string) - The login name of the database owner.
- **readOnly** (boolean) - Whether the database is read-only.
- **userAccess** (enum) - The user access mode. Valid values: `Single`,
  `Restricted`, `Multiple`.
- **autoClose** (boolean) - Whether the database automatically closes when
  the last user exits.
- **autoShrink** (boolean) - Whether the database automatically shrinks.
- **autoCreateStatistics** (boolean) - Whether statistics are automatically
  created.
- **autoUpdateStatistics** (boolean) - Whether statistics are automatically
  updated.
- **_exist** (boolean) - Indicates whether the database should exist.
  Default: `true`.

### Read-Only Properties

- **createDate** (datetime) - The creation date of the database.
- **lastBackupDate** (datetime) - The date of the last backup.
- **lastLogBackupDate** (datetime) - The date of the last log backup.
- **size** (number) - The size of the database in MB.
- **spaceAvailable** (number) - The available space in the database in KB.
- **status** (string) - The current status of the database.
- **isSystemObject** (boolean) - Whether this is a system database.

## Examples

### Get Database

Retrieve the current state of a database:

```yaml
serverInstance: .
name: MyAppDb
```

```powershell
dsc resource get -r OpenDsc.SqlServer/Database --input '{"serverInstance":".","name":"MyAppDb"}'
```

### Create Database with Defaults

Create a new database using server defaults:

```yaml
serverInstance: .
name: MyAppDb
```

```powershell
dsc resource set -r OpenDsc.SqlServer/Database --input '{"serverInstance":".","name":"MyAppDb"}'
```

### Create Database with Custom Settings

Create a database with specific configuration:

```yaml
serverInstance: .
name: MyAppDb
recoveryModel: Full
compatibilityLevel: Version150
collation: Latin1_General_CI_AS
autoCreateStatistics: true
autoUpdateStatistics: true
```

```powershell
dsc resource set -r OpenDsc.SqlServer/Database --input '{"serverInstance":".","name":"MyAppDb","recoveryModel":"Full","compatibilityLevel":"Version150"}'
```

### Create Database with Custom File Locations

Create a database with specific file paths and sizes:

```yaml
serverInstance: .
name: MyAppDb
primaryFilePath: D:\SQLData\MyAppDb.mdf
logFilePath: E:\SQLLogs\MyAppDb_log.ldf
primaryFileSize: 100
logFileSize: 50
primaryFileGrowth: 10
logFileGrowth: 10
```

### Set Database to Read-Only

Configure a database as read-only:

```yaml
serverInstance: .
name: ReportingDb
readOnly: true
```

### Change Recovery Model

Change the recovery model of a database:

```yaml
serverInstance: .
name: MyAppDb
recoveryModel: Simple
```

### Delete Database

Remove a database:

```yaml
serverInstance: .
name: MyAppDb
_exist: false
```

```powershell
dsc resource delete -r OpenDsc.SqlServer/Database --input '{"serverInstance":".","name":"MyAppDb"}'
```

### Export All Databases

List all databases on the server:

```powershell
dsc resource export -r OpenDsc.SqlServer/Database --input '{"serverInstance":"."}'
```

## Exit Codes

The resource returns the following exit codes:

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Invalid argument
- **4** - Unauthorized access
- **5** - Invalid operation
