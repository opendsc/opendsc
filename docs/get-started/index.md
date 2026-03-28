---
description: >-
  Get started with OpenDsc by learning how to discover resources, manage state with individual
  resources, and build configuration documents.
title: Get started with OpenDsc
date: 2026-03-27
topic: tutorial
---

# Get started with OpenDsc

OpenDsc provides built-in DSC Resources for managing Windows systems, SQL
Server, and
cross-platform components. This tutorial walks you through discovering
resources, using them
individually, and composing them into a configuration document.

## Prerequisites

- [Install OpenDsc][01] on a Windows machine.
- [Install DSC v3][02] version `3.1.0` or later.
- A terminal emulator, like [Windows Terminal][03].

## Discover resources

OpenDsc resources are discovered automatically by the DSC CLI. Run the following
command to list
all available OpenDsc resources:

```powershell
dsc resource list OpenDsc*
```

The output shows each resource with its type name, version, capabilities, and
description:

```plaintext
Type                                         Kind      Version  Capabilities  Description
------------------------------------------------------------------------------------------------------------
OpenDsc.Windows/Environment                  Resource  0.1.0    gsx-td--      Manage Windows environment...
OpenDsc.Windows/Service                      Resource  0.1.0    gs--td--      Manage Windows services
OpenDsc.Windows/Group                        Resource  0.1.0    gsx-tde-      Manage local Windows groups
OpenDsc.Windows/User                         Resource  0.1.0    gsx-tde-      Manage local Windows user...
OpenDsc.FileSystem/File                      Resource  0.1.0    gsx-td--      Manage files
OpenDsc.SqlServer/Database                   Resource  0.1.0    gsx-tde-      Manage SQL Server databases
...
```

You can filter the list to a specific area. For example, to list only Windows
resources:

```powershell
dsc resource list OpenDsc.Windows*
```

## Get the current state of a resource

Use `dsc resource get` to retrieve the current state of a resource instance. For
example, to
check the value of the `PATH` environment variable:

```powershell
dsc resource get -r OpenDsc.Windows/Environment --input '{"name":"PATH","scope":"Machine"}'
```

The output shows the actual state of the resource instance:

```yaml
actualState:
  name: PATH
  value: C:\Windows\system32;C:\Windows;...
  scope: Machine
```

If a resource instance doesn't exist, the `_exist` property is `false`:

```powershell
dsc resource get -r OpenDsc.Windows/Environment --input '{"name":"DOES_NOT_EXIST"}'
```

```yaml
actualState:
  name: DOES_NOT_EXIST
  _exist: false
```

## Set the desired state

Use `dsc resource set` to create or update a resource instance. The following
command creates a
user-scoped environment variable:

```powershell
dsc resource set -r OpenDsc.Windows/Environment --input '{
  "name": "DSC_GREETING",
  "value": "Hello from OpenDsc",
  "scope": "User"
}'
```

Verify that the variable was created:

```powershell
dsc resource get -r OpenDsc.Windows/Environment --input '{"name":"DSC_GREETING","scope":"User"}'
```

## Delete a resource instance

Use `dsc resource delete` to remove a resource instance:

```powershell
dsc resource delete -r OpenDsc.Windows/Environment --input '{"name":"DSC_GREETING","scope":"User"}'
```

Verify the deletion:

```powershell
dsc resource get -r OpenDsc.Windows/Environment --input '{"name":"DSC_GREETING","scope":"User"}'
```

```yaml
actualState:
  name: DSC_GREETING
  scope: User
  _exist: false
```

## Create a configuration document

A configuration document declares the desired state for multiple resource
instances. Create a file
called `example.dsc.yaml` with the following content:

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Set greeting variable
    type: OpenDsc.Windows/Environment
    properties:
      name: DSC_GREETING
      value: Hello from OpenDsc
      scope: User

  - name: Set team variable
    type: OpenDsc.Windows/Environment
    properties:
      name: DSC_TEAM
      value: Platform Engineering
      scope: User
```

### Test the configuration

Run `dsc config test` to check whether the system matches the desired state:

```powershell
dsc config test --file example.dsc.yaml
```

The output reports whether each resource instance is in the desired state.

### Apply the configuration

Run `dsc config set` to enforce the desired state:

```powershell
dsc config set --file example.dsc.yaml
```

### Clean up

Remove the environment variables created by this tutorial:

```powershell
dsc resource delete -r OpenDsc.Windows/Environment --input '{"name":"DSC_GREETING","scope":"User"}'
dsc resource delete -r OpenDsc.Windows/Environment --input '{"name":"DSC_TEAM","scope":"User"}'
```

## Next steps

- Set up the [Pull Server][04] for centralized configuration management.
- Configure the [LCM][05] for continuous monitoring.
- Browse the [resource reference][06] for all available resources.

<!-- Link references -->
[01]: ../installing.md
[02]: https://learn.microsoft.com/en-us/powershell/dsc/overview?view=dsc-3.0#installation
[03]: https://aka.ms/terminal
[04]: pull-server-setup.md
[05]: lcm-setup.md
[06]: ../reference/resources/overview.md
