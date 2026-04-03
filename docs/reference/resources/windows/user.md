---
description: >-
  Reference for the OpenDsc.Windows/User resource, which manages local Windows user accounts.
title: "OpenDsc.Windows/User"
date: 2026-03-27
topic: reference
---

# OpenDsc.Windows/User

## Synopsis

Manages local Windows user accounts, including creation, modification, and
removal.

## Type name

```plaintext
OpenDsc.Windows/User
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

| Property                   | Type   | Required | Access     | Description                                        |
| :------------------------- | :----- | :------- | :--------- | :------------------------------------------------- |
| `userName`                 | string | Yes      | Read/Write | The name of the user account.                      |
| `fullName`                 | string | No       | Read/Write | The full display name of the user.                 |
| `description`              | string | No       | Read/Write | A description of the user account.                 |
| `password`                 | string | No       | Write-only | The password for the account.                      |
| `disabled`                 | bool   | No       | Read/Write | Whether the account is disabled.                   |
| `passwordNeverExpires`     | bool   | No       | Read/Write | Whether the password is set to never expire.       |
| `userMayNotChangePassword` | bool   | No       | Read/Write | Whether the user can change their password.        |
| `_exist`                   | bool   | No       | Read/Write | Whether the user should exist. Defaults to `true`. |

> [!NOTE]
> This resource requires administrator privileges for all write operations.

## Examples

### Example 1 — Get a user account

```powershell
dsc resource get -r OpenDsc.Windows/User --input '{"userName":"Administrator"}'
```

### Example 2 — Create a user

```powershell
dsc resource set -r OpenDsc.Windows/User --input '{
  "userName": "svc-app",
  "fullName": "Application Service Account",
  "description": "Service account for the application",
  "password": "SecureP@ssw0rd!",
  "passwordNeverExpires": true
}'
```

### Example 3 — Delete a user

```powershell
dsc resource delete -r OpenDsc.Windows/User --input '{"userName":"svc-app"}'
```

### Example 4 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Create service account
    type: OpenDsc.Windows/User
    properties:
      userName: svc-app
      fullName: Application Service Account
      passwordNeverExpires: true

  - name: Disable guest account
    type: OpenDsc.Windows/User
    properties:
      userName: Guest
      disabled: true
```

## Exit codes

| Code | Description         |
| :--- | :------------------ |
| 0    | Success             |
| 1    | Error               |
| 2    | Invalid JSON        |
| 3    | Access denied       |
| 4    | Invalid argument    |
| 5    | Unauthorized access |
| 6    | User already exists |

## See also

- [`OpenDsc.Windows/Group`](group.md)
- [OpenDsc resource reference](../overview.md)
