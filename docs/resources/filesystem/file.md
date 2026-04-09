# File Resource

## Synopsis

Manages files on the local filesystem. Supports creating files with specified
content and removing files. Works on Windows, Linux, and macOS.

## Type

```text
OpenDsc.FileSystem/File
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

Path to the file.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### content

Content of the file.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### _exist

Whether the file should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

## Examples

### Example 1 — Get a file

```powershell
dsc resource get -r OpenDsc.FileSystem/File --input '{"path":"/etc/hostname"}'
```

### Example 2 — Create a file with content

```powershell
dsc resource set -r OpenDsc.FileSystem/File --input '{"path":"/tmp/hello.txt","content":"Hello, World!"}'
```

### Example 3 — Delete a file

```powershell
dsc resource delete -r OpenDsc.FileSystem/File --input '{"path":"/tmp/hello.txt"}'
```

### Example 4 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Application config file
    type: OpenDsc.FileSystem/File
    properties:
      path: /opt/myapp/config.json
      content: |
        {
          "logLevel": "Information",
          "port": 8080
        }
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

## See also

- [OpenDsc resource reference](../overview.md)
- [OpenDsc.FileSystem/Directory](directory.md)
- [OpenDsc.FileSystem/SymbolicLink](symbolic-link.md)
