---
description: >-
  Reference for the OpenDsc.Windows/Environment resource, which manages Windows environment
  variables at the User or Machine scope.
title: "OpenDsc.Windows/Environment"
date: 2026-03-27
topic: reference
---

# OpenDsc.Windows/Environment

## Synopsis

Manages Windows environment variables at the User or Machine scope.

## Type name

```plaintext
OpenDsc.Windows/Environment
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

| Property | Type   | Required | Access     | Description                                            |
| :------- | :----- | :------- | :--------- | :----------------------------------------------------- |
| `name`   | string | Yes      | Read/Write | The name of the environment variable.                  |
| `value`  | string | No       | Read/Write | The value of the environment variable.                 |
| `scope`  | enum   | No       | Read/Write | The scope: `User` or `Machine`. Defaults to `User`.    |
| `_exist` | bool   | No       | Read/Write | Whether the variable should exist. Defaults to `true`. |

> [!NOTE]
> Setting `scope` to `Machine` requires administrator privileges.

## Examples

### Example 1 — Get an environment variable

```powershell
dsc resource get -r OpenDsc.Windows/Environment --input '{"name":"PATH","scope":"Machine"}'
```

```yaml
actualState:
  name: PATH
  value: C:\Windows\system32;C:\Windows;...
  scope: Machine
```

### Example 2 — Set an environment variable

```powershell
dsc resource set -r OpenDsc.Windows/Environment --input '{
  "name": "APP_HOME",
  "value": "C:\\MyApp",
  "scope": "User"
}'
```

### Example 3 — Delete an environment variable

```powershell
dsc resource delete -r OpenDsc.Windows/Environment --input '{"name":"APP_HOME","scope":"User"}'
```

### Example 4 — Export all environment variables

```powershell
dsc resource export -r OpenDsc.Windows/Environment
```

### Example 5 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Set application home
    type: OpenDsc.Windows/Environment
    properties:
      name: APP_HOME
      value: C:\MyApp
      scope: Machine

  - name: Remove legacy variable
    type: OpenDsc.Windows/Environment
    properties:
      name: LEGACY_VAR
      scope: User
      _exist: false
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
