# Database Resource

## Synopsis

Manages SQL Server databases, including creation, configuration options, ANSI
settings, performance options, and availability features.

## Type

```text
OpenDsc.SqlServer/Database
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | Yes       |
| Export     | Yes       |

## Properties

### Connection properties

#### serverInstance

SQL Server instance name.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

#### connectUsername

Username for SQL authentication.

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

### Database properties

#### name

Name of the database.

```yaml
Type: string
Required: Yes
Access: Read/Write
Default value: None
```

#### collation

Database collation. Defaults to server collation.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### compatibilityLevel

Compatibility level (`Version90` through `Version160`).

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### recoveryModel

Recovery model: `Simple`, `Full`, or `BulkLogged`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### owner

Login name of database owner.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### readOnly

Whether the database is read-only.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### userAccess

User access: `Multi`, `Single`, or `Restricted`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### pageVerify

Page verification: `None`, `TornPageDetection`, or `Checksum`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### containmentType

Containment: `None` or `Partial`.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### File properties (write-only, used during creation)

#### primaryFilePath

Path to primary data file (.mdf).

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

#### logFilePath

Path to log file (.ldf).

```yaml
Type: string
Required: No
Access: Write-Only
Default value: None
```

#### primaryFileSize

Initial primary file size in MB.

```yaml
Type: int
Required: No
Access: Write-Only
Default value: None
```

#### logFileSize

Initial log file size in MB.

```yaml
Type: int
Required: No
Access: Write-Only
Default value: None
```

#### primaryFileGrowth

Primary file growth amount in MB.

```yaml
Type: int
Required: No
Access: Write-Only
Default value: None
```

#### logFileGrowth

Log file growth amount in MB.

```yaml
Type: int
Required: No
Access: Write-Only
Default value: None
```

### ANSI settings

#### ansiNullDefault

Whether ANSI NULL default is enabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### ansiNullsEnabled

Whether ANSI NULLs are enabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### ansiPaddingEnabled

Whether ANSI padding is enabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### ansiWarningsEnabled

Whether ANSI warnings are enabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### arithmeticAbortEnabled

Whether arithmetic abort is enabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### concatenateNullYieldsNull

Whether concatenating null yields null.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### numericRoundAbortEnabled

Whether numeric round-abort is enabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### quotedIdentifiersEnabled

Whether quoted identifiers are enabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

### Performance and behavior settings

#### autoClose

Auto-close when last user exits.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### autoShrink

Automatically shrink database.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### autoCreateStatisticsEnabled

Automatic statistics creation.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### autoUpdateStatisticsEnabled

Automatic statistics update.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### autoUpdateStatisticsAsync

Async statistics update.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### closeCursorsOnCommitEnabled

Close cursors on transaction commit.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### localCursorsDefault

Default to local cursor scope.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### nestedTriggersEnabled

Allow nested triggers.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### recursiveTriggersEnabled

Allow recursive triggers.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### trustworthy

Database is trustworthy.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### databaseOwnershipChaining

Cross-database ownership chaining.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### dateCorrelationOptimization

Date correlation optimization.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### brokerEnabled

Service Broker enabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### encryptionEnabled

Transparent data encryption.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### isParameterizationForced

Forced parameterization.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### isReadCommittedSnapshotOn

READ_COMMITTED_SNAPSHOT isolation.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### isFullTextEnabled

Full-text indexing.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### targetRecoveryTime

Target recovery time in seconds.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### delayedDurabilityEnabled

Delayed durability.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### acceleratedRecoveryEnabled

Accelerated database recovery.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

### Read-only properties

#### id

Database ID.

```yaml
Type: int
Required: No
Access: Read-Only
Default value: None
```

#### createDate

Creation date.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

#### size

Current size in MB.

```yaml
Type: double
Required: No
Access: Read-Only
Default value: None
```

#### spaceAvailable

Space available in KB.

```yaml
Type: double
Required: No
Access: Read-Only
Default value: None
```

#### dataSpaceUsage

Data space usage in KB.

```yaml
Type: double
Required: No
Access: Read-Only
Default value: None
```

#### indexSpaceUsage

Index space usage in KB.

```yaml
Type: double
Required: No
Access: Read-Only
Default value: None
```

#### activeConnections

Number of active connections.

```yaml
Type: int
Required: No
Access: Read-Only
Default value: None
```

#### lastBackupDate

Date of last full backup.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

#### lastDifferentialBackupDate

Date of last differential backup.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

#### lastLogBackupDate

Date of last log backup.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

#### status

Database status.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

#### isSystemObject

Whether it is a system database.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### isAccessible

Whether the database is accessible.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### isUpdateable

Whether the database is updateable.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### isDatabaseSnapshot

Whether it is a database snapshot.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### isMirroringEnabled

Whether mirroring is enabled.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### availabilityGroupName

Availability group name.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

#### caseSensitive

Whether the database is case-sensitive.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### primaryFilePathActual

Actual path to primary file.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

#### defaultFileGroup

Default file group name.

```yaml
Type: string
Required: No
Access: Read-Only
Default value: None
```

### DSC properties

#### _exist

Whether the database should exist. Defaults to `true`.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

## Examples

### Example 1 — Get a database

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    serverInstance: .
    name: master
    '@

    dsc resource get -r OpenDsc.SqlServer/Database --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    serverInstance: .
    name: master
    EOF
    )

    dsc resource get -r OpenDsc.SqlServer/Database --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 2 — Create a database

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    serverInstance: .
    name: AppDb
    recoveryModel: Simple
    collation: SQL_Latin1_General_CP1_CI_AS
    '@

    dsc resource set -r OpenDsc.SqlServer/Database --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    serverInstance: .
    name: AppDb
    recoveryModel: Simple
    collation: SQL_Latin1_General_CP1_CI_AS
    EOF
    )

    dsc resource set -r OpenDsc.SqlServer/Database --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Application database
    type: OpenDsc.SqlServer/Database
    properties:
      serverInstance: "."
      name: AppDb
      recoveryModel: Full
      autoShrink: false
      autoCreateStatisticsEnabled: true
      autoUpdateStatisticsEnabled: true
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
