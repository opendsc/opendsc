# Database Permission Resource

## Synopsis

Manages SQL Server database-level permissions for users and database roles.
Supports Grant, Grant With Grant, and Deny states.

## Type

```text
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

### principal

Name of the principal (user or database role).

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### permission

Database permission (e.g., `Connect`, `Select`, `Execute`, `Alter`).

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
