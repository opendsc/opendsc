# OpenDsc.Windows/UserRight

## Synopsis

Manages Windows user rights assignments (privileges) for principals. This is a
pure
list-management resource that uses the `_purge` pattern instead of `_exist`.

## Type name

```text
OpenDsc.Windows/UserRight
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | No        |
| Export     | Yes       |

## Properties

### principal

The principal (user or group). Accepts username, `DOMAIN\\user`, SID (`S-1-5-...`), or UPN (`user@domain.com`).

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### rights

User rights to assign. Values must be unique.

```yaml
Type: string[]
Required: Yes
Access: Read/Write
Default value: None
```

### _purge

When `true`, removes the principal from rights not in the list. When `false` (default), only adds the principal to the specified rights.

```yaml
Type: bool
Required: No
Access: Write-Only
Default value: false
```

### Supported rights

The `rights` property accepts the following Windows privilege constants:

| Logon rights                    | Description                           |
| :------------------------------ | :------------------------------------ |
| `SeNetworkLogonRight`           | Access this computer from the network |
| `SeBatchLogonRight`             | Log on as a batch job                 |
| `SeServiceLogonRight`           | Log on as a service                   |
| `SeInteractiveLogonRight`       | Allow log on locally                  |
| `SeRemoteInteractiveLogonRight` | Allow log on through Remote Desktop   |

| Deny logon rights                   | Description                        |
| :---------------------------------- | :--------------------------------- |
| `SeDenyNetworkLogonRight`           | Deny access from the network       |
| `SeDenyBatchLogonRight`             | Deny log on as a batch job         |
| `SeDenyServiceLogonRight`           | Deny log on as a service           |
| `SeDenyInteractiveLogonRight`       | Deny log on locally                |
| `SeDenyRemoteInteractiveLogonRight` | Deny log on through Remote Desktop |

| Privileges                                  | Description                                      |
| :------------------------------------------ | :----------------------------------------------- |
| `SeBackupPrivilege`                         | Back up files and directories                    |
| `SeRestorePrivilege`                        | Restore files and directories                    |
| `SeShutdownPrivilege`                       | Shut down the system                             |
| `SeDebugPrivilege`                          | Debug programs                                   |
| `SeChangeNotifyPrivilege`                   | Bypass traverse checking                         |
| `SeRemoteShutdownPrivilege`                 | Force shutdown from a remote system              |
| `SeSecurityPrivilege`                       | Manage auditing and security log                 |
| `SeTakeOwnershipPrivilege`                  | Take ownership of files or other objects         |
| `SeLoadDriverPrivilege`                     | Load and unload device drivers                   |
| `SeSystemtimePrivilege`                     | Change the system time                           |
| `SeTimeZonePrivilege`                       | Change the time zone                             |
| `SeCreateSymbolicLinkPrivilege`             | Create symbolic links                            |
| `SeIncreaseBasePriorityPrivilege`           | Increase scheduling priority                     |
| `SeCreatePagefilePrivilege`                 | Create a pagefile                                |
| `SeIncreaseQuotaPrivilege`                  | Adjust memory quotas for a process               |
| `SeSystemProfilePrivilege`                  | Profile system performance                       |
| `SeProfileSingleProcessPrivilege`           | Profile single process                           |
| `SeIncreaseWorkingSetPrivilege`             | Increase a process working set                   |
| `SeAssignPrimaryTokenPrivilege`             | Replace a process-level token                    |
| `SeImpersonatePrivilege`                    | Impersonate a client after authentication        |
| `SeCreateGlobalPrivilege`                   | Create global objects                            |
| `SeAuditPrivilege`                          | Generate security audits                         |
| `SeSystemEnvironmentPrivilege`              | Modify firmware environment values               |
| `SeManageVolumePrivilege`                   | Perform volume maintenance tasks                 |
| `SeLockMemoryPrivilege`                     | Lock pages in memory                             |
| `SeTcbPrivilege`                            | Act as part of the operating system              |
| `SeCreateTokenPrivilege`                    | Create a token object                            |
| `SeCreatePermanentPrivilege`                | Create permanent shared objects                  |
| `SeMachineAccountPrivilege`                 | Add workstations to domain                       |
| `SeTrustedCredManAccessPrivilege`           | Access Credential Manager as a trusted caller    |
| `SeEnableDelegationPrivilege`               | Enable computer and user accounts for delegation |
| `SeSyncAgentPrivilege`                      | Synchronize directory service data               |
| `SeUndockPrivilege`                         | Remove computer from docking station             |
| `SeRelabelPrivilege`                        | Modify an object label                           |
| `SeDelegateSessionUserImpersonatePrivilege` | Impersonate other users during delegation        |

## Examples

### Example 1 — Get rights for a principal

```powershell
dsc resource get -r OpenDsc.Windows/UserRight --input '{"principal":"Administrators","rights":["SeShutdownPrivilege"]}'
```

### Example 2 — Grant rights (additive)

```powershell
dsc resource set -r OpenDsc.Windows/UserRight --input '{"principal":"DOMAIN\\svc-backup","rights":["SeBackupPrivilege","SeRestorePrivilege"]}'
```

### Example 3 — Set exact rights (purge mode)

```powershell
dsc resource set -r OpenDsc.Windows/UserRight --input '{"principal":"DOMAIN\\svc-backup","rights":["SeBackupPrivilege","SeRestorePrivilege"],"_purge":true}'
```

### Example 4 — Export all assignments

```powershell
dsc resource export -r OpenDsc.Windows/UserRight
```

### Example 5 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Backup service rights
    type: OpenDsc.Windows/UserRight
    properties:
      principal: DOMAIN\svc-backup
      rights:
        - SeBackupPrivilege
        - SeRestorePrivilege
      _purge: true
```

## Exit codes

| Code | Description      |
| :--- | :--------------- |
| 0    | Success          |
| 1    | Error            |
| 2    | Invalid JSON     |
| 3    | Access denied    |
| 4    | Invalid argument |

## See also

- [OpenDsc resource reference](../overview.md)
