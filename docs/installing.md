---
description: >-
  Install OpenDSC on Windows, Linux, or macOS. OpenDSC provides DSC Resources, a Local
  Configuration Manager, and a Pull Server for centralized configuration management.
title: Install OpenDSC
date: 2026-03-27
topic: install
---

# Install OpenDSC

OpenDSC provides three installable components. You can install them
independently based on your
needs:

| Component             | Purpose                                        | Platforms             |
| :-------------------- | :--------------------------------------------- | :-------------------- |
| **OpenDSC Resources** | Built-in DSC Resources for system management   | Windows, Linux, macOS |
| **OpenDSC LCM**       | Local Configuration Manager background service | Windows, Linux, macOS |
| **OpenDSC Server**    | Pull Server with REST API and web UI           | Windows, Linux, macOS |

## Prerequisites

- [Microsoft DSC v3][01] installed and available in your `PATH`.
- .NET 10 runtime (included in portable and MSI builds).
- PowerShell 7 or later (recommended for scripting and testing).

## Install on Windows

### Using MSI installers

Download the MSI installers from the [OpenDSC releases][02] page. The following
example uses
version `0.5.1`:

```powershell
# Download the resource executable installer
$version = '0.5.1'
Invoke-WebRequest "https://github.com/opendsc/opendsc/releases/download/v$version/OpenDSC.Resources-$version.msi" `
    -OutFile "$env:TEMP\OpenDSC.Resources-$version.msi"

# Install silently
Start-Process msiexec.exe -Wait -ArgumentList "/i $env:TEMP\OpenDSC.Resources-$version.msi /quiet"
```

The installer places files in `C:\Program Files\OpenDSC` by default and adds the
directory to the
system `PATH`.

To install the LCM and Pull Server:

```powershell
# LCM installer
Invoke-WebRequest "https://github.com/opendsc/opendsc/releases/download/v$version/OpenDSC.Lcm-$version.msi" `
    -OutFile "$env:TEMP\OpenDSC.Lcm-$version.msi"
Start-Process msiexec.exe -Wait -ArgumentList "/i $env:TEMP\OpenDSC.Lcm-$version.msi /quiet"

# Pull Server installer
Invoke-WebRequest "https://github.com/opendsc/opendsc/releases/download/v$version/OpenDSC.Server-$version.msi" `
    -OutFile "$env:TEMP\OpenDSC.Server-$version.msi"
Start-Process msiexec.exe -Wait -ArgumentList "/i $env:TEMP\OpenDSC.Server-$version.msi /quiet"
```

### From source

Clone the repository and build with the included build script:

```powershell
git clone https://github.com/opendsc/opendsc.git
cd opendsc
.\build.ps1
```

Build artifacts are placed in the `artifacts/` directory:

| Path                 | Contents                             |
| :------------------- | :----------------------------------- |
| `artifacts/publish/` | Resource executable and dependencies |
| `artifacts/Lcm/`     | LCM service                          |
| `artifacts/Server/`  | Pull Server                          |

To build portable self-contained executables that include the .NET runtime:

```powershell
.\build.ps1 -Portable
```

To build MSI installers:

```powershell
.\build.ps1 -Msi
```

## Install on Linux and macOS

### From source

```sh
git clone https://github.com/opendsc/opendsc.git
cd opendsc
pwsh -File build.ps1
```

The cross-platform build includes resources for file system, JSON, XML, archive,
and POSIX
operations. Windows-specific resources (environment variables, services, users,
groups) are
excluded on non-Windows platforms.

## Verify the installation

After installation, verify that the DSC CLI can discover OpenDSC resources:

```powershell
dsc resource list OpenDsc*
```

You should see output listing the available OpenDsc resources:

```plaintext
Type                                         Kind      Version  Capabilities  Description
------------------------------------------------------------------------------------------------------------
OpenDsc.Windows/Environment                  Resource  0.1.0    gs-d----      Manage Windows environment...
OpenDsc.Windows/Service                      Resource  0.1.0    gs-d----      Manage Windows services
OpenDsc.Windows/Group                        Resource  0.1.0    gs-d----      Manage local Windows groups
...
```

## Next steps

- [Get started][03] with a hands-on tutorial.
- Learn about [OpenDSC Resources][04].
- Set up the [Pull Server][05].

<!-- Link references -->
[01]: https://learn.microsoft.com/powershell/dsc/install
[02]: https://github.com/opendsc/opendsc/releases
[03]: get-started/index.md
[04]: concepts/resources/overview.md
[05]: get-started/pull-server-setup.md
