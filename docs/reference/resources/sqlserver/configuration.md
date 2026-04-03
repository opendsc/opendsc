---
description: Reference for the OpenDsc.SqlServer/Configuration resource, which manages SQL Server instance configuration options.
title: "OpenDsc.SqlServer/Configuration"
date: 2026-03-27
topic: reference
---

# OpenDsc.SqlServer/Configuration

## Synopsis

Manages SQL Server instance configuration options (equivalent to
`sp_configure`). Covers
memory, parallelism, security, and advanced server options.

## Type name

```plaintext
OpenDsc.SqlServer/Configuration
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | No        |
| Export     | Yes       |

## Properties

### Connection properties

| Property          | Type   | Required | Access     | Description                      |
| :---------------- | :----- | :------- | :--------- | :------------------------------- |
| `serverInstance`  | string | No       | Read/Write | SQL Server instance name.        |
| `connectUsername` | string | No       | Write-Only | Username for SQL authentication. |
| `connectPassword` | string | No       | Write-Only | Password for SQL authentication. |

### Memory settings

| Property            | Type | Required | Access     | Description                                              |
| :------------------ | :--- | :------- | :--------- | :------------------------------------------------------- |
| `maxServerMemory`   | int  | No       | Read/Write | Maximum server memory in MB. `2147483647` for unlimited. |
| `minServerMemory`   | int  | No       | Read/Write | Minimum server memory in MB.                             |
| `minMemoryPerQuery` | int  | No       | Read/Write | Minimum memory per query in KB.                          |

### Parallelism settings

| Property                      | Type | Required | Access     | Description                                      |
| :---------------------------- | :--- | :------- | :--------- | :----------------------------------------------- |
| `maxDegreeOfParallelism`      | int  | No       | Read/Write | Max degree of parallelism. `0` = all processors. |
| `costThresholdForParallelism` | int  | No       | Read/Write | Cost threshold for parallel plans.               |

### Network settings

| Property             | Type | Required | Access     | Description                                        |
|:---------------------|:-----|:---------|:-----------|:---------------------------------------------------|
| `networkPacketSize`  | int  | No       | Read/Write | Network packet size in bytes (512–32767).          |
| `remoteLoginTimeout` | int  | No       | Read/Write | Remote login timeout in seconds. `0` = infinite.   |
| `remoteQueryTimeout` | int  | No       | Read/Write | Remote query timeout in seconds. `0` = no timeout. |

### Security and feature settings

| Property                          | Type | Required | Access     | Description                               |
| :-------------------------------- | :--- | :------- | :--------- | :---------------------------------------- |
| `xpCmdShellEnabled`               | bool | No       | Read/Write | Enable `xp_cmdshell`.                     |
| `databaseMailEnabled`             | bool | No       | Read/Write | Enable Database Mail XPs.                 |
| `agentXpsEnabled`                 | bool | No       | Read/Write | Enable SQL Server Agent XPs.              |
| `oleAutomationProceduresEnabled`  | bool | No       | Read/Write | Enable OLE Automation procedures.         |
| `adHocDistributedQueriesEnabled`  | bool | No       | Read/Write | Enable ad hoc distributed queries.        |
| `clrEnabled`                      | bool | No       | Read/Write | Enable CLR integration.                   |
| `remoteDacConnectionsEnabled`     | bool | No       | Read/Write | Enable remote DAC connections.            |
| `containmentEnabled`              | bool | No       | Read/Write | Enable contained database authentication. |
| `defaultBackupCompression`        | bool | No       | Read/Write | Default backup compression.               |
| `defaultBackupChecksum`           | bool | No       | Read/Write | Default backup checksum.                  |
| `c2AuditMode`                     | bool | No       | Read/Write | Enable C2 audit mode.                     |
| `commonCriteriaComplianceEnabled` | bool | No       | Read/Write | Enable Common Criteria compliance.        |
| `crossDbOwnershipChaining`        | bool | No       | Read/Write | Cross-database ownership chaining.        |
| `defaultTraceEnabled`             | bool | No       | Read/Write | Enable default trace.                     |

### Performance settings

| Property                        | Type | Required | Access     | Description                                           |
| :------------------------------ | :--- | :------- | :--------- | :---------------------------------------------------- |
| `queryGovernorCostLimit`        | int  | No       | Read/Write | Max estimated query cost. `0` = no limit.             |
| `queryWait`                     | int  | No       | Read/Write | Query wait in seconds. `-1` = auto.                   |
| `optimizeAdhocWorkloads`        | bool | No       | Read/Write | Optimize plan cache for ad hoc workloads.             |
| `nestedTriggers`                | bool | No       | Read/Write | Allow nested triggers (up to 32 levels).              |
| `serverTriggerRecursionEnabled` | bool | No       | Read/Write | Server-level trigger recursion.                       |
| `disallowResultsFromTriggers`   | bool | No       | Read/Write | Prevent triggers from returning result sets.          |
| `blockedProcessThreshold`       | int  | No       | Read/Write | Blocked process threshold in seconds. `0` = disabled. |
| `recoveryInterval`              | int  | No       | Read/Write | Recovery interval in minutes. `0` = automatic.        |
| `fillFactor`                    | int  | No       | Read/Write | Default fill factor. `0` or `100` = full pages.       |
| `userConnections`               | int  | No       | Read/Write | Max user connections. `0` = unlimited.                |
| `cursorThreshold`               | int  | No       | Read/Write | Rows for async cursor. `-1` = all sync.               |
| `filestreamAccessLevel`         | int  | No       | Read/Write | FILESTREAM access level: `0`, `1`, or `2`.            |
| `maxWorkerThreads`              | int  | No       | Read/Write | Max worker threads. `0` = auto.                       |

### Advanced settings

| Property              | Type | Required | Access     | Description                              |
| :-------------------- | :--- | :------- | :--------- | :--------------------------------------- |
| `showAdvancedOptions` | bool | No       | Read/Write | Show advanced options in `sp_configure`. |

### Read-only properties

| Property                      | Type | Access    | Description                                     |
| :---------------------------- | :--- | :-------- | :---------------------------------------------- |
| `showAdvancedOptionsRunValue` | bool | Read-Only | Current running value of show advanced options. |

## Examples

### Example 1 — Get current configuration

```powershell
dsc resource get -r OpenDsc.SqlServer/Configuration --input '{"serverInstance":"."}'
```

### Example 2 — Set memory and parallelism

```powershell
dsc resource set -r OpenDsc.SqlServer/Configuration --input '{
  "serverInstance": ".",
  "maxServerMemory": 8192,
  "maxDegreeOfParallelism": 4,
  "costThresholdForParallelism": 50
}'
```

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: SQL Server instance settings
    type: OpenDsc.SqlServer/Configuration
    properties:
      serverInstance: "."
      maxServerMemory: 16384
      minServerMemory: 4096
      maxDegreeOfParallelism: 4
      costThresholdForParallelism: 50
      xpCmdShellEnabled: false
      defaultBackupCompression: true
      optimizeAdhocWorkloads: true
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
