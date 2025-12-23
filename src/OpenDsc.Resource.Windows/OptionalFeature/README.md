# OpenDsc.Windows/OptionalFeature

## Synopsis

Manages Windows optional features using the DISM API.

## Description

The `OpenDsc.Windows/OptionalFeature` resource enables you to query, enable,
and disable Windows optional features using the native DISM (Deployment Image
Servicing and Management) API. This resource uses P/Invoke to directly call
Windows APIs without PowerShell dependencies, providing fast and lightweight
operations.

Some features require a system restart after installation or removal. When a
restart is needed, the resource returns `_metadata._restartRequired` in the
result with details about which system needs to be restarted. Use
`dism /online /get-features` to list all available features. Feature names are
case-sensitive.

## Requirements

- Windows operating system (Desktop or Server)
- .NET 10.0 runtime
- **Administrator privileges required** for all DISM operations

## Capabilities

- **get** - Query the state of a Windows feature
- **set** - Enable or disable a Windows feature
- **delete** - Disable a Windows feature
- **export** - List all installed Windows features

## Properties

### Required Properties

- **name** (string) - The name of the Windows feature
  (e.g., `TelnetClient`, `IIS-WebServer`)

### Optional Properties

- **includeAllSubFeatures** (boolean) - Include all sub-features when
  enabling or disabling the feature
- **source** (string array) - Source paths for feature files. If not
  specified, uses Windows default sources
- **_exist** (boolean) - Indicates whether the feature should be
  installed. Default: `true`

### Read-only Properties

- **state** (string) - Current state of the feature: `NotPresent`,
  `UninstallPending`, `Staged`, `Removed`, `Installed`, `InstallPending`,
  `Superseded`, `PartiallyInstalled`
- **displayName** (string) - Display name of the feature
- **description** (string) - Description of the feature
- **_metadata** (object) - Operation metadata including restart requirements

## Examples

### Query feature state

```powershell
$config = @'
name: TelnetClient
'@

dsc resource get -r OpenDsc.Windows/OptionalFeature -i $config
```

### Enable a feature

```powershell
$config = @'
name: TelnetClient
'@

dsc resource set -r OpenDsc.Windows/OptionalFeature -i $config
```

### Enable feature with all sub-features

```powershell
$config = @'
name: IIS-WebServer
includeAllSubFeatures: true
'@

dsc resource set -r OpenDsc.Windows/OptionalFeature -i $config
```

### Disable a feature

```powershell
$config = @'
name: TelnetClient
_exist: false
'@

dsc resource set -r OpenDsc.Windows/OptionalFeature -i $config
```

### Delete a feature

```powershell
$config = @'
name: TelnetClient
'@

dsc resource delete -r OpenDsc.Windows/OptionalFeature -i $config
```

### Export all installed features

```powershell
dsc resource export -r OpenDsc.Windows/OptionalFeature
```

## Exit Codes

- **0** - Success
- **1** - Error
- **2** - Feature not found
- **3** - Invalid JSON
- **4** - Access denied - administrator privileges required
- **5** - DISM operation failed
