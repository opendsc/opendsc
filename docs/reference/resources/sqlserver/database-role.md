---
description: Reference for the OpenDsc.SqlServer/DatabaseRole resource, which manages SQL Server database roles and membership.
title: "OpenDsc.SqlServer/DatabaseRole"
date: 2026-03-27
topic: reference
---

# OpenDsc.SqlServer/DatabaseRole

## Synopsis

Manages SQL Server database roles, including role creation, ownership, and
member management
with additive or exact (`_purge`) modes.

## Type name

```plaintext
OpenDsc.SqlServer/DatabaseRole
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

| Property           | Type     | Required | Access     | Description                                                                              |
| :----------------- | :------- | :------- | :--------- | :--------------------------------------------------------------------------------------- |
| `serverInstance`   | string   | Yes      | Read/Write | SQL Server instance name.                                                                |
| `connectUsername`  | string   | No       | Write-Only | Username for SQL authentication.                                                         |
| `connectPassword`  | string   | No       | Write-Only | Password for SQL authentication.                                                         |
| `databaseName`     | string   | Yes      | Read/Write | Name of the database containing the role.                                                |
| `name`             | string   | Yes      | Read/Write | Name of the database role.                                                               |
| `owner`            | string   | No       | Read/Write | Owner of the role (user or role).                                                        |
| `members`          | string[] | No       | Read/Write | Members of the role. Values must be unique.                                              |
| `_purge`           | bool     | No       | Write-Only | When `true`, removes members not in the list. When `false` (default), only adds members. |
| `createDate`       | datetime | No       | Read-Only  | Creation date.                                                                           |
| `dateLastModified` | datetime | No       | Read-Only  | Date last modified.                                                                      |
| `isFixedRole`      | bool     | No       | Read-Only  | Whether this is a fixed database role.                                                   |
| `_exist`           | bool     | No       | Read/Write | Whether the role should exist. Defaults to `true`.                                       |

## Examples

### Example 1 — Get a role

```powershell
dsc resource get -r OpenDsc.SqlServer/DatabaseRole --input '{"serverInstance":".","databaseName":"AppDb","name":"db_datareader"}'
```

### Example 2 — Create a role with members

```powershell
dsc resource set -r OpenDsc.SqlServer/DatabaseRole --input '{
  "serverInstance": ".",
  "databaseName": "AppDb",
  "name": "AppReaders",
  "members": ["AppUser", "ReportUser"]
}'
```

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Application reader role
    type: OpenDsc.SqlServer/DatabaseRole
    properties:
      serverInstance: "."
      databaseName: AppDb
      name: AppReaders
      members:
        - AppUser
        - ReportUser
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
- [OpenDsc.SqlServer/DatabaseUser](database-user.md)
- [OpenDsc.SqlServer/DatabasePermission](database-permission.md)
