# Resources

OpenDSC resources are command-based [DSC resources] that manage Windows, Linux,
and macOS systems. Each resource represents a single manageable component — a
file, a service, an environment variable — and exposes `get`, `set`, and `test`
operations through the DSC CLI.

!!! note

    This page assumes you have already [installed] the Resources package.

## List available resources

List all OpenDSC resources registered with the DSC CLI:

```sh
dsc resource list OpenDsc*
```

Each row shows the resource type name (e.g. `OpenDsc.Windows/Environment`), the
version, and a short description.

## Get the current state

Use `dsc resource get` to read the current state of a resource. Pass the
resource type and a JSON object describing the instance you want to inspect.

```sh
dsc resource get -r OpenDsc.FileSystem/Directory -i '{"path":"/tmp/demo"}'
```

The output is a JSON object with every property and its current value.

## Test for drift

Use `dsc resource test` to compare the current state against a desired state.
The result includes an `inDesiredState` field that tells you whether the system
matches.

```sh
dsc resource test -r OpenDsc.FileSystem/Directory -i '{"path":"/tmp/demo","exist":true}'
```

## Set the desired state

Use `dsc resource set` to enforce the desired state. DSC applies only the
changes needed to bring the system into compliance.

```sh
dsc resource set -r OpenDsc.FileSystem/Directory -i '{"path":"/tmp/demo","exist":true}'
```

## Configuration documents

Instead of managing resources one at a time, you can declare multiple resources
in a YAML configuration document and apply them together.

```yaml title="main.dsc.yaml"
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources:
  - name: Create demo directory
    type: OpenDsc.FileSystem/Directory
    properties:
      path: /tmp/demo
      exist: true
```

Apply the configuration with `dsc config set`:

```sh
dsc config set -i main.dsc.yaml
```

Test the configuration for drift with `dsc config test`:

```sh
dsc config test -i main.dsc.yaml
```

## Next steps

- Browse the [resource reference] for a full list of available resources and
  their properties.
- Set up the [LCM] to continuously monitor and remediate drift.

[DSC resources]: ../concepts/resources/index.md
[installed]: installation.md#resources
[resource reference]: ../reference/resources/index.md
[LCM]: lcm.md
