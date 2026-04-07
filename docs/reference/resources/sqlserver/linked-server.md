# OpenDsc.SqlServer/LinkedServer

## Synopsis

Manages SQL Server linked servers for distributed queries across SQL Server
instances and
other OLE DB data sources.

## Type name

```plaintext
OpenDsc.SqlServer/LinkedServer
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

| Property          | Type   | Required | Access     | Description                      |
| :---------------- | :----- | :------- | :--------- | :------------------------------- |
| `serverInstance`  | string | Yes      | Read/Write | SQL Server instance name.        |
| `connectUsername` | string | No       | Write-Only | Username for SQL authentication. |
| `connectPassword` | string | No       | Write-Only | Password for SQL authentication. |

### Linked server properties

| Property         | Type   | Required | Access     | Description                               |
| :--------------- | :----- | :------- | :--------- | :---------------------------------------- |
| `name`           | string | No       | Read/Write | Name of the linked server.                |
| `productName`    | string | No       | Read/Write | Product name of OLE DB data source.       |
| `providerName`   | string | No       | Read/Write | OLE DB provider name.                     |
| `dataSource`     | string | No       | Read/Write | OLE DB data source (server name or path). |
| `location`       | string | No       | Read/Write | Location of database for OLE DB provider. |
| `catalog`        | string | No       | Read/Write | Default catalog (database).               |
| `providerString` | string | No       | Read/Write | OLE DB provider connection string.        |

### Linked server options

| Property                                            | Type   | Required | Access     | Description                                |
| :-------------------------------------------------- | :----- | :------- | :--------- | :----------------------------------------- |
| `dataAccess`                                        | bool   | No       | Read/Write | Whether data access is enabled.            |
| `rpc`                                               | bool   | No       | Read/Write | Whether RPC from linked server is allowed. |
| `rpcOut`                                            | bool   | No       | Read/Write | Whether RPC out is enabled.                |
| `useRemoteCollation`                                | bool   | No       | Read/Write | Use remote server's collation.             |
| `collationName`                                     | string | No       | Read/Write | Collation name for character comparisons.  |
| `collationCompatible`                               | bool   | No       | Read/Write | Whether collation is compatible.           |
| `lazySchemaValidation`                              | bool   | No       | Read/Write | Use lazy schema validation.                |
| `connectTimeout`                                    | int    | No       | Read/Write | Connection timeout in seconds (min: 0).    |
| `queryTimeout`                                      | int    | No       | Read/Write | Query timeout in seconds (min: 0).         |
| `isPromotionofDistributedTransactionsForRPCEnabled` | bool   | No       | Read/Write | Promote distributed transactions for RPC.  |

### Read-only properties

| Property           | Type     | Access    | Description                             |
| :----------------- | :------- | :-------- | :-------------------------------------- |
| `id`               | int      | Read-Only | Unique identifier.                      |
| `dateLastModified` | datetime | Read-Only | Date last modified.                     |
| `distributor`      | bool     | Read-Only | Whether it is a distributor.            |
| `distPublisher`    | bool     | Read-Only | Whether it is a distribution publisher. |
| `publisher`        | bool     | Read-Only | Whether it is a publisher.              |
| `subscriber`       | bool     | Read-Only | Whether it is a subscriber.             |

### DSC properties

| Property | Type | Required | Access     | Description                                                 |
| :------- | :--- | :------- | :--------- | :---------------------------------------------------------- |
| `_exist` | bool | No       | Read/Write | Whether the linked server should exist. Defaults to `true`. |

## Examples

### Example 1 — Get a linked server

```powershell
dsc resource get -r OpenDsc.SqlServer/LinkedServer --input '{"serverInstance":".","name":"RemoteServer"}'
```

### Example 2 — Create a linked server

```powershell
dsc resource set -r OpenDsc.SqlServer/LinkedServer --input '{
  "serverInstance": ".",
  "name": "RemoteServer",
  "providerName": "SQLNCLI",
  "dataSource": "remote-sql.example.com",
  "rpcOut": true,
  "dataAccess": true
}'
```

### Example 3 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Remote SQL linked server
    type: OpenDsc.SqlServer/LinkedServer
    properties:
      serverInstance: "."
      name: RemoteServer
      providerName: SQLNCLI
      dataSource: remote-sql.example.com
      rpcOut: true
      dataAccess: true
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

## See also

- [OpenDsc resource reference](../overview.md)
