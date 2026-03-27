---
description: Reference for the OpenDsc.Windows/OptionalFeature resource, which manages Windows optional features via DISM.
title: "OpenDsc.Windows/OptionalFeature"
date: 2026-03-27
topic: reference
---

# OpenDsc.Windows/OptionalFeature

## Synopsis

Manages Windows optional features using the Deployment Image Servicing and
Management (DISM) API.
This resource supports signaling restart requirements when a feature enable or
disable operation
requires a system restart.

## Type name

```plaintext
OpenDsc.Windows/OptionalFeature
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | No        |
| Export     | Yes       |

> [!NOTE]
> This resource uses `SetReturn = SetReturn.State` and returns actual state from the `Set()`
> operation, including `_metadata` with restart requirements when applicable.

## Properties

| Property    | Type   | Required | Access     | Description                                                        |
| :---------- | :----- | :------- | :--------- | :----------------------------------------------------------------- |
| `name`      | string | Yes      | Read/Write | The name of the Windows optional feature.                          |
| `state`     | string | No       | Read/Write | The feature state: `Enabled` or `Disabled`.                        |
| `_metadata` | object | No       | Read-Only  | Metadata returned by the resource, may include `_restartRequired`. |

### State values

| Value      | Description                            |
| :--------- | :------------------------------------- |
| `Enabled`  | The feature is enabled on the system.  |
| `Disabled` | The feature is disabled on the system. |

## Examples

### Example 1 — Get a feature state

```powershell
dsc resource get -r OpenDsc.Windows/OptionalFeature --input '{"name":"Microsoft-Hyper-V-All"}'
```

### Example 2 — Enable a feature

```powershell
dsc resource set -r OpenDsc.Windows/OptionalFeature --input '{"name":"Microsoft-Hyper-V-All","state":"Enabled"}'
```

If a restart is required, the result includes `_metadata`:

```json
{
  "name": "Microsoft-Hyper-V-All",
  "state": "Enabled",
  "_metadata": {
    "_restartRequired": [
      { "system": "SERVER01" }
    ]
  }
}
```

### Example 3 — Export all features

```powershell
dsc resource export -r OpenDsc.Windows/OptionalFeature
```

### Example 4 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Enable Windows Subsystem for Linux
    type: OpenDsc.Windows/OptionalFeature
    properties:
      name: Microsoft-Windows-Subsystem-Linux
      state: Enabled
```

## Exit codes

| Code | Description |
| :--- | :---------- |
| 0    | Success     |
| 1    | Error       |

## See also

- [OpenDsc resource reference](../overview.md)
