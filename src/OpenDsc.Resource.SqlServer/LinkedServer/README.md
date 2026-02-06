# OpenDsc.SqlServer/LinkedServer

Manages SQL Server linked servers. A linked server allows access to distributed,
heterogeneous queries against OLE DB data sources. After a linked server is
created, distributed queries can be run against this server.

## Type

`OpenDsc.SqlServer/LinkedServer`

## Properties

| Property                                          | Type    | Required | Description                                                          |
|---------------------------------------------------|---------|----------|----------------------------------------------------------------------|
| serverInstance                                    | string  | Yes      | SQL Server instance name (e.g., `.`, `localhost`, `server\instance`) |
| name                                              | string  | Yes      | Name of the linked server                                            |
| _exist                                            | boolean | No       | Whether the linked server should exist (default: true)               |
| connectUsername                                   | string  | No       | Username for SQL Server authentication (write-only)                  |
| connectPassword                                   | string  | No       | Password for SQL Server authentication (write-only)                  |
| productName                                       | string  | No       | Product name of the linked server data source                        |
| providerName                                      | string  | No       | OLE DB provider name (e.g., `SQLNCLI11`, `MSOLEDBSQL`)               |
| dataSource                                        | string  | No       | Data source as interpreted by the OLE DB provider                    |
| location                                          | string  | No       | Location of the database as interpreted by the OLE DB provider       |
| catalog                                           | string  | No       | Default catalog for connections to the linked server                 |
| providerString                                    | string  | No       | OLE DB provider-specific connection string                           |
| dataAccess                                        | boolean | No       | Whether the linked server is used for distributed query access       |
| rpc                                               | boolean | No       | Enable RPC from the linked server                                    |
| rpcOut                                            | boolean | No       | Enable RPC to the linked server                                      |
| useRemoteCollation                                | boolean | No       | Whether to use the collation of remote data source                   |
| collationName                                     | string  | No       | Name of the collation for the linked server                          |
| collationCompatible                               | boolean | No       | Linked server has same collation as the local server                 |
| lazySchemaValidation                              | boolean | No       | Whether to skip schema checking of remote tables                     |
| connectTimeout                                    | integer | No       | Time (seconds) to wait for a connect response (0 = default)          |
| queryTimeout                                      | integer | No       | Time (seconds) to wait for a query to complete (0 = no timeout)      |
| isPromotionofDistributedTransactionsForRPCEnabled | boolean | No       | Enable automatic transaction promotion for RPC                       |
| id                                                | integer | No       | Linked server ID (read-only)                                         |
| dateLastModified                                  | string  | No       | Date the linked server was last modified (read-only)                 |
| distributor                                       | boolean | No       | Whether linked server is a replication distributor (read-only)       |
| distPublisher                                     | boolean | No       | Whether linked server is a dist publisher (read-only)                |
| publisher                                         | boolean | No       | Whether linked server is a replication publisher (read-only)         |
| subscriber                                        | boolean | No       | Whether linked server is a replication subscriber (read-only)        |

## Common OLE DB Providers

| Provider                                 | providerName | Description                      |
|------------------------------------------|--------------|----------------------------------|
| SQL Server Native Client 11              | SQLNCLI11    | For SQL Server 2012-2019         |
| Microsoft OLE DB Driver                  | MSOLEDBSQL   | Recommended for SQL Server 2017+ |
| Microsoft OLE DB Provider for SQL Server | SQLOLEDB     | Legacy provider (deprecated)     |
| Microsoft OLE DB Provider for Oracle     | MSDAORA      | For Oracle databases             |
| Microsoft OLE DB Provider for ODBC       | MSDASQL      | For ODBC data sources            |

## Examples

### Get linked server status

```yaml
# get.dsc.yaml
$schema: https://aka.ms/dsc/2023/10/config/document.schema.json
resources:
  - name: Check linked server
    type: OpenDsc.SqlServer/LinkedServer
    properties:
      serverInstance: localhost
      name: REMOTESERVER
```

```powershell
dsc config get --file get.dsc.yaml
```

### Create a linked server to another SQL Server

```yaml
# set-sqlserver.dsc.yaml
$schema: https://aka.ms/dsc/2023/10/config/document.schema.json
resources:
  - name: Create SQL Server linked server
    type: OpenDsc.SqlServer/LinkedServer
    properties:
      serverInstance: localhost
      name: REMOTESERVER
      productName: SQL Server
      providerName: SQLNCLI11
      dataSource: remoteserver.domain.com
      rpcOut: true
      dataAccess: true
```

```powershell
dsc config set --file set-sqlserver.dsc.yaml
```

### Create a linked server with timeouts

```yaml
# set-timeout.dsc.yaml
$schema: https://aka.ms/dsc/2023/10/config/document.schema.json
resources:
  - name: Create linked server with custom timeouts
    type: OpenDsc.SqlServer/LinkedServer
    properties:
      serverInstance: localhost
      name: SLOWSERVER
      productName: SQL Server
      providerName: MSOLEDBSQL
      dataSource: slowserver.domain.com
      connectTimeout: 30
      queryTimeout: 120
```

```powershell
dsc config set --file set-timeout.dsc.yaml
```

### Create a linked server with provider string

```yaml
# set-provider-string.dsc.yaml
$schema: https://aka.ms/dsc/2023/10/config/document.schema.json
resources:
  - name: Create linked server with provider string
    type: OpenDsc.SqlServer/LinkedServer
    properties:
      serverInstance: localhost
      name: MYLINKEDSERVER
      productName: ""
      providerName: SQLNCLI11
      providerString: "Server=remoteserver;Database=mydb;Trusted_Connection=yes"
```

```powershell
dsc config set --file set-provider-string.dsc.yaml
```

### Delete a linked server

```yaml
# delete.dsc.yaml
$schema: https://aka.ms/dsc/2023/10/config/document.schema.json
resources:
  - name: Remove linked server
    type: OpenDsc.SqlServer/LinkedServer
    properties:
      serverInstance: localhost
      name: OLDSERVER
      _exist: false
```

```powershell
dsc config set --file delete.dsc.yaml
```

### Export all linked servers

```powershell
dsc resource export -r OpenDsc.SqlServer/LinkedServer
```

This exports all linked servers from all accessible SQL Server instances.

## Exit Codes

| Code | Description               |
|------|---------------------------|
| 0    | Success                   |
| 1    | Exception (general error) |
| 2    | Invalid JSON              |
| 3    | Invalid argument          |
| 4    | Unauthorized access       |
| 5    | Invalid operation         |

## Notes

- The `productName` should match the type of data source. Use `SQL Server` for
  SQL Server instances.
- For SQL Server to SQL Server connections, use `SQLNCLI11` (SQL Server Native
  Client 11) or `MSOLEDBSQL` (Microsoft OLE DB Driver for SQL Server) as the
  provider.
- The `dataSource` property for SQL Server linked servers should be the server
  name or IP address, optionally with instance name (e.g., `server\instance`).
- Enable `rpcOut` to execute stored procedures on the linked server.
- Enable `dataAccess` to run distributed queries against the linked server.
- The `connectUsername` and `connectPassword` properties are write-only and are
  used to authenticate to the SQL Server instance specified by `serverInstance`
  when managing the linked server. They are not returned by Get operations and
  are not used to configure linked server login mappings (for that, use
  `sp_addlinkedsrvlogin` or other SQL Server tooling outside this resource).
- Remote collation (`useRemoteCollation`) should typically be set to `true`
  unless you have specific collation requirements.
