# Install and configure Local Configuration Manager

Besides providing resources, OpenDSC also comes with a Local Configuration
Manager (LCM), a background service that continuously evaluates a DSC
configuration document against the current state of the system. When drift is
detected the LCM can report it or automatically remediate it.

If you're familiar with PowerShell DSC, the [LCM concept][PSDSC] will be
recognizable.
OpenDSC reintroduces it as a standalone service built around Microsoft DSC v3,
bringing centralized configuration monitoring and remediation to modern
cross-platform environments.

## Installation

<!-- markdownlint-disable MD013 -->
<!-- markdownlint-disable MD033 -->
### with winget <small>recommended</small> { #with-winget data-toc-label="with winget" }
<!-- markdownlint-enable MD013 -->

The LCM is published as a WinGet package and can be installed on
Windows. Open up a terminal and install the LCM with:

<!-- markdownlint-disable MD046 -->

=== "Latest"

    ```powershell
    winget install OpenDsc.Lcm
    ```

=== "0.x"

    ```powershell
    winget install OpenDsc.Lcm --version 0.5.1
    ```

### with archive { #with-archive data-toc-label="with archive" }

On Linux and macOS, download the portable archive from [GitHub releases]
and extract it to a directory on your `PATH`.

=== ":fontawesome-brands-linux: Linux"

    ```sh
    version=$(curl -s https://api.github.com/repos/opendsc/opendsc/releases/latest \
        | grep '"tag_name"' | sed 's/.*"v\(.*\)".*/\1/')
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

    ```sh
    version=$(curl -s https://api.github.com/repos/opendsc/opendsc/releases/latest \
        | grep '"tag_name"' | sed 's/.*"v\(.*\)".*/\1/')
    archive="OpenDSC.Lcm.macOS-$version.zip"
    install_dir="$HOME/OpenDSC.Lcm"
    mkdir -p "$install_dir"
    curl -L -o "$archive" \
        "https://github.com/opendsc/opendsc/releases/download/v$version/$archive"
    unzip -o "$archive" -d "$install_dir"
    export PATH="$install_dir:$PATH"
    echo 'export PATH="$HOME/OpenDSC.Lcm:$PATH"' >> ~/.zshrc
    ```

!!! note
    Debian, RPM, and Homebrew package support is coming soon.

  [GitHub releases]: https://github.com/opendsc/opendsc/releases

## Configure

The LCM reads its settings from `appsettings.json` in a platform-specific
location.

!!! tip
    The LCM automatically hot-reloads configuration changes from
    `appsettings.json`, so updates take effect without restarting the service.

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

Below is a minimal configuration that monitors a local configuration document
every five minutes.

<!-- markdownlint-disable MD040 -->
```json title="appsettings.json"
{
  "LCM": {
    "ConfigurationMode": "Monitor",
    "ConfigurationSource": "Local",
    "ConfigurationPath": "<path-to-your-local-main.dsc.yaml>",
    "ConfigurationModeInterval": "00:05:00"
  }
}
```
<!-- markdownlint-enable MD040 -->

For a full explanation of each setting, see [LCM concepts].

## Manage the service

Once installed, the LCM runs as a background service. Use your platform's
service management tools to check its status, start, stop, or restart it.

=== ":fontawesome-brands-windows: Windows"

    ```powershell
    Get-Service OpenDscLcm
    ```

=== ":fontawesome-brands-linux: Linux"

    !!! note
        Linux service support is coming soon. In the meantime, start and manage
        the LCM process manually.

=== ":fontawesome-brands-apple: macOS"

    !!! note
        macOS service support is coming soon. In the meantime, start and manage
        the LCM process manually.

<!-- markdownlint-enable MD046 -->

<!-- Link reference definitions -->
[PSDSC]: https://learn.microsoft.com/en-us/powershell/dsc/managing-nodes/metaconfig?view=dsc-1.1
[LCM concepts]: ../concepts/lcm/index.md
