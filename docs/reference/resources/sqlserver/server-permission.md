---
description: Reference for the OpenDsc.SqlServer/ServerPermission resource, which manages SQL Server server-level permissions.
title: "OpenDsc.SqlServer/ServerPermission"
date: 2026-03-27
topic: reference
---

# OpenDsc.SqlServer/ServerPermission

## Synopsis

Manages SQL Server server-level permissions for logins and server roles.
Supports Grant,
Grant With Grant, and Deny states.

## Type name

```plaintext
OpenDsc.SqlServer/ServerPermission
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

| Property          | Type   | Required | Access     | Description                                                                       |
| :---------------- | :----- | :------- | :--------- | :-------------------------------------------------------------------------------- |
| `serverInstance`  | string | Yes      | Read/Write | SQL Server instance name.                                                         |
| `connectUsername` | string | No       | Write-Only | Username for SQL authentication.                                                  |
| `connectPassword` | string | No       | Write-Only | Password for SQL authentication.                                                  |
| `principal`       | string | Yes      | Read/Write | Name of the principal (login or server role).                                     |
| `permission`      | string | Yes      | Read/Write | Server-level permission (e.g., `ConnectSql`, `ViewServerState`, `ControlServer`). |
| `state`           | string | No       | Read/Write | Permission state: `Grant` (default), `GrantWithGrant`, `Deny`.                    |
| `grantor`         | string | No       | Read-Only  | Grantor of the permission.                                                        |
| `_exist`          | bool   | No       | Read/Write | Whether the permission should exist. Defaults to `true`.                          |

## Examples

### Example 1 — Grant a permission

```powershell
dsc resource set -r OpenDsc.SqlServer/ServerPermission --input '{
  "serverInstance": ".",
  "principal": "AppUser",
  "permission": "ViewServerState",
  "state": "Grant"
}'
```

### Example 2 — Deny a permission

```powershell
dsc resource set -r OpenDsc.SqlServer/ServerPermission --input '{
  "serverInstance": ".",
  "principal": "RestrictedUser",
  "permission": "ConnectSql",
  "state": "Deny"
}'
```

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Grant view server state
    type: OpenDsc.SqlServer/ServerPermission
    properties:
      serverInstance: "."
      principal: MonitoringLogin
      permission: ViewServerState
      state: Grant
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
- [OpenDsc.SqlServer/Login](login.md)
- [OpenDsc.SqlServer/ServerRole](server-role.md)
