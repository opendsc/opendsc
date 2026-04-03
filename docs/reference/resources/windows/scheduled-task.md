---
description: Reference for the OpenDsc.Windows/ScheduledTask resource, which manages Windows scheduled tasks.
title: "OpenDsc.Windows/ScheduledTask"
date: 2026-03-27
topic: reference
---

# OpenDsc.Windows/ScheduledTask

## Synopsis

Manages Windows scheduled tasks, including triggers, actions, and task settings.

## Type name

```plaintext
OpenDsc.Windows/ScheduledTask
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

| Property   | Type     | Required | Access     | Description                                        |
| :--------- | :------- | :------- | :--------- | :------------------------------------------------- |
| `taskName` | string   | Yes      | Read/Write | The name of the scheduled task.                    |
| `taskPath` | string   | No       | Read/Write | The folder path containing the task.               |
| `triggers` | object[] | No       | Read/Write | The triggers that start the task.                  |
| `actions`  | object[] | No       | Read/Write | The actions the task performs.                     |
| `user`     | string   | No       | Read/Write | The user context the task runs under.              |
| `enabled`  | bool     | No       | Read/Write | Whether the task is enabled.                       |
| `_exist`   | bool     | No       | Read/Write | Whether the task should exist. Defaults to `true`. |

> [!NOTE]
> This resource uses an embedded JSON schema due to the complexity of its nested trigger and
> action objects. Use `dsc resource schema -r OpenDsc.Windows/ScheduledTask` to view the full
> schema.

## Examples

### Example 1 — Get a scheduled task

```powershell
dsc resource get -r OpenDsc.Windows/ScheduledTask --input '{"taskName":"\\Microsoft\\Windows\\Defrag\\ScheduledDefrag"}'
```

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

## See also

- [OpenDsc resource reference](../overview.md)
