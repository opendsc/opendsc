# OpenDsc.SqlServer/Login

## Synopsis

Manages SQL Server logins, including SQL authentication, Windows authentication,
password policies, and server role membership.

## Type name

```plaintext
OpenDsc.SqlServer/Login
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

### Connection properties

#### serverInstance

SQL Server instance name. Use `.` or `(local)` for the default instance, or
`server\instance` for named instances.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

#### connectUsername

Username for SQL authentication. Omit for Windows authentication.

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

#### connectPassword

Password for SQL authentication.

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

### Login properties

#### name

Name of the login.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

#### loginType

Login type: `SqlLogin`, `WindowsUser`, `WindowsGroup`, `Certificate`,
`AsymmetricKey`, `ExternalUser`, or `ExternalGroup`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### password

Password. Required when creating SQL logins.

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

#### defaultDatabase

Default database for the login.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### language

Default language.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### disabled

Whether the login is disabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### passwordExpirationEnabled

Whether password expiration policy is enforced.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### passwordPolicyEnforced

Whether password policy is enforced.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### mustChangePassword

Whether the user must change the password at next login.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### denyWindowsLogin

Whether to deny Windows login access. Only applies to Windows logins.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### serverRoles

Server roles to assign. Values must be unique.

```yaml
Type: string[]
Required: No
Access: Read/Write
Default value: None
```

#### _purge

When `true`, removes roles not in `serverRoles`. When `false`, only adds roles.

```yaml
Type: bool
Required: No
Access: Write-Only
Default value: false
```

### Read-only properties

#### createDate

Creation date of the login.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

#### dateLastModified

Date the login was last modified.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

#### hasAccess

Whether the login has server access.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### isLocked

Whether the login is locked out.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### isPasswordExpired

Whether the password has expired.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### isSystemObject

Whether this is a system login.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

### DSC properties

#### _exist

Whether the login should exist. Defaults to `true`.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

## Examples

### Example 1 — Get a login

```powershell
dsc resource get -r OpenDsc.SqlServer/Login --input '{"serverInstance":".","name":"sa"}'
```

### Example 2 — Create a SQL login

```powershell
dsc resource set -r OpenDsc.SqlServer/Login --input '{
  "serverInstance": ".",
  "name": "AppUser",
  "loginType": "SqlLogin",
  "password": "P@ssw0rd!",
  "defaultDatabase": "AppDb",
  "passwordPolicyEnforced": true,
  "serverRoles": ["public"]
}'
```

### Example 3 — Delete a login

```powershell
dsc resource delete -r OpenDsc.SqlServer/Login --input '{"serverInstance":".","name":"AppUser"}'
```

### Example 4 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Application login
    type: OpenDsc.SqlServer/Login
    properties:
      serverInstance: "."
      name: AppUser
      loginType: SqlLogin
      password: "[parameter('appUserPassword')]"
      defaultDatabase: AppDb
      passwordPolicyEnforced: true
      passwordExpirationEnabled: true
      serverRoles:
        - public
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
- [OpenDsc.SqlServer/ServerRole](server-role.md)
- [OpenDsc.SqlServer/ServerPermission](server-permission.md)
