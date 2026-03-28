---
description: Reference for the OpenDsc.SqlServer/Database resource, which manages SQL Server databases.
title: "OpenDsc.SqlServer/Database"
date: 2026-03-27
topic: reference
---

# OpenDsc.SqlServer/Database

## Synopsis

Manages SQL Server databases, including creation, configuration options, ANSI
settings,
performance options, and availability features.

## Type name

```plaintext
OpenDsc.SqlServer/Database
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

### Connection properties

| Property          | Type   | Required | Access     | Description                      |
| :---------------- | :----- | :------- | :--------- | :------------------------------- |
| `serverInstance`  | string | Yes      | Read/Write | SQL Server instance name.        |
| `connectUsername` | string | No       | Write-Only | Username for SQL authentication. |
| `connectPassword` | string | No       | Write-Only | Password for SQL authentication. |

### Database properties

| Property             | Type   | Required | Access     | Description                                                 |
| :------------------- | :----- | :------- | :--------- | :---------------------------------------------------------- |
| `name`               | string | Yes      | Read/Write | Name of the database.                                       |
| `collation`          | string | No       | Read/Write | Database collation. Defaults to server collation.           |
| `compatibilityLevel` | string | No       | Read/Write | Compatibility level (`Version90` through `Version160`).     |
| `recoveryModel`      | string | No       | Read/Write | Recovery model: `Simple`, `Full`, `BulkLogged`.             |
| `owner`              | string | No       | Read/Write | Login name of database owner.                               |
| `readOnly`           | bool   | No       | Read/Write | Whether the database is read-only.                          |
| `userAccess`         | string | No       | Read/Write | User access: `Multi`, `Single`, `Restricted`.               |
| `pageVerify`         | string | No       | Read/Write | Page verification: `None`, `TornPageDetection`, `Checksum`. |
| `containmentType`    | string | No       | Read/Write | Containment: `None`, `Partial`.                             |

### File properties (write-only, used during creation)

| Property            | Type   | Required | Access     | Description                       |
| :------------------ | :----- | :------- | :--------- | :-------------------------------- |
| `primaryFilePath`   | string | No       | Write-Only | Path to primary data file (.mdf). |
| `logFilePath`       | string | No       | Write-Only | Path to log file (.ldf).          |
| `primaryFileSize`   | int    | No       | Write-Only | Initial primary file size in MB.  |
| `logFileSize`       | int    | No       | Write-Only | Initial log file size in MB.      |
| `primaryFileGrowth` | int    | No       | Write-Only | Primary file growth amount in MB. |
| `logFileGrowth`     | int    | No       | Write-Only | Log file growth amount in MB.     |

### ANSI settings

| Property                    | Type | Required | Access     | Description                             |
| :-------------------------- | :--- | :------- | :--------- | :-------------------------------------- |
| `ansiNullDefault`           | bool | No       | Read/Write | Whether ANSI NULL default is enabled.   |
| `ansiNullsEnabled`          | bool | No       | Read/Write | Whether ANSI NULLs are enabled.         |
| `ansiPaddingEnabled`        | bool | No       | Read/Write | Whether ANSI padding is enabled.        |
| `ansiWarningsEnabled`       | bool | No       | Read/Write | Whether ANSI warnings are enabled.      |
| `arithmeticAbortEnabled`    | bool | No       | Read/Write | Whether arithmetic abort is enabled.    |
| `concatenateNullYieldsNull` | bool | No       | Read/Write | Whether concatenating null yields null. |
| `numericRoundAbortEnabled`  | bool | No       | Read/Write | Whether numeric round-abort is enabled. |
| `quotedIdentifiersEnabled`  | bool | No       | Read/Write | Whether quoted identifiers are enabled. |

### Performance and behavior settings

| Property                      | Type | Required | Access     | Description                          |
| :---------------------------- | :--- | :------- | :--------- | :----------------------------------- |
| `autoClose`                   | bool | No       | Read/Write | Auto-close when last user exits.     |
| `autoShrink`                  | bool | No       | Read/Write | Automatically shrink database.       |
| `autoCreateStatisticsEnabled` | bool | No       | Read/Write | Automatic statistics creation.       |
| `autoUpdateStatisticsEnabled` | bool | No       | Read/Write | Automatic statistics update.         |
| `autoUpdateStatisticsAsync`   | bool | No       | Read/Write | Async statistics update.             |
| `closeCursorsOnCommitEnabled` | bool | No       | Read/Write | Close cursors on transaction commit. |
| `localCursorsDefault`         | bool | No       | Read/Write | Default to local cursor scope.       |
| `nestedTriggersEnabled`       | bool | No       | Read/Write | Allow nested triggers.               |
| `recursiveTriggersEnabled`    | bool | No       | Read/Write | Allow recursive triggers.            |
| `trustworthy`                 | bool | No       | Read/Write | Database is trustworthy.             |
| `databaseOwnershipChaining`   | bool | No       | Read/Write | Cross-database ownership chaining.   |
| `dateCorrelationOptimization` | bool | No       | Read/Write | Date correlation optimization.       |
| `brokerEnabled`               | bool | No       | Read/Write | Service Broker enabled.              |
| `encryptionEnabled`           | bool | No       | Read/Write | Transparent data encryption.         |
| `isParameterizationForced`    | bool | No       | Read/Write | Forced parameterization.             |
| `isReadCommittedSnapshotOn`   | bool | No       | Read/Write | READ_COMMITTED_SNAPSHOT isolation.   |
| `isFullTextEnabled`           | bool | No       | Read/Write | Full-text indexing.                  |
| `targetRecoveryTime`          | int  | No       | Read/Write | Target recovery time in seconds.     |
| `delayedDurabilityEnabled`    | bool | No       | Read/Write | Delayed durability.                  |
| `acceleratedRecoveryEnabled`  | bool | No       | Read/Write | Accelerated database recovery.       |

### Read-only properties

| Property                     | Type     | Access    | Description                             |
| :--------------------------- | :------- | :-------- | :-------------------------------------- |
| `id`                         | int      | Read-Only | Database ID.                            |
| `createDate`                 | datetime | Read-Only | Creation date.                          |
| `size`                       | double   | Read-Only | Current size in MB.                     |
| `spaceAvailable`             | double   | Read-Only | Space available in KB.                  |
| `dataSpaceUsage`             | double   | Read-Only | Data space usage in KB.                 |
| `indexSpaceUsage`            | double   | Read-Only | Index space usage in KB.                |
| `activeConnections`          | int      | Read-Only | Number of active connections.           |
| `lastBackupDate`             | datetime | Read-Only | Date of last full backup.               |
| `lastDifferentialBackupDate` | datetime | Read-Only | Date of last differential backup.       |
| `lastLogBackupDate`          | datetime | Read-Only | Date of last log backup.                |
| `status`                     | string   | Read-Only | Database status.                        |
| `isSystemObject`             | bool     | Read-Only | Whether it is a system database.        |
| `isAccessible`               | bool     | Read-Only | Whether the database is accessible.     |
| `isUpdateable`               | bool     | Read-Only | Whether the database is updateable.     |
| `isDatabaseSnapshot`         | bool     | Read-Only | Whether it is a database snapshot.      |
| `isMirroringEnabled`         | bool     | Read-Only | Whether mirroring is enabled.           |
| `availabilityGroupName`      | string   | Read-Only | Availability group name.                |
| `caseSensitive`              | bool     | Read-Only | Whether the database is case-sensitive. |
| `primaryFilePathActual`      | string   | Read-Only | Actual path to primary file.            |
| `defaultFileGroup`           | string   | Read-Only | Default file group name.                |

### DSC properties

| Property | Type | Required | Access     | Description                                            |
| :------- | :--- | :------- | :--------- | :----------------------------------------------------- |
| `_exist` | bool | No       | Read/Write | Whether the database should exist. Defaults to `true`. |

## Examples

### Example 1 — Get a database

```powershell
dsc resource get -r OpenDsc.SqlServer/Database --input '{"serverInstance":".","name":"master"}'
```

### Example 2 — Create a database

```powershell
dsc resource set -r OpenDsc.SqlServer/Database --input '{
  "serverInstance": ".",
  "name": "AppDb",
  "recoveryModel": "Simple",
  "collation": "SQL_Latin1_General_CP1_CI_AS"
}'
```

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Application database
    type: OpenDsc.SqlServer/Database
    properties:
      serverInstance: "."
      name: AppDb
      recoveryModel: Full
      autoShrink: false
      autoCreateStatisticsEnabled: true
      autoUpdateStatisticsEnabled: true
```

## Exit codes

| Code | Description         |
| :--- | :------------------ |
| 0    | Success             |
| 1    | Error               |
| 2    | Invalid JSON        |
| 3    | Invalid argument    |
| 4    | Unauthorized access |
| 5    | Invalid operation   |

## See also

- [OpenDsc resource reference](../overview.md)
- [OpenDsc.SqlServer/DatabaseUser](database-user.md)
- [OpenDsc.SqlServer/DatabaseRole](database-role.md)
