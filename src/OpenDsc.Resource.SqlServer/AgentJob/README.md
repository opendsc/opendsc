# OpenDsc.SqlServer/AgentJob

Manages SQL Server Agent jobs. This resource allows you to create, modify,
and delete SQL Server Agent jobs, configure their properties, and manage
notification settings.

## Properties

| Property                  | Type              | Required | Description                                                    |
|---------------------------|-------------------|----------|----------------------------------------------------------------|
| `serverInstance`          | string            | Yes      | The SQL Server instance to connect to                          |
| `connectUsername`         | string            | No       | Username for SQL authentication (write-only)                   |
| `connectPassword`         | string            | No       | Password for SQL authentication (write-only)                   |
| `name`                    | string            | Yes      | The name of the Agent job                                      |
| `description`             | string            | No       | Description of the job                                         |
| `isEnabled`               | bool              | No       | Whether the job is enabled                                     |
| `category`                | string            | No       | The job category                                               |
| `ownerLoginName`          | string            | No       | The login name of the job owner                                |
| `startStepId`             | int               | No       | The step ID at which execution starts (minimum: 1)             |
| `emailLevel`              | CompletionAction  | No       | When to send email notification                                |
| `operatorToEmail`         | string            | No       | Operator to email when `emailLevel` condition is met           |
| `pageLevel`               | CompletionAction  | No       | When to send page notification                                 |
| `operatorToPage`          | string            | No       | Operator to page when `pageLevel` condition is met             |
| `netSendLevel`            | CompletionAction  | No       | When to send net send notification                             |
| `operatorToNetSend`       | string            | No       | Operator for net send when `netSendLevel` condition is met     |
| `eventLogLevel`           | CompletionAction  | No       | When to write to Windows Application event log                 |
| `deleteLevel`             | CompletionAction  | No       | When to delete the job after completion                        |
| `jobId`                   | Guid              | No       | Unique identifier of the job (read-only)                       |
| `dateCreated`             | DateTime          | No       | Creation date (read-only)                                      |
| `dateLastModified`        | DateTime          | No       | Last modified date (read-only)                                 |
| `lastRunDate`             | DateTime          | No       | Date of the last job run (read-only)                           |
| `lastRunOutcome`          | CompletionResult  | No       | Outcome of the last job run (read-only)                        |
| `nextRunDate`             | DateTime          | No       | Date of the next scheduled run (read-only)                     |
| `currentRunStatus`        | JobExecutionStatus| No       | Current execution status (read-only)                           |
| `currentRunStep`          | string            | No       | Current step being executed (read-only)                        |
| `currentRunRetryAttempt`  | int               | No       | Current retry attempt number (read-only)                       |
| `hasStep`                 | bool              | No       | Whether the job has any steps defined (read-only)              |
| `hasSchedule`             | bool              | No       | Whether the job has any schedules defined (read-only)          |
| `versionNumber`           | int               | No       | Job version number (read-only)                                 |
| `_exist`                  | bool              | No       | Whether the job should exist (default: true)                   |

## CompletionAction Enum Values

Used for `emailLevel`, `pageLevel`, `netSendLevel`, `eventLogLevel`, and
`deleteLevel`:

| Value       | Description                                      |
|-------------|--------------------------------------------------|
| `Never`     | Never perform the action                         |
| `OnSuccess` | Perform the action only on successful completion |
| `OnFailure` | Perform the action only on failure               |
| `Always`    | Always perform the action                        |

## CompletionResult Enum Values

Returned by `lastRunOutcome` (read-only):

| Value       | Description                    |
|-------------|--------------------------------|
| `Failed`    | The job failed                 |
| `Succeeded` | The job succeeded              |
| `Retry`     | The job is being retried       |
| `Cancelled` | The job was cancelled          |
| `Unknown`   | The outcome is unknown         |

## JobExecutionStatus Enum Values

Returned by `currentRunStatus` (read-only):

| Value                       | Description                           |
|-----------------------------|---------------------------------------|
| `Executing`                 | The job is currently executing        |
| `WaitingForWorkerThread`    | Waiting for a worker thread           |
| `BetweenRetries`            | Between retry attempts                |
| `Idle`                      | The job is idle                       |
| `Suspended`                 | The job is suspended                  |
| `WaitingForStepToFinish`    | Waiting for a step to finish          |
| `PerformingCompletionAction`| Performing completion actions         |

## Examples

### Get a job

```powershell
dsc resource get -r OpenDsc.SqlServer/AgentJob --input '{"serverInstance": ".", "name": "MyJob"}'
```

### Create a basic job

```powershell
dsc resource set -r OpenDsc.SqlServer/AgentJob --input '{
  "serverInstance": ".",
  "name": "MyBackupJob",
  "description": "Daily backup job",
  "isEnabled": true
}'
```

### Create a disabled job

```powershell
dsc resource set -r OpenDsc.SqlServer/AgentJob --input '{
  "serverInstance": ".",
  "name": "MyMaintenanceJob",
  "description": "Weekly maintenance job",
  "isEnabled": false
}'
```

### Create a job with owner and category

```powershell
dsc resource set -r OpenDsc.SqlServer/AgentJob --input '{
  "serverInstance": ".",
  "name": "MyJob",
  "description": "Custom job",
  "ownerLoginName": "sa",
  "category": "[Uncategorized (Local)]"
}'
```

### Create a job with event log notification

```powershell
dsc resource set -r OpenDsc.SqlServer/AgentJob --input '{
  "serverInstance": ".",
  "name": "CriticalJob",
  "description": "Critical process that logs to event log",
  "isEnabled": true,
  "eventLogLevel": "OnFailure"
}'
```

### Create a job with email notification

```powershell
dsc resource set -r OpenDsc.SqlServer/AgentJob --input '{
  "serverInstance": ".",
  "name": "ImportantJob",
  "description": "Job with email notifications",
  "isEnabled": true,
  "emailLevel": "OnFailure",
  "operatorToEmail": "DBA Team"
}'
```

### Create a job with multiple notification levels

```powershell
dsc resource set -r OpenDsc.SqlServer/AgentJob --input '{
  "serverInstance": ".",
  "name": "MonitoredJob",
  "description": "Fully monitored job",
  "isEnabled": true,
  "emailLevel": "OnFailure",
  "operatorToEmail": "DBA Team",
  "eventLogLevel": "Always",
  "deleteLevel": "Never"
}'
```

### Update an existing job

```powershell
dsc resource set -r OpenDsc.SqlServer/AgentJob --input '{
  "serverInstance": ".",
  "name": "MyJob",
  "description": "Updated description",
  "isEnabled": false
}'
```

### Delete a job

```powershell
dsc resource delete -r OpenDsc.SqlServer/AgentJob --input '{"serverInstance": ".", "name": "MyJob"}'
```

### Using SQL Server Authentication

```powershell
dsc resource get -r OpenDsc.SqlServer/AgentJob --input '{
  "serverInstance": "myserver\\instance",
  "connectUsername": "sa",
  "connectPassword": "MyPassword123",
  "name": "MyJob"
}'
```

### Export all jobs

```powershell
$env:SQLSERVER_INSTANCE = "."
dsc resource export -r OpenDsc.SqlServer/AgentJob
```

## DSC Configuration Example

```yaml
$schema: https://aka.ms/dsc/schemas/v3/config/document.json
resources:
  - name: Create backup job
    type: OpenDsc.SqlServer/AgentJob
    properties:
      serverInstance: "."
      name: DailyBackupJob
      description: Performs daily database backups
      isEnabled: true
      category: "[Uncategorized (Local)]"
      eventLogLevel: OnFailure
      emailLevel: OnFailure
      operatorToEmail: DBA Team

  - name: Create maintenance job (disabled)
    type: OpenDsc.SqlServer/AgentJob
    properties:
      serverInstance: "."
      name: WeeklyMaintenance
      description: Weekly index maintenance
      isEnabled: false
      eventLogLevel: Always
```

## Notes

- This resource manages job metadata only. To manage job steps and schedules,
  use the corresponding step and schedule resources (when available).
- The `startStepId` property must reference a valid step ID. If the job has
  no steps, this property has no effect.
- Notification operators must exist before being referenced in
  `operatorToEmail`, `operatorToPage`, or `operatorToNetSend`.
- The `deleteLevel` property can be used to automatically delete jobs after
  completion based on success/failure criteria.
- SQL Server Agent must be running for jobs to execute.

## See Also

- [SQL Server Agent Job SMO Reference][00]
- [CompletionAction Enum][01]

<!-- Link reference definitions -->
[00]: https://learn.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.management.smo.agent.job
[01]: https://learn.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.management.smo.agent.completionaction
