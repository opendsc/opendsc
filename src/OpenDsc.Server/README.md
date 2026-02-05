# OpenDSC Pull Server

A modern REST API-based pull server for DSC v3, providing centralized
configuration management for distributed systems.

## Features

- **Configuration Management**: Store and distribute DSC configurations
  to registered nodes
- **Composite Configurations**: Combine multiple configurations into
  single deployment units with version pinning and ordering
- **Hierarchical Parameter Merging**: Merge parameters across multiple
  scope types (Default, Region, Environment, Node) with precedence-based
  ordering and node tagging
- **Node Registration**: FQDN-based node identification with mTLS
  authentication
- **mTLS Security**: Mutual TLS authentication using client certificates
  for all node connections
- **Certificate Rotation**: Secure certificate rotation with atomic
  updates
- **Compliance Reporting**: Collect and store compliance reports from
  LCM agents
- **Multi-Database Support**: SQLite (default), SQL Server, and
  PostgreSQL
- **Docker Ready**: Multi-stage Dockerfile and docker-compose
  configurations
- **Interactive API Documentation**: Built-in Scalar API reference at
  `/scalar/v1`

## Quick Start

### Running with Docker

```sh
# Start with SQLite (default)
docker-compose up -d

# Start with PostgreSQL
docker-compose --profile postgres up -d

# Start with SQL Server
docker-compose --profile sqlserver up -d
```

### Running Locally

```sh
# Build the server
dotnet build

# Run the server
dotnet run
```

## API Documentation

For interactive API testing and detailed endpoint documentation, visit the
**Scalar API Reference** at `/scalar/v1` when running the server. The Scalar
interface provides complete request/response schemas, authentication examples,
and the ability to test endpoints directly in your browser.

For conceptual guides and real-world examples, see:

- [Scope System Guide](../../docs/pull-server/scope-system.md) - Understanding scope types, values, and node tagging
- [Parameter Merging](../../docs/pull-server/parameter-merging.md) - How parameters are merged and version management
- [Configuration Management](../../docs/pull-server/configuration-management.md) - Version lifecycle and bundle generation
- [Composite Configurations](../../docs/pull-server/composite-configurations.md) - Combining multiple configurations into deployable units
- [Quick Start Tutorial](../../docs/pull-server/quickstart.md) - Step-by-step walkthrough
- [Real-World Examples](../../docs/pull-server/examples/) - Multi-team collaboration scenarios

## API Endpoints

### Health

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/health` | GET | None | Liveness check |
| `/health/ready` | GET | None | Readiness check (includes database) |

### Nodes

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/nodes/register` | POST | Registration Key | Register or re-register a node |
| `/api/v1/nodes` | GET | Admin | List all nodes |
| `/api/v1/nodes/{nodeId}` | GET | Admin | Get node details |
| `/api/v1/nodes/{nodeId}` | DELETE | Admin | Delete a node |
| `/api/v1/nodes/{nodeId}/configuration` | GET | Node | Get assigned configuration info |
| `/api/v1/nodes/{nodeId}/configuration` | PUT | Admin | Assign configuration to node |
| `/api/v1/nodes/{nodeId}/configuration/checksum` | GET | Node | Get configuration checksum |
| `/api/v1/nodes/{nodeId}/configuration/bundle` | GET | Node | Download configuration bundle with merged parameters |
| `/api/v1/nodes/{nodeId}/rotate-certificate` | POST | Node | Rotate client certificate |
| `/api/v1/nodes/{nodeId}/parameters/provenance` | GET | Admin | Get parameter provenance for node |
| `/api/v1/nodes/{nodeId}/tags` | GET | Admin | List node tags (scope value assignments) |

### Configurations

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/configurations` | GET | Admin | List all configurations |
| `/api/v1/configurations` | POST | Admin | Create a new configuration (multipart/form-data upload) |
| `/api/v1/configurations/{name}` | GET | Admin | Get configuration details |
| `/api/v1/configurations/{name}` | DELETE | Admin | Delete a configuration |
| `/api/v1/configurations/{name}/versions` | GET | Admin | List all versions for a configuration |
| `/api/v1/configurations/{name}/versions` | POST | Admin | Create new version (multipart/form-data upload) |
| `/api/v1/configurations/{name}/versions/{version}` | GET | Admin | Get version details |
| `/api/v1/configurations/{name}/versions/{version}/publish` | PUT | Admin | Publish a draft version |
| `/api/v1/configurations/{name}/versions/{version}` | DELETE | Admin | Delete a version (if not in use) |

### Composite Configurations

Composite configurations (also called meta configurations) allow you to
combine multiple existing configurations into a single deployment unit.
Composites do not contain their own files or parameters but reference
other configurations as children.

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/composite-configurations` | GET | Admin | List all composite configurations |
| `/api/v1/composite-configurations` | POST | Admin | Create a composite configuration |
| `/api/v1/composite-configurations/{name}` | GET | Admin | Get composite details |
| `/api/v1/composite-configurations/{name}` | PUT | Admin | Update composite properties |
| `/api/v1/composite-configurations/{name}` | DELETE | Admin | Delete composite |
| `/api/v1/composite-configurations/{name}/versions` | GET | Admin | List all versions |
| `/api/v1/composite-configurations/{name}/versions` | POST | Admin | Create a new version |
| `/api/v1/composite-configurations/{name}/versions/{version}` | GET | Admin | Get version details |
| `/api/v1/composite-configurations/{name}/versions/{version}/publish` | PUT | Admin | Publish a draft version |
| `/api/v1/composite-configurations/{name}/versions/{version}` | DELETE | Admin | Delete version |
| `/api/v1/composite-configurations/{name}/versions/{version}/children` | POST | Admin | Add child configuration |
| `/api/v1/composite-configurations/{name}/versions/{version}/children/{id}` | PUT | Admin | Update child configuration |
| `/api/v1/composite-configurations/{name}/versions/{version}/children/{id}` | DELETE | Admin | Remove child configuration |

### Scope Types

Scope types define categories for parameter organization with
precedence-based ordering. Two system scope types are pre-configured:
**Default** (precedence 0, no values) and **Node** (precedence 1,
FQDN-based). You can create custom scope types like Region, Environment,
or Team.

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/scope-types` | GET | Admin | List all scope types ordered by precedence |
| `/api/v1/scope-types` | POST | Admin | Create a new scope type |
| `/api/v1/scope-types/{id}` | GET | Admin | Get scope type details by GUID |
| `/api/v1/scope-types/{id}` | PUT | Admin | Update scope type properties |
| `/api/v1/scope-types/{id}` | DELETE | Admin | Delete a scope type (only if unused) |
| `/api/v1/scope-types/reorder` | PUT | Admin | Atomically reorder scope types by GUID array |

### Scope Values

Scope values are specific instances within a scope type (e.g.,
"Production" for Environment scope type). The Default scope type does
not allow values. The Node scope type values are implicitly assigned based
on node FQDN.

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/scope-types/{scopeTypeId}/values` | GET | Admin | List all values for a scope type |
| `/api/v1/scope-types/{scopeTypeId}/values` | POST | Admin | Create a new scope value |
| `/api/v1/scope-types/{scopeTypeId}/values/{id}` | GET | Admin | Get scope value by GUID |
| `/api/v1/scope-types/{scopeTypeId}/values/{id}` | PUT | Admin | Update scope value properties |
| `/api/v1/scope-types/{scopeTypeId}/values/{id}` | DELETE | Admin | Delete a scope value (only if unused) |

### Node Tags

Node tags associate nodes with specific scope values for parameter
merging. Nodes can have one tag per scope type. Admin-only to prevent
self-escalation.

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/nodes/{nodeId}/tags` | GET | Admin | List all tags for a node |
| `/api/v1/nodes/{nodeId}/tags` | POST | Admin | Assign a scope value to a node (body: `{scopeValueId: guid}`) |
| `/api/v1/nodes/{nodeId}/tags/{scopeValueId}` | DELETE | Admin | Remove a specific tag by scope value GUID |

### Parameters

Parameters are YAML files (JSON also supported) stored per configuration
and scope type/value combination. Parameter files support versioning with
draft/active states.

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/parameters/{scopeTypeId}/{configurationId}` | PUT | Admin | Create/update parameter version (use `?scopeValue=xyz` for non-Default scopes) |
| `/api/v1/parameters/{scopeTypeId}/{configurationId}/versions` | GET | Admin | List parameter versions (use `?scopeValue=xyz` for non-Default scopes) |
| `/api/v1/parameters/{scopeTypeId}/{configurationId}/versions/{version}/activate` | PUT | Admin | Activate a parameter version (use `?scopeValue=xyz` for non-Default scopes) |
| `/api/v1/parameters/{scopeTypeId}/{configurationId}/versions/{version}` | DELETE | Admin | Delete parameter version (use `?scopeValue=xyz` for non-Default scopes) |
| `/api/v1/nodes/{nodeId}/parameters/provenance` | GET | Admin | Get parameter provenance for node (use `?configurationId=guid`) |

### Reports

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/nodes/{nodeId}/reports` | POST | Node | Submit a compliance report |
| `/api/v1/nodes/{nodeId}/reports` | GET | Admin | Get reports for a node |
| `/api/v1/reports` | GET | Admin | List all reports |
| `/api/v1/reports/{reportId}` | GET | Admin | Get report details |

### Retention

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/retention/configurations/cleanup` | POST | Admin | Cleanup old configuration versions |
| `/api/v1/retention/parameters/cleanup` | POST | Admin | Cleanup old parameter versions |

### Settings

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/settings` | GET | Admin | Get server settings |
| `/api/v1/settings` | PUT | Admin | Update server settings |

### Registration Keys

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/admin/registration-keys` | POST | Admin | Create registration key with expiration/limits |
| `/api/v1/admin/registration-keys` | GET | Admin | List all registration keys |
| `/api/v1/admin/registration-keys/{keyId}` | DELETE | Admin | Revoke a registration key |

## Authentication

### mTLS (Mutual TLS) Authentication

The OpenDSC Pull Server uses mutual TLS (mTLS) for secure node
authentication:

**Client Certificate Requirement:**

- All HTTPS connections require a client certificate
- Configured via `ClientCertificateMode.RequireCertificate` in Kestrel
- The server extracts and validates the certificate thumbprint during
  registration

**Node Registration Flow:**

1. LCM connects with a client certificate (self-signed or from platform
   store)
2. Server validates the registration key
3. Server stores the certificate thumbprint, subject DN, and expiration date
4. Node receives a unique NodeId for subsequent operations

**Certificate Rotation:**

- Nodes can rotate certificates via the
  `/api/v1/nodes/{nodeId}/rotate-certificate` endpoint
- The server updates the stored certificate information atomically
- Old certificate is immediately invalidated after successful rotation

### Admin Authentication

Administrators authenticate using the authorization header and admin API key.

**Note:** In Testing environment, client certificates are optional and
simulated with test data.

## Configuration

### Environment Variables

| Variable | Description | Default |
| :-------- | :---------- | :------ |
| `Server__RegistrationKey` | Pre-shared key for node registration | (empty) |
| `Server__AdminApiKey` | API key for admin endpoints | (empty) |
| `Server__CertificateRotationInterval` | Informational certificate rotation interval | `60.00:00:00` (60 days) |
| `Database__Provider` | Database provider (SQLite, SqlServer, PostgreSQL) | SQLite |
| `Database__ConnectionString` | Database connection string | Data Source=opendsc-server.db |
| `ASPNETCORE_URLS` | Server URL(s) | `https://localhost:5001` |
| `ASPNETCORE_Kestrel__Certificates__Default__Path` | Path to server certificate | (empty) |
| `ASPNETCORE_Kestrel__Certificates__Default__Password` | Server certificate password | (empty) |

### appsettings.json

```json
{
  "Server": {
    "RegistrationKey": "your-registration-key",
    "AdminApiKey": "your-admin-api-key",
    "CertificateRotationInterval": "60.00:00:00"
  },
  "Database": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=opendsc-server.db"
  },
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:5001",
        "Certificate": {
          "Path": "path/to/server-cert.pfx",
          "Password": "cert-password"
        }
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

## LCM Configuration for Pull Mode

Configure the LCM to use pull mode by updating its configuration:

```json
{
  "LCM": {
    "ConfigurationMode": "Remediate",
    "ConfigurationSource": "Pull",
    "ConfigurationModeInterval": "00:15:00",
    "PullServer": {
      "ServerUrl": "http://your-server:5000",
      "RegistrationKey": "your-registration-key",
      "ReportCompliance": true
    }
  }
}
```

## Security Considerations

1. **mTLS Required**: The server requires mutual TLS (client certificates)
   for all node connections
2. **HTTPS Only**: Always use TLS/HTTPS for production deployments with
   valid server certificates
3. **Secure Registration Key**: The registration key should be kept
   confidential and rotated periodically
4. **Admin API Key**: Store the admin API key securely (consider using
   environment variables or secret management)
5. **Certificate Validation**: Client certificates are validated via
   thumbprint matching stored in the database
6. **Automatic Certificate Rotation**: LCM nodes automatically rotate
   managed certificates every 60 days
7. **Database Security**: Use proper database authentication, encryption
   at rest, and encrypted connections
8. **Certificate Storage**: Store server certificates securely with
   appropriate file permissions

## Development

### Building

```sh
dotnet build
```

### Running Tests

```sh
dotnet test
```

### Publishing

```sh
dotnet publish -c Release
```

## License

MIT License - See LICENSE file for details.
