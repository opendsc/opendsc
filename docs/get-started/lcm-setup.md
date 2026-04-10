# Tutorial: Set up the LCM

The Local Configuration Manager (LCM) is a background service that continuously
evaluates a DSC
configuration document against the current state of the system. This tutorial
walks you through
installing the LCM, configuring it in local mode, and observing drift detection.

## Prerequisites

- A Windows machine with [OpenDSC installed][01].
- [DSC v3][02] version `3.0.0` or later.
- PowerShell 7 or later.
- Administrator privileges.

## Step 1: Install the LCM

Download and install the LCM MSI:

```powershell
$version = '0.5.1'
Invoke-WebRequest "https://github.com/opendsc/opendsc/releases/download/v$version/OpenDSC.Lcm-$version.msi" `
    -OutFile "$env:TEMP\OpenDSC.Lcm-$version.msi"
Start-Process msiexec.exe -Wait -ArgumentList "/i $env:TEMP\OpenDSC.Lcm-$version.msi"
```

The installer places files under `C:\Program Files\OpenDSC\LCM` and registers a
Windows service
named **OpenDscLcm**.

## Step 2: Create a configuration document

Create a DSC configuration document that the LCM will monitor. Save the
following as
`C:\DSC\main.dsc.yaml`:

```powershell
New-Item -ItemType Directory -Path 'C:\DSC' -Force

@'
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Ensure greeting variable
    type: OpenDsc.Windows/Environment
    properties:
      name: DSC_GREETING
      value: Hello from OpenDSC
      scope: User
'@ | Set-Content -Path 'C:\DSC\main.dsc.yaml' -Encoding UTF8
```

## Step 3: Configure the LCM in local mode

Create the LCM configuration file. The LCM reads settings from
`%ProgramData%\OpenDSC\LCM\appsettings.json`:

```powershell
$configPath = "$env:ProgramData\OpenDSC\LCM\appsettings.json"
New-Item -ItemType Directory -Path (Split-Path $configPath) -Force

@{
    LCM = @{
        ConfigurationMode         = 'Monitor'
        ConfigurationSource       = 'Local'
        ConfigurationPath         = 'C:\DSC\main.dsc.yaml'
        ConfigurationModeInterval = '00:05:00'
    }
} | ConvertTo-Json -Depth 5 | Set-Content -Path $configPath -Encoding UTF8
```

This configures the LCM to:

- Operate in **Monitor** mode (detect drift, don't fix it).
- Read the configuration from a local file.
- Check every 5 minutes.

## Step 4: Start the LCM service

```powershell
Restart-Service OpenDscLcm
```

The LCM begins monitoring immediately. Check the Windows Event Log or the
service logs to see the
test results.

## Step 5: Observe drift detection

The configuration expects the `DSC_GREETING` environment variable to be set.
Since it doesn't
exist yet, the LCM reports drift.

Apply the configuration manually first, then modify the variable to observe the
LCM detecting
the change:

```powershell
# Apply the configuration so the variable exists
dsc config set --file 'C:\DSC\main.dsc.yaml'

# Verify it's set
[System.Environment]::GetEnvironmentVariable('DSC_GREETING', 'User')
```

Now change the variable to a different value:

```powershell
[System.Environment]::SetEnvironmentVariable('DSC_GREETING', 'Modified value', 'User')
```

Wait for the next LCM check interval (or restart the service to trigger
immediately). The LCM
reports that the system has drifted from the desired state.

## Step 6: Switch to Remediate mode

To have the LCM automatically correct drift, change the configuration mode:

```powershell
$configPath = "$env:ProgramData\OpenDSC\LCM\appsettings.json"
$config = Get-Content $configPath | ConvertFrom-Json

$config.LCM.ConfigurationMode = 'Remediate'

$config | ConvertTo-Json -Depth 5 | Set-Content -Path $configPath -Encoding UTF8
```

The LCM detects configuration changes and switches modes without a service
restart. On the next
evaluation cycle, the LCM runs `dsc config set` and restores the environment
variable to its
desired value.

Verify:

```powershell
# Wait for the next interval or restart the service
Restart-Service OpenDscLcm
Start-Sleep -Seconds 30

[System.Environment]::GetEnvironmentVariable('DSC_GREETING', 'User')
```

The output should show `Hello from OpenDSC`.

## Step 7: Clean up

```powershell
# Remove the test variable
dsc resource delete -r OpenDsc.Windows/Environment --input '{"name":"DSC_GREETING","scope":"User"}'

# Stop the LCM service
Stop-Service OpenDscLcm

# Remove the configuration
Remove-Item -Path 'C:\DSC' -Recurse -Force
```

## LCM configuration reference

| Setting                     | Description                                   | Default         |
| :-------------------------- | :-------------------------------------------- | :-------------- |
| `ConfigurationMode`         | `Monitor` or `Remediate`                      | `Monitor`       |
| `ConfigurationSource`       | `Local` or `Pull`                             | `Local`         |
| `ConfigurationPath`         | Path to the configuration document            | —               |
| `ConfigurationModeInterval` | How often the LCM evaluates the configuration | `00:15:00`      |
| `DscExecutablePath`         | Path to the `dsc` CLI executable              | `dsc` (in PATH) |

For Pull mode configuration, see the [Pull Server setup tutorial][03].

## Next steps

- Configure the LCM for [Pull mode][03] with the Pull Server.
- Learn about [LCM concepts][04] including certificate management and compliance
  reporting.
- Browse the [resource reference][05] for available resources.

<!-- Link references -->
[01]: ../installing.md
[02]: https://learn.microsoft.com/powershell/dsc/install
[03]: pull-server-setup.md
[04]: ../concepts/lcm/overview.md
[05]: ../reference/resources/overview.md
