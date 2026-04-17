# Availability Group Resource

## Synopsis

Manages SQL Server Always On Availability Groups, including creation,
configuration, and removal.

## Type

```text
OpenDsc.SqlServer/AvailabilityGroup
```

## Capabilities

- Get
- Set
- Delete
- Export

## Properties

### Connection properties

#### serverInstance

SQL Server instance name. Use `.` or `(local)` for the default instance, or
`server\instance` for named instances.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

#### connectUsername

Username for SQL authentication. Omit for Windows authentication.

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

#### connectPassword

Password for SQL authentication.

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

### Availability group properties

#### name

Name of the availability group.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

#### automatedBackupPreference

Automated backup preference for the availability group: `Primary`,
`SecondaryOnly`, `Secondary`, or `None`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### failureConditionLevel

Failure condition level that triggers an automatic failover:
`OnServerDown`, `OnServerUnresponsive`, `OnCriticalServerErrors`,
`OnModerateServerErrors`, or `OnAnyQualifiedFailureCondition`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### healthCheckTimeout

Health check timeout value in milliseconds.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### basicAvailabilityGroup

Whether this is a basic availability group (limited to two replicas and one
database). Create-only; cannot be changed after creation.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### databaseHealthTrigger

Whether the availability group supports database-level health detection.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### dtcSupportEnabled

Whether DTC support is enabled for the availability group.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### isDistributedAvailabilityGroup

Whether the availability group is a distributed availability group.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### clusterType

Cluster type of the availability group: `Wsfc`, `None`, or `External`.
Create-only; cannot be changed after creation.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### requiredSynchronizedSecondariesToCommit

Number of required synchronized secondaries to commit.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### isContained

Whether the availability group is contained. Create-only; cannot be changed
after creation.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

### Read-only properties

#### databases

Databases participating in the availability group.

```yaml
Type: string[]
Required: No
Access: Read-Only
Default value: None
```

#### primaryReplicaServerName

Name of the server that is the current primary replica.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

#### localReplicaRole

Role of the local replica in the availability group: `Resolving`, `Primary`,
or `Secondary`.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

#### uniqueId

Unique identifier of the availability group.

```yaml
Type: guid
Required: No
Access: Read-Only
Default value: None
```

### DSC properties

#### _exist

Whether the availability group should exist. Defaults to `true`.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

## Examples

### Example 1 — Get an availability group

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    serverInstance: .
    name: MyAG
    '@

    dsc resource get -r OpenDsc.SqlServer/AvailabilityGroup --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    serverInstance: .
    name: MyAG
    EOF
    )

    dsc resource get -r OpenDsc.SqlServer/AvailabilityGroup --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 2 — Create an availability group

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    serverInstance: .
    name: MyAG
    automatedBackupPreference: Secondary
    failureConditionLevel: OnCriticalServerErrors
    healthCheckTimeout: 30000
    databaseHealthTrigger: true
    clusterType: Wsfc
    '@

    dsc resource set -r OpenDsc.SqlServer/AvailabilityGroup --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    serverInstance: .
    name: MyAG
    automatedBackupPreference: Secondary
    failureConditionLevel: OnCriticalServerErrors
    healthCheckTimeout: 30000
    databaseHealthTrigger: true
    clusterType: Wsfc
    EOF
    )

    dsc resource set -r OpenDsc.SqlServer/AvailabilityGroup --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 3 — Delete an availability group

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    serverInstance: .
    name: MyAG
    '@

    dsc resource delete -r OpenDsc.SqlServer/AvailabilityGroup --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    serverInstance: .
    name: MyAG
    EOF
    )

    dsc resource delete -r OpenDsc.SqlServer/AvailabilityGroup --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 4 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Production availability group
    type: OpenDsc.SqlServer/AvailabilityGroup
    properties:
      serverInstance: "."
      name: ProdAG
      automatedBackupPreference: Secondary
      failureConditionLevel: OnCriticalServerErrors
      healthCheckTimeout: 30000
      databaseHealthTrigger: true
      dtcSupportEnabled: false
      clusterType: Wsfc
      requiredSynchronizedSecondariesToCommit: 1
```

## Exit codes

| Code | Description         |
| :--- | :------------------ |
| 0    | Success             |
| 1    | Error               |
| 2    | Invalid JSON        |
| 3    | Invalid argument    |
| 4    | Unauthorized access |
| 5    | Invalid operation   |
