# OpenDsc.Windows.FileSystem/AccessControlList

## Synopsis

Manages Windows file and directory permissions (Access Control Lists).

## Description

The `OpenDsc.Windows.FileSystem/AccessControlList` resource enables you to
manage file and directory permissions on Windows systems. It supports reading
and setting owners and groups, managing access control entries (ACEs) with
full control over rights, inheritance, and propagation.

The resource supports both additive and exact (purge) modes for access rules.
When `_purge` is `false` (default), rules are added without removing existing
rules. When `_purge` is `true`, only the specified rules remain, and all
others are removed. Exercise caution with purge mode to avoid locking
yourself out of resources.

Identity values can be specified as username, DOMAIN\username, or SID
(S-1-5-21-...). Inheritance flags only apply to directories. The resource
supports both files and directories.

## Requirements

- Windows operating system
- .NET 10.0 runtime
- **Administrator privileges required** for most operations (changing
  ownership, modifying permissions)

## Capabilities

- **get** - Read current ACL, owner, and group
- **set** - Apply ACL configuration

## Properties

### Required Properties

- **path** (string) - The full path to the file or directory

### Optional Properties

- **owner** (string) - The owner of the file or directory
  (username, DOMAIN\username, or SID)
- **group** (string) - The primary group of the file or directory
  (username, DOMAIN\username, or SID)
- **accessRules** (array) - List of access control entries (ACEs). Each
  entry contains:
  - **identity** (required, string) - The user or group the rule applies to
  - **rights** (required, array) - Array of file system rights:
    `ListDirectory`, `CreateFiles`, `CreateDirectories`,
    `ReadExtendedAttributes`, `WriteExtendedAttributes`, `Traverse`,
    `DeleteSubdirectoriesAndFiles`, `ReadAttributes`, `WriteAttributes`,
    `Write`, `Delete`, `ReadPermissions`, `Read`, `ReadAndExecute`, `Modify`,
    `ChangePermissions`, `TakeOwnership`, `Synchronize`, `FullControl`
  - **inheritanceFlags** (array) - How the rule is inherited: `None`,
    `ContainerInherit`, `ObjectInherit`
  - **propagationFlags** (array) - How inheritance is propagated: `None`,
    `NoPropagateInherit`, `InheritOnly`
  - **accessControlType** (required, enum) - `Allow` or `Deny`
- **_purge** (boolean) - When `true`, removes access rules not in the
  `accessRules` list. When `false`, only adds rules without removing others.
  Default: `false`

## Examples

### Read current ACL

```powershell
$config = @'
path: C:\MyFolder
'@

dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList -i $config
```

### Set owner and group

```powershell
$config = @'
path: C:\MyFolder
owner: BUILTIN\Administrators
group: BUILTIN\Users
'@

dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList -i $config
```

### Add access rule (additive mode)

```powershell
$config = @'
path: C:\MyFolder
accessRules:
  - identity: BUILTIN\Users
    rights:
      - Read
    inheritanceFlags:
      - ContainerInherit
    propagationFlags:
      - None
    accessControlType: Allow
_purge: false
'@

dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList -i $config
```

### Set exact permissions (purge mode)

```powershell
$config = @'
path: C:\MyFolder
accessRules:
  - identity: BUILTIN\Administrators
    rights:
      - FullControl
    inheritanceFlags:
      - ContainerInherit
    propagationFlags:
      - None
    accessControlType: Allow
  - identity: BUILTIN\Users
    rights:
      - Read
    inheritanceFlags:
      - ContainerInherit
    propagationFlags:
      - None
    accessControlType: Allow
_purge: true
'@

dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList -i $config
```

### Deny access

```powershell
$config = @'
path: C:\MyFolder
accessRules:
  - identity: DOMAIN\RestrictedUser
    rights:
      - Write
    accessControlType: Deny
'@

dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList -i $config
```

### Combine multiple rights

```powershell
$config = @'
path: C:\MyFolder
accessRules:
  - identity: BUILTIN\Users
    rights:
      - Read
      - Write
      - Delete
    inheritanceFlags:
      - ContainerInherit
      - ObjectInherit
    propagationFlags:
      - None
    accessControlType: Allow
'@

dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList -i $config
```

## Exit Codes

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Access denied
- **4** - Invalid argument
- **5** - Unauthorized access
- **6** - File or directory not found
- **7** - Directory not found
- **8** - Identity not found
