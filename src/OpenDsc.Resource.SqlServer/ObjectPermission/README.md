# OpenDsc.SqlServer/ObjectPermission

Manages SQL Server object-level permissions on database objects such as tables,
views, stored procedures, user-defined functions, schemas, sequences, and
synonyms.

## Description

The `OpenDsc.SqlServer/ObjectPermission` resource allows you to grant, deny,
or revoke permissions on specific database objects to database principals
(users and roles). Unlike database-level permissions (managed by
`OpenDsc.SqlServer/DatabasePermission`), object permissions apply to
individual objects within a database.

## Supported Object Types

**Table** - Database tables. Common permissions: Select, Insert, Update, Delete,
  References, Alter, Control, ViewDefinition, TakeOwnership.

**View** - Database views. Common permissions: Select, Insert, Update, Delete,
  References, Alter, Control, ViewDefinition, TakeOwnership.

**StoredProcedure** - Stored procedures. Common permissions: Execute, Alter,
  Control, ViewDefinition, TakeOwnership.

**UserDefinedFunction** - Scalar and table-valued functions. Common permissions:
  Execute, Select, References, Alter, Control, ViewDefinition, TakeOwnership.

**Schema** - Database schemas. Common permissions: Select, Insert, Update,
  Delete, Execute, Alter, Control, ViewDefinition, TakeOwnership,
  CreateSequence.

**Sequence** - Sequences. Common permissions: Update, Alter, Control,
  ViewDefinition, TakeOwnership, References.

**Synonym** - Synonyms. Common permissions: Select, Insert, Update, Delete,
Execute, Control, ViewDefinition, TakeOwnership.

## Available Permissions

**Select** - Read data from the object. Applicable to: Table, View, Function,
Schema, Synonym.

**Insert** - Insert rows into the object. Applicable to: Table, View, Schema,
Synonym.

**Update** - Modify data in the object. Applicable to: Table, View, Sequence,
Schema, Synonym.

**Delete** - Remove rows from the object. Applicable to: Table, View, Schema,
Synonym.

**Execute** - Execute the object. Applicable to: StoredProcedure, Function,
Schema, Synonym.

**References** - Reference the object in a foreign key. Applicable to: Table,
View, Function, Sequence.

**Alter** - Modify the definition of the object. Applicable to all object types.

**Control** - Full control over the object. Applicable to all object types.

**ViewDefinition** - View the definition of the object. Applicable to all object
  types.

**TakeOwnership** - Take ownership of the object. Applicable to all
  object types.

**ViewChangeTracking** - View change tracking information. Applicable to: Table,
  View.

**CreateSequence** - Create sequences in the schema. Applicable to: Schema.

## Properties

| Property          | Type   | Required | Description                                                                                                |
|-------------------|--------|----------|------------------------------------------------------------------------------------------------------------|
| `serverInstance`  | string | Yes      | SQL Server instance name (e.g., `.`, `localhost`, `server\instance`)                                       |
| `connectUsername` | string | No       | Username for SQL authentication (write-only)                                                               |
| `connectPassword` | string | No       | Password for SQL authentication (write-only)                                                               |
| `databaseName`    | string | Yes      | Name of the database containing the object                                                                 |
| `schemaName`      | string | No       | Schema name of the object (defaults to `dbo`)                                                              |
| `objectType`      | string | Yes      | Type of object: `Table`, `View`, `StoredProcedure`, `UserDefinedFunction`, `Schema`, `Sequence`, `Synonym` |
| `objectName`      | string | Yes      | Name of the database object                                                                                |
| `principal`       | string | Yes      | Database principal (user or role) to manage permissions for                                                |
| `permission`      | string | Yes      | Permission name (e.g., `Select`, `Execute`, `Insert`)                                                      |
| `state`           | string | No       | Permission state: `Grant`, `GrantWithGrant`, or `Deny` (default: `Grant`)                                  |
| `grantor`         | string | No       | Principal who granted the permission (read-only)                                                           |
| `_exist`          | bool   | No       | Whether the permission should exist (default: `true`)                                                      |

## Examples

### Grant SELECT on a Table

```yaml
# Grant SELECT permission on the Customers table to the AppReader user
$schema: https://schemas.microsoft.com/dsc/2024/03/configuration
resources:
  - name: Grant SELECT on Customers table
    type: OpenDsc.SqlServer/ObjectPermission
    properties:
      serverInstance: "."
      databaseName: "SalesDB"
      schemaName: "dbo"
      objectType: "Table"
      objectName: "Customers"
      principal: "AppReader"
      permission: "Select"
      state: "Grant"
```

### Grant EXECUTE on a Stored Procedure

```yaml
# Grant EXECUTE permission on a stored procedure
$schema: https://schemas.microsoft.com/dsc/2024/03/configuration
resources:
  - name: Grant EXECUTE on GetCustomerOrders procedure
    type: OpenDsc.SqlServer/ObjectPermission
    properties:
      serverInstance: "localhost"
      databaseName: "SalesDB"
      schemaName: "dbo"
      objectType: "StoredProcedure"
      objectName: "GetCustomerOrders"
      principal: "AppUser"
      permission: "Execute"
```

### Deny DELETE on a View

```yaml
# Deny DELETE permission on a view
$schema: https://schemas.microsoft.com/dsc/2024/03/configuration
resources:
  - name: Deny DELETE on CustomerSummary view
    type: OpenDsc.SqlServer/ObjectPermission
    properties:
      serverInstance: "."
      databaseName: "SalesDB"
      objectType: "View"
      objectName: "CustomerSummary"
      principal: "ReportingRole"
      permission: "Delete"
      state: "Deny"
```

### Grant SELECT with GRANT Option

```yaml
# Grant SELECT with the ability to grant to others
$schema: https://schemas.microsoft.com/dsc/2024/03/configuration
resources:
  - name: Grant SELECT with GRANT on Products
    type: OpenDsc.SqlServer/ObjectPermission
    properties:
      serverInstance: "."
      databaseName: "Inventory"
      schemaName: "Sales"
      objectType: "Table"
      objectName: "Products"
      principal: "DataAdmin"
      permission: "Select"
      state: "GrantWithGrant"
```

### Grant Permissions on a Schema

```yaml
# Grant SELECT on all objects in a schema
$schema: https://schemas.microsoft.com/dsc/2024/03/configuration
resources:
  - name: Grant SELECT on Reports schema
    type: OpenDsc.SqlServer/ObjectPermission
    properties:
      serverInstance: "."
      databaseName: "BI"
      objectType: "Schema"
      objectName: "Reports"
      principal: "ReportReaders"
      permission: "Select"
```

### Grant EXECUTE on a Function

```yaml
# Grant EXECUTE on a user-defined function
$schema: https://schemas.microsoft.com/dsc/2024/03/configuration
resources:
  - name: Grant EXECUTE on CalculateTax function
    type: OpenDsc.SqlServer/ObjectPermission
    properties:
      serverInstance: "."
      databaseName: "Finance"
      schemaName: "dbo"
      objectType: "UserDefinedFunction"
      objectName: "CalculateTax"
      principal: "FinanceApp"
      permission: "Execute"
```

### Remove (Revoke) a Permission

```yaml
# Revoke SELECT permission from a user
$schema: https://schemas.microsoft.com/dsc/2024/03/configuration
resources:
  - name: Revoke SELECT on Employees table
    type: OpenDsc.SqlServer/ObjectPermission
    properties:
      serverInstance: "."
      databaseName: "HR"
      objectType: "Table"
      objectName: "Employees"
      principal: "FormerEmployee"
      permission: "Select"
      _exist: false
```

### SQL Authentication

```yaml
# Use SQL authentication to connect
$schema: https://schemas.microsoft.com/dsc/2024/03/configuration
resources:
  - name: Grant INSERT on Orders (SQL Auth)
    type: OpenDsc.SqlServer/ObjectPermission
    properties:
      serverInstance: "sqlserver.company.com"
      connectUsername: "dsc_admin"
      connectPassword: "SecurePassword123!"
      databaseName: "OrderSystem"
      objectType: "Table"
      objectName: "Orders"
      principal: "OrderService"
      permission: "Insert"
```

## CLI Examples

### Get current permission state

```powershell
# Check if SELECT permission exists on a table
$json = @{
    serverInstance = "."
    databaseName = "SalesDB"
    schemaName = "dbo"
    objectType = "Table"
    objectName = "Customers"
    principal = "AppReader"
    permission = "Select"
} | ConvertTo-Json -Compress

dsc resource get -r OpenDsc.SqlServer/ObjectPermission --input $json
```

### Grant a permission

```powershell
# Grant EXECUTE on a stored procedure
$json = @{
    serverInstance = "."
    databaseName = "SalesDB"
    objectType = "StoredProcedure"
    objectName = "CreateOrder"
    principal = "OrderApp"
    permission = "Execute"
    state = "Grant"
} | ConvertTo-Json -Compress

dsc resource set -r OpenDsc.SqlServer/ObjectPermission --input $json
```

### Revoke a permission

```powershell
# Revoke INSERT permission from a user
$json = @{
    serverInstance = "."
    databaseName = "SalesDB"
    objectType = "Table"
    objectName = "Customers"
    principal = "OldUser"
    permission = "Insert"
    _exist = $false
} | ConvertTo-Json -Compress

dsc resource delete -r OpenDsc.SqlServer/ObjectPermission --input $json
```

## Notes

- The resource uses Windows Authentication by default. Specify `connectUsername`
  and `connectPassword` for SQL Server authentication.
- Schema permissions apply to all current and future objects within the schema.
- When `state` is set to `GrantWithGrant`, the principal can grant the
  permission to other principals.
- The `Deny` state takes precedence over `Grant` in SQL Server's permission
  evaluation.
- Deleting a permission (`_exist: false`) revokes it, removing both grants
  and denies.
- The `grantor` property is read-only and shows who granted the permission.

## Related Resources

- [OpenDsc.SqlServer/DatabasePermission](../DatabasePermission/README.md) -
  Manage database-level permissions
- [OpenDsc.SqlServer/ServerPermission](../ServerPermission/README.md) -
  Manage server-level permissions
- [OpenDsc.SqlServer/DatabaseRole](../DatabaseRole/README.md) - Manage database
  roles
