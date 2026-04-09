# Resource operations

OpenDsc Resources support the standard DSC v3 operations. Each operation serves
a specific purpose
in the configuration management lifecycle.

## Get

The **Get** operation retrieves the current (actual) state of a resource
instance. It returns the
instance properties as they currently exist on the system.

If the resource instance doesn't exist, the Get operation returns the key
properties with
`_exist` set to `false`.

```powershell
dsc resource get -r OpenDsc.Windows/Environment --input '{"name":"PATH","scope":"Machine"}'
```

## Set

The **Set** operation applies the desired state to a resource instance. It
creates the instance if
it doesn't exist, or updates it to match the desired state.

The DSC engine routes the Set operation based on the `_exist` canonical
property:

- When `_exist` is `true` (the default), DSC calls the Set operation.
- When `_exist` is `false`, DSC calls the Delete operation instead.

```powershell
dsc resource set -r OpenDsc.Windows/Environment --input '{
  "name": "DSC_EXAMPLE",
  "value": "Hello",
  "scope": "User"
}'
```

## Test

The **Test** operation checks whether a resource instance matches its desired
state. DSC performs
a synthetic test by comparing the output of Get against the desired state when
the resource
doesn't implement a custom test.

```powershell
dsc resource test -r OpenDsc.Windows/Environment --input '{
  "name": "DSC_EXAMPLE",
  "value": "Hello",
  "scope": "User"
}'
```

## Delete

The **Delete** operation removes a resource instance from the system. DSC
invokes Delete
automatically when `_exist` is `false` and the resource has the `delete`
capability.

```powershell
dsc resource delete -r OpenDsc.Windows/Environment --input '{"name":"DSC_EXAMPLE","scope":"User"}'
```

## Export

The **Export** operation enumerates all instances of a resource on the system.
It returns a
configuration document containing every discovered instance.

```powershell
dsc resource export -r OpenDsc.Windows/Environment
```

## Operation routing with `_exist` and `_purge`

OpenDsc resources follow specific patterns based on their canonical properties:

### Instance management (`_exist`)

Resources that manage the lifecycle of discrete instances (users, groups, files,
services) use
the `_exist` property. DSC calls Set when the instance should exist and Delete
when it shouldn't.

### List management (`_purge`)

Resources that manage collections within an existing container (ACL rules, user
rights) use the
`_purge` property. When `_purge` is `false` (default), only items in the desired
list are added.
When `_purge` is `true`, items not in the desired list are removed.

### Hybrid management (`_exist` + `_purge`)

Resources that manage both a container and its contents (a group with members)
use both
properties. `_exist` controls the container and `_purge` controls the list items
within it.

## See also

- [OpenDsc Resources overview][01]
- [Resource reference][02]

<!-- Link references -->
[01]: overview.md
[02]: ../../reference/resources/overview.md
