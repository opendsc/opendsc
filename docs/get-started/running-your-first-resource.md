# Running your first resource

After you've [installed] OpenDSC resources, you can start discover and manage
your system right away. Go to a PowerShell terminal and discover which
resources are available:

```powershell
(OpenDsc.Resources manifest | 
    ConvertFrom-Json).resources |
    Select-Object -ExpandProperty type
```

This lists every resource type shipped with the package, for example
`OpenDsc.FileSystem/Directory`, `OpenDsc.Windows/Environment`, and many more.

## Reading current state

The simplest way to use a resource is to read the current state of a system
component. Use `OpenDsc.Resources get` and pass the resource type and a JSON
object describing what you want to inspect:

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $jsonInput = @{ 
        path = 'C:\temp\demo' 
    } | ConvertTo-Json
    OpenDsc.Resources get -r OpenDsc.FileSystem/Directory -i $jsonInput
    ```

=== "Shell"

    ```sh
    OpenDsc.Resources get -r OpenDsc.FileSystem/Directory -i '{"path":"/tmp/demo"}'
    ```

The output is a JSON object with every property and its current value. If the
directory does not exist yet, the `exist` property is `false`.

## Testing for drift

Use `OpenDsc.Resources test` to compare the current state against a desired
state. The result includes an `inDesiredState` field that tells you whether the
system matches:

=== "PowerShell"

    ```powershell
    $jsonInput = @{ 
        path  = 'C:\temp\demo'
        exist = $true 
    } | ConvertTo-Json
    OpenDsc.Resources test -r OpenDsc.FileSystem/Directory -i $jsonInput
    ```

=== "Shell"

    ```sh
    OpenDsc.Resources test -r OpenDsc.FileSystem/Directory -i '{"path":"/tmp/demo","exist":true}'
    ```

If the directory does not exist yet, `inDesiredState` is `false`. Once the
directory is created, `inDesiredState` returns `true`.

## Applying desired state

If `inDesiredState` was `false`, you can bring the system into compliance using
`OpenDsc.Resources set`. This applies only the changes needed — in this case,
creating the directory:

=== "PowerShell"

    ```powershell
    $jsonInput = @{ 
        path  = 'C:\temp\demo'
        exist = $true 
    } | ConvertTo-Json
    OpenDsc.Resources set -r OpenDsc.FileSystem/Directory -i $jsonInput
    ```

=== "Shell"

    ```sh
    OpenDsc.Resources set -r OpenDsc.FileSystem/Directory -i '{"path":"/tmp/demo","exist":true}'
    ```

!!! tip
    Run `OpenDsc.Resources test` again after applying. You should notice
    `inDesiredState` is now `true`.

<!-- markdownlint-enable MD046 -->

The **get**, **test**, and **set** operations are core concepts inherited from
[PowerShell DSC][get-test-set]. Microsoft DSC extends this model with
additional operations like `export`, but get, test, and set remain the
building blocks for managing desired state.

<!-- Link reference definitions -->
[installed]: installation.md
[get-test-set]: https://learn.microsoft.com/en-us/powershell/dsc/concepts/get-test-set?view=dsc-2.0
