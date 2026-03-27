---
description: Reference for the OpenDsc.SqlServer/ServerRole resource, which manages SQL Server server roles and membership.
title: "OpenDsc.SqlServer/ServerRole"
date: 2026-03-27
topic: reference
---

# OpenDsc.SqlServer/ServerRole

## Synopsis

Manages SQL Server server roles, including custom role creation, ownership, and
member
management with additive or exact (`_purge`) modes.

## Type name

```plaintext
OpenDsc.SqlServer/ServerRole
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

| Property          | Type     | Required | Access     | Description                                                                              |
| :---------------- | :------- | :------- | :--------- | :--------------------------------------------------------------------------------------- |
| `serverInstance`  | string   | Yes      | Read/Write | SQL Server instance name.                                                                |
| `connectUsername` | string   | No       | Write-Only | Username for SQL authentication.                                                         |
| `connectPassword` | string   | No       | Write-Only | Password for SQL authentication.                                                         |
| `name`            | string   | Yes      | Read/Write | Name of the server role.                                                                 |
| `owner`           | string   | No       | Read/Write | Owner of the role (login or role).                                                       |
| `members`         | string[] | No       | Read/Write | Members (logins or roles). Values must be unique.                                        |
| `_purge`          | bool     | No       | Write-Only | When `true`, removes members not in the list. When `false` (default), only adds members. |
| `dateCreated`     | datetime | No       | Read-Only  | Creation date.                                                                           |
| `dateModified`    | datetime | No       | Read-Only  | Date last modified.                                                                      |
| `isFixedRole`     | bool     | No       | Read-Only  | Whether this is a fixed server role.                                                     |
| `_exist`          | bool     | No       | Read/Write | Whether the role should exist. Defaults to `true`.                                       |

## Examples

### Example 1 — Get a server role

```powershell
dsc resource get -r OpenDsc.SqlServer/ServerRole --input '{"serverInstance":".","name":"sysadmin"}'
```

### Example 2 — Create a custom server role

```powershell
dsc resource set -r OpenDsc.SqlServer/ServerRole --input '{
  "serverInstance": ".",
  "name": "AppAdmins",
  "members": ["AppLoginAdmin", "AppLoginDBA"]
}'
```

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Application admin role
    type: OpenDsc.SqlServer/ServerRole
    properties:
      serverInstance: "."
      name: AppAdmins
      members:
        - AppLoginAdmin
        - AppLoginDBA
      _purge: true
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
- [OpenDsc.SqlServer/ServerPermission](server-permission.md)
