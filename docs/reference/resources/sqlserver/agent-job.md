---
description: Reference for the OpenDsc.SqlServer/AgentJob resource, which manages SQL Server Agent jobs.
title: "OpenDsc.SqlServer/AgentJob"
date: 2026-03-27
topic: reference
---

# OpenDsc.SqlServer/AgentJob

## Synopsis

Manages SQL Server Agent jobs, including job creation, notification settings,
and monitoring
of job execution status.

## Type name

```plaintext
OpenDsc.SqlServer/AgentJob
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

### Job properties

| Property         | Type   | Required | Access     | Description                                 |
| :--------------- | :----- | :------- | :--------- | :------------------------------------------ |
| `name`           | string | No       | Read/Write | Name of the Agent job.                      |
| `description`    | string | No       | Read/Write | Description of the job.                     |
| `isEnabled`      | bool   | No       | Read/Write | Whether the job is enabled.                 |
| `category`       | string | No       | Read/Write | Category of the job.                        |
| `ownerLoginName` | string | No       | Read/Write | Login name of the job owner.                |
| `startStepId`    | int    | No       | Read/Write | Step ID at which execution starts (min: 1). |

### Notification properties

| Property            | Type   | Required | Access     | Description                                                              |
| :------------------ | :----- | :------- | :--------- | :----------------------------------------------------------------------- |
| `emailLevel`        | string | No       | Read/Write | When to email: `Never`, `OnSuccess`, `OnFailure`, `Always`.              |
| `operatorToEmail`   | string | No       | Read/Write | Operator to email.                                                       |
| `pageLevel`         | string | No       | Read/Write | When to page: `Never`, `OnSuccess`, `OnFailure`, `Always`.               |
| `operatorToPage`    | string | No       | Read/Write | Operator to page.                                                        |
| `netSendLevel`      | string | No       | Read/Write | When to net send: `Never`, `OnSuccess`, `OnFailure`, `Always`.           |
| `operatorToNetSend` | string | No       | Read/Write | Operator for net send.                                                   |
| `eventLogLevel`     | string | No       | Read/Write | When to write to event log: `Never`, `OnSuccess`, `OnFailure`, `Always`. |
| `deleteLevel`       | string | No       | Read/Write | When to delete job: `Never`, `OnSuccess`, `OnFailure`, `Always`.         |

### Read-only properties

| Property                 | Type     | Access    | Description                                                                      |
| :----------------------- | :------- | :-------- | :------------------------------------------------------------------------------- |
| `jobId`                  | guid     | Read-Only | Unique identifier of the job.                                                    |
| `dateCreated`            | datetime | Read-Only | Date the job was created.                                                        |
| `dateLastModified`       | datetime | Read-Only | Date the job was last modified.                                                  |
| `lastRunDate`            | datetime | Read-Only | Date of the last run.                                                            |
| `lastRunOutcome`         | string   | Read-Only | Outcome: `Failed`, `Succeeded`, `Retry`, `Cancelled`.                            |
| `nextRunDate`            | datetime | Read-Only | Date of the next scheduled run.                                                  |
| `currentRunStatus`       | string   | Read-Only | Current status: `Idle`, `Executing`, `WaitingForWorkerThread`, `BetweenRetries`. |
| `currentRunStep`         | string   | Read-Only | Current step being executed.                                                     |
| `currentRunRetryAttempt` | int      | Read-Only | Current retry attempt number.                                                    |
| `hasStep`                | bool     | Read-Only | Whether the job has steps defined.                                               |
| `hasSchedule`            | bool     | Read-Only | Whether the job has schedules.                                                   |
| `versionNumber`          | int      | Read-Only | Job version number.                                                              |

### DSC properties

| Property | Type | Required | Access     | Description                                       |
| :------- | :--- | :------- | :--------- | :------------------------------------------------ |
| `_exist` | bool | No       | Read/Write | Whether the job should exist. Defaults to `true`. |

## Examples

### Example 1 — Get a job

```powershell
dsc resource get -r OpenDsc.SqlServer/AgentJob --input '{"serverInstance":".","name":"syspolicy_purge_history"}'
```

### Example 2 — Create a job

```powershell
dsc resource set -r OpenDsc.SqlServer/AgentJob --input '{
  "serverInstance": ".",
  "name": "NightlyMaintenance",
  "description": "Nightly index rebuild and statistics update",
  "isEnabled": true,
  "category": "Database Maintenance",
  "emailLevel": "OnFailure",
  "operatorToEmail": "DBA Team"
}'
```

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Nightly maintenance job
    type: OpenDsc.SqlServer/AgentJob
    properties:
      serverInstance: "."
      name: NightlyMaintenance
      description: Nightly index rebuild and statistics update
      isEnabled: true
      category: Database Maintenance
      emailLevel: OnFailure
      operatorToEmail: DBA Team
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
