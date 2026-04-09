# Object Permission Resource

## Synopsis

Manages SQL Server object-level permissions on tables, views, stored procedures,
and other database objects. Supports Grant, Grant With Grant, and Deny states.

## Type

```text
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

### serverInstance

SQL Server instance name.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### connectUsername

Username for SQL authentication.

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

### connectPassword

Password for SQL authentication.

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

### databaseName

Name of the database.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### schemaName

Schema of the object.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: dbo
```

### objectType

Type of database object. See [Object types](#object-types) below.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### objectName

Name of the database object.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### principal

Name of the principal (user or role).

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### permission

Object permission. See [Permissions](#permissions) below.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### state

Permission state. Accepts `Grant`, `GrantWithGrant`, or `Deny`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: Grant
```

### grantor

Grantor of the permission.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

### _exist

Whether the permission should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

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
