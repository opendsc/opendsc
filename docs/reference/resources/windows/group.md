# OpenDsc.Windows/Group

## Synopsis

Manages local Windows groups, including creation, member management, and
removal. Supports both additive and exact member lists through the `_purge`
property.

## Type name

```text
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

### groupName

The name of the local group.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### description

A description of the group.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### members

The group members.

```yaml
Type: string[]
Required: No
Access: Read/Write
Default value: None
```

### _purge

When `true`, removes members not in the list. When `false` (default), only adds
members.

```yaml
Type: bool
Required: No
Access: Write-Only
Default value: false
```

### _exist

Whether the group should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

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
