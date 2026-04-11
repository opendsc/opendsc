# Permission Resource

## Synopsis

Manages POSIX file and directory permissions (mode, owner, group) on Linux and
macOS. Equivalent to the `chmod` and `chown` commands.

!!! note
    This resource is only available on Linux and macOS. It is not supported on
    Windows.

## Type

```text
OpenDsc.Posix.FileSystem/Permission
```

## Capabilities

- Get
- Set

## Properties

### path

Full path to the file or directory. Must start with `/`.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### mode

File mode in octal notation (e.g., `0644`, `0755`, `644`). Accepts 3 or 4 digit
octal strings.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### owner

Owner. Accepts username (e.g., `root`) or numeric UID (e.g., `0`).

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### group

Group. Accepts group name (e.g., `wheel`) or numeric GID (e.g., `0`).

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

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

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    path: /etc/passwd
    '@

    dsc resource get -r OpenDsc.Posix.FileSystem/Permission --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    path: /etc/passwd
    EOF
    )

    dsc resource get -r OpenDsc.Posix.FileSystem/Permission --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 2 — Set file permissions

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    path: /opt/myapp/config.json
    mode: 0644
    owner: appuser
    group: appgroup
    '@

    dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    path: /opt/myapp/config.json
    mode: 0644
    owner: appuser
    group: appgroup
    EOF
    )

    dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 3 — Secure a private key

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    path: /etc/ssl/private/server.key
    mode: 0600
    owner: root
    group: root
    '@

    dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    path: /etc/ssl/private/server.key
    mode: 0600
    owner: root
    group: root
    EOF
    )

    dsc resource set -r OpenDsc.Posix.FileSystem/Permission --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

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
