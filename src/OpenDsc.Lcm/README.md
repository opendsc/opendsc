# OpenDSC Local Configuration Manager (LCM)

A cross-platform background service that continuously monitors and optionally
remediates DSC configurations, ensuring systems remain in their desired state.

## Features

- **Dual Operating Modes**:
  - **Monitor Mode** - Periodically runs `dsc config test` to detect drift from
    desired state
  - **Remediate Mode** - Automatically applies corrections when drift is
    detected using `dsc config set`
- **Dynamic Configuration Reload** - Seamlessly switches modes and updates
  intervals without restart
- **Pull Server Integration** - Download configurations from OpenDSC Pull
  Server with automatic updates
- **mTLS Authentication** - Secure mutual TLS authentication with the pull
  server using managed or platform certificates
- **Automatic Certificate Rotation** - Self-managed certificate lifecycle with
  seamless rotation
- **Compliance Reporting** - Submit test/set results to pull server for
  centralized monitoring
- **Cross-Platform** - Windows, Linux, and macOS support
- **Platform-Native Logging** - Windows Event Log, systemd journal, and macOS
  Unified Logging
- **Flexible Configuration** - Environment variables, JSON files, or
  command-line arguments

## Quick Start

### Installation

**Windows** (MSI Installer):

```powershell
# Build the MSI installer
.\build.ps1 -Msi

# Install as Windows Service
msiexec /i artifacts\msi\OpenDsc.Lcm.msi

# Service is automatically installed and started
# Manage via Services.msc or:
sc start OpenDsc.Lcm
sc stop OpenDsc.Lcm
```

**Linux/macOS** (Console or systemd/launchd):

```sh
# Build the LCM
.\build.ps1

# Run as console application
./artifacts/Lcm/OpenDsc.Lcm

# Or configure as systemd service (Linux)
sudo cp artifacts/Lcm/opendsc-lcm.service /etc/systemd/system/
sudo systemctl enable opendsc-lcm
sudo systemctl start opendsc-lcm
```

### Basic Configuration

Create a configuration file in the platform-specific location:

**Windows** (`%ProgramData%\OpenDSC\LCM\appsettings.json`):

```json
{
  "LCM": {
    "ConfigurationMode": "Monitor",
    "ConfigurationPath": "C:\\configs\\main.dsc.yaml",
    "ConfigurationModeInterval": "00:15:00"
  }
}
```

**Linux** (`/etc/opendsc/lcm/appsettings.json`):

```json
{
  "LCM": {
    "ConfigurationMode": "Remediate",
    "ConfigurationPath": "/etc/opendsc/config/main.dsc.yaml",
    "ConfigurationModeInterval": "00:15:00"
  }
}
```

**macOS** (`/Library/Preferences/OpenDSC/LCM/appsettings.json`):

```json
{
  "LCM": {
    "ConfigurationMode": "Monitor",
    "ConfigurationPath": "/Library/OpenDSC/config/main.dsc.yaml",
    "ConfigurationModeInterval": "00:30:00"
  }
}
```

## Configuration

### Configuration Sources

The LCM loads configuration from multiple sources with the following priority
(highest to lowest):

1. **Command-line arguments** - `--LCM:ConfigurationMode=Remediate`
2. **Environment variables** - `LCM_ConfigurationMode=Remediate`
3. **Platform-specific configuration file** - See paths below
4. **Bundled appsettings.json** - In the service directory
5. **Environment-specific file** - `appsettings.{Environment}.json`

### Configuration File Locations

| Platform | Configuration Directory | Logging Destination |
| --- | --- | --- |
| **Windows** | `%ProgramData%\OpenDSC\LCM` | Windows Event Log (Application) |
| **Linux** | `/etc/opendsc/lcm` | systemd journal |
| **macOS** | `/Library/Preferences/OpenDSC/LCM` | Unified Logging |

### Local Configuration Settings

All settings must be under the `"LCM"` section in JSON configuration:

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `ConfigurationMode` | string | `Monitor` | Operating mode: `Monitor` or `Remediate` |
| `ConfigurationSource` | string | `Local` | Configuration source: `Local` or `Pull` |
| `ConfigurationPath` | string | `{ConfigDir}/config/main.dsc.yaml` | Full path to the main DSC configuration file |
| `ConfigurationModeInterval` | timespan | `00:15:00` | Interval between checks (format: `hh:mm:ss`) |
| `DscExecutablePath` | string | `null` | Path to DSC executable (defaults to PATH) |

### Pull Server Configuration

When using `ConfigurationSource: Pull`, configure the pull server settings:

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `PullServer.ServerUrl` | string | - | Base URL of the pull server (e.g., `https://dsc.example.com`) |
| `PullServer.RegistrationKey` | string | - | Pre-shared key for node registration |
| `PullServer.ReportCompliance` | bool | `false` | Submit compliance reports to the server |
| `PullServer.NodeId` | string | `null` | Assigned node ID (auto-populated on registration) |
| `PullServer.CertificateSource` | string | `Managed` | Certificate source: `Managed` (auto-generated) or `Platform` (system store) |
| `PullServer.CertificateThumbprint` | string | `null` | Certificate thumbprint (required for Platform source) |
| `PullServer.CertificatePath` | string | `{ConfigDir}/certs/client.pfx` | Path to certificate file (Managed source) |
| `PullServer.CertificatePassword` | string | `null` | Password for certificate file (Managed source) |

**Example pull server configuration:**

```json
{
  "LCM": {
    "ConfigurationMode": "Remediate",
    "ConfigurationSource": "Pull",
    "ConfigurationModeInterval": "00:15:00",
    "PullServer": {
      "ServerUrl": "https://dsc-server.example.com",
      "RegistrationKey": "your-registration-key-here",
      "ReportCompliance": true,
      "CertificateSource": "Managed"
    }
  }
}
```

### mTLS Certificate Configuration

The LCM uses mutual TLS (mTLS) for secure authentication with the pull
server. You can configure certificates in two ways:

**Managed Certificates (Default):**

The LCM automatically generates and manages self-signed client certificates:

```json
{
  "PullServer": {
    "CertificateSource": "Managed",
    "CertificatePath": "C:\\ProgramData\\OpenDSC\\LCM\\certs\\client.pfx",
    "CertificatePassword": "optional-password"
  }
}
```

- Certificates are auto-generated on first run if they don't exist
- Stored as password-protected PFX files
- Automatically rotated when 2/3 of their lifetime has elapsed
  (90-day validity)
- Certificate rotation is coordinated with the pull server

**Platform Certificate Store:**

Use a certificate from the platform's certificate store:

```json
{
  "PullServer": {
    "CertificateSource": "Platform",
    "CertificateThumbprint": "ABC123..."
  }
}
```

- Load certificate from `CurrentUser\My` store (Windows) or equivalent
  platform store
- Certificate must have private key with client authentication EKU
- No automatic rotation - certificate lifecycle is managed externally

### Configuration Examples

#### Using Environment Variables

**Windows (PowerShell):**

```powershell
$env:LCM_ConfigurationPath = "C:\configs\main.dsc.yaml"
$env:LCM_ConfigurationMode = "Remediate"
$env:LCM_ConfigurationModeInterval = "00:15:00"
$env:LCM_DscExecutablePath = "C:\Program Files\dsc\dsc.exe"

.\artifacts\Lcm\OpenDsc.Lcm.exe
```

**Linux/macOS (Bash):**

```sh
export LCM_ConfigurationPath="/etc/opendsc/config/main.dsc.yaml"
export LCM_ConfigurationMode="Remediate"
export LCM_ConfigurationModeInterval="00:15:00"

./artifacts/Lcm/OpenDsc.Lcm
```

#### Using Command-Line Arguments

```powershell
.\artifacts\Lcm\OpenDsc.Lcm.exe `
  --LCM:ConfigurationMode=Remediate `
  --LCM:ConfigurationPath="C:\configs\main.dsc.yaml" `
  --LCM:ConfigurationModeInterval="00:15:00"
```

```sh
./artifacts/Lcm/OpenDsc.Lcm \
  --LCM:ConfigurationMode=Remediate \
  --LCM:ConfigurationPath=/etc/opendsc/config/main.dsc.yaml \
  --LCM:ConfigurationModeInterval=00:15:00
```

## Logging

### Log Categories

The LCM supports detailed logging configuration through the `"Logging"` section
in `appsettings.json`:

| Category | Description | Default Level |
| --- | --- | --- |
| `Default` | Fallback for unspecified categories | `Warning` |
| `DSC` | DSC executable trace messages | `Warning` |
| `OpenDsc.Lcm` | LCM service operational logs | `Information` |

### Logging Configuration

**Example with separate DSC logging:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "DSC": "Debug",
      "OpenDsc.Lcm": "Information"
    }
  }
}
```

This allows you to enable detailed DSC debugging (`Debug` or `Trace`) while
keeping LCM service logs at `Information` or `Warning`.

### Viewing Logs

#### Windows (Event Viewer)

```powershell
# Open Event Viewer GUI
eventvwr.msc

# Navigate to: Windows Logs > Application
# Filter by Source: OpenDsc.Lcm

# PowerShell query
Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='OpenDsc.Lcm'} -MaxEvents 50
```

#### Linux (systemd journal)

```sh
# View all LCM logs
journalctl -u opendsc-lcm

# Follow logs in real-time
journalctl -u opendsc-lcm -f

# Filter by severity
journalctl -u opendsc-lcm -p err
journalctl -u opendsc-lcm -p warning

# View logs since last boot
journalctl -u opendsc-lcm -b

# View logs from last hour
journalctl -u opendsc-lcm --since "1 hour ago"
```

#### macOS (Unified Logging)

```sh
# View logs in Console.app (GUI)
open -a Console

# View logs via command line
log show --predicate 'process == "OpenDsc.Lcm"' --info --last 1h

# Stream logs in real-time
log stream --predicate 'process == "OpenDsc.Lcm"' --level info

# Filter by subsystem
log show --predicate 'subsystem == "OpenDsc.Lcm"' --last 1d
```

## Operating Modes

### Monitor Mode

Periodically tests the configuration without applying changes:

```json
{
  "LCM": {
    "ConfigurationMode": "Monitor",
    "ConfigurationPath": "C:\\configs\\main.dsc.yaml",
    "ConfigurationModeInterval": "00:15:00"
  }
}
```

**Behavior:**

- Runs `dsc config test` every 15 minutes
- Logs resources that are not in desired state
- Does not modify the system
- Useful for compliance monitoring and drift detection

### Remediate Mode

Automatically corrects configuration drift:

```json
{
  "LCM": {
    "ConfigurationMode": "Remediate",
    "ConfigurationPath": "C:\\configs\\main.dsc.yaml",
    "ConfigurationModeInterval": "00:15:00"
  }
}
```

**Behavior:**

- Runs `dsc config test` to detect drift
- If drift detected, runs `dsc config set` to apply corrections
- Logs all changes made to the system
- Ensures continuous compliance with desired state

## Pull Server Integration

The LCM can download configurations from an OpenDSC Pull Server instead of
using a local file.

### Configuration

```json
{
  "LCM": {
    "ConfigurationMode": "Remediate",
    "ConfigurationSource": "Pull",
    "ConfigurationModeInterval": "00:15:00",
    "PullServer": {
      "ServerUrl": "https://dsc-server.example.com",
      "RegistrationKey": "shared-registration-key",
      "ReportCompliance": true
    }
  }
}
```

### Node Registration

On first run, the LCM automatically:

1. Generates or loads a client certificate for mTLS authentication
2. Registers with the pull server using the FQDN, registration key, and
   certificate
3. Receives a unique `NodeId`
4. Stores the NodeId and certificate for future authentication
5. Downloads the configuration assigned to the node

The server validates the client certificate during registration and stores
the certificate thumbprint, subject, and expiration date for the node.

### Certificate Rotation

For managed certificates, the LCM automatically rotates certificates to
maintain security:

- Certificates are valid for 90 days from creation
- Rotation occurs automatically when 2/3 of the certificate lifetime has
  elapsed (after 60 days)
- The LCM generates a new self-signed certificate
- The new certificate is registered with the pull server via the
  `/rotate-certificate` endpoint
- The server updates the stored certificate thumbprint, subject, and
  expiration date
- Rotation is seamless - no service interruption occurs

For platform store certificates, rotation must be managed externally.

### Compliance Reporting

When `ReportCompliance: true`, the LCM submits reports to the pull server:

- After each `dsc config test` operation (Monitor mode)
- After each `dsc config set` operation (Remediate mode)
- Reports include:
  - Timestamp
  - Operation type (Test or Set)
  - Configuration checksum
  - Resource-level results
  - Error messages (if any)

### Configuration Updates

The LCM checks for configuration updates before each operation:

1. Requests the current configuration checksum from the server
2. Compares with the local configuration checksum
3. Downloads the new configuration if changed
4. Applies the new configuration immediately

## Advanced Usage

### Dynamic Configuration Reload

The LCM watches for configuration changes and reloads without restarting:

- Detects changes to `appsettings.json`
- Switches modes seamlessly (Monitor ↔ Remediate)
- Updates intervals on the fly
- Cancels current operation and starts new mode

### Custom DSC Executable Path

If DSC is not in your PATH or you need a specific version:

```json
{
  "LCM": {
    "DscExecutablePath": "C:\\custom\\path\\dsc.exe"
  }
}
```

### Minimum Check Interval

The LCM enforces a minimum 60-second delay between checks when errors occur,
regardless of the configured interval. This prevents rapid error loops.

## Troubleshooting

### LCM Not Running

**Windows:**

```powershell
# Check service status
sc query OpenDsc.Lcm

# View recent service events
Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='OpenDsc.Lcm'} -MaxEvents 10
```

**Linux:**

```sh
# Check service status
systemctl status opendsc-lcm

# View recent logs
journalctl -u opendsc-lcm -n 50
```

### Configuration File Not Found

Verify the configuration path:

```powershell
# Windows
Test-Path "C:\configs\main.dsc.yaml"

# Linux/macOS
ls -l /etc/opendsc/config/main.dsc.yaml
```

Check LCM logs for the actual path being used.

### DSC Executable Not Found

Ensure DSC is installed and in PATH:

```powershell
# Check if DSC is available
dsc --version

# Or specify full path in config
{
  "LCM": {
    "DscExecutablePath": "C:\\Program Files\\dsc\\dsc.exe"
  }
}
```

### Pull Server Connection Issues

Check connectivity and credentials:

```powershell
# Test server connectivity
Invoke-WebRequest -Uri "https://dsc-server.example.com/health" -UseBasicParsing

# Verify registration key is correct
# Check LCM logs for authentication errors
```

Common issues:

- Incorrect server URL (missing `https://` or wrong port)
- Invalid registration key
- Network/firewall blocking connection
- Server certificate validation failures

## Architecture

The LCM consists of two main components:

### LcmWorker

The background service that orchestrates the monitoring/remediation loop:

- Manages the operational mode (Monitor/Remediate)
- Schedules periodic checks based on configured interval
- Handles dynamic configuration reload
- Coordinates with pull server (if configured)

### DscExecutor

Executes DSC CLI commands and parses results:

- Invokes `dsc config test` and `dsc config set`
- Parses JSON output from DSC
- Captures trace messages for logging
- Reports errors and warnings

## Requirements

- **.NET 10 Runtime** (included in MSI installer for Windows)
- **DSC v3 CLI** - Installed and in PATH (or configured via `DscExecutablePath`)
- **Platform Support**:
  - Windows 10/11, Windows Server 2019+
  - Linux (systemd-based distributions)
  - macOS 10.15+

## License

MIT
