---
description: Reference for the OpenDsc.SqlServer/DatabaseUser resource, which manages SQL Server database users.
title: "OpenDsc.SqlServer/DatabaseUser"
date: 2026-03-27
topic: reference
---

# OpenDsc.SqlServer/DatabaseUser

## Synopsis

Manages SQL Server database users, including SQL users mapped to logins, Windows
users,
contained database users, and certificate or asymmetric key mapped users.

## Type name

```plaintext
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

| Property             | Type     | Required | Access     | Description                                          |
| :------------------- | :------- | :------- | :--------- | :--------------------------------------------------- |
| `serverInstance`     | string   | No       | Read/Write | SQL Server instance name.                            |
| `connectUsername`    | string   | No       | Write-Only | Username for SQL authentication.                     |
| `connectPassword`    | string   | No       | Write-Only | Password for SQL authentication.                     |
| `databaseName`       | string   | No       | Read/Write | Name of the database containing the user.            |
| `name`               | string   | No       | Read/Write | Name of the database user.                           |
| `userType`           | string   | No       | Read/Write | User type (see table below).                         |
| `login`              | string   | No       | Read/Write | Login mapped to this user. Required for `SqlUser`.   |
| `defaultSchema`      | string   | No       | Read/Write | Default schema. Defaults to `dbo`.                   |
| `password`           | string   | No       | Write-Only | Password for contained database users.               |
| `asymmetricKey`      | string   | No       | Read/Write | Asymmetric key name (for `AsymmetricKeyMappedUser`). |
| `certificate`        | string   | No       | Read/Write | Certificate name (for `CertificateMappedUser`).      |
| `defaultLanguage`    | string   | No       | Read-Only  | Default language.                                    |
| `createDate`         | datetime | No       | Read-Only  | Creation date.                                       |
| `dateLastModified`   | datetime | No       | Read-Only  | Date last modified.                                  |
| `hasDBAccess`        | bool     | No       | Read-Only  | Whether the user has database access.                |
| `isSystemObject`     | bool     | No       | Read-Only  | Whether this is a system user.                       |
| `sid`                | string   | No       | Read-Only  | Security identifier (SID).                           |
| `authenticationType` | string   | No       | Read-Only  | Authentication type.                                 |
| `_exist`             | bool     | No       | Read/Write | Whether the user should exist. Defaults to `true`.   |

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
