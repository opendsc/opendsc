---
description: >-
  OpenDsc Resources provide a standardized interface for managing system state. Learn about
  resource types, naming conventions, and how resources work with DSC v3.
title: OpenDsc Resources
date: 2026-03-27
topic: concept
---

# OpenDsc Resources

OpenDsc provides built-in DSC Resources for managing Windows systems, SQL
Server, and
cross-platform components. Resources are packaged into a single executable per
platform and work
with the standard DSC v3 CLI.

## Resource architecture

Unlike traditional DSC Resources that each ship as a separate executable with
their own resource
manifest, OpenDsc bundles all resources into a single platform-specific
executable. This
executable implements the DSC multi-resource manifest format, allowing DSC to
discover and invoke
all OpenDsc resources through one binary.

The executable is:

- `OpenDsc.Resources.exe` on Windows
- `OpenDsc.Resources` on Linux and macOS

On Windows, the executable includes all Windows-specific, SQL Server, and
cross-platform resources.
On Linux and macOS, it includes only cross-platform and POSIX resources.

## Resource type names

Every OpenDsc resource has a fully qualified type name following this syntax:

```plaintext
OpenDsc.<Area>/<Name>
```

The area component groups related resources by platform or domain:

| Area prefix                | Domain                    | Platform      |
| :------------------------- | :------------------------ | :------------ |
| `OpenDsc.Windows`          | Windows system management | Windows only  |
| `OpenDsc.SqlServer`        | SQL Server management     | All platforms |
| `OpenDsc.FileSystem`       | File and directory ops    | All platforms |
| `OpenDsc.Json`             | JSON file manipulation    | All platforms |
| `OpenDsc.Xml`              | XML file manipulation     | All platforms |
| `OpenDsc.Archive.Zip`      | ZIP archive operations    | All platforms |
| `OpenDsc.Posix.FileSystem` | POSIX file permissions    | Linux, macOS  |

Some resources have a sub-area for further organization:

- `OpenDsc.Windows.FileSystem/AccessControlList` — file system ACL management
- `OpenDsc.Archive.Zip/Compress` and `OpenDsc.Archive.Zip/Expand` — ZIP
  operations

## Resource capabilities

Each resource implements one or more capability interfaces:

| Capability | Operation | Description                                           |
| :--------- | :-------- | :---------------------------------------------------- |
| Get        | `get`     | Retrieve the current state of a resource instance     |
| Set        | `set`     | Apply the desired state to a resource instance        |
| Test       | `test`    | Check whether an instance matches the desired state   |
| Delete     | `delete`  | Remove a resource instance                            |
| Export     | `export`  | Enumerate all instances of the resource on the system |

Not every resource implements all capabilities. Use `dsc resource list` to see
which capabilities
each resource supports.

## Canonical properties

OpenDsc resources use DSC canonical properties to participate in shared
semantics:

- **`_exist`** — indicates whether a resource instance should exist. When
  `_exist` is `false`, DSC
  invokes the Delete operation instead of Set.
- **`_purge`** — controls whether a list-management resource operates in
  additive mode (only add
  specified items) or exact mode (remove items not in the list).
- **`_inDesiredState`** — a read-only property returned by the Test operation
  indicating whether
  the instance is in the desired state.

## Available resources

For a complete list of OpenDsc resources with their properties and examples, see
the
[resource reference][01].

## See also

- [Get started with OpenDsc][02]
- [DSC Resources overview][03] (Microsoft DSC documentation)

<!-- Link references -->
[01]: ../../reference/resources/overview.md
[02]: ../../get-started/index.md
[03]: https://learn.microsoft.com/en-us/powershell/dsc/concepts/resources/overview?view=dsc-3.0
