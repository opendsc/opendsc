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

#### Action Properties

- **execute** (string) - The executable or script to run.
- **arguments** (string) - The arguments to pass to the executable.
- **workingDirectory** (string) - The working directory for the task.

#### Trigger Properties

- **taskPath** (string) - The path where the task is located in Task Scheduler.
  Default: `\`.
- **triggerType** (enum) - The trigger schedule for the task. Valid values:
  `Once`, `Daily`, `Weekly`, `AtLogon`, `AtStartup`.
- **startTime** (string) - The time to run the task (for time-based triggers,
  format: HH:mm).
- **daysOfWeek** (array) - Days of the week for weekly triggers. Valid values:
  `Monday`, `Tuesday`, `Wednesday`, `Thursday`, `Friday`, `Saturday`, `Sunday`.
- **daysInterval** (integer) - Interval in days for daily triggers. Minimum: 1.

#### Repetition Properties

- **repetitionInterval** (string) - Time between task repetitions. Minimum: 1
  minute. Format: TimeSpan (e.g., `00:10:00` for 10 minutes).
- **repetitionDuration** (string) - How long to repeat the task. Zero for
  infinite. Format: TimeSpan (e.g., `01:00:00` for 1 hour, `00:00:00` for
  infinite).
- **repetitionStopAtDurationEnd** (boolean) - Stop running task instances at
  duration end. Default: `false`.
- **randomDelay** (string) - Random delay before task starts. Format: TimeSpan
  (e.g., `00:05:00` for 5 minutes).

#### Security Properties

- **user** (string) - The user account to run the task under. Default: `SYSTEM`.
- **runWithHighestPrivileges** (boolean) - Whether to run the task with highest
  privileges.
- **logonType** (enum) - The security logon method for the task. Valid values:
  `None`, `Password`, `S4U`, `InteractiveToken`, `Group`, `ServiceAccount`,
  `InteractiveTokenOrPassword`.

#### Execution Settings

- **enabled** (boolean) - Whether the task is enabled. Default: `true`.
- **executionTimeLimit** (string) - Maximum time allowed to complete the task.
  Default: 3 days (`3.00:00:00`). Zero (`00:00:00`) for no limit. Format:
  TimeSpan.
- **multipleInstances** (enum) - Policy for handling multiple instances of the
  task. Default: `IgnoreNew`. Valid values: `Parallel`, `Queue`, `IgnoreNew`,
  `StopExisting`.
- **priority** (integer) - Task process priority (0-10). 0=Realtime, 4=Normal,
  7=Below Normal (default), 10=Idle.
- **restartCount** (integer) - Number of times to attempt to restart the task
  on failure. Default: `0`.
- **restartInterval** (string) - Time interval between task restart attempts.
  Format: TimeSpan (e.g., `00:01:00` for 1 minute).
- **startWhenAvailable** (boolean) - Start the task as soon as possible after
  a scheduled start is missed. Default: `false`.
- **allowDemandStart** (boolean) - Allow the task to be run on demand. Default:
  `true`.
- **allowHardTerminate** (boolean) - Allow the task to be terminated using
  TerminateProcess. Default: `true`.

#### Power Management

- **disallowStartIfOnBatteries** (boolean) - Do not start the task if the
  computer is running on batteries. Default: `true`.
- **stopIfGoingOnBatteries** (boolean) - Stop the task if the computer switches
  to battery power. Default: `true`.
- **wakeToRun** (boolean) - Wake the computer to run this task. Default:
  `false`.

#### Idle Conditions

- **runOnlyIfIdle** (boolean) - Run the task only if the computer is idle.
  Default: `false`.
- **idleDuration** (string) - Computer must be idle for this duration before
  the task runs. Format: TimeSpan (e.g., `00:10:00` for 10 minutes).
- **idleWaitTimeout** (string) - Maximum time to wait for an idle condition.
  Format: TimeSpan (e.g., `01:00:00` for 1 hour).
- **idleRestartOnIdle** (boolean) - Restart the task if the idle condition
  resumes. Default: `false`.
- **idleStopOnIdleEnd** (boolean) - Stop the task when the computer is no
  longer idle. Default: `true`.

#### Network Settings

- **runOnlyIfNetworkAvailable** (boolean) - Whether to run the task only if
  a network connection is available.

#### Advanced Settings

- **description** (string) - The task description.
- **hidden** (boolean) - Hide the task in the Task Scheduler UI. Default:
  `false`.
- **compatibility** (enum) - Task Scheduler compatibility level. Default: `V2`.
  Valid values: `AT`, `V1`, `V2`, `V2_1`, `V2_2`, `V2_3`.
- **disallowStartOnRemoteAppSession** (boolean) - Do not start the task in a
  Remote Desktop session (requires V2_1 or later). Default: `false`.
- **_exist** (boolean) - Indicates whether the scheduled task should exist.
  Default: `true`.

### TriggerType Values

- `Once` - Run the task once at a specified time
- `Daily` - Run the task daily at a specified time
- `Weekly` - Run the task weekly on specified days
- `AtLogon` - Run the task when a user logs on
- `AtStartup` - Run the task when the system starts

### TaskInstancesPolicy Values

- `Parallel` - Start new instance while existing instance runs
- `Queue` - Start new instance after all other instances complete
- `IgnoreNew` - Do not start new instance if another is running (default)
- `StopExisting` - Stop existing instance before starting new one

### TaskLogonType Values

- `None` - No logon type specified (default)
- `Password` - Use password (requires password during registration)
- `S4U` - Service For User (stores credentials without password)
- `InteractiveToken` - Use interactive token (user must be logged in)
- `Group` - Group activation
- `ServiceAccount` - Service account (no password needed)
- `InteractiveTokenOrPassword` - Use interactive token or password

### TaskCompatibility Values

- `AT` - Compatible with AT command
- `V1` - Windows Server 2003, XP, 2000
- `V2` - Windows Vista, Server 2008 (default)
- `V2_1` - Windows 7, Server 2008 R2
- `V2_2` - Windows 8, Server 2012
- `V2_3` - Windows 10, Server 2016

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

### Create Task with Repetition

Create a task that repeats every 15 minutes for 8 hours:

```powershell
$config = @'
taskName: MonitoringTask
execute: C:\Scripts\monitor.ps1
triggerType: Daily
startTime: "08:00"
repetitionInterval: "00:15:00"
repetitionDuration: "08:00:00"
description: Runs every 15 minutes during business hours
'@

dsc resource set -r OpenDsc.Windows/ScheduledTask -i $config
```

### Create Task with Power Management

Create a task that wakes the computer and runs on battery:

```powershell
$config = @'
taskName: NightlyMaintenance
execute: C:\Scripts\maintenance.bat
triggerType: Daily
startTime: "03:00"
wakeToRun: true
disallowStartIfOnBatteries: false
stopIfGoingOnBatteries: false
executionTimeLimit: "02:00:00"
'@

dsc resource set -r OpenDsc.Windows/ScheduledTask -i $config
```

### Create Idle Task

Create a task that runs only when the computer is idle:

```powershell
$config = @'
taskName: IdleOptimization
execute: C:\Scripts\optimize.bat
triggerType: Daily
startTime: "12:00"
runOnlyIfIdle: true
idleDuration: "00:10:00"
idleWaitTimeout: "01:00:00"
'@

dsc resource set -r OpenDsc.Windows/ScheduledTask -i $config
```

### Create Task with Multiple Instances Policy

Create a task that queues new instances instead of ignoring them:

```powershell
$config = @'
taskName: DataProcessor
execute: C:\Scripts\process.exe
triggerType: AtLogon
multipleInstances: Queue
executionTimeLimit: "00:30:00"
priority: 4
'@

dsc resource set -r OpenDsc.Windows/ScheduledTask -i $config
```

### Create Task with Restart on Failure

Create a task that automatically restarts on failure:

```powershell
$config = @'
taskName: CriticalService
execute: C:\Services\critical.exe
triggerType: AtStartup
restartCount: 3
restartInterval: "00:05:00"
startWhenAvailable: true
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
