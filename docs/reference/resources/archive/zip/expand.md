# Expand Resource

## Synopsis

Extracts ZIP archives to a destination directory. Verifies whether the
destination contains all files from the archive with matching checksums.

## Type

```text
OpenDsc.Archive.Zip/Expand
```

## Capabilities

- Get
- Set
- Test

## Properties

### archivePath

Path to the ZIP archive file.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### destinationPath

Destination directory to extract into.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### _inDesiredState

Whether the destination matches the archive contents.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

## Examples

### Example 1 — Extract a ZIP archive

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    archivePath: /tmp/release.zip
    destinationPath: /opt/myapp
    '@

    dsc resource set -r OpenDsc.Archive.Zip/Expand --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    archivePath: /tmp/release.zip
    destinationPath: /opt/myapp
    EOF
    )

    dsc resource set -r OpenDsc.Archive.Zip/Expand --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 2 — Check extraction state

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    archivePath: C:\Packages\app.zip
    destinationPath: C:\Program Files\MyApp
    '@

    dsc resource get -r OpenDsc.Archive.Zip/Expand --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    archivePath: C:\Packages\app.zip
    destinationPath: C:\Program Files\MyApp
    EOF
    )

    dsc resource get -r OpenDsc.Archive.Zip/Expand --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Deploy application package
    type: OpenDsc.Archive.Zip/Expand
    properties:
      archivePath: /packages/myapp-v2.0.zip
      destinationPath: /opt/myapp
```

## Exit codes

| Code | Description                |
| :--- | :------------------------- |
| 0    | Success                    |
| 1    | Error                      |
| 2    | Invalid JSON               |
| 3    | Archive not found          |
| 4    | Invalid argument           |
| 5    | IO error                   |
| 6    | Invalid or corrupt archive |
