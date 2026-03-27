---
description: Reference for the OpenDsc.FileSystem/SymbolicLink resource, which manages symbolic links across platforms.
title: "OpenDsc.FileSystem/SymbolicLink"
date: 2026-03-27
topic: reference
---

# OpenDsc.FileSystem/SymbolicLink

## Synopsis

Manages symbolic links on the local filesystem. Works on Windows, Linux, and
macOS.

## Type name

```plaintext
OpenDsc.FileSystem/SymbolicLink
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | No        |

## Properties

| Property | Type   | Required | Access     | Description                                                              |
| :------- | :----- | :------- | :--------- | :----------------------------------------------------------------------- |
| `path`   | string | Yes      | Read/Write | Path where the symbolic link should be created.                          |
| `target` | string | Yes      | Read/Write | Target path that the symbolic link points to.                            |
| `type`   | string | No       | Read/Write | Link target type: `File` or `Directory`. Auto-detected if not specified. |
| `_exist` | bool   | No       | Read/Write | Whether the link should exist. Defaults to `true`.                       |

## Examples

### Example 1 — Get a symbolic link

```powershell
dsc resource get -r OpenDsc.FileSystem/SymbolicLink --input '{"path":"/usr/local/bin/myapp","target":"/opt/myapp/bin/myapp"}'
```

### Example 2 — Create a symbolic link

```powershell
dsc resource set -r OpenDsc.FileSystem/SymbolicLink --input '{"path":"/usr/local/bin/myapp","target":"/opt/myapp/bin/myapp"}'
```

### Example 3 — Create a directory symbolic link (Windows)

```powershell
dsc resource set -r OpenDsc.FileSystem/SymbolicLink --input '{"path":"C:\\Links\\Logs","target":"D:\\AppLogs","type":"Directory"}'
```

### Example 4 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Application symlink
    type: OpenDsc.FileSystem/SymbolicLink
    properties:
      path: /usr/local/bin/myapp
      target: /opt/myapp/bin/myapp

  - name: Log directory link
    type: OpenDsc.FileSystem/SymbolicLink
    properties:
      path: /var/log/myapp
      target: /mnt/storage/logs/myapp
      type: Directory
```

## Exit codes

| Code | Description             |
| :--- | :---------------------- |
| 0    | Success                 |
| 1    | Error                   |
| 2    | Invalid JSON            |
| 3    | Access denied           |
| 4    | Invalid argument        |
| 5    | IO error                |
| 6    | Insufficient privileges |

## See also

- [OpenDsc resource reference](../overview.md)
- [OpenDsc.FileSystem/File](file.md)
- [OpenDsc.FileSystem/Directory](directory.md)
