# Resources

OpenDSC ships a Resources package to manage Windows, Linux, and MacOS.

## Install

<!-- markdownlint-disable MD046 -->

=== ":fontawesome-brands-windows: Windows"

    ```powershell
    winget install OpenDsc.Resources
    ```

=== ":fontawesome-brands-linux: Linux"

    !!! note
        Debian and RPM package support is coming soon. In the meantime, use the archive install below.

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

    !!! note
        Homebrew package support is coming soon. In the meantime, use the archive install below.

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

<!-- markdownlint-enable MD046 -->

## Usage

You can list installed OpenDSC resources with the DSC CLI:

```powershell
dsc resource list OpenDsc*
```
