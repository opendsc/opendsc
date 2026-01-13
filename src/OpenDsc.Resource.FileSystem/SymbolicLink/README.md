# SymbolicLink Resource

Manages symbolic links (symlinks) across Windows, Linux, and macOS platforms.

## Properties

- `path` (required): The path where the symbolic link should be created
- `target` (required): The target path that the symbolic link points to.
  The target must exist.
- `type` (optional): The type of the symbolic link (File or Directory).
  If not specified, it will be auto-detected from the target path.
- `_exist` (optional): Whether the symlink should exist (default: `true`)

## Examples

### Create a file symlink

```json
{
  "path": "/path/to/link",
  "target": "/path/to/existing/file.txt"
}
```

### Create a directory symlink

```json
{
  "path": "/path/to/link",
  "target": "/path/to/existing/directory"
}
```

### Remove a symlink

```json
{
  "path": "/path/to/link",
  "target": "/any/target",
  "_exist": false
}
```

## Platform Notes

- **Windows**: Requires administrator privileges or Developer Mode enabled
- **Linux/macOS**: Requires appropriate filesystem permissions
- The resource automatically detects whether the target is a file or directory
  by checking what exists at the target path
- If target does not exist type is required
