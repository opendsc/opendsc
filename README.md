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

| Library                           | Description                         |
|-----------------------------------|-------------------------------------|
| [OpenDsc.Templates][t]            | DSC project templates               |
| [OpenDsc.Resource][r]             | Core DSC resource implementation    |
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
- **[OpenDsc.Windows/ScheduledTask][scheduledtask]** - Manage Windows scheduled
  tasks
- **[OpenDsc.Windows/User][user]** - Manage local Windows user accounts
- **[OpenDsc.Windows/UserRight][userright]** - Manage Windows user rights
  assignments (privileges)
- **[OpenDsc.Windows/Shortcut][shortcut]** - Manage Windows shortcuts
  (.lnk files)
- **[OpenDsc.Windows/OptionalFeature][optionalfeature]** - Manage Windows
  optional features via DISM
- **[OpenDsc.Windows.FileSystem/AccessControlList][acl]** - Manage file and
  directory permissions (ACLs)

### Cross-Platform Resources

- **[OpenDsc.FileSystem/File][file]** - Manage files
- **[OpenDsc.FileSystem/Directory][directory]** - Manage directories with
  hash-based synchronization
- **[OpenDsc.FileSystem/SymbolicLink][symlink]** - Manage symbolic links
- **[OpenDsc.Json/Value][json]** - Manage JSON values at JSONPath locations
- **[OpenDsc.Xml/Element][xml]** - Manage XML element content and attributes
- **[OpenDsc.Archive.Zip/Compress][zipcompress]** - Create ZIP archives from
  files and directories
- **[OpenDsc.Archive.Zip/Expand][zipexpand]** - Extract ZIP archives to
  specified locations

### POSIX Resources

POSIX (Portable Operating System Interface) resources are designed for Unix-like
operating systems that follow POSIX standards, including Linux and macOS. These
resources provide Unix-specific functionality not available on Windows.

- **[OpenDsc.Posix.FileSystem/Permission][posixpermission]** - Manage POSIX
  file and directory permissions and ownership

[env]: src/OpenDsc.Resource.Windows/Environment/README.md
[group]: src/OpenDsc.Resource.Windows/Group/README.md
[service]: src/OpenDsc.Resource.Windows/Service/README.md
[scheduledtask]: src/OpenDsc.Resource.Windows/ScheduledTask/README.md
[user]: src/OpenDsc.Resource.Windows/User/README.md
[userright]: src/OpenDsc.Resource.Windows/UserRight/README.md
[shortcut]: src/OpenDsc.Resource.Windows/Shortcut/README.md
[optionalfeature]: src/OpenDsc.Resource.Windows/OptionalFeature/README.md
[acl]: src/OpenDsc.Resource.Windows/FileSystem/Acl/README.md
[file]: src/OpenDsc.Resource.FileSystem/File/README.md
[directory]: src/OpenDsc.Resource.FileSystem/Directory/README.md
[symlink]: src/OpenDsc.Resource.FileSystem/SymbolicLink/README.md
[posixpermission]: src/OpenDsc.Resource.Posix/FileSystem/Permission/README.md
[json]: src/OpenDsc.Resource.Json/Value/README.md
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

The LCM is a cross-platform background service that continuously monitors and
optionally remediates DSC configurations. It supports two operational modes:

- **Monitor Mode** - Periodically runs `dsc config test` to detect drift from
  desired state
- **Remediate Mode** - Automatically applies corrections when drift is detected
  using `dsc config set`

The LCM also supports pull mode, allowing it to download configurations from
the OpenDSC Pull Server with automatic updates, API key rotation, and
compliance reporting.

For detailed documentation, see the [LCM README][lcm-readme].

[lcm-readme]: src/OpenDsc.Lcm/README.md

### Quick Start

Install as a Windows Service:

```powershell
.\build.ps1 -Msi
msiexec /i artifacts\msi\OpenDsc.Lcm.msi
```

Or run as a console application:

```powershell
# Configure via environment variables
$env:LCM_ConfigurationPath = "C:\configs\main.dsc.yaml"
$env:LCM_ConfigurationMode = "Monitor"
$env:LCM_ConfigurationModeInterval = "00:15:00"

.\artifacts\Lcm\OpenDsc.Lcm.exe
```

### Configuration File Locations

| Platform | Configuration Directory | Logging |
| --- | --- | --- |
| Windows | `%ProgramData%\OpenDSC\LCM` | Windows Event Log (Application) |
| Linux | `/etc/opendsc/lcm` | systemd journal |
| macOS | `/Library/Preferences/OpenDSC/LCM` | Unified Logging |

## OpenDSC Pull Server

The OpenDSC Pull Server is a REST API-based centralized configuration server
that integrates with the LCM for pull mode operations. It provides:

- Configuration storage and distribution
- Node registration and management with mTLS authentication
- Automatic certificate rotation
- Compliance reporting
- Multi-database support (SQLite, SQL Server, PostgreSQL)
- Interactive API documentation via Scalar

For detailed documentation, see the [Server README][server-readme].

[server-readme]: src/OpenDsc.Server/README.md

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
