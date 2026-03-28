---
description: >-
  Export all current instances of an OpenDsc resource to discover existing system state and
  generate baseline configuration documents.
title: "How to: Export resource instances"
date: 2026-03-27
topic: how-to
---

# Export resource instances

Some OpenDsc resources support the **Export** operation, which enumerates all
current instances of
that resource on the system. This is useful for discovering existing state and
generating baseline
configurations.

## When to use this guide

Use export when you need to:

- Discover all environment variables, users, groups, or other resources on a
  system.
- Generate a baseline configuration document from existing state.
- Audit what's currently configured on a machine.

## Check export capability

Not all resources support export. Check the capabilities column in the resource
list:

```powershell
dsc resource list OpenDsc* | ConvertFrom-Json |
    Where-Object { $_.capabilities -match 'e' } |
    Select-Object type, capabilities
```

The `e` flag in the capabilities column indicates export support.

## Export all instances

Use `dsc resource export` to retrieve all instances:

```powershell
dsc resource export -r OpenDsc.Windows/Environment
```

The output contains every instance of the resource as a configuration document:

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: 'Environment: PATH (Machine)'
    type: OpenDsc.Windows/Environment
    properties:
      name: PATH
      value: C:\Windows\system32;C:\Windows;...
      scope: Machine
  - name: 'Environment: TEMP (User)'
    type: OpenDsc.Windows/Environment
    properties:
      name: TEMP
      value: C:\Users\admin\AppData\Local\Temp
      scope: User
```

## Save export output to a file

Redirect the output to a file to create a baseline configuration:

```powershell
dsc resource export -r OpenDsc.Windows/Environment > baseline-environment.dsc.yaml
```

## Export from multiple resources

Use `dsc config export` with a configuration document that lists the resources
you want to export:

```yaml
# export-template.dsc.yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: All environment variables
    type: OpenDsc.Windows/Environment
    properties: {}
  - name: All local users
    type: OpenDsc.Windows/User
    properties: {}
```

```powershell
dsc config export --file export-template.dsc.yaml > baseline.dsc.yaml
```

## See also

- [Resource operations concepts][01]
- [Resource reference][02]

<!-- Link references -->
[01]: ../concepts/resources/operations.md
[02]: ../reference/resources/overview.md
