# Windows SMB Share

## Synopsis

Creates, updates, and manages Windows SMB file shares with optional permission
control.

## Type

```text
OpenDsc.Windows.FileSystem/Share
```

## Capabilities

- Get
- Set
- Delete
- Export

## Properties

### name

The name of the SMB share. Must be between 1 and 80 characters and contain no
invalid characters.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### path

The local file system path to share.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### description

Optional description or comment for the share.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### permissions

Array of share permission entries. Each entry specifies a principal (user or
group) and their access level.

```yaml
Type: object[]
Required: No
Access: Read/Write
Default value: None
```

#### Permission object properties

- **principal** (string, required) — User or group name in format:
  `DOMAIN\name`, `BUILTIN\name`, or `.\name` for local principals
- **access** (enum, required) — Access level: `None`, `Read`, `Change`, or
  `Full`

### _purge

When `true`, removes permissions not included in the `permissions` array. When
`false`, only manages listed permissions.

```yaml
Type: bool
Required: No
Access: Write-Only
Default value: false
```

!!! warning
    The `_purge` property is write-only and only applied during Set
    operations. Use with caution as it will remove all unlisted permissions.

### _exist

Indicates whether the share should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

!!! note
    Administrator privileges are required to create, modify, or delete SMB
    shares on this system.

## Examples

### Example 1 — Get an existing share

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    name: Documents
    '@

    dsc resource get -r OpenDsc.Windows.FileSystem/Share --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    name: Documents
    EOF
    )

    dsc resource get -r OpenDsc.Windows.FileSystem/Share --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

```yaml
actualState:
  name: Documents
  path: C:\Users\Public\Documents
  description: Public documents share
  permissions:
    - principal: BUILTIN\Users
      access: Read
```

### Example 2 — Create a new share

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    name: Data
    path: C:\Data
    description: Shared data directory
    '@

    dsc resource set -r OpenDsc.Windows.FileSystem/Share --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    name: Data
    path: C:\Data
    description: Shared data directory
    EOF
    )

    dsc resource set -r OpenDsc.Windows.FileSystem/Share --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 3 — Create a share with permissions

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    name: Department
    path: C:\Shares\Department
    description: Department shared folder
    permissions:
      - principal: BUILTIN\Administrators
        access: Full
      - principal: CONTOSO\DeptGroup
        access: Change
      - principal: BUILTIN\Users
        access: Read
    '@

    dsc resource set -r OpenDsc.Windows.FileSystem/Share --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    name: Department
    path: C:\Shares\Department
    description: Department shared folder
    permissions:
      - principal: BUILTIN\Administrators
        access: Full
      - principal: CONTOSO\DeptGroup
        access: Change
      - principal: BUILTIN\Users
        access: Read
    EOF
    )

    dsc resource set -r OpenDsc.Windows.FileSystem/Share --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 4 — Update share permissions with purge

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    name: Department
    path: C:\Shares\Department
    permissions:
      - principal: BUILTIN\Administrators
        access: Full
      - principal: CONTOSO\DeptGroup
        access: Change
    _purge: true
    '@

    dsc resource set -r OpenDsc.Windows.FileSystem/Share --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    name: Department
    path: C:\Shares\Department
    permissions:
      - principal: BUILTIN\Administrators
        access: Full
      - principal: CONTOSO\DeptGroup
        access: Change
    _purge: true
    EOF
    )

    dsc resource set -r OpenDsc.Windows.FileSystem/Share --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 5 — Delete a share

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    name: OldShare
    path: C:\Shares\OldShare
    _exist: false
    '@

    dsc resource set -r OpenDsc.Windows.FileSystem/Share --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    name: OldShare
    path: C:\Shares\OldShare
    _exist: false
    EOF
    )

    dsc resource set -r OpenDsc.Windows.FileSystem/Share --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 6 — Configuration document with multiple shares

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Create Documents share
    type: OpenDsc.Windows.FileSystem/Share
    properties:
      name: Documents
      path: C:\Shares\Documents
      description: Company documents
      permissions:
        - principal: BUILTIN\Administrators
          access: Full
        - principal: CONTOSO\Managers
          access: Change
        - principal: BUILTIN\Users
          access: Read

  - name: Create Data share
    type: OpenDsc.Windows.FileSystem/Share
    properties:
      name: Data
      path: C:\Shares\Data
      description: Shared data directory
      permissions:
        - principal: BUILTIN\Administrators
          access: Full
        - principal: CONTOSO\DataTeam
          access: Change
```

## Access Levels

The following access levels are supported for share permissions:

| Level | Description |
| :--- | :--- |
| `None` | No access; used to explicitly remove permissions |
| `Read` | Read-only access |
| `Change` | Read and write access |
| `Full` | Full administrative access |

## Exit codes

| Code | Description |
| :--- | :--- |
| 0 | Success |
| 1 | General error |
| 2 | Invalid JSON |
| 3 | Share operation failed |
| 4 | Invalid argument or missing required parameter |
| 5 | Access denied (administrator privileges required) |
