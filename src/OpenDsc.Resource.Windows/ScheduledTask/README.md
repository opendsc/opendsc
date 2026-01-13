# OpenDsc.Windows/ScheduledTask

## Synopsis

Manage Windows scheduled tasks.

## Description

The `OpenDsc.Windows/ScheduledTask` resource enables you to manage Windows
scheduled tasks using Microsoft DSC. You can create, update, retrieve, and
delete scheduled tasks with various trigger types and execution settings.

Scheduled tasks can be configured with different triggers (daily, weekly,
at logon, at startup), execution parameters, and security settings. Some
operations may require administrator privileges.

## Requirements

- Windows operating system
- Administrator privileges required for certain operations (e.g., system tasks)

## Capabilities

The resource has the following capabilities:

- `get` - Retrieve the current configuration of a scheduled task
- `set` - Create or update a scheduled task
- `delete` - Remove a scheduled task
- `export` - List all scheduled tasks

## Properties

### Required Properties

- **taskName** (string) - The name of the scheduled task. Cannot contain
  backslashes, colons, asterisks, question marks, quotes, less-than,
  greater-than, pipes, or null characters.

### Optional Properties

- **taskPath** (string) - The path where the task is located in Task Scheduler.
  Default: `\`.
- **execute** (string) - The executable or script to run.
- **arguments** (string) - The arguments to pass to the executable.
- **workingDirectory** (string) - The working directory for the task.
- **user** (string) - The user account to run the task under. Default: `SYSTEM`.
- **triggerType** (enum) - The trigger schedule for the task. Valid values:
  `Once`, `Daily`, `Weekly`, `AtLogon`, `AtStartup`.
- **startTime** (string) - The time to run the task (for time-based triggers,
  format: HH:mm).
- **daysOfWeek** (array) - Days of the week for weekly triggers. Valid values:
  `Monday`, `Tuesday`, `Wednesday`, `Thursday`, `Friday`, `Saturday`, `Sunday`.
- **daysInterval** (integer) - Interval in days for daily triggers. Minimum: 1.
- **enabled** (boolean) - Whether the task is enabled. Default: `true`.
- **runWithHighestPrivileges** (boolean) - Whether to run the task with highest
  privileges.
- **runOnlyIfNetworkAvailable** (boolean) - Whether to run the task only if
  the computer is on AC power.
- **description** (string) - The task description.
- **_exist** (boolean) - Indicates whether the scheduled task should exist.
  Default: `true`.

### TriggerType Values

- `Once` - Run the task once at a specified time
- `Daily` - Run the task daily at a specified time
- `Weekly` - Run the task weekly on specified days
- `AtLogon` - Run the task when a user logs on
- `AtStartup` - Run the task when the system starts

### DaysOfWeek Values

- `Monday`, `Tuesday`, `Wednesday`, `Thursday`, `Friday`, `Saturday`, `Sunday`

## Examples

### Get Scheduled Task

Retrieve the configuration of a scheduled task:

```powershell
$config = @'
taskName: MyTask
'@

dsc resource get -r OpenDsc.Windows/ScheduledTask -i $config
```

Output:

```yaml
actualState:
  taskName: MyTask
  taskPath: \
  execute: C:\Windows\System32\cmd.exe
  arguments: /c echo Hello World
  user: SYSTEM
  triggerType: Daily
  startTime: "09:00"
  enabled: true
  _exist: true
```

### Get Non-Existent Task

Query a task that doesn't exist:

```powershell
$config = @'
taskName: NonExistentTask
'@

dsc resource get -r OpenDsc.Windows/ScheduledTask -i $config
```

Output:

```yaml
actualState:
  taskName: NonExistentTask
  taskPath: \
  _exist: false
```

### Create Daily Task

Create a new scheduled task that runs daily:

```powershell
$config = @'
taskName: BackupScript
execute: C:\Scripts\backup.bat
arguments: /full
workingDirectory: C:\Scripts
user: SYSTEM
triggerType: Daily
startTime: "02:00"
daysInterval: 1
enabled: true
description: Daily backup task
'@

dsc resource set -r OpenDsc.Windows/ScheduledTask -i $config
```

### Create Weekly Task

Create a task that runs on specific days of the week:

```powershell
$config = @'
taskName: WeeklyReport
execute: powershell.exe
arguments: -File C:\Scripts\report.ps1
user: Administrator
triggerType: Weekly
startTime: "08:00"
daysOfWeek:
  - Monday
  - Wednesday
  - Friday
enabled: true
runWithHighestPrivileges: true
'@

dsc resource set -r OpenDsc.Windows/ScheduledTask -i $config
```

### Create At Startup Task

Create a task that runs when the system starts:

```powershell
$config = @'
taskName: StartupService
execute: C:\Services\MyService.exe
user: SYSTEM
triggerType: AtStartup
enabled: true
'@

dsc resource set -r OpenDsc.Windows/ScheduledTask -i $config
```

### Update Existing Task

Update the configuration of an existing task:

```powershell
$config = @'
taskName: BackupScript
execute: C:\Scripts\backup.bat
arguments: /incremental
startTime: "03:00"
'@

dsc resource set -r OpenDsc.Windows/ScheduledTask -i $config
```

### Delete Scheduled Task

Remove a scheduled task:

```powershell
$config = @'
taskName: BackupScript
_exist: false
'@

dsc resource delete -r OpenDsc.Windows/ScheduledTask -i $config
```

### Export All Tasks

List all scheduled tasks:

```powershell
dsc resource export -r OpenDsc.Windows/ScheduledTask
```

## Exit Codes

The resource returns the following exit codes:

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Access denied
- **4** - Invalid argument
