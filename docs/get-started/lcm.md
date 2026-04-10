# Local Configuration Manager

The Local Configuration Manager (LCM) is a background service that continuously
evaluates a DSC configuration document against the current state of the system.
This tutorial walks you through installing the LCM, configuring it in local
mode, and observing drift detection.

## Install

<!-- markdownlint-disable MD046 -->

=== ":fontawesome-brands-windows: Windows"

    ```powershell
    $version = '0.5.1'
    Invoke-WebRequest "https://github.com/opendsc/opendsc/releases/download/v$version/OpenDSC.Lcm-$version.msi" `
        -OutFile "$env:TEMP\OpenDSC.Lcm-$version.msi"
    Start-Process msiexec.exe -Wait -ArgumentList "/i $env:TEMP\OpenDSC.Lcm-$version.msi"
    ```

=== ":fontawesome-brands-linux: Linux"

    !!! note
        Debian and RPM package support is coming soon. In the meantime, use the archive install below.

    ```sh
    version='0.5.1'
    archive="OpenDSC.Lcm.Linux-$version.zip"
    install_dir="$HOME/OpenDSC.Lcm"
    mkdir -p "$install_dir"
    curl -L -o "$archive" \
      "https://github.com/opendsc/opendsc/releases/download/v$version/$archive"
    unzip -o "$archive" -d "$install_dir"
    export PATH="$install_dir:$PATH"
    echo 'export PATH="$HOME/OpenDSC.Lcm:$PATH"' >> ~/.bashrc
    ```

=== ":fontawesome-brands-apple: macOS"

    !!! note
        Homebrew package support is coming soon. In the meantime, use the archive install below.

    ```sh
    version='0.5.1'
    archive="OpenDSC.Lcm.macOS-$version.zip"
    install_dir="$HOME/OpenDSC.Lcm"
    mkdir -p "$install_dir"
    curl -L -o "$archive" \
      "https://github.com/opendsc/opendsc/releases/download/v$version/$archive"
    unzip -o "$archive" -d "$install_dir"
    export PATH="$install_dir:$PATH"
    echo 'export PATH="$HOME/OpenDSC.Lcm:$PATH"' >> ~/.zshrc
    ```

<!-- markdownlint-enable MD046 -->

## Configure

Create or update the LCM configuration file in the platform-specific default
location.

!!! note
    The LCM automatically hot-reloads configuration changes from
    `appsettings.json`, so updates take effect without restarting the service.

Each tab below includes the `appsettings.json` path.

<!-- markdownlint-disable MD046 -->

=== ":fontawesome-brands-windows: Windows"

    ```text
    $env:ProgramData\OpenDSC\LCM\appsettings.json
    ```

=== ":fontawesome-brands-linux: Linux"

    ```text
    /etc/opendsc/lcm/appsettings.json
    ```

=== ":fontawesome-brands-apple: macOS"

    ```text
    /Library/Preferences/OpenDSC/LCM/appsettings.json
    ```

<!-- markdownlint-enable MD046 -->

Below is a platform-agnostic example of an `appsettings.json` file for the LCM.
Use this as a starting point and update `ConfigurationPath` if you choose a
custom local configuration location.

!!! warning
    Use a `ConfigurationPath` that only administrators can write to. This
    prevents unauthorized modifications to the configuration document.

```json
{
  "LCM": {
    "ConfigurationMode": "Monitor",
    "ConfigurationSource": "Local",
    "ConfigurationPath": "<path-to-your-local-main.dsc.yaml>",
    "ConfigurationModeInterval": "00:05:00"
  }
}
```

### Configuration Mode

`ConfigurationMode` controls how the LCM responds when the system state differs
from the configuration document.

It has two values:

- `Monitor` — the LCM checks the configuration and reports drift, but does not
  change anything.
- `Remediate` — the LCM checks the configuration and automatically applies the
  configuration when drift is detected.

Start with `Monitor` first to verify that your configuration is correct.
Once you are comfortable with the behavior, switch to `Remediate` so the LCM can
keep the system in the desired state automatically.

The LCM detects the change and switches modes without requiring a service
restart. On the next evaluation cycle, it will automatically remediate any drift
it finds.

The default is `Monitor`.

### Configuration Source

`ConfigurationSource` determines where the LCM gets its configuration.

- `Local` — the LCM reads a file from disk.
- `Pull` — the LCM gets the configuration from a pull server.

The default is `Local` when using a file-based configuration document.

For `Pull` source configuration, see [Pull Server].

### Configuration Path

`ConfigurationPath` is the full path to the configuration document the LCM
should evaluate. When using the default local setup, this typically points to
`main.dsc.yaml` under the platform-specific local configuration folder.

<!-- markdownlint-disable MD046 -->

=== ":fontawesome-brands-windows: Windows"

    The default is `$env:ProgramData\OpenDSC\LCM\config\local\main.dsc.yaml`.

=== ":fontawesome-brands-linux: Linux"

    The default is `/etc/opendsc/lcm/config/local/main.dsc.yaml`.

=== ":fontawesome-brands-apple: macOS"

    The default is `/Library/Preferences/OpenDSC/LCM/config/local/main.dsc.yaml`.

<!-- markdownlint-enable MD046 -->

### Configuration Mode Interval

`ConfigurationModeInterval` sets how often the LCM evaluates the configuration.
It is expressed as a TimeSpan string such as `00:15:00` for 15 minutes.

### DSC Executable Path

`DscExecutablePath` specifies the path to the `dsc` CLI executable. If not set,
it defaults to `dsc` on the system `PATH`.

## Service

<!-- markdownlint-disable MD046 -->

=== ":fontawesome-brands-windows: Windows"

    ```powershell
    Get-Service OpenDscLcm
    ```

=== ":fontawesome-brands-linux: Linux"

    !!! note
        Linux service support is coming soon. In the meantime, start and manage
        the OpenDSC LCM process manually.

    ```sh
    # Start or monitor the OpenDsc.Lcm process manually until service support is available.
    ```

=== ":fontawesome-brands-apple: macOS"

    !!! note
        macOS service support is coming soon. In the meantime, start and manage
        the OpenDSC LCM process manually.

    ```sh
    # Start or monitor the OpenDsc.Lcm process manually until service support is available.
    ```

<!-- markdownlint-enable MD046 -->

[Pull Server]: pull-server.md
