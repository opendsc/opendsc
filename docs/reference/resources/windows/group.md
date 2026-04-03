---
description: >-
  Reference for the OpenDsc.Windows/Group resource, which manages local Windows groups and their
  members with support for additive and exact member management.
title: "OpenDsc.Windows/Group"
date: 2026-03-27
topic: reference
---

# OpenDsc.Windows/Group

## Synopsis

Manages local Windows groups, including creation, member management, and
removal. Supports both
additive and exact member lists through the `_purge` property.

## Type name

```plaintext
OpenDsc.Windows/Group
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

| Property      | Type     | Required | Access     | Description                                                                              |
| :------------ | :------- | :------- | :--------- | :--------------------------------------------------------------------------------------- |
| `groupName`   | string   | Yes      | Read/Write | The name of the local group.                                                             |
| `description` | string   | No       | Read/Write | A description of the group.                                                              |
| `members`     | string[] | No       | Read/Write | The group members.                                                                       |
| `_purge`      | bool     | No       | Write-only | When `true`, removes members not in the list. When `false` (default), only adds members. |
| `_exist`      | bool     | No       | Read/Write | Whether the group should exist. Defaults to `true`.                                      |

## Member management patterns

### Additive mode (default)

When `_purge` is `false` or omitted, the Set operation only adds the specified
members. Existing
members that aren't in the list are left unchanged.

```json
{ "groupName": "Developers", "members": ["alice", "bob"] }
```

If the group already has members `charlie` and `dave`, after Set the group
contains `alice`,
`bob`, `charlie`, and `dave`.

### Exact mode

When `_purge` is `true`, the Set operation ensures only the specified members
are present. Members
not in the list are removed.

```json
{ "groupName": "Developers", "members": ["alice", "bob"], "_purge": true }
```

After Set, the group contains only `alice` and `bob`.

> [!NOTE]
> This resource requires administrator privileges for all write operations.

## Examples

### Example 1 — Get a group

```powershell
dsc resource get -r OpenDsc.Windows/Group --input '{"groupName":"Administrators"}'
```

### Example 2 — Create a group with members

```powershell
dsc resource set -r OpenDsc.Windows/Group --input '{
  "groupName": "AppOperators",
  "description": "Application operators group",
  "members": ["svc-app", "jane.doe"]
}'
```

### Example 3 — Set exact membership

```powershell
dsc resource set -r OpenDsc.Windows/Group --input '{
  "groupName": "AppOperators",
  "members": ["svc-app"],
  "_purge": true
}'
```

### Example 4 — Delete a group

```powershell
dsc resource delete -r OpenDsc.Windows/Group --input '{"groupName":"AppOperators"}'
```

### Example 5 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Application operators group
    type: OpenDsc.Windows/Group
    properties:
      groupName: AppOperators
      description: Application operators
      members:
        - svc-app
      _purge: true
```

## Exit codes

| Code | Description          |
| :--- | :------------------- |
| 0    | Success              |
| 1    | Error                |
| 2    | Invalid JSON         |
| 3    | Access denied        |
| 4    | Invalid argument     |
| 5    | Unauthorized access  |
| 6    | Group already exists |

## See also

- [`OpenDsc.Windows/User`](user.md)
- [`OpenDsc.Windows/UserRight`](user-right.md)
- [OpenDsc resource reference](../overview.md)
