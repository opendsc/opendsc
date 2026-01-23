# OpenDsc.Windows/UserRight

## Synopsis

Manage Windows user rights assignments (also known as privileges).

## Description

The `OpenDsc.Windows/UserRight` resource enables you to grant or revoke
Windows user rights (privileges) to user accounts and groups using Microsoft
DSC. User rights determine what system-level operations a user or group can
perform, such as logging on as a service, backing up files, or shutting down
the system.

User rights are managed through the Local Security Authority (LSA) and are
distinct from file permissions. Common examples include `SeBackupPrivilege`,
`SeServiceLogonRight`, and `SeDebugPrivilege`.

**Important**: All operations require administrator privileges.

## Requirements

- Windows operating system
- Administrator privileges required for all operations

## Capabilities

The resource has the following capabilities:

- `get` - Check if a principal has specified user rights
- `set` - Grant user rights to a principal
- `delete` - Revoke user rights from a principal
- `export` - List all user rights assignments grouped by principal

## Properties

### Required Properties

- **rights** (UserRight[]) - Array of user rights to manage. Must contain at
  least one right. Valid values include:
  - Logon rights: `SeNetworkLogonRight`, `SeInteractiveLogonRight`,
    `SeServiceLogonRight`, `SeBatchLogonRight`, `SeRemoteInteractiveLogonRight`
  - Deny logon rights: `SeDenyNetworkLogonRight`, `SeDenyInteractiveLogonRight`,
    `SeDenyServiceLogonRight`, `SeDenyBatchLogonRight`,
    `SeDenyRemoteInteractiveLogonRight`
  - Privileges: `SeBackupPrivilege`, `SeRestorePrivilege`, `SeDebugPrivilege`,
    `SeShutdownPrivilege`, `SeSystemtimePrivilege`, `SeTakeOwnershipPrivilege`,
    `SeChangeNotifyPrivilege`, `SeCreateSymbolicLinkPrivilege`, and many more
    (see full list in schema)
- **principal** (string) - The user or group to which rights apply. Can be
  specified as:
  - Local username: `Administrator`
  - Domain account: `DOMAIN\username`
  - Security Identifier (SID): `S-1-5-32-544`
  - User Principal Name (UPN): `user@domain.com`

### Optional Properties

- **_purge** (boolean) - Controls whether other principals should be removed
  from the specified rights. Default: `false`.
  - `false` (additive mode): Grants rights to the principal without affecting
    other principals who also have those rights
  - `true` (exclusive mode): Ensures only the specified principal has the
    rights, removing all other principals
- **_exist** (boolean) - Indicates whether the user right assignment should
  exist. Default: `true`.

## Examples

### Get User Rights for a Principal

Check if the Administrators group has backup privilege:

```yaml
# get-backup-right.dsc.yaml
$schema: https://aka.ms/dsc/schemas/v3/config/document.json
resources:
  - name: Check Backup Right
    type: OpenDsc.Windows/UserRight
    properties:
      rights:
        - SeBackupPrivilege
      principal: Administrators
```

```powershell
dsc config get --file get-backup-right.dsc.yaml
```

### Grant Service Logon Right

Grant logon as a service right to a service account:

```yaml
# grant-service-logon.dsc.yaml
$schema: https://aka.ms/dsc/schemas/v3/config/document.json
resources:
  - name: Service Account Logon Right
    type: OpenDsc.Windows/UserRight
    properties:
      rights:
        - SeServiceLogonRight
      principal: NT SERVICE\MyServiceAccount
```

```powershell
dsc config set --file grant-service-logon.dsc.yaml
```

**Note**: Changes to logon rights may require restarting affected services.

### Grant Multiple Rights to a User

Grant backup and restore privileges to a backup operator:

```yaml
# grant-backup-rights.dsc.yaml
$schema: https://aka.ms/dsc/schemas/v3/config/document.json
resources:
  - name: Backup Operator Rights
    type: OpenDsc.Windows/UserRight
    properties:
      rights:
        - SeBackupPrivilege
        - SeRestorePrivilege
      principal: DOMAIN\BackupOperator
```

```powershell
dsc config set --file grant-backup-rights.dsc.yaml
```

### Exclusive Rights Assignment (Purge Mode)

Ensure only the specified principal has a right, removing all others:

```yaml
# exclusive-debug-right.dsc.yaml
$schema: https://aka.ms/dsc/schemas/v3/config/document.json
resources:
  - name: Exclusive Debug Right
    type: OpenDsc.Windows/UserRight
    properties:
      rights:
        - SeDebugPrivilege
      principal: Administrators
      _purge: true
```

```powershell
dsc config set --file exclusive-debug-right.dsc.yaml
```

This removes `SeDebugPrivilege` from all other principals, granting it
exclusively to the Administrators group.

### Revoke User Rights

Remove user rights from a principal:

```yaml
# revoke-batch-logon.dsc.yaml
$schema: https://aka.ms/dsc/schemas/v3/config/document.json
resources:
  - name: Revoke Batch Logon
    type: OpenDsc.Windows/UserRight
    properties:
      rights:
        - SeBatchLogonRight
      principal: TestUser
      _exist: false
```

```powershell
dsc config set --file revoke-batch-logon.dsc.yaml
```

Alternatively, use the delete operation:

```powershell
dsc resource delete -r OpenDsc.Windows/UserRight --input '{
  "rights": ["SeBatchLogonRight"],
  "principal": "TestUser"
}'
```

### Export All User Rights Assignments

List all user rights assignments grouped by principal:

```powershell
dsc resource export -r OpenDsc.Windows/UserRight
```

Example output:

```yaml
resources:
  - type: OpenDsc.Windows/UserRight
    properties:
      principal: BUILTIN\Administrators
      rights:
        - SeBackupPrivilege
        - SeRestorePrivilege
        - SeDebugPrivilege
        - SeTakeOwnershipPrivilege
  - type: OpenDsc.Windows/UserRight
    properties:
      principal: NT SERVICE\TrustedInstaller
      rights:
        - SeBackupPrivilege
        - SeRestorePrivilege
```

## User Rights Reference

### Logon Rights

- **SeNetworkLogonRight** - Access this computer from the network
- **SeInteractiveLogonRight** - Allow log on locally
- **SeServiceLogonRight** - Log on as a service
- **SeBatchLogonRight** - Log on as a batch job
- **SeRemoteInteractiveLogonRight** - Allow log on through Remote Desktop
  Services

### Deny Logon Rights

- **SeDenyNetworkLogonRight** - Deny access to this computer from the network
- **SeDenyInteractiveLogonRight** - Deny log on locally
- **SeDenyServiceLogonRight** - Deny log on as a service
- **SeDenyBatchLogonRight** - Deny log on as a batch job
- **SeDenyRemoteInteractiveLogonRight** - Deny log on through Remote Desktop
  Services

### Privileges

- **SeAssignPrimaryTokenPrivilege** - Replace a process level token
- **SeAuditPrivilege** - Generate security audits
- **SeBackupPrivilege** - Back up files and directories
- **SeChangeNotifyPrivilege** - Bypass traverse checking
- **SeCreateGlobalPrivilege** - Create global objects
- **SeCreatePagefilePrivilege** - Create a pagefile
- **SeCreatePermanentPrivilege** - Create permanent shared objects
- **SeCreateSymbolicLinkPrivilege** - Create symbolic links
- **SeCreateTokenPrivilege** - Create a token object
- **SeDebugPrivilege** - Debug programs
- **SeDelegateSessionUserImpersonatePrivilege** - Obtain an impersonation token
  for another user in the same session
- **SeEnableDelegationPrivilege** - Enable computer and user accounts to be
  trusted for delegation
- **SeImpersonatePrivilege** - Impersonate a client after authentication
- **SeIncreaseBasePriorityPrivilege** - Increase scheduling priority
- **SeIncreaseQuotaPrivilege** - Adjust memory quotas for a process
- **SeIncreaseWorkingSetPrivilege** - Increase a process working set
- **SeLoadDriverPrivilege** - Load and unload device drivers
- **SeLockMemoryPrivilege** - Lock pages in memory
- **SeMachineAccountPrivilege** - Add workstations to domain
- **SeManageVolumePrivilege** - Perform volume maintenance tasks
- **SeProfileSingleProcessPrivilege** - Profile single process
- **SeRelabelPrivilege** - Modify an object label
- **SeRemoteShutdownPrivilege** - Force shutdown from a remote system
- **SeRestorePrivilege** - Restore files and directories
- **SeSecurityPrivilege** - Manage auditing and security log
- **SeShutdownPrivilege** - Shut down the system
- **SeSyncAgentPrivilege** - Synchronize directory service data
- **SeSystemEnvironmentPrivilege** - Modify firmware environment values
- **SeSystemProfilePrivilege** - Profile system performance
- **SeSystemtimePrivilege** - Change the system time
- **SeTakeOwnershipPrivilege** - Take ownership of files or other objects
- **SeTcbPrivilege** - Act as part of the operating system
- **SeTimeZonePrivilege** - Change the time zone
- **SeTrustedCredManAccessPrivilege** - Access Credential Manager as a trusted
  caller
- **SeUndockPrivilege** - Remove computer from docking station

## Restart Requirements

Granting or revoking certain logon rights may require restarting affected
services or logon sessions. The resource returns metadata indicating when
restarts are recommended:

- **SeServiceLogonRight** - May require restarting services
- **SeBatchLogonRight** - May require restarting Task Scheduler
- **SeInteractiveLogonRight** - May require users to log off and log on again

## Exit Codes

The resource returns the following exit codes:

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Access denied
- **4** - Invalid argument

## Notes

- All operations require administrator privileges
- User rights are system-wide and affect all logon sessions
- Changes to logon rights typically take effect at the next logon
- Use `_purge: true` carefully as it removes rights from all other principals
- The resource uses Security Identifiers (SIDs) internally for reliable
  principal matching, but returns friendly names for readability
