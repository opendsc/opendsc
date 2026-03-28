---
description: Reference for the OpenDsc.SqlServer/ObjectPermission resource, which manages SQL Server object-level permissions.
title: "OpenDsc.SqlServer/ObjectPermission"
date: 2026-03-27
topic: reference
---

# OpenDsc.SqlServer/ObjectPermission

## Synopsis

Manages SQL Server object-level permissions on tables, views, stored procedures,
and other
database objects. Supports Grant, Grant With Grant, and Deny states.

## Type name

```plaintext
OpenDsc.SqlServer/ObjectPermission
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

| Property          | Type   | Required | Access     | Description                                                    |
| :---------------- | :----- | :------- | :--------- | :------------------------------------------------------------- |
| `serverInstance`  | string | Yes      | Read/Write | SQL Server instance name.                                      |
| `connectUsername` | string | No       | Write-Only | Username for SQL authentication.                               |
| `connectPassword` | string | No       | Write-Only | Password for SQL authentication.                               |
| `databaseName`    | string | Yes      | Read/Write | Name of the database.                                          |
| `schemaName`      | string | No       | Read/Write | Schema of the object. Defaults to `dbo`.                       |
| `objectType`      | string | Yes      | Read/Write | Type of database object (see table below).                     |
| `objectName`      | string | Yes      | Read/Write | Name of the database object.                                   |
| `principal`       | string | Yes      | Read/Write | Name of the principal (user or role).                          |
| `permission`      | string | Yes      | Read/Write | Object permission (see table below).                           |
| `state`           | string | No       | Read/Write | Permission state: `Grant` (default), `GrantWithGrant`, `Deny`. |
| `grantor`         | string | No       | Read-Only  | Grantor of the permission.                                     |
| `_exist`          | bool   | No       | Read/Write | Whether the permission should exist. Defaults to `true`.       |

### Object types

| Value                 | Description           |
| :-------------------- | :-------------------- |
| `Table`               | Table                 |
| `View`                | View                  |
| `StoredProcedure`     | Stored procedure      |
| `UserDefinedFunction` | User-defined function |
| `Schema`              | Schema                |
| `Sequence`            | Sequence              |
| `Synonym`             | Synonym               |

### Permissions

| Value            | Description                            |
| :--------------- | :------------------------------------- |
| `Select`         | Read data from the object              |
| `Insert`         | Insert data into the object            |
| `Update`         | Modify data in the object              |
| `Delete`         | Delete data from the object            |
| `Execute`        | Execute a stored procedure or function |
| `References`     | Reference the object in a foreign key  |
| `ViewDefinition` | View the object definition             |
| `Alter`          | Alter the object                       |
| `Control`        | Full control over the object           |
| `TakeOwnership`  | Take ownership of the object           |

## Examples

### Example 1 — Grant SELECT on a table

```powershell
dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input '{
  "serverInstance": ".",
  "databaseName": "AppDb",
  "objectType": "Table",
  "objectName": "Customers",
  "principal": "AppUser",
  "permission": "Select",
  "state": "Grant"
}'
```

### Example 2 — Grant EXECUTE on a stored procedure

```powershell
dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input '{
  "serverInstance": ".",
  "databaseName": "AppDb",
  "schemaName": "dbo",
  "objectType": "StoredProcedure",
  "objectName": "usp_GetCustomers",
  "principal": "AppUser",
  "permission": "Execute",
  "state": "Grant"
}'
```

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Grant select on Customers
    type: OpenDsc.SqlServer/ObjectPermission
    properties:
      serverInstance: "."
      databaseName: AppDb
      objectType: Table
      objectName: Customers
      principal: AppUser
      permission: Select
      state: Grant

  - name: Grant execute on stored procedure
    type: OpenDsc.SqlServer/ObjectPermission
    properties:
      serverInstance: "."
      databaseName: AppDb
      objectType: StoredProcedure
      objectName: usp_GetCustomers
      principal: AppUser
      permission: Execute
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
- [OpenDsc.SqlServer/DatabasePermission](database-permission.md)
- [OpenDsc.SqlServer/DatabaseUser](database-user.md)
