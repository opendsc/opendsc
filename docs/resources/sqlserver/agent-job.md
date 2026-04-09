# Agent Job Resource

## Synopsis

Manages SQL Server Agent jobs, including job creation, notification settings,
and monitoring of job execution status.

## Type

```text
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

#### serverInstance

SQL Server instance name.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

#### connectUsername

Username for SQL authentication.

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

#### connectPassword

Password for SQL authentication.

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

### Job properties

#### name

Name of the Agent job.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### description

Description of the job.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### isEnabled

Whether the job is enabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### category

Category of the job.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### ownerLoginName

Login name of the job owner.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### startStepId

Step ID at which execution starts. Minimum value is 1.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

### Notification properties

#### emailLevel

When to send email. Accepts `Never`, `OnSuccess`, `OnFailure`, or `Always`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### operatorToEmail

Operator to email.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### pageLevel

When to page. Accepts `Never`, `OnSuccess`, `OnFailure`, or `Always`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### operatorToPage

Operator to page.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### netSendLevel

When to net send. Accepts `Never`, `OnSuccess`, `OnFailure`, or `Always`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### operatorToNetSend

Operator for net send.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### eventLogLevel

When to write to the event log. Accepts `Never`, `OnSuccess`, `OnFailure`, or
`Always`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### deleteLevel

When to delete the job. Accepts `Never`, `OnSuccess`, `OnFailure`, or `Always`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### Read-only properties

#### jobId

Unique identifier of the job.

```yaml
Type: guid
Required: No
Access: Read-Only
Default value: None
```

#### dateCreated

Date the job was created.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

#### dateLastModified

Date the job was last modified.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

#### lastRunDate

Date of the last run.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

#### lastRunOutcome

Outcome of the last run. Returns `Failed`, `Succeeded`, `Retry`, or `Cancelled`.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

#### nextRunDate

Date of the next scheduled run.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

#### currentRunStatus

Current status. Returns `Idle`, `Executing`, `WaitingForWorkerThread`, or
`BetweenRetries`.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

#### currentRunStep

Current step being executed.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

#### currentRunRetryAttempt

Current retry attempt number.

```yaml
Type: int
Required: No
Access: Read-Only
Default value: None
```

#### hasStep

Whether the job has steps defined.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### hasSchedule

Whether the job has schedules.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### versionNumber

Job version number.

```yaml
Type: int
Required: No
Access: Read-Only
Default value: None
```

### DSC properties

#### _exist

Whether the job should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

## Examples

### Example 1 — Get a job

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    serverInstance: .
    name: syspolicy_purge_history
    '@

    dsc resource get -r OpenDsc.SqlServer/AgentJob --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    serverInstance: .
    name: syspolicy_purge_history
    EOF
    )

    dsc resource get -r OpenDsc.SqlServer/AgentJob --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 2 — Create a job

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    serverInstance: .
    name: NightlyMaintenance
    description: Nightly index rebuild and statistics update
    isEnabled: true
    category: Database Maintenance
    emailLevel: OnFailure
    operatorToEmail: DBA Team
    '@

    dsc resource set -r OpenDsc.SqlServer/AgentJob --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    serverInstance: .
    name: NightlyMaintenance
    description: Nightly index rebuild and statistics update
    isEnabled: true
    category: Database Maintenance
    emailLevel: OnFailure
    operatorToEmail: DBA Team
    EOF
    )

    dsc resource set -r OpenDsc.SqlServer/AgentJob --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

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
