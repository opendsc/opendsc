# Compress Resource

## Synopsis

Creates ZIP archives from a source directory or file. Supports configurable
compression levels and verifies whether the archive contents match the source.

## Type

```text
OpenDsc.Archive.Zip/Compress
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Test       | Yes       |
| Delete     | No        |
| Export     | No        |

## Properties

### archivePath

Path to the ZIP archive file to create.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### sourcePath

Path to the source directory or file.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### compressionLevel

Compression level to use when creating the archive.

Accepted values are:

- Optimal
- Fastest
- NoCompression

```yaml
Type: string
Required: No
Access: Read/Write
Default value: Optimal
```

### _inDesiredState

Whether the archive contents match the source.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

## Examples

### Example 1 — Create a ZIP archive

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    archivePath: /tmp/backup.zip
    sourcePath: /var/data
    '@

    dsc resource set -r OpenDsc.Archive.Zip/Compress --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    archivePath: /tmp/backup.zip
    sourcePath: /var/data
    EOF
    )

    dsc resource set -r OpenDsc.Archive.Zip/Compress --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 2 — Create with fastest compression

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    archivePath: C:\Backups\logs.zip
    sourcePath: C:\Logs
    compressionLevel: Fastest
    '@

    dsc resource set -r OpenDsc.Archive.Zip/Compress --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    archivePath: C:\Backups\logs.zip
    sourcePath: C:\Logs
    compressionLevel: Fastest
    EOF
    )

    dsc resource set -r OpenDsc.Archive.Zip/Compress --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Archive application logs
    type: OpenDsc.Archive.Zip/Compress
    properties:
      archivePath: /backups/app-logs.zip
      sourcePath: /var/log/myapp
      compressionLevel: Optimal
```

## Exit codes

| Code | Description                |
| :--- | :------------------------- |
| 0    | Success                    |
| 1    | Error                      |
| 2    | Invalid JSON               |
| 3    | Source path not found      |
| 4    | Invalid argument           |
| 5    | IO error                   |
| 6    | Invalid or corrupt archive |
