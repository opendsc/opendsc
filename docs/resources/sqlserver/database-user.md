# OpenDsc.SqlServer/DatabaseUser

## Synopsis

Manages SQL Server database users, including SQL users mapped to logins, Windows
users, contained database users, and certificate or asymmetric key mapped users.

## Type name

```text
OpenDsc.SqlServer/DatabaseUser
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
Required: No
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

Name of the database containing the user.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### name

Name of the database user.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### userType

User type. See [User types](#user-types) below.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### login

Login mapped to this user. Required for `SqlUser`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### defaultSchema

Default schema for the user.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: dbo
```

### password

Password for contained database users.

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

### asymmetricKey

Asymmetric key name. Used for `AsymmetricKeyMappedUser`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### certificate

Certificate name. Used for `CertificateMappedUser`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### defaultLanguage

Default language.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
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

### hasDBAccess

Whether the user has database access.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

### isSystemObject

Whether this is a system user.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

### sid

Security identifier (SID).

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

### authenticationType

Authentication type.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

### _exist

Whether the user should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

### User types

| Value                     | Description                         |
| :------------------------ | :---------------------------------- |
| `SqlUser`                 | SQL user mapped to a server login   |
| `NoLogin`                 | User without a login                |
| `WindowsUser`             | Windows user                        |
| `WindowsGroup`            | Windows group                       |
| `CertificateMappedUser`   | User mapped to a certificate        |
| `AsymmetricKeyMappedUser` | User mapped to an asymmetric key    |
| `ExternalUser`            | External user (Microsoft Entra ID)  |
| `ExternalGroup`           | External group (Microsoft Entra ID) |

## Examples

### Example 1 — Get a database user

```powershell
dsc resource get -r OpenDsc.SqlServer/DatabaseUser --input '{"serverInstance":".","databaseName":"AppDb","name":"dbo"}'
```

### Example 2 — Create a user mapped to a login

```powershell
dsc resource set -r OpenDsc.SqlServer/DatabaseUser --input '{
  "serverInstance": ".",
  "databaseName": "AppDb",
  "name": "AppUser",
  "userType": "SqlUser",
  "login": "AppUser",
  "defaultSchema": "dbo"
}'
```

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Application database user
    type: OpenDsc.SqlServer/DatabaseUser
    properties:
      serverInstance: "."
      databaseName: AppDb
      name: AppUser
      userType: SqlUser
      login: AppUser
      defaultSchema: dbo
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
- [OpenDsc.SqlServer/DatabaseRole](database-role.md)
