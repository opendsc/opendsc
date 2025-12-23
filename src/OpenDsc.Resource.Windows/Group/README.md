# OpenDsc.Windows/Group

## Synopsis

Manage local Windows groups and group membership.

## Description

The `OpenDsc.Windows/Group` resource enables you to manage local Windows
groups including creating, updating, and deleting groups, as well as managing
group membership. The resource supports both additive and exact membership
modes using the `_purge` canonical property.

Members can be specified using usernames, domain\username format, or SIDs.

## Requirements

- Windows operating system
- Administrator privileges for set, delete, and some get operations

## Capabilities

The resource has the following capabilities:

- `get` - Retrieve the current state of a local Windows group
- `set` - Create or update a local Windows group
- `delete` - Remove a local Windows group
- `export` - List all local Windows groups

## Properties

### Required Properties

- **groupName** (string) - The name of the local group

### Optional Properties

- **description** (string) - A description of the group
- **members** (string[]) - List of group members (username, DOMAIN\username,
  or SID format)
- **_purge** (boolean) - When `true`, removes members not in the members list
  (exact mode). When `false`, only adds members without removing others
  (additive mode). Default: `false`
- **_exist** (boolean) - Whether the group should exist. Default: `true`

### Member Management Modes

- **Additive mode** (`_purge: false`, default) - Adds specified members to the
  group without removing existing members
- **Exact mode** (`_purge: true`) - Ensures only the specified members are
  present; removes any members not in the list

## Examples

### Create a new group

```powershell
$config = @'
groupName: Developers
description: Development team members
'@

dsc resource set -r OpenDsc.Windows/Group -i $config
```

### Create a group with exact membership

```powershell
$config = @'
groupName: ProjectAdmins
description: Project administrators
members:
  - john
  - jane
  - bob
_purge: true
'@

dsc resource set -r OpenDsc.Windows/Group -i $config
```

### Add members without removing others

```powershell
$config = @'
groupName: Users
members:
  - john
  - jane
_purge: false
'@

dsc resource set -r OpenDsc.Windows/Group -i $config
```

### Get group information

```powershell
$config = @'
groupName: Administrators
'@

dsc resource get -r OpenDsc.Windows/Group -i $config
```

### Delete a group

```powershell
$config = @'
groupName: OldGroup
'@

dsc resource delete -r OpenDsc.Windows/Group -i $config
```

### Export all groups

```powershell
dsc resource export -r OpenDsc.Windows/Group
```

## Exit Codes

The resource returns the following exit codes:

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Access denied
- **4** - Invalid argument
- **5** - Unauthorized access
- **6** - Group already exists
