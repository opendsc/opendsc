# OpenDsc.SqlServer/Login

## Synopsis

Manage SQL Server logins.

## Description

The `OpenDsc.SqlServer/Login` resource enables you to manage SQL Server
logins including SQL Server authentication logins and Windows authentication
logins. You can create, update, retrieve, and delete logins using Microsoft DSC.

This resource supports configuring login properties such as password policies,
default database, language settings, and server role membership.

## Requirements

- SQL Server instance accessible from the machine running DSC
- Appropriate SQL Server permissions to manage logins (typically sysadmin or
  securityadmin role membership)
- Windows authentication is used for connecting to SQL Server

## Capabilities

The resource has the following capabilities:

- `get` - Retrieve the current state of a login
- `set` - Create or update a login
- `test` - Test if a login is in the desired state
- `delete` - Remove a login
- `export` - List all logins on the server

## Properties

### Required Properties

- **serverInstance** (string) - The name of the SQL Server instance to connect
  to. Use `.` or `(local)` for the default local instance, or
  `servername\instancename` for named instances.
- **name** (string) - The name of the login.

### Optional Properties

- **loginType** (enum) - The type of login. Valid values: `SqlLogin`,
  `WindowsUser`, `WindowsGroup`, `Certificate`, `AsymmetricKey`.
- **password** (string) - The password for the login. Write-only, required when
  creating SQL logins. Not applicable for Windows authentication.
- **defaultDatabase** (string) - The default database for the login.
- **language** (string) - The default language for the login.
- **disabled** (boolean) - Whether the login is disabled.
- **passwordExpirationEnabled** (boolean) - Whether password expiration policy
  is enforced.
- **passwordPolicyEnforced** (boolean) - Whether password policy is enforced.
- **mustChangePassword** (boolean) - Whether the user must change the password
  on next login.
- **denyWindowsLogin** (boolean) - Whether to deny Windows login access. Only
  applicable for Windows logins.
- **serverRoles** (string[]) - Server roles to assign to the login.
- **_exist** (boolean) - Indicates whether the login should exist.
  Default: `true`.

### Read-Only Properties

- **createDate** (datetime) - The creation date of the login.
- **dateLastModified** (datetime) - The date the login was last modified.
- **hasAccess** (boolean) - Whether the login has access to the server.
- **isLocked** (boolean) - Whether the login is locked out.
- **isPasswordExpired** (boolean) - Whether the login's password has expired.
- **isSystemObject** (boolean) - Whether this is a system login.

## Examples

### Login Type Property Requirements

Different login types have different property requirements:

| Login Type    | Password            | PasswordPolicyEnforced | PasswordExpirationEnabled | DenyWindowsLogin |
|---------------|---------------------|------------------------|---------------------------|------------------|
| SqlLogin      | Required for create | Optional               | Optional                  | N/A              |
| WindowsUser   | N/A                 | N/A                    | N/A                       | Optional         |
| WindowsGroup  | N/A                 | N/A                    | N/A                       | Optional         |
| Certificate   | N/A                 | N/A                    | N/A                       | N/A              |
| AsymmetricKey | N/A                 | N/A                    | N/A                       | N/A              |

### Get Login

Retrieve the current state of a login:

```yaml
serverInstance: .
name: MyAppLogin
```

```powershell
dsc resource get -r OpenDsc.SqlServer/Login --input '{"serverInstance":".","name":"MyAppLogin"}'
```

### Create SQL Server Login

Create a new SQL Server authentication login with password:

```yaml
serverInstance: .
name: MyAppLogin
loginType: SqlLogin
password: SecureP@ssw0rd!
defaultDatabase: MyAppDb
passwordPolicyEnforced: true
passwordExpirationEnabled: true
```

```powershell
dsc resource set -r OpenDsc.SqlServer/Login --input '{"serverInstance":".","name":"MyAppLogin","loginType":"SqlLogin","password":"SecureP@ssw0rd!","defaultDatabase":"MyAppDb"}'
```

### Create Windows User Login

Create a Windows user authentication login (no password needed):

```yaml
serverInstance: .
name: DOMAIN\ServiceAccount
loginType: WindowsUser
defaultDatabase: master
```

```powershell
dsc resource set -r OpenDsc.SqlServer/Login --input '{"serverInstance":".","name":"DOMAIN\\ServiceAccount","loginType":"WindowsUser"}'
```

### Create Windows Group Login

Create a Windows group login (no password needed):

```yaml
serverInstance: .
name: DOMAIN\DBAdmins
loginType: WindowsGroup
defaultDatabase: master
```

```powershell
dsc resource set -r OpenDsc.SqlServer/Login --input '{"serverInstance":".","name":"DOMAIN\\DBAdmins","loginType":"WindowsGroup"}'
```

### Create Certificate Login

Create a login from a certificate (certificate must exist on server):

```yaml
serverInstance: .
name: MyCertificateLogin
loginType: Certificate
```

### Create Asymmetric Key Login

Create a login from an asymmetric key (key must exist on server):

```yaml
serverInstance: .
name: MyKeyLogin
loginType: AsymmetricKey
```

### Deny Windows Login Access

Create a Windows login but deny it access to the server:

```yaml
serverInstance: .
name: DOMAIN\RestrictedUser
loginType: WindowsUser
denyWindowsLogin: true
```

### Assign Server Roles

Create a login and assign server roles:

```yaml
serverInstance: .
name: AdminLogin
loginType: SqlLogin
password: SecureP@ssw0rd!
serverRoles:
  - sysadmin
  - securityadmin
```

### Disable a Login

Disable an existing login:

```yaml
serverInstance: .
name: MyAppLogin
disabled: true
```

### Delete Login

Remove a login:

```yaml
serverInstance: .
name: MyAppLogin
_exist: false
```

```powershell
dsc resource delete -r OpenDsc.SqlServer/Login --input '{"serverInstance":".","name":"MyAppLogin"}'
```

### Export All Logins

List all logins on the server:

```powershell
dsc resource export -r OpenDsc.SqlServer/Login --input '{"serverInstance":"."}'
```

## Exit Codes

The resource returns the following exit codes:

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Invalid argument
- **4** - Unauthorized access
- **5** - Invalid operation
