# Optional Feature Resource

## Synopsis

Manages Windows optional features using the Deployment Image Servicing and
Management (DISM) API.
This resource supports signaling restart requirements when a feature enable or
disable operation
requires a system restart.

## Type

```text
OpenDsc.Windows/OptionalFeature
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

> [!NOTE]
> This resource uses `SetReturn = SetReturn.State` and returns actual state from
> the `Set()` operation, including `_metadata` with restart requirements when
> applicable.

## Properties

### name

The name of the Windows optional feature.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

### includeAllSubFeatures

Include all sub-features when enabling or disabling the feature.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

### source

Source file locations for the feature. If omitted, Windows uses its default sources.

```yaml
Type: string[]
Required: No
Access: Read/Write
Default value: None
```

### state

The DISM feature state reported by the system.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

### displayName

The display name of the feature.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

### description

The description of the feature.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

### _exist

`true` (default) to enable the feature, `false` to disable it.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

### _metadata

Metadata returned by the resource, may include `_restartRequired`.

```yaml
Type: object
Required: No
Access: Read-Only
Default value: None
```

### State values

| Value               | Description                                        |
| :------------------ | :------------------------------------------------- |
| `NotPresent`        | The feature is not present on the system.           |
| `UninstallPending`  | The feature is pending uninstall.                   |
| `Staged`            | The feature is staged but not installed.            |
| `Removed`           | The feature has been removed.                       |
| `Installed`         | The feature is installed and enabled.               |
| `InstallPending`    | The feature is pending installation.                |
| `Superseded`        | The feature has been superseded by a newer version. |
| `PartiallyInstalled`| The feature is partially installed.                 |

## Examples

### Example 1 — Get a feature state

```powershell
dsc resource get -r OpenDsc.Windows/OptionalFeature --input '{"name":"Microsoft-Hyper-V-All"}'
```

### Example 2 — Enable a feature

```powershell
dsc resource set -r OpenDsc.Windows/OptionalFeature --input '{"name":"Microsoft-Hyper-V-All"}'
```

If a restart is required, the result includes `_metadata`:

```json
{
  "name": "Microsoft-Hyper-V-All",
  "state": "Installed",
  "_metadata": {
    "_restartRequired": [
      { "system": "SERVER01" }
    ]
  }
}
```

### Example 3 — Disable a feature

```powershell
dsc resource delete -r OpenDsc.Windows/OptionalFeature --input '{"name":"Microsoft-Hyper-V-All"}'
```

### Example 4 — Export all features

```powershell
dsc resource export -r OpenDsc.Windows/OptionalFeature
```

### Example 5 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Enable Windows Subsystem for Linux
    type: OpenDsc.Windows/OptionalFeature
    properties:
      name: Microsoft-Windows-Subsystem-Linux
```

## Exit codes

| Code | Description                                       |
| :--- | :------------------------------------------------ |
| 0    | Success                                           |
| 1    | Error                                             |
| 2    | Feature not found                                 |
| 3    | Invalid JSON                                      |
| 4    | Access denied - administrator privileges required |
| 5    | DISM operation failed                             |

## See also

- [OpenDsc resource reference](../overview.md)
