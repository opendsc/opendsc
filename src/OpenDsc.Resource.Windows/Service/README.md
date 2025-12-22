# OpenDsc.Windows/Service

## Synopsis

Manage Windows services.

## Description

The `OpenDsc.Windows/Service` resource manages Windows services. It can create,
configure, start, stop, and delete Windows services on the local system. The
resource supports setting service properties such as display name,
description, binary path, dependencies, start type, and status.

## Requirements

- Windows operating system
- Administrator privileges required for Set and Delete operations

## Capabilities

- **Get**: Retrieve current state of a Windows service
- **Set**: Create or configure a Windows service
- **Delete**: Remove a Windows service
- **Export**: List all Windows services on the system

## Properties

### Required Properties

- **name** (string): The name of the service. Case insensitive. Cannot
  contain forward slash (/) or backslash (\\) characters. Max 256 characters.

### Optional Properties

- **displayName** (string): The display name of the service. Max 256 characters.
- **description** (string): The description of the service.
- **path** (string): The fully qualified path to the service binary file.
  If the path contains a space, it must be quoted. The path can also include
  arguments. Required when creating a new service.
- **dependencies** (string[]): Array of service names that this service
  depends on.
- **status** (enum): The desired status of the service. Valid values:
  `Stopped`, `Running`, `Paused`.
- **startType** (enum): The start type of the service. Valid values:
  `Automatic`, `Manual`, `Disabled`. Required when creating a new service.
- **_exist** (boolean): Indicates whether the service should exist.
  Default: `true`.

## Examples

### Get Service State

Retrieve information about the Windows Update service:

```powershell
$config = @'
name: wuauserv
'@

dsc resource get -r OpenDsc.Windows/Service -i $config
```

### Create a New Service

Create a new service with basic configuration:

```powershell
$config = @'
name: MyCustomService
displayName: My Custom Service
description: A custom service for my application
path: C:\MyApp\service.exe
startType: Automatic
'@

dsc resource set -r OpenDsc.Windows/Service -i $config
```

### Create Service with Arguments

Create a service with command-line arguments:

```powershell
$config = @'
name: MyService
path: C:\Program Files\MyApp\service.exe --config C:\configs\app.json
startType: Manual
'@

dsc resource set -r OpenDsc.Windows/Service -i $config
```

### Configure Service Dependencies

Create a service that depends on other services:

```powershell
$config = @'
name: MyService
path: C:\MyApp\service.exe
startType: Automatic
dependencies:
  - Tcpip
  - Dnscache
'@

dsc resource set -r OpenDsc.Windows/Service -i $config
```

### Update Existing Service

Modify an existing service's configuration:

```powershell
$config = @'
name: MyService
displayName: Updated Display Name
description: Updated description
startType: Manual
'@

dsc resource set -r OpenDsc.Windows/Service -i $config
```

### Start a Service

Set a service to running state:

```powershell
$config = @'
name: MyService
status: Running
'@

dsc resource set -r OpenDsc.Windows/Service -i $config
```

### Stop a Service

Set a service to stopped state:

```powershell
$config = @'
name: MyService
status: Stopped
'@

dsc resource set -r OpenDsc.Windows/Service -i $config
```

### Configure Service Start Type

Change a service to start automatically:

```powershell
$config = @'
name: wuauserv
startType: Automatic
'@

dsc resource set -r OpenDsc.Windows/Service -i $config
```

Disable a service:

```powershell
$config = @'
name: MyService
startType: Disabled
'@

dsc resource set -r OpenDsc.Windows/Service -i $config
```

### Delete a Service

Remove a service from the system:

```powershell
$config = @'
name: MyService
'@

dsc resource delete -r OpenDsc.Windows/Service -i $config
```

### Export All Services

Export the configuration of all services on the system:

```powershell
dsc resource export -r OpenDsc.Windows/Service
```

### Check if Service Exists

```powershell
$config = @'
name: MyService
'@

$result = dsc resource get -r OpenDsc.Windows/Service -i $config | ConvertFrom-Json

if ($result.actualState._exist -eq $false) {
    Write-Host "Service does not exist"
}
```

### Ensure Service is Running

```powershell
$config = @'
name: MyService
status: Running
'@

dsc resource set -r OpenDsc.Windows/Service -i $config
```

### Create and Start Service in One Operation

```powershell
$config = @'
name: MyService
path: C:\MyApp\service.exe
startType: Automatic
status: Running
'@

dsc resource set -r OpenDsc.Windows/Service -i $config
```

## Exit Codes

- **0** - Success
- **1** - Error
- **2** - Invalid JSON
- **3** - Windows API error
- **4** - Invalid argument or missing required parameter
- **5** - Invalid operation or service state
- **6** - Service operation timed out
