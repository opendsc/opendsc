# OpenDsc.SqlServer/AvailabilityGroup

## Synopsis

Manage SQL Server Always On Availability Groups.

## Description

The `OpenDsc.SqlServer/AvailabilityGroup` resource enables you to manage
SQL Server Always On Availability Groups including creation, configuration,
and deletion. You can configure availability group options such as automated
backup preference, failure condition level, health check timeout, and
cluster type.

This resource manages the availability group object itself. Management of
availability replicas and availability databases within the group is not
yet supported and is planned for a future release.

## Requirements

- SQL Server instance with Always On Availability Groups (HADR) enabled
- Appropriate SQL Server permissions (typically sysadmin role membership)
- For WSFC cluster type: Windows Server Failover Clustering configured
- For NONE cluster type: SQL Server 2017 or later

## Capabilities

The resource has the following capabilities:

- `get` - Retrieve the current state of an availability group
- `set` - Create or update an availability group
- `delete` - Remove an availability group
- `export` - List all availability groups on the server

## Properties

### Required Properties

- **serverInstance** (string) - The name of the SQL Server instance to connect
  to. Use `.` or `(local)` for the default local instance, or
  `servername\instancename` for named instances.
- **name** (string) - The name of the availability group.

### Optional Properties (Write-Only)

- **connectUsername** (string) - The username for SQL Server authentication.
  If not specified, Windows Authentication is used.
- **connectPassword** (string) - The password for SQL Server authentication.
  Required when `connectUsername` is specified.

### Optional Properties (Configurable)

- **automatedBackupPreference** (enum) - The automated backup preference.
  Valid values: `Primary`, `SecondaryOnly`, `Secondary`, `None`.
- **failureConditionLevel** (enum) - The failure condition level that triggers
  automatic failover. Valid values: `OnServerDown`,
  `OnServerUnresponsive`, `OnCriticalServerErrors`,
  `OnModerateServerErrors`, `OnAnyQualifiedFailureCondition`.
- **healthCheckTimeout** (integer) - The health check timeout value in
  milliseconds. Minimum: `0`.
- **databaseHealthTrigger** (boolean) - Whether database-level health
  detection is enabled.
- **dtcSupportEnabled** (boolean) - Whether DTC support is enabled.
- **requiredSynchronizedSecondariesToCommit** (integer) - The number of
  required synchronized secondaries to commit. Minimum: `0`.
- **_exist** (boolean) - Indicates whether the availability group should
  exist. Default: `true`.

### Optional Properties (Create-Only)

These properties can only be set when creating a new availability group.
Changes to these properties on an existing availability group are ignored.

- **basicAvailabilityGroup** (boolean) - Whether this is a basic availability
  group (limited to two replicas and one database).
- **clusterType** (enum) - The cluster type. Valid values: `Wsfc`, `None`,
  `External`.
- **isContained** (boolean) - Whether the availability group is contained.

### Read-Only Properties

- **databases** (string[]) - The databases participating in the availability
  group.
- **primaryReplicaServerName** (string) - The name of the server that is the
  current primary replica.
- **localReplicaRole** (enum) - The role of the local replica.
- **uniqueId** (guid) - The unique identifier of the availability group.
- **isDistributedAvailabilityGroup** (boolean) - Whether this is a
  distributed availability group.

## Examples

### Get Availability Group

Retrieve the current state of an availability group:

```yaml
serverInstance: .
name: MyAG
```

```powershell
dsc resource get -r OpenDsc.SqlServer/AvailabilityGroup --input '{"serverInstance":".","name":"MyAG"}'
```

### Create Availability Group

Create a new availability group with cluster type None:

```yaml
serverInstance: .
name: MyAG
clusterType: None
```

```powershell
dsc resource set -r OpenDsc.SqlServer/AvailabilityGroup --input '{"serverInstance":".","name":"MyAG","clusterType":"None"}'
```

### Delete Availability Group

Remove an availability group:

```yaml
serverInstance: .
name: MyAG
_exist: false
```

```powershell
dsc resource delete -r OpenDsc.SqlServer/AvailabilityGroup --input '{"serverInstance":".","name":"MyAG"}'
```

### Export All Availability Groups

List all availability groups on the server:

```powershell
dsc resource export -r OpenDsc.SqlServer/AvailabilityGroup --input '{"serverInstance":"."}'
```
