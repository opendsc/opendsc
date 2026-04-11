# Pull Server

The OpenDSC Pull Server manages configuration delivery to registered nodes.
It provides a central service for distributing DSC configuration documents,
node registration, and compliance reporting.

## Install

<!-- markdownlint-disable MD046 -->

=== ":fontawesome-brands-windows: Windows"

    ```powershell
    winget install OpenDsc.Server
    ```

=== ":fontawesome-brands-linux: Linux"

    !!! note
        Debian and RPM package support is coming soon. In the meantime, use the
        archive install below.

    ```sh
    version='0.5.1'
    archive="OpenDSC.Server.Linux.$version.tar.gz"
    install_dir="$HOME/OpenDSC.Server"
    mkdir -p "$install_dir"
    curl -L -o "$archive" \
      "https://github.com/opendsc/opendsc/releases/download/v$version/$archive"
    tar -xzf "$archive" -C "$install_dir"
    export PATH="$install_dir:$PATH"
    echo 'export PATH="$HOME/OpenDSC.Server:$PATH"' >> ~/.bashrc
    ```

=== ":fontawesome-brands-apple: macOS"

    !!! note
        Homebrew package support is coming soon. In the meantime, use the
        archive install below.

    ```sh
    version='0.5.1'
    archive="OpenDSC.Server.macOS.$version.tar.gz"
    install_dir="$HOME/OpenDSC.Server"
    mkdir -p "$install_dir"
    curl -L -o "$archive" \
      "https://github.com/opendsc/opendsc/releases/download/v$version/$archive"
    tar -xzf "$archive" -C "$install_dir"
    export PATH="$install_dir:$PATH"
    echo 'export PATH="$HOME/OpenDSC.Server:$PATH"' >> ~/.zshrc
    ```

=== ":fontawesome-brands-docker: Docker"

    !!! note
        Docker support is coming soon. In the meantime, use the Windows, Linux,
        or macOS install instructions.

<!-- markdownlint-enable MD046 -->

## Login

Open your browser and navigate to [http://localhost:5000][pull-server-url].
The web UI prompts you to sign in.

![OpenDSC login page][login-page]

Sign in with the default administrator credentials:

| Field    | Value   |
| :------- | :------ |
| Username | `admin` |
| Password | `admin` |

## Create Registration Key

Nodes need a registration key to authenticate during their initial registration
with the Pull
Server. After registration, nodes use client certificates for authentication.

1. Navigate to **Settings → Registration Keys**.
2. Click **Create**.
3. Enter a description, such as `Lab registration`.
4. Click **Save**.
5. Copy the key value that appears and save it securely.

![Create registration key][create-registration-key]

## Configure LCM

Update the LCM configuration file on the managed node so it pulls its desired
state from the Pull Server. The LCM reads `appsettings.json` from its default
location on each platform.

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

Below is a platform-agnostic example of the LCM configuration for Pull Server
mode. Copy it into the file above and replace `<registration-key>` with the key
generated in the previous step.

```json
{
  "LCM": {
    "PullServer": {
      "ServerUrl": "http://localhost:5000",
      "RegistrationKey": "<registration-key>",
      "CertificateSource": "Managed",
      "ReportCompliance": true
    }
  }
}
```

### Server Url

This is the address of the Pull Server, for example `http://localhost:5000`.
It must be provided when `ConfigurationSource` is set to `Pull`.

### Registration Key

The `RegistrationKey` is the secret used by the node during initial
registration.

### CertificateSource

`CertificateSource` controls how the node obtains its client certificate for
mTLS authentication.

- `Managed` — the LCM provisions and manages the client certificate
automatically.
- `Platform` — the node uses a platform-managed certificate store or
external certificate management. Use this if you already manage certificates
using an enterprise CA.

Default: `Managed`.

### Report Compliance

When `ReportCompliance` is set to `true`, the LCM sends compliance data back to
the Pull Server. If omitted, compliance reporting is not enabled.

Default: `false`.

## Verify Registered Node

Navigate to the **Nodes** page. Your machine should appear with its FQDN and
registration timestamp.

## Configuration Management

### Upload configuration

1. Navigate to **Configurations**.
2. Click **Create**.
3. Enter the name `LabConfig`.
4. Set the entry point to `main.dsc.yaml`.
5. Upload the `main.dsc.yaml` file.
6. Click **Save**.

![Upload configuration][upload-configuration]

### Publish the configuration

The configuration is created in `Draft` status. Publish it to make it available
to nodes:

1. On the **Configurations** page, click on `LabConfig`.
2. Click **Publish** next to version `1.0.0`.

![Publish configuration version][publish-configuration-version]

### Assign the configuration to the node

1. Navigate to **Nodes**.
2. Click on your registered node.
3. Under **Configuration**, select `LabConfig`.
4. Click **Save**.

![Assign configuration to node][assign-configuration]

[pull-server-url]: http://localhost:5000

[login-page]: media/pull-server-setup/login-page.png
[create-registration-key]: media/pull-server-setup/create-registration-key.png
[upload-configuration]: media/pull-server-setup/upload-configuration.png
[publish-configuration-version]: media/pull-server-setup/publish-version.png
[assign-configuration]: media/pull-server-setup/assign-configuration.png
