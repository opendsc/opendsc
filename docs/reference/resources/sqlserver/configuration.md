# Configuration Resource

## Synopsis

Manages SQL Server instance configuration options (equivalent to
`sp_configure`). Covers memory, parallelism, security, and advanced server
options.

## Type

```text
OpenDsc.SqlServer/Configuration
```

## Capabilities

- Get
- Set
- Export

## Properties

### Connection properties

#### serverInstance

SQL Server instance name.

```yaml
Type: string
Required: No
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

### Memory settings

#### maxServerMemory

Maximum server memory in MB. Use `2147483647` for unlimited.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### minServerMemory

Minimum server memory in MB.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### minMemoryPerQuery

Minimum memory per query in KB.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

### Parallelism settings

#### maxDegreeOfParallelism

Max degree of parallelism. Use `0` for all processors.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### costThresholdForParallelism

Cost threshold for parallel plans.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

### Network settings

#### networkPacketSize

Network packet size in bytes (512–32767).

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### remoteLoginTimeout

Remote login timeout in seconds. Use `0` for infinite.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### remoteQueryTimeout

Remote query timeout in seconds. Use `0` for no timeout.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

### Security and feature settings

#### xpCmdShellEnabled

Enable `xp_cmdshell`.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### databaseMailEnabled

Enable Database Mail XPs.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### agentXpsEnabled

Enable SQL Server Agent XPs.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### oleAutomationProceduresEnabled

Enable OLE Automation procedures.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### adHocDistributedQueriesEnabled

Enable ad hoc distributed queries.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### clrEnabled

Enable CLR integration.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### remoteDacConnectionsEnabled

Enable remote DAC connections.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### containmentEnabled

Enable contained database authentication.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### defaultBackupCompression

Default backup compression.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### defaultBackupChecksum

Default backup checksum.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### c2AuditMode

Enable C2 audit mode.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### commonCriteriaComplianceEnabled

Enable Common Criteria compliance.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### crossDbOwnershipChaining

Cross-database ownership chaining.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### defaultTraceEnabled

Enable default trace.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

### Performance settings

#### queryGovernorCostLimit

Max estimated query cost. Use `0` for no limit.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### queryWait

Query wait in seconds. Use `-1` for auto.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### optimizeAdhocWorkloads

Optimize plan cache for ad hoc workloads.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### nestedTriggers

Allow nested triggers (up to 32 levels).

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### serverTriggerRecursionEnabled

Server-level trigger recursion.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### disallowResultsFromTriggers

Prevent triggers from returning result sets.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### blockedProcessThreshold

Blocked process threshold in seconds. Use `0` to disable.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### recoveryInterval

Recovery interval in minutes. Use `0` for automatic.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### fillFactor

Default fill factor. Use `0` or `100` for full pages.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### userConnections

Max user connections. Use `0` for unlimited.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### cursorThreshold

Rows for async cursor. Use `-1` for all synchronous.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### filestreamAccessLevel

FILESTREAM access level. Accepts `0`, `1`, or `2`.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### maxWorkerThreads

Max worker threads. Use `0` for auto.

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

### Advanced settings

#### showAdvancedOptions

Show advanced options in `sp_configure`.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

### Read-only properties

#### showAdvancedOptionsRunValue

Current running value of show advanced options.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

## Examples

### Example 1 — Get current configuration

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    serverInstance: .
    '@

    dsc resource get -r OpenDsc.SqlServer/Configuration --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    serverInstance: .
    EOF
    )

    dsc resource get -r OpenDsc.SqlServer/Configuration --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 2 — Set memory and parallelism

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $resourceInput = @'
    serverInstance: .
    maxServerMemory: 8192
    maxDegreeOfParallelism: 4
    costThresholdForParallelism: 50
    '@

    dsc resource set -r OpenDsc.SqlServer/Configuration --input $resourceInput
    ```

=== "Shell"

    ```sh
    resource_input=$(cat <<'EOF'
    serverInstance: .
    maxServerMemory: 8192
    maxDegreeOfParallelism: 4
    costThresholdForParallelism: 50
    EOF
    )

    dsc resource set -r OpenDsc.SqlServer/Configuration --input "$resource_input"
    ```

<!-- markdownlint-enable MD046 -->

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: SQL Server instance settings
    type: OpenDsc.SqlServer/Configuration
    properties:
      serverInstance: "."
      maxServerMemory: 16384
      minServerMemory: 4096
      maxDegreeOfParallelism: 4
      costThresholdForParallelism: 50
      xpCmdShellEnabled: false
      defaultBackupCompression: true
      optimizeAdhocWorkloads: true
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
