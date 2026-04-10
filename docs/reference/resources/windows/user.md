# User Resource

## Synopsis

Manages local Windows user accounts, including creation, modification, and
removal.

## Type

```text
OpenDsc.Windows/User
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

### userName

The name of the user account.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### fullName

The full display name of the user.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### description

A description of the user account.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### password

The password for the account.

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

### disabled

Whether the account is disabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

### passwordNeverExpires

Whether the password is set to never expire.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

### userMayNotChangePassword

Whether the user can change their password.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

### _exist

Whether the user should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

!!! note
    This resource requires administrator privileges for all write operations.

## Examples

### Example 1 — Get a user account

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    userName: Administrator
    '@

    dsc resource get -r OpenDsc.Windows/User --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    userName: Administrator
    EOF
    )

    dsc resource get -r OpenDsc.Windows/User --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 2 — Create a user

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    userName: svc-app
    fullName: Application Service Account
    description: Service account for the application
    password: 'SecureP@ssw0rd!'
    passwordNeverExpires: true
    '@

    dsc resource set -r OpenDsc.Windows/User --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    userName: svc-app
    fullName: Application Service Account
    description: Service account for the application
    password: 'SecureP@ssw0rd!'
    passwordNeverExpires: true
    EOF
    )

    dsc resource set -r OpenDsc.Windows/User --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 3 — Delete a user

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    userName: svc-app
    '@

    dsc resource delete -r OpenDsc.Windows/User --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    userName: svc-app
    EOF
    )

    dsc resource delete -r OpenDsc.Windows/User --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 4 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Create service account
    type: OpenDsc.Windows/User
    properties:
      userName: svc-app
      fullName: Application Service Account
      passwordNeverExpires: true

  - name: Disable guest account
    type: OpenDsc.Windows/User
    properties:
      userName: Guest
      disabled: true
```

## Exit codes

| Code | Description         |
| :--- | :------------------ |
| 0    | Success             |
| 1    | Error               |
| 2    | Invalid JSON        |
| 3    | Access denied       |
| 4    | Invalid argument    |
| 5    | Unauthorized access |
| 6    | User already exists |
