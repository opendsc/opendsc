# Server Role Resource

## Synopsis

Manages SQL Server server roles, including custom role creation, ownership, and
member management with additive or exact (`_purge`) modes.

## Type

```text
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

### name

Name of the server role.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### owner

Owner of the role (login or role).

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### members

Members (logins or roles). Values must be unique.

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

### dateCreated

Creation date.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

### dateModified

Date last modified.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

### isFixedRole

Whether this is a fixed server role.

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
