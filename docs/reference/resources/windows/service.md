---
description: Reference for the OpenDsc.Windows/Service resource, which manages Windows services.
title: "OpenDsc.Windows/Service"
date: 2026-03-27
topic: reference
---

# OpenDsc.Windows/Service

## Synopsis

Manages Windows services, including start type, status, and service
configuration.

## Type name

```plaintext
OpenDsc.Windows/Service
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

| Property       | Type     | Required | Access     | Description                                           |
| :------------- | :------- | :------- | :--------- | :---------------------------------------------------- |
| `name`         | string   | Yes      | Read/Write | The service name (not display name).                  |
| `displayName`  | string   | No       | Read-only  | The display name of the service.                      |
| `description`  | string   | No       | Read-only  | The service description.                              |
| `path`         | string   | No       | Read/Write | The path to the service executable.                   |
| `dependencies` | string[] | No       | Read/Write | Service dependencies.                                 |
| `status`       | enum     | No       | Read/Write | Desired status: `Running`, `Stopped`, `Paused`.       |
| `startType`    | enum     | No       | Read/Write | Start type: `Automatic`, `Manual`, `Disabled`.        |
| `_exist`       | bool     | No       | Read/Write | Whether the service should exist. Defaults to `true`. |

> [!NOTE]
> This resource requires administrator privileges.

## Examples

### Example 1 — Ensure a service is running

```powershell
dsc resource set -r OpenDsc.Windows/Service --input '{
  "name": "Spooler",
  "status": "Running",
  "startType": "Automatic"
}'
```

### Example 2 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Ensure Print Spooler is running
    type: OpenDsc.Windows/Service
    properties:
      name: Spooler
      status: Running
      startType: Automatic

  - name: Disable Remote Registry
    type: OpenDsc.Windows/Service
    properties:
      name: RemoteRegistry
      status: Stopped
      startType: Disabled
```

## Exit codes

| Code | Description                              |
| :--- | :--------------------------------------- |
| 0    | Success                                  |
| 1    | Error                                    |
| 2    | Invalid JSON                             |
| 3    | Windows API error                        |
| 4    | Invalid argument or missing required parameter |
| 5    | Invalid operation or service state       |
| 6    | Service operation timed out              |

## See also

- [OpenDsc resource reference](../overview.md)
