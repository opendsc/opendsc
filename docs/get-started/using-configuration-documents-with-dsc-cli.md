# Using configuration documents with DSC CLI

After [running your first resource], you know how to manage individual resources
with `get`, `test`, and `set`. Configuration documents let you declare multiple
resources together and apply them as a single unit using the [DSC CLI].

OpenDSC resources integrate seamlessly with the DSC CLI because they ship with
a generated [DSC resource manifest], a JSON file that tells the DSC CLI how to
discover, invoke, and validate each resource. No additional registration or
configuration is needed.

!!! note
    This page assumes you have the [DSC CLI] installed.

## Discovering resources through the DSC CLI

Before writing a configuration document, first verify that the DSC CLI can
discover your OpenDSC resources:

```sh
dsc resource list OpenDsc*
```

Each row shows the resource type name, the version, and a short description.
To see where the DSC CLI reads the resource manifest from, run:

```powershell
dsc resource list |
    ConvertFrom-Json |
    Where-Object -Property type -EQ 'OpenDsc.FileSystem/Directory' |
    Select-Object -ExpandProperty path
```

Browse the list to find the resources you want to use in your configuration.
The next section will use the `OpenDsc.FileSystem/Directory` in a configuration
document.

!!! note
    If you installed OpenDSC resources using the MSI from the WinGet package,
    the executable should be added automatically to your `PATH` and the
    DSC CLI can discover the resources without any additional configuration.

## Writing a configuration document

A configuration document is a YAML file that describes the desired state of one
or more resources. Create a file called `filesystem.dsc.yaml`:

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```yaml title="filesystem.dsc.yaml"
    $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
    resources:
      - name: Create demo directory
        type: OpenDsc.FileSystem/Directory
        properties:
          path: C:\temp\demo
          exist: true
    ```

=== "Shell"

    ```yaml title="filesystem.dsc.yaml"
    $schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
    resources:
      - name: Create demo directory
        type: OpenDsc.FileSystem/Directory
        properties:
          path: /tmp/demo
          exist: true
    ```

Each entry in `resources` specifies a `name`, the resource `type`, and the
desired `properties`.

## Applying the configuration

Use `dsc config set` to apply the entire configuration at once:

=== "PowerShell"

    ```powershell
    dsc config set --file filesystem.dsc.yaml
    ```

=== "Shell"

    ```sh
    cat filesystem.dsc.yaml | dsc config set -f -
    ```

DSC evaluates every resource in the document and applies only the changes
needed to bring the system into the desired state.

## Testing for drift

Use `dsc config test` to check whether the system still matches the
configuration:

=== "PowerShell"

    ```powershell
    dsc config test --file filesystem.dsc.yaml
    ```

=== "Shell"

    ```sh
    cat filesystem.dsc.yaml | dsc config test -f -
    ```

The output shows the `inDesiredState` result for each resource in the document.

## Getting current state

Use `dsc config get` to read the current state of every resource in the
document:

=== "PowerShell"

    ```powershell
    dsc config get --file filesystem.dsc.yaml
    ```

=== "Shell"

    ```sh
    cat filesystem.dsc.yaml | dsc config get -f -
    ```

<!-- markdownlint-enable MD046 -->

!!! tip
    Configuration documents are useful for repeatable, version-controlled
    infrastructure. Keep them in source control alongside your application code.

[running your first resource]: running-your-first-resource.md
[DSC CLI]: https://learn.microsoft.com/en-us/powershell/dsc/overview?view=dsc-3.0
[DSC resource manifest]: https://learn.microsoft.com/en-us/powershell/dsc/glossary?view=dsc-3.0#dsc-resource-manifest
