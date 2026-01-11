# OpenDSC

A C# library ecosystem for building Microsoft DSC v3 resources with ease,
including a comprehensive set of built-in resources for Windows and
cross-platform management, plus a Local Configuration Manager (LCM) service
for continuous monitoring and remediation.

## Features

- 🚀 Quick scaffolding with project templates
- 📦 Supports .NET Standard 2.0, .NET 8, .NET 9, and .NET 10
- ⚡ Native AOT compilation support
- 🔧 Automatic CLI generation
- 📋 Automatic JSON schema generation
- 📄 Automatic resource manifest generation
- 🎯 Type-safe DSC resource implementation
- 🔀 Multi-resource support (requires DSC v3.2+)
- 🏗️ Built-in resources for Windows and cross-platform management
- 🔄 Local Configuration Manager (LCM) service for continuous monitoring

## Libraries

| Library | Description |
| --- | --- |
| [OpenDsc.Templates][t] | DSC project templates |
| [OpenDsc.Resource][r] | Core DSC resource implementation |
| [OpenDsc.Resource.CommandLine][c] | CLI and resource manifest generator |

[t]: https://www.nuget.org/packages/OpenDsc.Templates
[r]: https://www.nuget.org/packages/OpenDsc.Resource
[c]: https://www.nuget.org/packages/OpenDsc.Resource.CommandLine

## Built-in Resources

This repository includes a comprehensive set of DSC resources for
managing Windows and cross-platform systems:

### Windows Resources

- **[OpenDsc.Windows/Environment][env]** - Manage Windows environment variables
- **[OpenDsc.Windows/Group][group]** - Manage local Windows groups
  and membership
- **[OpenDsc.Windows/Service][service]** - Manage Windows services
- **[OpenDsc.Windows/User][user]** - Manage local Windows user accounts
- **[OpenDsc.Windows/Shortcut][shortcut]** - Manage Windows shortcuts
  (.lnk files)
- **[OpenDsc.Windows/OptionalFeature][optionalfeature]** - Manage Windows
  optional features via DISM
- **[OpenDsc.Windows.FileSystem/AccessControlList][acl]** - Manage file and
  directory permissions (ACLs)

### Cross-Platform Resources

- **[OpenDsc.FileSystem/File][file]** - Manage files (Windows, Linux, macOS)
- **[OpenDsc.FileSystem/Directory][directory]** - Manage directories with
  hash-based synchronization
- **[OpenDsc.Xml/Element][xml]** - Manage XML element content and attributes
- **[OpenDsc.Archive.Zip/Compress][zipcompress]** - Create ZIP archives from
  files and directories
- **[OpenDsc.Archive.Zip/Expand][zipexpand]** - Extract ZIP archives to
  specified locations

[env]: src/OpenDsc.Resource.Windows/Environment/README.md
[group]: src/OpenDsc.Resource.Windows/Group/README.md
[service]: src/OpenDsc.Resource.Windows/Service/README.md
[user]: src/OpenDsc.Resource.Windows/User/README.md
[shortcut]: src/OpenDsc.Resource.Windows/Shortcut/README.md
[optionalfeature]: src/OpenDsc.Resource.Windows/OptionalFeature/README.md
[acl]: src/OpenDsc.Resource.Windows/FileSystem/Acl/README.md
[file]: src/OpenDsc.Resource.FileSystem/File/README.md
[directory]: src/OpenDsc.Resource.FileSystem/Directory/README.md
[xml]: src/OpenDsc.Resource.Xml/Element/README.md
[zipcompress]: src/OpenDsc.Resource.Archive/Zip/Compress/README.md
[zipexpand]: src/OpenDsc.Resource.Archive/Zip/Expand/README.md

## Quick Start

### 1. Install the Templates

```powershell
dotnet new install OpenDsc.Templates
```

### 2. Create a New DSC Resource Project

```powershell
dotnet new dsc --resource-name "MyCompany/MyResource" --resource-description "My DSC resource"
```

For Native AOT support:

```powershell
dotnet new dsc --aot true --resource-name "MyCompany/MyResource"
```

### 3. Implement Your Resource

```csharp
using OpenDsc.Resource;
using OpenDsc.Resource.CommandLine;

[DscResource("MyCompany/MyResource", Description = "Manage my resource")]
public class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>
{
    public Schema Get(Schema instance)
    {
        // Implementation
    }
}
```

### 4. Create the Command Line Interface

```csharp
using OpenDsc.Resource.CommandLine;

var resource = new Resource(SourceGenerationContext.Default);
var command = new CommandBuilder()
    .AddResource<Resource, Schema>(resource)
    .Build();
return command.Parse(args).Invoke();
```

### 5. Build and Run

```powershell
.\build.ps1
```

## Local Configuration Manager (LCM)

The LCM is a background service that continuously monitors and optionally
remediates DSC configurations. It supports two operational modes:

- **Monitor Mode** - Periodically runs `dsc config test` to detect drift from
  desired state
- **Remediate Mode** - Automatically applies corrections when drift is detected
  using `dsc config set`

### Configuration

The LCM can be configured through multiple sources, with the following
priority order (highest to lowest):

1. Command-line arguments
2. Environment variables (prefixed with `LCM_`)
3. Platform-specific configuration file (see paths below)
4. Bundled `appsettings.json` in the service directory
5. Environment-specific `appsettings.{Environment}.json`

#### Configuration File Locations

| Platform | Configuration Directory | Logging |
| --- | --- | --- |
| Windows | `%ProgramData%\OpenDSC\LCM` | Windows Event Log (Application) |
| Linux | `/etc/opendsc/lcm` | systemd journal |
| macOS | `/Library/Preferences/OpenDSC/LCM` | Unified Logging |

Place your `appsettings.json` in the configuration directory for your platform.

#### Configurable Values

All settings must be under the `"LCM"` section in JSON configuration:

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `ConfigurationMode` | string | `Monitor` | Operating mode: `Monitor` or `Remediate` |
| `ConfigurationPath` | string | `{ConfigDir}/config/main.dsc.yaml` | Full path to the main DSC configuration file |
| `ConfigurationModeInterval` | timespan | `00:15:00` | Interval between checks (format: `hh:mm:ss`) |
| `DscExecutablePath` | string | `null` | Path to DSC executable (if not in PATH) |

#### Configuration Examples

**Using environment variables:**

```powershell
# Windows
$env:LCM_ConfigurationPath = "C:\configs\main.dsc.yaml"
$env:LCM_ConfigurationMode = "Remediate"
$env:LCM_ConfigurationModeInterval = "00:15:00"
$env:LCM_DscExecutablePath = "C:\Program Files\dsc\dsc.exe"

.\artifacts\Lcm\OpenDsc.Lcm.exe
```

```sh
# Linux/macOS
export LCM_ConfigurationPath="/etc/opendsc/config/main.dsc.yaml"
export LCM_ConfigurationMode="Remediate"
export LCM_ConfigurationModeInterval="00:15:00"

./artifacts/Lcm/OpenDsc.Lcm
```

**Using configuration file (appsettings.json):**

Windows (`%ProgramData%\OpenDSC\LCM\appsettings.json`):

```json
{
  "LCM": {
    "ConfigurationMode": "Remediate",
    "ConfigurationPath": "C:\\configs\\main.dsc.yaml",
    "ConfigurationModeInterval": "00:15:00",
    "DscExecutablePath": "C:\\Program Files\\dsc\\dsc.exe"
  }
}
```

Linux (`/etc/opendsc/lcm/appsettings.json`):

```json
{
  "LCM": {
    "ConfigurationMode": "Monitor",
    "ConfigurationPath": "/etc/opendsc/config/main.dsc.yaml",
    "ConfigurationModeInterval": "00:30:00"
  }
}
```

**Using command-line arguments:**

```powershell
.\artifacts\Lcm\OpenDsc.Lcm.exe --LCM:ConfigurationMode=Remediate --LCM:ConfigurationPath="C:\configs\main.dsc.yaml" --LCM:ConfigurationModeInterval="00:15:00"
```

### Installation

**Windows**: Use the MSI installer to install as a Windows Service:

```powershell
.\build.ps1 -Msi
msiexec /i artifacts\msi\OpenDsc.Lcm.msi
```

**Linux/macOS**: Run as a console application or configure as a systemd/launchd
service.

### Viewing Logs

Logs are automatically captured by each platform's native logging system:

**Windows** (Event Viewer):

```powershell
# Open Event Viewer GUI
eventvwr.msc

# Navigate to: Windows Logs > Application
# Filter by Source: OpenDsc.Lcm
```

**Linux** (journald):

```sh
# View all LCM logs
journalctl -u opendsc-lcm

# Follow logs in real-time
journalctl -u opendsc-lcm -f

# Filter by severity
journalctl -u opendsc-lcm -p err
journalctl -u opendsc-lcm -p warning
```

**macOS** (Unified Logging):

```sh
# View logs in Console.app (GUI)
open -a Console

# View logs via command line
log show --predicate 'process == "OpenDsc.Lcm"' --info --last 1h

# Stream logs in real-time
log stream --predicate 'process == "OpenDsc.Lcm"' --level info
```

## Examples

See the built-in resources and test projects for real-world examples:

- **Windows Management**: User accounts, groups, services, environment
  variables, optional features
- **File System**: Files, directories, access control lists, archives
- **Cross-Platform**: XML element management, ZIP compression and extraction

### Using Built-in Resources

The built-in resources are available as platform-specific executables:

```powershell
# Install DSC CLI
winget install Microsoft.DSC

# List available resources
dsc resource list

# Get environment variable
dsc resource get -r OpenDsc.Windows/Environment --input '{"name":"PATH"}'

# Set environment variable
dsc resource set -r OpenDsc.Windows/Environment --input '{"name":"TEST","value":"123"}'

# Create a ZIP archive
dsc resource set -r OpenDsc.Archive.Zip/Compress --input '{"path":"archive.zip","sourcePath":"C:\\Source"}'

# Extract a ZIP archive
dsc resource set -r OpenDsc.Archive.Zip/Expand --input '{"path":"archive.zip","destinationPath":"C:\\Destination"}'
```

## Requirements

- .NET 8 SDK or later (for development)
- DSC v3 (v3.2+ for multi-resource support)
- Windows, Linux, or macOS

## Documentation

For detailed documentation on each library, see their respective README files:

- [OpenDsc.Templates][td]
- [OpenDsc.Resource][rd]
- [OpenDsc.Resource.CommandLine][cd]

[td]: src/OpenDsc.Templates/README.md
[rd]: src/OpenDsc.Resource/README.md
[cd]: src/OpenDsc.Resource.CommandLine/README.md

## License

MIT
