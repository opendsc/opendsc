# Server Permission Resource

## Synopsis

Manages SQL Server server-level permissions for logins and server roles.
Supports Grant, Grant With Grant, and Deny states.

## Type

```text
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

### principal

Name of the principal (login or server role).

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### permission

Server-level permission (e.g., `ConnectSql`, `ViewServerState`, `ControlServer`).

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
