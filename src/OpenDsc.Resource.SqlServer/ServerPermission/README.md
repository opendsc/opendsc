# OpenDsc.SqlServer/ServerPermission

## Synopsis

Manage SQL Server server-level permissions.

## Description

The `OpenDsc.SqlServer/ServerPermission` resource enables you to manage
server-level permissions in SQL Server. You can grant, deny, or revoke
permissions for server principals (logins or server roles).

This resource supports all standard server-level permissions including
VIEW SERVER STATE, ALTER ANY LOGIN, CONTROL SERVER, and many more.

## Requirements

- SQL Server instance accessible from the machine running DSC
- Appropriate SQL Server permissions to manage server permissions (typically
  requires CONTROL SERVER permission or sysadmin role membership)
- Windows authentication is used for connecting to SQL Server

## Capabilities

The resource has the following capabilities:

- `get` - Retrieve the current state of a server permission
- `set` - Grant or deny a server permission
- `test` - Test if a permission is in the desired state
- `delete` - Revoke a server permission
- `export` - List all server permissions for principals

## Properties

### Required Properties

- **serverInstance** (string) - The name of the SQL Server instance to connect
  to. Use `.` or `(local)` for the default local instance, or
  `servername\instancename` for named instances.
- **principal** (string) - The name of the principal (login or server role)
  to grant or deny permissions to.
- **permission** (enum) - The server-level permission to grant or deny. See
  Available Permissions section below.

### Optional Properties

- **state** (enum) - The state of the permission. Valid values:
  - `Grant` - Grants the permission (default)
  - `GrantWithGrant` - Grants the permission with the ability to grant it
    to others
  - `Deny` - Denies the permission
- **_exist** (boolean) - Indicates whether the permission should exist.
  Default: `true`. Set to `false` to revoke the permission.

### Read-Only Properties

- **grantor** (string) - The principal who granted the permission.
- **_inDesiredState** (boolean) - Indicates whether the permission is in the
  desired state.

## Available Permissions

The following server-level permissions are supported:

### Administration Permissions

- **AdministerBulkOperations** - Ability to run BULK INSERT
- **ControlServer** - Full control over the server
- **Shutdown** - Ability to shut down the server
- **UnsafeAssembly** - Ability to create UNSAFE assemblies

### Alter Permissions

- **AlterAnyAvailabilityGroup** - Ability to alter any availability group
- **AlterAnyConnection** - Ability to alter any connection
- **AlterAnyCredential** - Ability to alter any credential
- **AlterAnyDatabase** - Ability to alter any database
- **AlterAnyEndpoint** - Ability to alter any endpoint
- **AlterAnyEventNotification** - Ability to alter any event notification
- **AlterAnyEventSession** - Ability to alter any event session
- **AlterAnyLinkedServer** - Ability to alter any linked server
- **AlterAnyLogin** - Ability to alter any login
- **AlterAnyServerAudit** - Ability to alter any server audit
- **AlterAnyServerRole** - Ability to alter any server role
- **AlterResources** - Ability to alter resources
- **AlterServerState** - Ability to alter server state
- **AlterSettings** - Ability to alter server settings
- **AlterTrace** - Ability to alter traces

### Connect Permissions

- **ConnectSql** - Ability to connect to the server
- **ConnectAnyDatabase** - Ability to connect to any database

### Create Permissions

- **CreateAnyDatabase** - Ability to create any database
- **CreateAvailabilityGroup** - Ability to create availability groups
- **CreateDdlEventNotification** - Ability to create DDL event notifications
- **CreateEndpoint** - Ability to create endpoints
- **CreateServerRole** - Ability to create server roles
- **CreateTraceEventNotification** - Ability to create trace event notifications

### Security Permissions

- **Authenticate** - Ability to authenticate as the principal
- **AuthenticateServer** - Ability to authenticate to the server
- **ImpersonateAnyLogin** - Ability to impersonate any login
- **SelectAllUserSecurables** - Ability to select all user securables

### View Permissions

- **ViewAnyDatabase** - Ability to view any database
- **ViewAnyDefinition** - Ability to view any definition
- **ViewServerState** - Ability to view server state

## Examples

### Get Permission State

Retrieve the current state of a server permission:

```yaml
serverInstance: .
principal: MonitoringLogin
permission: ViewServerState
```

```powershell
dsc resource get -r OpenDsc.SqlServer/ServerPermission --input '{"serverInstance":".","principal":"MonitoringLogin","permission":"ViewServerState"}'
```

### Grant View Server State

Grant VIEW SERVER STATE permission for monitoring:

```yaml
serverInstance: .
principal: MonitoringLogin
permission: ViewServerState
state: Grant
```

```powershell
dsc resource set -r OpenDsc.SqlServer/ServerPermission --input '{"serverInstance":".","principal":"MonitoringLogin","permission":"ViewServerState","state":"Grant"}'
```

### Grant Permission with Grant Option

Grant a permission that the principal can grant to others:

```yaml
serverInstance: .
principal: SecurityAdmin
permission: AlterAnyLogin
state: GrantWithGrant
```

### Deny Permission

Deny a permission to prevent server access:

```yaml
serverInstance: .
principal: RestrictedLogin
permission: AlterAnyDatabase
state: Deny
```

### Grant Multiple Server Permissions

Grant common administrative permissions (requires multiple resource instances):

```yaml
# First resource - View server state
serverInstance: .
principal: AdminLogin
permission: ViewServerState

# Second resource - View any database
serverInstance: .
principal: AdminLogin
permission: ViewAnyDatabase

# Third resource - Connect SQL
serverInstance: .
principal: AdminLogin
permission: ConnectSql
```

### Revoke Permission

Remove a previously granted or denied permission:

```yaml
serverInstance: .
principal: OldLogin
permission: ViewServerState
_exist: false
```

```powershell
dsc resource delete -r OpenDsc.SqlServer/ServerPermission --input '{"serverInstance":".","principal":"OldLogin","permission":"ViewServerState"}'
```

### Test Permission State

Test if a permission is in the desired state:

```powershell
dsc resource test -r OpenDsc.SqlServer/ServerPermission --input '{"serverInstance":".","principal":"MonitoringLogin","permission":"ViewServerState","state":"Grant"}'
```

### Export All Server Permissions

List all server permissions:

```powershell
dsc resource export -r OpenDsc.SqlServer/ServerPermission --input '{"serverInstance":"."}'
```

## Exit Codes

The resource returns the following exit codes:

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Invalid argument
- **4** - Unauthorized access
- **5** - Invalid operation
