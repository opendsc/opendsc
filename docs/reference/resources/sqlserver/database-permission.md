---
description: Reference for the OpenDsc.SqlServer/DatabasePermission resource, which manages SQL Server database-level permissions.
title: "OpenDsc.SqlServer/DatabasePermission"
date: 2026-03-27
topic: reference
---

# OpenDsc.SqlServer/DatabasePermission

## Synopsis

Manages SQL Server database-level permissions for users and database roles.
Supports Grant,
Grant With Grant, and Deny states.

## Type name

```plaintext
OpenDsc.SqlServer/DatabasePermission
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

| Property          | Type   | Required | Access     | Description                                                          |
| :---------------- | :----- | :------- | :--------- | :------------------------------------------------------------------- |
| `serverInstance`  | string | Yes      | Read/Write | SQL Server instance name.                                            |
| `connectUsername` | string | No       | Write-Only | Username for SQL authentication.                                     |
| `connectPassword` | string | No       | Write-Only | Password for SQL authentication.                                     |
| `databaseName`    | string | Yes      | Read/Write | Name of the database.                                                |
| `principal`       | string | Yes      | Read/Write | Name of the principal (user or database role).                       |
| `permission`      | string | Yes      | Read/Write | Database permission (e.g., `Connect`, `Select`, `Execute`, `Alter`). |
| `state`           | string | No       | Read/Write | Permission state: `Grant` (default), `GrantWithGrant`, `Deny`.       |
| `grantor`         | string | No       | Read-Only  | Grantor of the permission.                                           |
| `_exist`          | bool   | No       | Read/Write | Whether the permission should exist. Defaults to `true`.             |

## Examples

### Example 1 — Grant SELECT to a user

```powershell
dsc resource set -r OpenDsc.SqlServer/DatabasePermission --input '{
  "serverInstance": ".",
  "databaseName": "AppDb",
  "principal": "AppUser",
  "permission": "Select",
  "state": "Grant"
}'
```

### Example 2 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Grant database connect
    type: OpenDsc.SqlServer/DatabasePermission
    properties:
      serverInstance: "."
      databaseName: AppDb
      principal: AppUser
      permission: Connect
      state: Grant

  - name: Grant database select
    type: OpenDsc.SqlServer/DatabasePermission
    properties:
      serverInstance: "."
      databaseName: AppDb
      principal: AppUser
      permission: Select
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
- [OpenDsc.SqlServer/DatabaseUser](database-user.md)
- [OpenDsc.SqlServer/DatabaseRole](database-role.md)
