# Pull Server

The OpenDSC Pull Server is a central service for distributing DSC configuration
documents, managing node registration, and collecting compliance reports.

## Installation

### with winget <small>recommended</small> { #with-winget data-toc-label="with winget" }

The Pull Server is published as a WinGet package and can be installed on
Windows. Open up a terminal and install the Pull Server with:

<!-- markdownlint-disable MD046 -->

=== "Latest"

    ```powershell
    winget install OpenDsc.Server
    ```

=== "0.x"

    ```powershell
    winget install OpenDsc.Server --version 0.5.1
    ```

### with archive { #with-archive data-toc-label="with archive" }

On Linux and macOS, download the portable archive from [GitHub releases]
and extract it to a directory on your `PATH`.

=== ":fontawesome-brands-linux: Linux"

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

<!-- markdownlint-enable MD046 -->

### with docker { #with-docker data-toc-label="with docker" }

!!! note
    Docker support is coming soon. In the meantime, use the Windows, Linux,
    or macOS install instructions.

  [GitHub releases]: https://github.com/opendsc/opendsc/releases

## Sign in

Open your browser and navigate to [http://localhost:5000][pull-server-url].

![OpenDSC login page][login-page]

Sign in with the default administrator credentials:

| Field    | Value   |
| :------- | :------ |
| Username | `admin` |
| Password | `admin` |

!!! warning
    Change the default password immediately after your first sign-in.

## Create a registration key

Nodes use a registration key to authenticate during their initial registration.
After registration, nodes switch to client certificates for authentication.

1. Navigate to **Settings → Registration Keys**.
2. Click **Create**.
3. Enter a description, such as `Lab registration`.
4. Click **Save**.
5. Copy the key value and save it securely.

![Create registration key][create-registration-key]

## Connect the LCM

Update the LCM configuration on the managed node so it pulls its desired state
from the Pull Server. Replace `<registration-key>` with the key from the
previous step.

```json title="appsettings.json"
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

| Setting | Description |
| :------ | :---------- |
| `ServerUrl` | Address of the Pull Server |
| `RegistrationKey` | Secret used by the node during initial registration |
| `CertificateSource` | `Managed` (auto-provisioned) or `Platform` (enterprise CA) |
| `ReportCompliance` | Send compliance data back to the Pull Server |

See [LCM configuration] for the `appsettings.json` location on each platform.

## Verify the node

Navigate to the **Nodes** page. Your machine should appear with its FQDN and
registration timestamp.

## Upload a configuration

1. Navigate to **Configurations**.
2. Click **Create**.
3. Enter the name `LabConfig`.
4. Set the entry point to `main.dsc.yaml`.
5. Upload the `main.dsc.yaml` file.
6. Click **Save**.

![Upload configuration][upload-configuration]

## Publish the configuration

The configuration is created in `Draft` status. Publish it to make it available
to nodes:

1. On the **Configurations** page, click on `LabConfig`.
2. Click **Publish** next to version `1.0.0`.

![Publish configuration version][publish-configuration-version]

## Assign the configuration

1. Navigate to **Nodes**.
2. Click on your registered node.
3. Under **Configuration**, select `LabConfig`.
4. Click **Save**.

![Assign configuration to node][assign-configuration]

## Next steps

- Learn about [configuration management] concepts such as versioning,
  parameter merging, and scopes.
- Set up [HTTPS] for production deployments.

[LCM]: lcm.md
[LCM configuration]: lcm.md#configure
[configuration management]: ../concepts/pull-server/configuration-management.md
[HTTPS]: ../guides/configure-https.md
[pull-server-url]: http://localhost:5000

[login-page]: media/pull-server-setup/login-page.png
[create-registration-key]: media/pull-server-setup/create-registration-key.png
[upload-configuration]: media/pull-server-setup/upload-configuration.png
[publish-configuration-version]: media/pull-server-setup/publish-version.png
[assign-configuration]: media/pull-server-setup/assign-configuration.png
