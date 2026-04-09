# Environment Resource

## Synopsis

Manages Windows environment variables at the User or Machine scope.

## Type

```text
OpenDsc.Windows/Environment
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

### name

The name of the environment variable.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### value

The value of the environment variable.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### scope

The scope. Accepts `User` or `Machine`.

```yaml
Type: enum
Required: No
Access: Read/Write
Default value: User
```

### _exist

Whether the variable should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

> [!NOTE]
> Setting `scope` to `Machine` requires administrator privileges.

## Examples

### Example 1 — Get an environment variable

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    name: PATH
    scope: Machine
    '@

    dsc resource get -r OpenDsc.Windows/Environment --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    name: PATH
    scope: Machine
    EOF
    )

    dsc resource get -r OpenDsc.Windows/Environment --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

```yaml
actualState:
  name: PATH
  value: C:\Windows\system32;C:\Windows;...
  scope: Machine
```

### Example 2 — Set an environment variable

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    name: APP_HOME
    value: C:\MyApp
    scope: User
    '@

    dsc resource set -r OpenDsc.Windows/Environment --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    name: APP_HOME
    value: C:\MyApp
    scope: User
    EOF
    )

    dsc resource set -r OpenDsc.Windows/Environment --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 3 — Delete an environment variable

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    name: APP_HOME
    scope: User
    '@

    dsc resource delete -r OpenDsc.Windows/Environment --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    name: APP_HOME
    scope: User
    EOF
    )

    dsc resource delete -r OpenDsc.Windows/Environment --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 4 — Export all environment variables

```powershell
dsc resource export -r OpenDsc.Windows/Environment
```

### Example 5 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Set application home
    type: OpenDsc.Windows/Environment
    properties:
      name: APP_HOME
      value: C:\MyApp
      scope: Machine

  - name: Remove legacy variable
    type: OpenDsc.Windows/Environment
    properties:
      name: LEGACY_VAR
      scope: User
      _exist: false
```

## Exit codes

| Code | Description      |
| :--- | :--------------- |
| 0    | Success          |
| 1    | Error            |
| 2    | Invalid JSON     |
| 3    | Access denied    |
| 4    | Invalid argument |

## See also

- [OpenDsc resource reference](../overview.md)
