# Symbolic Link Resource

## Synopsis

Manages symbolic links on the local filesystem. Works on Windows, Linux, and macOS.

## Type

```text
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

### path

Path where the symbolic link should be created.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### target

Target path that the symbolic link points to.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### type

Link target type. Accepts `File` or `Directory`. Auto-detected if not specified.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### _exist

Whether the link should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

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
