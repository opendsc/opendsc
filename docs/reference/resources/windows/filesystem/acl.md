# Access Control List Resource

## Synopsis

Manages Windows file and directory access control lists (ACLs), including owner,
group, and
access rules. This is a pure list-management resource that uses the `_purge`
pattern for access
rules instead of `_exist`.

## Type

```text
OpenDsc.Windows.FileSystem/AccessControlList
```

## Capabilities

- Get
- Set
- Export

## Properties

### path

Full path to the file or directory.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### owner

Owner. Accepts username, `domain\\user`, or SID.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### group

Primary group. Accepts group name, `domain\\group`, or SID.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### accessRules

Access control entries to apply.

```yaml
Type: AccessRule[]
Required: No
Access: Read/Write
Default value: None
```

### _purge

When `true`, removes access rules not in the list. When `false` (default), only
adds rules.

```yaml
Type: bool
Required: No
Access: Write-Only
Default value: false
```

### AccessRule object

Each element in the `accessRules` array is an object with the following
properties:

### identity

Identity (username, `domain\\user`, or SID).

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### rights

File system rights to grant or deny. Values must be unique.

```yaml
Type: string[]
Required: Yes
Access: Read/Write
Default value: None
```

### inheritanceFlags

Inheritance flags: `ContainerInherit`, `ObjectInherit`, `None`.

```yaml
Type: string[]
Required: No
Access: Read/Write
Default value: None
```

### propagationFlags

Propagation flags: `InheritOnly`, `NoPropagateInherit`, `None`.

```yaml
Type: string[]
Required: No
Access: Read/Write
Default value: None
```

### accessControlType

`Allow` or `Deny`.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### File system rights

| Right                          | Description                             |
| :----------------------------- | :-------------------------------------- |
| `FullControl`                  | Full control over the file or directory |
| `Modify`                       | Read, write, execute, and delete        |
| `ReadAndExecute`               | Read and execute                        |
| `Read`                         | Read data, attributes, and permissions  |
| `Write`                        | Write data and attributes               |
| `ListDirectory`                | List directory contents                 |
| `CreateFiles`                  | Create files                            |
| `CreateDirectories`            | Create subdirectories                   |
| `Delete`                       | Delete the file or directory            |
| `DeleteSubdirectoriesAndFiles` | Delete subdirectories and files         |
| `ReadAttributes`               | Read file attributes                    |
| `WriteAttributes`              | Write file attributes                   |
| `ReadExtendedAttributes`       | Read extended attributes                |
| `WriteExtendedAttributes`      | Write extended attributes               |
| `Traverse`                     | Traverse directory                      |
| `ReadPermissions`              | Read access control entries             |
| `ChangePermissions`            | Change access control entries           |
| `TakeOwnership`                | Take ownership                          |
| `Synchronize`                  | Synchronize access                      |

## Examples

### Example 1 — Get the ACL for a file

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    path: C:\Data\config.xml
    '@

    dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    path: C:\Data\config.xml
    EOF
    )

    dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 2 — Add an access rule (additive)

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    path: C:\Data
    accessRules:
      - identity: BUILTIN\Users
        rights:
          - Read
          - ReadAndExecute
        inheritanceFlags:
          - ContainerInherit
          - ObjectInherit
        propagationFlags:
          - None
        accessControlType: Allow
    '@

    dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    path: C:\Data
    accessRules:
      - identity: BUILTIN\Users
        rights:
          - Read
          - ReadAndExecute
        inheritanceFlags:
          - ContainerInherit
          - ObjectInherit
        propagationFlags:
          - None
        accessControlType: Allow
    EOF
    )

    dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 3 — Set exact ACL (purge mode)

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    path: C:\Data
    owner: BUILTIN\Administrators
    accessRules:
      - identity: BUILTIN\Administrators
        rights:
          - FullControl
        inheritanceFlags:
          - ContainerInherit
          - ObjectInherit
        propagationFlags:
          - None
        accessControlType: Allow
      - identity: BUILTIN\Users
        rights:
          - Read
          - ReadAndExecute
        inheritanceFlags:
          - ContainerInherit
          - ObjectInherit
        propagationFlags:
          - None
        accessControlType: Allow
    _purge: true
    '@

    dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    path: C:\Data
    owner: BUILTIN\Administrators
    accessRules:
      - identity: BUILTIN\Administrators
        rights:
          - FullControl
        inheritanceFlags:
          - ContainerInherit
          - ObjectInherit
        propagationFlags:
          - None
        accessControlType: Allow
      - identity: BUILTIN\Users
        rights:
          - Read
          - ReadAndExecute
        inheritanceFlags:
          - ContainerInherit
          - ObjectInherit
        propagationFlags:
          - None
        accessControlType: Allow
    _purge: true
    EOF
    )

    dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 4 — Export the ACL for a file

!!! note
    `dsc resource export` requires a valid `path` filter for this resource. If
    no filter is provided, the resource will return no results.

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    path: C:\Data\config.xml
    '@

    dsc resource export -r OpenDsc.Windows.FileSystem/AccessControlList --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    path: C:\Data\config.xml
    EOF
    )

    dsc resource export -r OpenDsc.Windows.FileSystem/AccessControlList --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 5 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Secure data folder
    type: OpenDsc.Windows.FileSystem/AccessControlList
    properties:
      path: C:\Data
      owner: BUILTIN\Administrators
      accessRules:
        - identity: BUILTIN\Administrators
          rights:
            - FullControl
          inheritanceFlags:
            - ContainerInherit
            - ObjectInherit
          propagationFlags:
            - None
          accessControlType: Allow
        - identity: BUILTIN\Users
          rights:
            - Read
            - ReadAndExecute
          inheritanceFlags:
            - ContainerInherit
            - ObjectInherit
          propagationFlags:
            - None
          accessControlType: Allow
      _purge: true
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
| 8    | Identity not found          |
