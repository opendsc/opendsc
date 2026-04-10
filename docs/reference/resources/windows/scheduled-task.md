# Scheduled Task Resource

## Synopsis

Manages Windows scheduled tasks, including triggers, actions, and task settings.

## Type

```text
OpenDsc.Windows/ScheduledTask
```

## Capabilities

- Get
- Set
- Delete
- Export

## Properties

### taskName

The name of the scheduled task.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### taskPath

The folder path containing the task.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### triggers

The triggers that start the task.

```yaml
Type: object[]
Required: No
Access: Read/Write
Default value: None
```

### actions

The actions the task performs.

```yaml
Type: object[]
Required: No
Access: Read/Write
Default value: None
```

### user

The user context the task runs under.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### enabled

Whether the task is enabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

### _exist

Whether the task should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

!!! note
    This resource uses an embedded JSON schema due to the complexity of its
    nested trigger and action objects.
    Use `dsc resource schema -r OpenDsc.Windows/ScheduledTask` to view the full
    schema.

## Examples

### Example 1 — Get a scheduled task

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    taskName: '\Microsoft\Windows\Defrag\ScheduledDefrag'
    '@

    dsc resource get -r OpenDsc.Windows/ScheduledTask --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    taskName: '\Microsoft\Windows\Defrag\ScheduledDefrag'
    EOF
    )

    dsc resource get -r OpenDsc.Windows/ScheduledTask --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 2 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Daily cleanup task
    type: OpenDsc.Windows/ScheduledTask
    properties:
      taskName: DailyCleanup
      taskPath: \MyTasks\
      enabled: true
      actions:
        - execute: C:\Scripts\cleanup.bat
      triggers:
        - daily:
            daysInterval: 1
            startBoundary: "2026-01-01T02:00:00"
```

## Exit codes

| Code | Description      |
| :--- | :--------------- |
| 0    | Success          |
| 1    | Error            |
| 2    | Invalid JSON     |
| 3    | Access denied    |
| 4    | Invalid argument |
