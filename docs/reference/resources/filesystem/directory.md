# Directory Resource

## Synopsis

Manages directories on the local filesystem. Supports creating directories,
copying directory contents from a source, and removing directories. Works on
Windows, Linux, and macOS.

## Type

```text
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

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    path: /var/log/myapp
    '@

    dsc resource get -r OpenDsc.FileSystem/Directory --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    path: /var/log/myapp
    EOF
    )

    dsc resource get -r OpenDsc.FileSystem/Directory --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 2 — Create a directory

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    path: /var/log/myapp
    '@

    dsc resource set -r OpenDsc.FileSystem/Directory --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    path: /var/log/myapp
    EOF
    )

    dsc resource set -r OpenDsc.FileSystem/Directory --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 3 — Copy directory contents from source

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    path: /opt/myapp/config
    sourcePath: /opt/myapp/config-template
    '@

    dsc resource set -r OpenDsc.FileSystem/Directory --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    path: /opt/myapp/config
    sourcePath: /opt/myapp/config-template
    EOF
    )

    dsc resource set -r OpenDsc.FileSystem/Directory --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 4 — Delete a directory

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    path: /tmp/staging
    '@

    dsc resource delete -r OpenDsc.FileSystem/Directory --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    path: /tmp/staging
    EOF
    )

    dsc resource delete -r OpenDsc.FileSystem/Directory --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

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
