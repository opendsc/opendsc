# OpenDsc.SqlServer/LinkedServer

## Synopsis

Manages SQL Server linked servers for distributed queries across SQL Server
instances and other OLE DB data sources.

## Type name

```text
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

### Linked server properties

#### name

Name of the linked server.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### productName

Product name of OLE DB data source.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### providerName

OLE DB provider name.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### dataSource

OLE DB data source (server name or path).

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### location

Location of database for OLE DB provider.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### catalog

Default catalog (database).

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### providerString

OLE DB provider connection string.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

### Linked server options

#### dataAccess

Whether data access is enabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### rpc

Whether RPC from the linked server is allowed.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### rpcOut

Whether RPC out is enabled.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### useRemoteCollation

Use remote server's collation.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### collationName

Collation name for character comparisons.

```yaml
Type: string
Required: No
Access: Read/Write
Default value: None
```

#### collationCompatible

Whether collation is compatible.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### lazySchemaValidation

Use lazy schema validation.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

#### connectTimeout

Connection timeout in seconds (min: 0).

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### queryTimeout

Query timeout in seconds (min: 0).

```yaml
Type: int
Required: No
Access: Read/Write
Default value: None
```

#### isPromotionofDistributedTransactionsForRPCEnabled

Promote distributed transactions for RPC.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: None
```

### Read-only properties

#### id

Unique identifier.

```yaml
Type: int
Required: No
Access: Read-Only
Default value: None
```

#### dateLastModified

Date last modified.

```yaml
Type: datetime
Required: No
Access: Read-Only
Default value: None
```

#### distributor

Whether it is a distributor.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### distPublisher

Whether it is a distribution publisher.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### publisher

Whether it is a publisher.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

#### subscriber

Whether it is a subscriber.

```yaml
Type: bool
Required: No
Access: Read-Only
Default value: None
```

### DSC properties

#### _exist

Whether the linked server should exist. Defaults to `true`.

```yaml
Type: bool
Required: No
Access: Read/Write
Default value: true
```

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
