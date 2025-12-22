# OpenDsc.Windows/User

## Synopsis

Manages local Windows user accounts.

## Description

The `OpenDsc.Windows/User` resource enables you to create, configure, update,
and delete local Windows user accounts. This resource provides comprehensive
user account management including password policies, account status, and user
properties.

When creating a new user, the `password` property is required. Passwords are
write-only and never returned by the Get operation for security. Windows
enforces password complexity requirements based on local security policy.

Usernames must be 1-20 characters and cannot contain: `\ / [ ] : ; | = , + * ?
< > @ "`. Exercise caution when managing built-in accounts (Administrator,
Guest, etc.).

## Requirements

- Windows operating system
- .NET 10.0 runtime
- **Administrator privileges required** for all user management operations

## Capabilities

- **get** - Query user account information
- **set** - Create or update user accounts
- **delete** - Remove user accounts
- **export** - List all local user accounts

## Properties

### Required Properties

- **userName** (string) - The username of the local user account. Must be
  1-20 characters and cannot contain: `\ / [ ] : ; | = , + * ? < > @ "`

### Optional Properties

- **fullName** (string) - The full name (display name) of the user
- **description** (string) - A description of the user account
- **password** (string) - The password for the user account. Write-only,
  not returned by Get operation. Required when creating a new user
- **disabled** (boolean) - Whether the user account is disabled
- **passwordNeverExpires** (boolean) - Whether the user's password never expires
- **userMayNotChangePassword** (boolean) - Whether the user may change
  their own password
- **_exist** (boolean) - Indicates whether the user account should exist.
  Default: `true`

## Examples

### Get user information

```powershell
$config = @'
userName: jdoe
'@

dsc resource get -r OpenDsc.Windows/User -i $config
```

### Create a new user

```powershell
$config = @'
userName: jdoe
fullName: John Doe
password: P@ssw0rd123!
description: Developer
'@

dsc resource set -r OpenDsc.Windows/User -i $config
```

### Update user properties

```powershell
$config = @'
userName: jdoe
fullName: John A. Doe
description: Senior Developer
'@

dsc resource set -r OpenDsc.Windows/User -i $config
```

### Configure service account with password policy

```powershell
$config = @'
userName: svcaccount
passwordNeverExpires: true
userMayNotChangePassword: true
description: Service account for background tasks
'@

dsc resource set -r OpenDsc.Windows/User -i $config
```

### Create a disabled user

```powershell
$config = @'
userName: tempuser
password: Temp@Pass123
disabled: true
description: Temporary account
'@

dsc resource set -r OpenDsc.Windows/User -i $config
```

### Delete a user

```powershell
$config = @'
userName: jdoe
'@

dsc resource delete -r OpenDsc.Windows/User -i $config
```

### Export all users

```powershell
dsc resource export -r OpenDsc.Windows/User
```

## Exit Codes

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Access denied
- **4** - Invalid argument
- **5** - Unauthorized access
- **6** - User already exists
