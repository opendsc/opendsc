# OpenDsc.Windows/Shortcut

## Synopsis

Manages Windows shortcut (.lnk) files using COM interop with the Windows Shell.

## Type name

```text
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

### path

The full path to the .lnk file.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### targetPath

The target path the shortcut points to.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### arguments

Command-line arguments for the target.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### workingDirectory

The working directory for the target.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### description

A description for the shortcut.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### iconLocation

The icon file path and index.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### hotkey

The hotkey combination for the shortcut.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### windowStyle

Window style. Accepts `Normal`, `Minimized`, or `Maximized`.

```yaml
Type: enum
Required: No
Access: Read/Write
Default value: None
```

### _exist

Whether the shortcut should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

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

| Code | Description                |
| :--- | :------------------------- |
| 0    | Success                    |
| 1    | Error                      |
| 2    | Invalid JSON               |
| 3    | Failed to generate schema  |
| 4    | Directory not found        |

## See also

- [OpenDsc resource reference](../overview.md)
