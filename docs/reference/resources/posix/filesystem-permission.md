---
description: Reference for the OpenDsc.Posix.FileSystem/Permission resource, which manages POSIX file permissions, ownership, and group.
title: "OpenDsc.Posix.FileSystem/Permission"
date: 2026-03-27
topic: reference
---

# OpenDsc.Posix.FileSystem/Permission

## Synopsis

Manages POSIX file and directory permissions (mode, owner, group) on Linux and
macOS.
Equivalent to the `chmod` and `chown` commands.

> [!IMPORTANT]
> This resource is only available on Linux and macOS. It is not supported on Windows.

## Type name

```plaintext
OpenDsc.Posix.FileSystem/Permission
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | No        |
| Export     | No        |

## Properties

| Property | Type   | Required | Access     | Description                                                                                    |
| :------- | :----- | :------- | :--------- | :--------------------------------------------------------------------------------------------- |
| `path`   | string | Yes      | Read/Write | Full path to the file or directory. Must start with `/`.                                       |
| `mode`   | string | No       | Read/Write | File mode in octal notation (e.g., `0644`, `0755`, `644`). Accepts 3 or 4 digit octal strings. |
| `owner`  | string | No       | Read/Write | Owner. Accepts username (e.g., `root`) or numeric UID (e.g., `0`).                             |
| `group`  | string | No       | Read/Write | Group. Accepts group name (e.g., `wheel`) or numeric GID (e.g., `0`).                          |

### Common mode values

| Mode   | Permissions | Typical use                  |
| :----- | :---------- | :--------------------------- |
| `0644` | rw-r--r--   | Regular files                |
| `0755` | rwxr-xr-x   | Executables and directories  |
| `0600` | rw-------   | Private files (keys, certs)  |
| `0700` | rwx------   | Private directories          |
| `0750` | rwxr-x---   | Group-accessible directories |
| `0444` | r--r--r--   | Read-only files              |

## Examples

### Example 1 — Get permissions

```powershell
dsc resource get -r OpenDsc.Posix.FileSystem/Permission --input '{"path":"/etc/passwd"}'
```

### Example 2 — Set file permissions

```powershell
dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input '{
  "path": "/opt/myapp/config.json",
  "mode": "0644",
  "owner": "appuser",
  "group": "appgroup"
}'
```

### Example 3 — Secure a private key

```powershell
dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input '{
  "path": "/etc/ssl/private/server.key",
  "mode": "0600",
  "owner": "root",
  "group": "root"
}'
```

### Example 4 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Application directory permissions
    type: OpenDsc.Posix.FileSystem/Permission
    properties:
      path: /opt/myapp
      mode: "0755"
      owner: appuser
      group: appgroup

  - name: Private key permissions
    type: OpenDsc.Posix.FileSystem/Permission
    properties:
      path: /etc/ssl/private/server.key
      mode: "0600"
      owner: root
      group: root
```

## Exit codes

| Code | Description                 |
| :--- | :-------------------------- |
| 0    | Success                     |
| 1    | Error                       |
| 2    | Invalid JSON                |
| 3    | Access denied               |
| 4    | Invalid argument            |
| 5    | Unauthorized access         |
| 6    | File or directory not found |
| 7    | Directory not found         |
| 8    | Platform not supported      |

## See also

- [OpenDsc resource reference](../overview.md)
- [OpenDsc.FileSystem/File](../filesystem/file.md)
- [OpenDsc.FileSystem/Directory](../filesystem/directory.md)
