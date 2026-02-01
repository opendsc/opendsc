# OpenDsc.SqlServer/Configuration

Manages SQL Server instance-level configuration options. These are the settings
typically configured through `sp_configure` in T-SQL. This resource allows you
to manage server configuration options declaratively.

## Type

`OpenDsc.SqlServer/Configuration`

## Properties

### Connection Properties

| Property        | Type   | Required | Description                                                          |
| --------------- | ------ | -------- | -------------------------------------------------------------------- |
| serverInstance  | string | Yes      | SQL Server instance name (e.g., `.`, `localhost`, `server\instance`) |
| connectUsername | string | No       | Username for SQL Server authentication (write-only)                  |
| connectPassword | string | No       | Password for SQL Server authentication (write-only)                  |

### Memory Configuration

| Property          | Type    | Description                                               |
| ----------------- | ------- | --------------------------------------------------------- |
| maxServerMemory   | integer | Maximum server memory in MB. Use 2147483647 for unlimited |
| minServerMemory   | integer | Minimum server memory in MB                               |
| minMemoryPerQuery | integer | Minimum memory per query in KB                            |

### Parallelism Configuration

| Property                    | Type    | Description                                                     |
| --------------------------- | ------- | --------------------------------------------------------------- |
| maxDegreeOfParallelism      | integer | Maximum degree of parallelism (MAXDOP). 0 = use all CPUs        |
| costThresholdForParallelism | integer | Cost threshold for parallelism. Queries below this run serially |

### Network Configuration

| Property           | Type    | Description                                     |
| ------------------ | ------- | ----------------------------------------------- |
| networkPacketSize  | integer | Network packet size in bytes (512-32767)        |
| remoteLoginTimeout | integer | Remote login timeout in seconds. 0 = infinite   |
| remoteQueryTimeout | integer | Remote query timeout in seconds. 0 = no timeout |

### Feature Toggles

| Property                       | Type    | Description                                        |
| ------------------------------ | ------- | -------------------------------------------------- |
| xpCmdShellEnabled              | boolean | Enable xp_cmdshell extended stored procedure       |
| databaseMailEnabled            | boolean | Enable Database Mail extended stored procedures    |
| agentXpsEnabled                | boolean | Enable SQL Server Agent extended stored procedures |
| oleAutomationProceduresEnabled | boolean | Enable OLE Automation extended stored procedures   |
| adHocDistributedQueriesEnabled | boolean | Enable OPENROWSET and OPENDATASOURCE               |
| clrEnabled                     | boolean | Enable SQL Server CLR integration                  |
| remoteDacConnectionsEnabled    | boolean | Enable remote Dedicated Administrator Connection   |
| containmentEnabled             | boolean | Enable contained database authentication           |

### Backup Configuration

| Property                 | Type    | Description                          |
| ------------------------ | ------- | ------------------------------------ |
| defaultBackupCompression | boolean | Enable backup compression by default |
| defaultBackupChecksum    | boolean | Enable backup checksum by default    |

### Query Configuration

| Property               | Type    | Description                                              |
| ---------------------- | ------- | -------------------------------------------------------- |
| queryGovernorCostLimit | integer | Maximum estimated cost for query execution. 0 = no limit |
| queryWait              | integer | Time (seconds) a query waits for resources. -1 = auto    |
| optimizeAdhocWorkloads | boolean | Improve plan cache efficiency for ad hoc queries         |

### Trigger Configuration

| Property                      | Type    | Description                                             |
| ----------------------------- | ------- | ------------------------------------------------------- |
| nestedTriggers                | boolean | Allow triggers to fire other triggers (up to 32 levels) |
| serverTriggerRecursionEnabled | boolean | Allow server-level triggers to fire recursively         |
| disallowResultsFromTriggers   | boolean | Prevent triggers from returning result sets             |

### Security Configuration

| Property                        | Type    | Description                                |
| ------------------------------- | ------- | ------------------------------------------ |
| c2AuditMode                     | boolean | Enable C2 audit mode for security auditing |
| commonCriteriaComplianceEnabled | boolean | Enable Common Criteria compliance mode     |
| crossDbOwnershipChaining        | boolean | Enable cross-database ownership chaining   |

### Miscellaneous Configuration

| Property                | Type    | Description                                                   |
| ----------------------- | ------- | ------------------------------------------------------------- |
| defaultTraceEnabled     | boolean | Enable the default trace for diagnostics                      |
| blockedProcessThreshold | integer | Threshold (seconds) for blocked process reports. 0 = disabled |
| showAdvancedOptions     | boolean | Enable display of advanced configuration options              |
| recoveryInterval        | integer | Maximum time (minutes) per database for recovery. 0 = auto    |
| fillFactor              | integer | Default fill factor percentage. 0 or 100 = full pages         |
| userConnections         | integer | Maximum simultaneous user connections. 0 = unlimited          |
| cursorThreshold         | integer | Rows for async cursor generation. -1 = all sync               |
| filestreamAccessLevel   | integer | FILESTREAM: 0 = Disabled, 1 = T-SQL only, 2 = T-SQL and I/O   |
| maxWorkerThreads        | integer | Maximum worker threads. 0 = automatic                         |

### Read-Only Properties

| Property                    | Type    | Description                                    |
| --------------------------- | ------- | ---------------------------------------------- |
| showAdvancedOptionsRunValue | boolean | Current running value of show advanced options |

## Examples

### Get current server configuration

```yaml
# get-configuration.dsc.config.yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Get SQL Server configuration
    type: OpenDsc.SqlServer/Configuration
    properties:
      serverInstance: localhost
```

```powershell
dsc config get --file get.dsc.config.yaml
```

### Configure MAXDOP and Cost Threshold

A common best practice configuration for OLTP workloads:

```yaml
# set-parallelism.dsc.config.yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Configure parallelism settings
    type: OpenDsc.SqlServer/Configuration
    properties:
      serverInstance: localhost
      maxDegreeOfParallelism: 4
      costThresholdForParallelism: 50
```

```powershell
dsc config set --file set-parallelism.dsc.config.yaml
```

### Configure memory settings

```yaml
# set-memory.dsc.config.yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Configure memory settings
    type: OpenDsc.SqlServer/Configuration
    properties:
      serverInstance: localhost
      maxServerMemory: 16384
      minServerMemory: 4096
```

```powershell
dsc config set --file set-memory.dsc.config.yaml
```

### Enable backup best practices

```yaml
# set-backup.dsc.config.yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Configure backup defaults
    type: OpenDsc.SqlServer/Configuration
    properties:
      serverInstance: localhost
      defaultBackupCompression: true
      defaultBackupChecksum: true
```

```powershell
dsc config set --file set-backup.dsc.config.yaml
```

### Enable performance optimization features

```yaml
# set-performance.dsc.config.yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Configure performance settings
    type: OpenDsc.SqlServer/Configuration
    properties:
      serverInstance: localhost
      optimizeAdhocWorkloads: true
      blockedProcessThreshold: 10
```

```powershell
dsc config set --file set-performance.dsc.config.yaml
```

### Enable remote DAC for troubleshooting

```yaml
# set-dac.dsc.config.yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Enable remote DAC
    type: OpenDsc.SqlServer/Configuration
    properties:
      serverInstance: localhost
      remoteDacConnectionsEnabled: true
```

```powershell
dsc config set --file set-dac.dsc.config.yaml
```

### Standard production server configuration

A comprehensive configuration for production SQL Server instances:

```yaml
# set-production.dsc.config.yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Production SQL Server configuration
    type: OpenDsc.SqlServer/Configuration
    properties:
      serverInstance: localhost
      # Memory - adjust based on server RAM
      maxServerMemory: 28672
      minServerMemory: 8192
      # Parallelism - adjust based on CPU cores
      maxDegreeOfParallelism: 4
      costThresholdForParallelism: 50
      # Backup
      defaultBackupCompression: true
      defaultBackupChecksum: true
      # Performance
      optimizeAdhocWorkloads: true
      # Diagnostics
      blockedProcessThreshold: 10
      remoteDacConnectionsEnabled: true
      # Security - disable dangerous features
      xpCmdShellEnabled: false
      oleAutomationProceduresEnabled: false
      adHocDistributedQueriesEnabled: false
```

```powershell
dsc config set --file set-production.dsc.config.yaml
```

## Exit Codes

| Code | Description              |
| ---- | ------------------------ |
| 0    | Success                  |
| 1    | General error            |
| 2    | JSON serialization error |
| 3    | Invalid argument         |
| 4    | Unauthorized access      |
| 5    | Invalid operation        |

## Best Practices

For comprehensive SQL Server configuration best practices, refer to Microsoft's
official guidance:

- [SQL Server Design Considerations][00]

### Memory Configuration

**Reserve adequate memory for the OS and other processes:**

- **Minimum server memory (`minServerMemory`)**: Microsoft recommends at least
  4 GB for production SQL Server instances.
- **Maximum server memory (`maxServerMemory`)**: Reserve memory for the OS and
  other services. A typical formula is:
  - 1 GB of RAM for the OS
  - 1 GB per every 4 GB of RAM installed (up to 16 GB)
  - 1 GB per every 8 GB of RAM installed (above 16 GB)
  
See Microsoft's latest guidance: [Reserve memory][01]

### Parallelism Configuration

**Configure max degree of parallelism appropriately:**

- **MAXDOP (`maxDegreeOfParallelism`)**: Microsoft's recommendations:
  - For servers with **more than 8 processors**: Use MAXDOP=8
  - For servers with **8 or fewer processors**: Use MAXDOP=0 to N (where N =
    number of processors)
  - For **NUMA configured servers**: MAXDOP shouldn't exceed the number of CPUs
    per NUMA node
  - For **hyperthreading enabled servers**: MAXDOP shouldn't exceed the number
    of physical processors
- **Cost threshold (`costThresholdForParallelism`)**: The default value of 5 is
  often too low for modern hardware. Values between 25-50 are commonly
  recommended for production workloads.

See Microsoft's latest guidance:
[Set the max degree of parallelism option for optimal performance][02]

### General Notes

- This resource is a singleton - there is only one configuration per SQL Server
  instance.
- Some configuration options require `show advanced options` to be enabled
  before they can be modified. The SMO library typically handles this
  automatically.
- Configuration changes via SMO use `sp_configure` internally. Some settings
  take effect immediately (dynamic), while others require a SQL Server restart.
- Enable `optimizeAdhocWorkloads` on servers with many ad hoc queries to reduce
  plan cache bloat.
- Use `blockedProcessThreshold` to enable blocked process reports in Extended
  Events for deadlock troubleshooting.

<!-- Link reference definitions -->
[00]: https://learn.microsoft.com/en-us/system-center/scom/plan-sqlserver-design?view=sc-om-2025
[01]: https://learn.microsoft.com/en-us/system-center/scom/plan-sqlserver-design?view=sc-om-2025#reserve-memory
[02]: https://learn.microsoft.com/en-us/sql/relational-databases/policy-based-management/set-the-max-degree-of-parallelism-option-for-optimal-performance
