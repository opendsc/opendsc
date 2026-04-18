# Getting started

OpenDSC consists of a suite of components that can be installed
independently depending on your needs:

| Component                       | Purpose                                                                    |
|:--------------------------------|:---------------------------------------------------------------------------|
| **Resources**                   | Command-based DSC resources for managing Windows, Linux, and macOS systems |
| **Local Configuration Manager** | Background service that monitors and remediates configuration drift        |
| **Pull Server**                 | Central service for distributing configurations to managed nodes           |

If you are getting started with OpenDSC and Microsoft DSC, start by
exploring individual command-based DSC resources before setting up
the Local Configuration Manager (LCM) or a fully-fledged Pull Server.

## Installation

OpenDSC resources are published in various package formats and can be installed
on Windows, Linux, and macOS systems. Choose the installation method depending
on the system you're working on.

<!-- markdownlint-disable MD013 -->
<!-- markdownlint-disable MD033 -->
### with winget <small>recommended</small> { #with-winget data-toc-label="with winget" }
<!-- markdownlint-enable MD013 -->

OpenDSC resources are published as a [WinGet package] and can be installed on
Windows. Open up a terminal and install resources with:

<!-- markdownlint-disable MD046 -->

=== "Latest"

    ```powershell
    winget install OpenDsc.Resources
    ```

=== "0.x"

    ```powershell
    winget install OpenDsc.Resources --version 0.5.1
    ```

!!! tip
    You can also install the `OpenDsc.Resources.Portable` portable package.

### with archive { #with-archive data-toc-label="with archive" }

On Linux and macOS, download the latest portable archive from [GitHub releases]
and extract it to a directory on your `PATH`.

=== ":fontawesome-brands-linux: Linux"

    ```sh
    latest_tag=$(curl -s https://api.github.com/repos/opendsc/opendsc/releases/latest | grep -oP '"tag_name":\s*"\K(.*?)(?=")')
    latest_version=${latest_tag#v}
    archive=OpenDSC.Resources.Linux.Portable-${latest_version}.zip
    install_dir="$HOME/OpenDSC.Resources"

    mkdir -p "$install_dir"
    curl -L -o "$archive" \
      "https://github.com/opendsc/opendsc/releases/download/${latest_tag}/${archive}"
    unzip -o "$archive" -d "$install_dir"
    export PATH="$install_dir:$PATH"
    echo 'export PATH="$HOME/OpenDSC.Resources:$PATH"' >> ~/.bashrc
    ```

=== ":fontawesome-brands-apple: macOS"

    ```sh
    latest_tag=$(curl -s https://api.github.com/repos/opendsc/opendsc/releases/latest | grep -oP '"tag_name":\s*"\K(.*?)(?=")')
    latest_version=${latest_tag#v}
    archive=OpenDSC.Resources.macOS.Portable-${latest_version}.zip
    install_dir="$HOME/OpenDSC.Resources"

    mkdir -p "$install_dir"
    curl -L -o "$archive" \
      "https://github.com/opendsc/opendsc/releases/download/${latest_tag}/${archive}"
    unzip -o "$archive" -d "$install_dir"
    export PATH="$install_dir:$PATH"
    echo 'export PATH="$HOME/OpenDSC.Resources:$PATH"' >> ~/.zshrc
    ```

!!! note
    Debian, RPM, and Homebrew package support is coming soon.

<!-- markdownlint-enable MD046 -->

<!-- Link reference definitions -->
[WinGet package]: https://github.com/microsoft/winget-pkgs
[GitHub releases]: https://github.com/opendsc/opendsc/releases
