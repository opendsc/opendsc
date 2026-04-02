# OpenDSC

Microsoft DSC v3 resources for Windows and cross-platform system management, plus a Local Configuration Manager (LCM) background service and a Pull Server for centralized configuration.

## Major Components

| Component | Path | Purpose |
|-----------|------|---------|
| DSC Resources | `src/OpenDsc.Resource*/` | Windows, SQL Server, and cross-platform resources |
| Resource executable | `src/OpenDsc.Resources/` | Single platform executable bundling all resources |
| LCM Service | `src/OpenDsc.Lcm/` | Background service that monitors/remediates configs |
| Pull Server | `src/OpenDsc.Server/` | ASP.NET Core REST API + Blazor Server admin UI |
| Resource framework | `src/OpenDsc.Resource.CommandLine/` | Base classes and CLI infrastructure |

## Available Resources

**Windows** (`src/OpenDsc.Resource.Windows/`):
- `OpenDsc.Windows/Environment` — environment variable management
- `OpenDsc.Windows/Service` — Windows service control
- `OpenDsc.Windows/Shortcut` — shortcut (.lnk) file management
- `OpenDsc.Windows/Group` — local Windows group management
- `OpenDsc.Windows/User` — local Windows user accounts
- `OpenDsc.Windows/OptionalFeature` — Windows optional features via DISM
- `OpenDsc.Windows/ScheduledTask` — scheduled task management
- `OpenDsc.Windows/UserRight` — user rights assignment management
- `OpenDsc.Windows.FileSystem/AccessControlList` — file system ACL management

**SQL Server** (`src/OpenDsc.Resource.SqlServer/`):
- `OpenDsc.SqlServer/Login`, `Database`, `DatabaseRole`, `DatabaseUser`, `ServerRole`
- `OpenDsc.SqlServer/DatabasePermission`, `ServerPermission`, `ObjectPermission`
- `OpenDsc.SqlServer/Configuration`, `LinkedServer`, `AgentJob`

**Cross-Platform**:
- `OpenDsc.FileSystem/File`, `Directory`, `SymbolicLink` (`src/OpenDsc.Resource.FileSystem/`)
- `OpenDsc.Xml/Element` (`src/OpenDsc.Resource.Xml/`)
- `OpenDsc.Json/Value` (`src/OpenDsc.Resource.Json/`)
- `OpenDsc.Archive.Zip/Compress`, `OpenDsc.Archive.Zip/Expand` (`src/OpenDsc.Resource.Archive/`)
- `OpenDsc.Posix.FileSystem/Permission` (`src/OpenDsc.Resource.Posix/`)

## Resource Naming Convention

- Windows: `OpenDsc.Windows/<Name>` → namespace `OpenDsc.Resource.Windows.<Name>`
- SQL Server: `OpenDsc.SqlServer/<Name>` → namespace `OpenDsc.Resource.SqlServer.<Name>`
- Cross-platform: `OpenDsc.<Area>/<Name>` → namespace `OpenDsc.Resource.<Area>.<Name>`
- Sub-area: `OpenDsc.Windows.FileSystem/AccessControlList` → folder `src/OpenDsc.Resource.Windows/FileSystem/Acl/` → namespace `OpenDsc.Resource.Windows.FileSystem.Acl`

## Build Commands

```powershell
.\build.ps1                       # Build + test
.\build.ps1 -Configuration Debug
.\build.ps1 -SkipTest             # Skip all tests
.\build.ps1 -SkipUnitTests
.\build.ps1 -SkipIntegrationTests
.\build.ps1 -SkipFunctionalTests  # Skip Testcontainers tests
.\build.ps1 -Portable             # Self-contained with embedded .NET runtime
.\build.ps1 -Msi                  # Build MSI installer
.\build.ps1 -Pack                 # Create NuGet packages
```

Output: `artifacts/publish/` (resources), `artifacts/Lcm/` (LCM), `artifacts/Server/` (Pull Server).

## Code Conventions

**File headers** — all `.cs` files require MIT license header (IDE0073 enforced):

```csharp
// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.
```

**Comments** — avoid self-explanatory comments; code should be self-documenting. Remove comments that restate obvious code. Keep only non-obvious logic explanations.

## Package Dependencies

- `OpenDsc.Resource.CommandLine` — DSC resource base class, interfaces, and CLI framework
- `JsonSchema.Net.Generation` — JSON Schema generation attributes
- `System.DirectoryServices.AccountManagement` — user/group management (Windows)
- `System.ServiceProcess.ServiceController` — service management (Windows)

**Target Frameworks:** Libraries multi-target .NET Standard 2.0, .NET 8/9/10. Executables target .NET 10 (`net10.0-windows` for Windows executables).

