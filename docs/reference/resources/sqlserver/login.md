---
description: Reference for the OpenDsc.SqlServer/Login resource, which manages SQL Server logins.
title: "OpenDsc.SqlServer/Login"
date: 2026-03-27
topic: reference
---

# OpenDsc.SqlServer/Login

## Synopsis

Manages SQL Server logins, including SQL authentication, Windows authentication,
password policies,
and server role membership.

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

| Property          | Type   | Required | Access     | Description                                                                                           |
| :---------------- | :----- | :------- | :--------- | :---------------------------------------------------------------------------------------------------- |
| `serverInstance`  | string | Yes      | Read/Write | SQL Server instance name. Use `.` or `(local)` for default, or `server\instance` for named instances. |
| `connectUsername` | string | No       | Write-Only | Username for SQL authentication. Omit for Windows authentication.                                     |
| `connectPassword` | string | No       | Write-Only | Password for SQL authentication.                                                                      |

### Login properties

| Property                    | Type     | Required | Access     | Description                                                                                                             |
| :-------------------------- | :------- | :------- | :--------- | :---------------------------------------------------------------------------------------------------------------------- |
| `name`                      | string   | Yes      | Read/Write | Name of the login.                                                                                                      |
| `loginType`                 | string   | No       | Read/Write | Login type: `SqlLogin`, `WindowsUser`, `WindowsGroup`, `Certificate`, `AsymmetricKey`, `ExternalUser`, `ExternalGroup`. |
| `password`                  | string   | No       | Write-Only | Password. Required when creating SQL logins.                                                                            |
| `defaultDatabase`           | string   | No       | Read/Write | Default database for the login.                                                                                         |
| `language`                  | string   | No       | Read/Write | Default language.                                                                                                       |
| `disabled`                  | bool     | No       | Read/Write | Whether the login is disabled.                                                                                          |
| `passwordExpirationEnabled` | bool     | No       | Read/Write | Whether password expiration policy is enforced.                                                                         |
| `passwordPolicyEnforced`    | bool     | No       | Read/Write | Whether password policy is enforced.                                                                                    |
| `mustChangePassword`        | bool     | No       | Read/Write | Whether the user must change the password at next login.                                                                |
| `denyWindowsLogin`          | bool     | No       | Read/Write | Whether to deny Windows login access. Only for Windows logins.                                                          |
| `serverRoles`               | string[] | No       | Read/Write | Server roles to assign. Values must be unique.                                                                          |
| `_purge`                    | bool     | No       | Write-Only | When `true`, removes roles not in `serverRoles`. When `false` (default), only adds roles.                               |

### Read-only properties

| Property            | Type     | Access    | Description                          |
| :------------------ | :------- | :-------- | :----------------------------------- |
| `createDate`        | datetime | Read-Only | Creation date of the login.          |
| `dateLastModified`  | datetime | Read-Only | Date the login was last modified.    |
| `hasAccess`         | bool     | Read-Only | Whether the login has server access. |
| `isLocked`          | bool     | Read-Only | Whether the login is locked out.     |
| `isPasswordExpired` | bool     | Read-Only | Whether the password has expired.    |
| `isSystemObject`    | bool     | Read-Only | Whether this is a system login.      |

### DSC properties

| Property | Type | Required | Access     | Description                                         |
| :------- | :--- | :------- | :--------- | :-------------------------------------------------- |
| `_exist` | bool | No       | Read/Write | Whether the login should exist. Defaults to `true`. |

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
