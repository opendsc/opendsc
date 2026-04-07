# OpenDsc.FileSystem/Directory

## Synopsis

Manages directories on the local filesystem. Supports creating directories,
copying directory contents from a source, and removing directories. Works on
Windows, Linux, and macOS.

## Type name

```plaintext
OpenDsc.FileSystem/Directory
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Test       | Yes       |
| Delete     | Yes       |
| Export     | No        |

## Properties

### path

Path to the directory.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### sourcePath

Source directory to copy contents from.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### _exist

Whether the directory should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

### _inDesiredState

Whether the directory is in the desired state.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

## Examples

### Example 1 — Get a directory

```powershell
dsc resource get -r OpenDsc.FileSystem/Directory --input '{"path":"/var/log/myapp"}'
```

### Example 2 — Create a directory

```powershell
dsc resource set -r OpenDsc.FileSystem/Directory --input '{"path":"/var/log/myapp"}'
```

### Example 3 — Copy directory contents from source

```powershell
dsc resource set -r OpenDsc.FileSystem/Directory --input '{"path":"/opt/myapp/config","sourcePath":"/opt/myapp/config-template"}'
```

### Example 4 — Delete a directory

```powershell
dsc resource delete -r OpenDsc.FileSystem/Directory --input '{"path":"/tmp/staging"}'
```

### Example 5 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Application log directory
    type: OpenDsc.FileSystem/Directory
    properties:
      path: /var/log/myapp

  - name: Application configuration
    type: OpenDsc.FileSystem/Directory
    properties:
      path: /opt/myapp/config
      sourcePath: /opt/myapp/config-template
```

## Exit codes

| Code | Description      |
| :--- | :--------------- |
| 0    | Success          |
| 1    | Error            |
| 2    | Invalid JSON     |
| 3    | Access denied    |
| 4    | Invalid argument |
| 5    | IO error         |
| 6    | Access denied    |

## See also

- [OpenDsc resource reference](../overview.md)
- [OpenDsc.FileSystem/File](file.md)
- [OpenDsc.FileSystem/SymbolicLink](symbolic-link.md)
