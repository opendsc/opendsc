---
description: "Use when working on the LCM service — background worker, DSC executor, configuration model, certificate management, or Pull Server client. Covers operational modes, configuration priority, dynamic mode switching, and DSC CLI execution patterns."
applyTo: "src/OpenDsc.Lcm/**"
---

# LCM Service (OpenDsc.Lcm)

Cross-platform .NET background service that continuously monitors and/or remediates DSC configurations.

## Key Components

| File | Purpose |
|------|---------|
| `LcmWorker.cs` | Main background service implementing Monitor and Remediate modes |
| `DscExecutor.cs` | Runs `dsc config test` / `dsc config set`, parses structured output |
| `LcmConfig.cs` | Configuration model with validation |
| `ConfigPaths.cs` | Platform-specific configuration paths |
| `CertificateManager.cs` | Client certificate lifecycle (Managed / Platform source) |
| `PullServerClient.cs` | HTTP client for Pull Server communication |

## Operational Modes

- **Monitor** — periodically runs `dsc config test` to detect drift, logs results
- **Remediate** — runs `dsc config test` then applies `dsc config set` when drift detected

## Configuration Model

```csharp
public class LcmConfig
{
    public ConfigurationMode ConfigurationMode { get; set; } = ConfigurationMode.Monitor;
    public ConfigurationSource ConfigurationSource { get; set; } = ConfigurationSource.Local;
    public string ConfigurationPath { get; set; }          // Path to main.dsc.yaml (Local mode)
    public TimeSpan ConfigurationModeInterval { get; set; } = TimeSpan.FromMinutes(15);
    public string? DscExecutablePath { get; set; }         // Defaults to 'dsc' in PATH
    public PullServerSettings? PullServer { get; set; }    // Pull mode only
}
```

## Configuration Priority (highest wins)

1. Command-line arguments
2. Environment variables (prefix `LCM_`)
3. System-wide config file:
   - Windows: `%ProgramData%\OpenDSC\LCM\appsettings.json`
   - Linux: `/etc/opendsc/lcm/appsettings.json`
   - macOS: `/Library/Preferences/OpenDSC/LCM/appsettings.json`
4. Bundled `appsettings.json` in service directory
5. `appsettings.{Environment}.json`

## Key Patterns

**Dynamic mode switching** — watches for config changes, gracefully switches without restart:

```csharp
_configChangeToken = lcmMonitor.OnChange((config, name) => {
    OnConfigurationReloaded(config);  // Cancels current operation, switches mode
});
```

**DSC execution:**

```csharp
var result = await dscExecutor.ExecuteTestAsync(config.ConfigurationPath, config, LogLevel.Information);
// Returns DscResult with messages, results[], hadErrors
```

**Logging** — uses `LoggerMessage` source-generated attributes for performance. All log messages are defined as partial methods in worker classes.

## Pull Mode Flow

1. Load/generate client certificate (`CertificateSource: Managed` auto-generates; `Platform` loads from store by thumbprint)
2. Register node if not already registered → receives `NodeId`
3. Check certificate expiry → rotate via `RotateCertificateAsync()` if needed
4. Check configuration checksum → download if changed
5. Execute `dsc config test`; apply `dsc config set` if Remediate mode and drift found
6. Submit compliance report if `ReportCompliance: true`

## Pull Server Configuration

```json
{
  "LCM": {
    "ConfigurationSource": "Pull",
    "PullServer": {
      "ServerUrl": "https://pull-server.example.com",
      "RegistrationKey": "shared-secret",
      "NodeId": null,
      "ReportCompliance": true,
      "CertificateSource": "Managed",
      "CertificateRotationInterval": "60.00:00:00",
      "CertificatePath": "{ConfigDir}/certs/client.pfx"
    }
  }
}
```

## Local Testing

```powershell
$env:LCM_ConfigurationPath = "C:\configs\local\main.dsc.yaml"
$env:LCM_ConfigurationMode = "Monitor"
$env:LCM_ConfigurationModeInterval = "00:05:00"
.\artifacts\Lcm\OpenDsc.Lcm.exe
```
