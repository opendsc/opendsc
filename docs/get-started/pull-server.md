# Setting up Pull Server

Install and configure the Pull Server

In the [LCM guide][LCM], you set up a standalone node that monitors a local
configuration document. That approach works well for individual machines, but
as your environment grows you need a central place to store configurations,
register nodes, and track compliance across the fleet.

The OpenDSC Pull Server fulfills that role. It is an ASP.NET Core service that
distributes DSC configuration documents, manages node registration, and
collects compliance reports. That's why the Pull Server makes it a suitable
candidate for large-scale operations where dozens or hundreds of nodes
need to stay in their desired state.

!!! tip
    If you already want to take a deeper look at the architecture
    and terminology, see the [Pull Server concepts][pull-server-concepts].

## Installation

<!-- markdownlint-disable MD013 -->
<!-- markdownlint-disable MD033 -->
### with winget <small>recommended</small> { #with-winget data-toc-label="with winget" }
<!-- markdownlint-enable MD013 -->
<!-- markdownlint-enable MD033 -->

Just like the resources and LCM, a WinGet package is published and can
be used to install the Pull Server on Windows. Open up a terminal and install
the Pull Server with:

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
    version=$(curl -s https://api.github.com/repos/opendsc/opendsc/releases/latest \
        | grep '"tag_name"' | sed 's/.*"v\(.*\)".*/\1/')
    archive="OpenDSC.Server.Linux-$version.tar.gz"
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
    version=$(curl -s https://api.github.com/repos/opendsc/opendsc/releases/latest \
        | grep '"tag_name"' | sed 's/.*"v\(.*\)".*/\1/')
    archive="OpenDSC.Server.macOS-$version.tar.gz"
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

## Sign in to the admin console

After installation, the Pull Server exposes a REST API and a admin
console on `http://localhost:5000` by default. Open your browser and navigate
to [http://localhost:5000][pull-server-url].

![OpenDSC login page][login-page]

Sign in with the default administrator credentials:

| Field    | Value   |
| :------- | :------ |
| Username | `admin` |
| Password | `admin` |

!!! warning
    After signing in for the first time, you have to change the
    default password immediately.

## Create a registration key

Before connecting a node to the Pull Server, you need a registration key. The
LCM presents this key during its initial registration so the Pull Server knows
the node is authorized. After registration, communication switches to client
certificates automatically. See [authentication concepts][authentication] for
details on the certificate lifecycle.

1. Navigate to **Settings → Registration Keys**.
2. Click **Create**.
3. Enter a description, such as `Register nodes`.
4. Click **Save**.
5. Copy the key value and save it securely.

![Create registration key][create-registration-key]

You will need this key in the next step when
[registering a node][register-node].

[LCM]: lcm.md
[pull-server-concepts]: ../concepts/pull-server/index.md
[authentication]: ../concepts/pull-server/authentication.md
[register-node]: register-node.md
[pull-server-url]: http://localhost:5000

[login-page]: media/pull-server-setup/login-page.png
[create-registration-key]: media/pull-server-setup/create-registration-key.png
