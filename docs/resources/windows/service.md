# Service Resource

## Synopsis

Manages Windows services, including start type, status, and service configuration.

## Type

```text
OpenDsc.Windows/Service
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

The service name (not display name).

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### displayName

The display name of the service.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

### description

The service description.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

### path

The path to the service executable.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### dependencies

Service dependencies.

```yaml
Type: string[]
Required: No
Access: Read/Write
Default value: None
```

### status

Desired status. Accepts `Running`, `Stopped`, or `Paused`.

```yaml
Type: enum
Required: No
Access: Read/Write
Default value: None
```

### startType

Start type. Accepts `Automatic`, `Manual`, or `Disabled`.

```yaml
Type: enum
Required: No
Access: Read/Write
Default value: None
```

### _exist

Whether the service should exist.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

!!! note
    This resource requires administrator privileges.

## Examples

### Example 1 — Ensure a service is running

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    name: Spooler
    status: Running
    startType: Automatic
    '@

    dsc resource set -r OpenDsc.Windows/Service --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    name: Spooler
    status: Running
    startType: Automatic
    EOF
    )

    dsc resource set -r OpenDsc.Windows/Service --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 2 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Ensure Print Spooler is running
    type: OpenDsc.Windows/Service
    properties:
      name: Spooler
      status: Running
      startType: Automatic

  - name: Disable Remote Registry
    type: OpenDsc.Windows/Service
    properties:
      name: RemoteRegistry
      status: Stopped
      startType: Disabled
```

## Exit codes

| Code | Description                              |
| :--- | :--------------------------------------- |
| 0    | Success                                  |
| 1    | Error                                    |
| 2    | Invalid JSON                             |
| 3    | Windows API error                        |
| 4    | Invalid argument or missing required parameter |
| 5    | Invalid operation or service state       |
| 6    | Service operation timed out              |

## See also

- [OpenDsc resource reference](../overview.md)
