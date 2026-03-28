---
description: Reference for the OpenDsc.Windows/Shortcut resource, which manages Windows shortcut (.lnk) files.
title: "OpenDsc.Windows/Shortcut"
date: 2026-03-27
topic: reference
---

# OpenDsc.Windows/Shortcut

## Synopsis

Manages Windows shortcut (.lnk) files using COM interop with the Windows Shell.

## Type name

```plaintext
OpenDsc.Windows/Shortcut
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | No        |

## Properties

| Property           | Type   | Required | Access     | Description                                            |
| :----------------- | :----- | :------- | :--------- | :----------------------------------------------------- |
| `path`             | string | Yes      | Read/Write | The full path to the .lnk file.                        |
| `targetPath`       | string | No       | Read/Write | The target path the shortcut points to.                |
| `arguments`        | string | No       | Read/Write | Command-line arguments for the target.                 |
| `workingDirectory` | string | No       | Read/Write | The working directory for the target.                  |
| `description`      | string | No       | Read/Write | A description for the shortcut.                        |
| `iconLocation`     | string | No       | Read/Write | The icon file path and index.                          |
| `hotkey`           | string | No       | Read/Write | The hotkey combination for the shortcut.               |
| `windowStyle`      | enum   | No       | Read/Write | Window style: `Normal`, `Minimized`, `Maximized`.      |
| `_exist`           | bool   | No       | Read/Write | Whether the shortcut should exist. Defaults to `true`. |

## Examples

### Example 1 — Create a desktop shortcut

```powershell
dsc resource set -r OpenDsc.Windows/Shortcut --input '{
  "path": "C:\\Users\\Public\\Desktop\\Notepad.lnk",
  "targetPath": "C:\\Windows\\notepad.exe",
  "description": "Open Notepad"
}'
```

### Example 2 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Notepad shortcut on desktop
    type: OpenDsc.Windows/Shortcut
    properties:
      path: C:\Users\Public\Desktop\Notepad.lnk
      targetPath: C:\Windows\notepad.exe
      description: Open Notepad
```

## Exit codes

| Code | Description |
| :--- | :---------- |
| 0    | Success     |
| 1    | Error       |

## See also

- [OpenDsc resource reference](../overview.md)
