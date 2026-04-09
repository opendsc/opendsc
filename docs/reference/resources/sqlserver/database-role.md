# OpenDsc.SqlServer/DatabaseRole

## Synopsis

Manages SQL Server database roles, including role creation, ownership, and
member management with additive or exact (`_purge`) modes.

## Type name

```text
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

Name of the database containing the role.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### name

Name of the database role.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### owner

Owner of the role (user or role).

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### members

Members of the role. Values must be unique.

```yaml
Type: string[]
Required: No
Access: Read/Write
Default value: None
```

### _purge

When `true`, removes members not in the list. When `false` (default), only adds
members.

```yaml
Type: bool
Required: No
Access: Write-Only
Default value: false
```

### createDate

Creation date.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

### dateLastModified

Date last modified.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

### isFixedRole

Whether this is a fixed database role.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

### _exist

Whether the role should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

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
