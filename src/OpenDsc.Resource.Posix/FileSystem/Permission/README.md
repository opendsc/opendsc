# OpenDsc.Posix.FileSystem/Permission

## Description

The `OpenDsc.Posix.FileSystem/Permission` resource manages POSIX file and
directory permissions (mode, owner, group) on Linux and macOS systems. This
resource allows you to configure file permissions using octal notation and set
ownership using usernames or numeric IDs.

## Requirements

- **Platform**: Linux or macOS
- **Permissions**:
  - Read operations: No special permissions required
  - Mode changes: Write permissions on the target file/directory
  - Owner/Group changes: Requires root/administrator privileges

## Properties

### Required Properties

- **path** (string) - The full path to the file or directory. Must be an
  absolute path starting with `/`.

### Optional Properties

- **mode** (string) - The file mode in octal notation (e.g., `'0644'`,
  `'0755'`, `'644'`). Accepts 3 or 4 digit octal strings with optional leading
  zero.
- **owner** (string) - The owner of the file or directory. Can be specified as
  a username (e.g., `'root'`, `'john'`) or numeric UID (e.g., `'0'`, `'1000'`).
- **group** (string) - The group of the file or directory. Can be specified as
  a group name (e.g., `'wheel'`, `'staff'`, `'developers'`) or numeric GID
  (e.g., `'0'`, `'1000'`).

## Capabilities

- **Get**: Read current permissions, owner, and group of a file or directory
- **Set**: Apply desired permissions, owner, and group
- **Delete**: Not supported (permissions cannot be "deleted", only changed)
- **Export**: Not supported

## Examples

### Get Current Permissions

```powershell
$config = @'
path: "/etc/hosts"
'@

dsc resource get -r OpenDsc.Posix.FileSystem/Permission -i $config
```

**Output:**

```yaml
actualState:
  path: "/etc/hosts"
  mode: "0644"
  owner: "root"
  group: "root"
```

### Set File Mode (Permissions)

```powershell
$config = @'
path: "/home/user/script.sh"
mode: "0755"
'@

dsc resource set -r OpenDsc.Posix.FileSystem/Permission -i $config
```

```powershell
$config = @'
path: "/home/user/.ssh/id_rsa"
mode: "0600"
'@

dsc resource set -r OpenDsc.Posix.FileSystem/Permission -i $config
```

### Change File Owner

**Note:** Changing ownership requires root privileges.

```powershell
$config = @'
path: "/var/www/html/index.html"
owner: "www-data"
'@

dsc resource set -r OpenDsc.Posix.FileSystem/Permission -i $config
```

```powershell
$config = @'
path: "/opt/app/config"
owner: "1000"
'@

dsc resource set -r OpenDsc.Posix.FileSystem/Permission -i $config
```

### Change File Group

**Note:** Changing group requires root privileges.

```powershell
$config = @'
path: "/var/log/app.log"
group: "syslog"
'@

dsc resource set -r OpenDsc.Posix.FileSystem/Permission -i $config
```

```powershell
$config = @'
path: "/Users/Shared/Documents"
group: "staff"
'@

dsc resource set -r OpenDsc.Posix.FileSystem/Permission -i $config
```

### Set Multiple Properties

```powershell
$config = @'
path: "/opt/app/app.conf"
mode: "0640"
owner: "nginx"
group: "nginx"
'@

dsc resource set -r OpenDsc.Posix.FileSystem/Permission -i $config
```

### Using in DSC Configuration

```yaml
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources:
  - name: Secure SSH Key
    type: OpenDsc.Posix.FileSystem/Permission
    properties:
      path: /home/user/.ssh/id_rsa
      mode: '0600'
      owner: user
      group: user

  - name: Application Directory
    type: OpenDsc.Posix.FileSystem/Permission
    properties:
      path: /opt/webapp
      mode: '0755'
      owner: www-data
      group: www-data

  - name: Log File
    type: OpenDsc.Posix.FileSystem/Permission
    properties:
      path: /var/log/app.log
      mode: '0644'
      owner: root
      group: syslog
```

## Common Permission Modes

| Mode | Octal | Description |
| --- | --- | --- |
| `rwxrwxrwx` | `0777` | All permissions for everyone (generally unsafe) |
| `rwxr-xr-x` | `0755` | Owner can modify, others can read/execute (common for executables) |
| `rwxr-x---` | `0750` | Owner can modify, group can read/execute, no access for others |
| `rw-rw-rw-` | `0666` | Read/write for everyone (generally unsafe) |
| `rw-r--r--` | `0644` | Owner can modify, others can only read (common for data files) |
| `rw-r-----` | `0640` | Owner can modify, group can read, no access for others |
| `rw-------` | `0600` | Only owner can read/write (common for private files like SSH keys) |
| `r--------` | `0400` | Owner can only read (immutable for owner, no access for others) |

## Behavior Notes

1. **Mode Format**: The resource accepts octal mode with or without the leading
   `0` (e.g., both `'0644'` and `'644'` are valid).

2. **Username vs UID**: You can specify owners and groups by name or numeric ID.
   Numeric IDs are resolved during the set operation.

3. **Partial Updates**: You can update mode, owner, or group independently.
   Omitted properties are not changed.

4. **Error Handling**:
   - Throws `FileNotFoundException` if the specified path does not exist
   - Throws `ArgumentException` if a specified username or group name is not
     found
   - Throws `UnauthorizedAccessException` if you lack privileges to change
     ownership (requires root)
   - Throws `PlatformNotSupportedException` if run on Windows

5. **Platform Differences**:
   - Default groups differ between Linux and macOS (e.g., `root` vs `wheel`)
   - User databases may differ between systems
   - Always test configurations on the target platform

## Exit Codes

| Code | Description |
| --- | --- |
| 0 | Success |
| 1 | General error |
| 2 | Invalid JSON |
| 3 | Access denied (security) |
| 4 | Invalid argument |
| 5 | Unauthorized access |
| 6 | File or directory not found |
| 7 | Directory not found |
| 8 | Platform not supported (e.g., running on Windows) |

## See Also

- [chmod command](https://man7.org/linux/man-pages/man1/chmod.1.html)
- [chown command](https://man7.org/linux/man-pages/man1/chown.1.html)
- [POSIX File Permissions][posix-file-permissions]

[posix-file-permissions]: https://en.wikipedia.org/wiki/File-system_permissions#Notation_of_traditional_Unix_permissions
