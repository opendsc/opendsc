# OpenDSC Pull Server

A modern REST API-based pull server for DSC v3, providing centralized
configuration management for distributed systems.

## Features

- **Configuration Management**: Store and distribute DSC configurations
  to registered nodes
- **Hierarchical Parameter Merging**: Merge parameters across multiple
  scopes (global, environment, node) using
  [OpenDsc.Parameters](../OpenDsc.Parameters/README.md)
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

### Configurations

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/configurations` | GET | Admin | List all configurations |
| `/api/v1/configurations` | POST | Admin | Create a new configuration |
| `/api/v1/configurations/{name}` | GET | Admin | Get configuration details |
| `/api/v1/configurations/{name}` | PUT | Admin | Update configuration content |
| `/api/v1/configurations/{name}` | DELETE | Admin | Delete a configuration |

### Scopes

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/scopes` | GET | Admin | List all scopes ordered by precedence |
| `/api/v1/scopes` | POST | Admin | Create a new scope |
| `/api/v1/scopes/{name}` | GET | Admin | Get scope details |
| `/api/v1/scopes/{name}` | PUT | Admin | Update scope properties |
| `/api/v1/scopes/{name}` | DELETE | Admin | Delete a scope (only if unused) |
| `/api/v1/scopes/reorder` | PUT | Admin | Atomically reorder all scopes |

### Parameters

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/parameters/{scopeName}/{configurationName}` | PUT | Admin | Create or update parameter version |
| `/api/v1/parameters/{scopeName}/{configurationName}/versions` | GET | Admin | List parameter versions |
| `/api/v1/parameters/{scopeName}/{configurationName}/versions/{version}/activate` | PUT | Admin | Activate a parameter version |
| `/api/v1/parameters/{scopeName}/{configurationName}/versions/{version}` | DELETE | Admin | Delete parameter version (only if not active) |

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
| `/api/v1/settings/registration-keys` | POST | Admin | Create new registration key |

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

## Parameter Merging

The Pull Server supports hierarchical parameter merging across multiple
scopes using [OpenDsc.Parameters](../OpenDsc.Parameters/README.md). This
allows you to define parameters at different levels (global,
environment, node) and automatically merge them with proper precedence.

### How It Works

1. **Scope Creation**: Create scopes with precedence values (e.g.,
   "Global"=1, "Production"=2, "Node"=3)
2. **Parameter Files**: Upload parameter files (YAML/JSON) for each
   scope and configuration
3. **Node Assignment**: Assign one or more scopes to a node
4. **Automatic Merging**: When a node requests configuration, the server:
   - Queries the node's assigned scopes
   - Loads active parameter files for each scope
   - Merges parameters based on scope precedence
   - Bundles the merged parameters with the configuration files

### Scope Management

**Create a scope:**

```sh
curl -X POST https://server/api/v1/scopes \
  -H "Authorization: Bearer $ADMIN_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Production",
    "description": "Production environment settings",
    "precedence": 2
  }'
```

**List all scopes (ordered by precedence):**

```sh
curl https://server/api/v1/scopes \
  -H "Authorization: Bearer $ADMIN_API_KEY"
```

**Reorder scopes atomically:**

```sh
curl -X PUT https://server/api/v1/scopes/reorder \
  -H "Authorization: Bearer $ADMIN_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "scopes": [
      {"name": "Global", "precedence": 1},
      {"name": "Production", "precedence": 2},
      {"name": "WebServer", "precedence": 3},
      {"name": "Node-Specific", "precedence": 4}
    ]
  }'
```

### Parameter Version Management

**Upload parameter version:**

```sh
curl -X PUT https://server/api/v1/parameters/Production/MyApp \
  -H "Authorization: Bearer $ADMIN_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "version": "1.0.0",
    "content": "logLevel: Warning\nserver: prod.example.com\n",
    "contentType": "application/x-yaml",
    "isDraft": false
  }'
```

**Activate a parameter version:**

```sh
curl -X PUT https://server/api/v1/parameters/Production/MyApp/versions/1.0.0/activate \
  -H "Authorization: Bearer $ADMIN_API_KEY"
```

**List parameter versions:**

```sh
curl https://server/api/v1/parameters/Production/MyApp/versions \
  -H "Authorization: Bearer $ADMIN_API_KEY"
```

### Parameter Provenance

View where each parameter value originated and what was overridden:

```bash
curl https://server/api/v1/nodes/{nodeId}/parameters/provenance?configurationName=MyApp \
  -H "Authorization: Bearer $ADMIN_API_KEY"
```

Response shows the merged parameters and provenance information:

```json
{
  "nodeId": "abc123...",
  "configurationName": "MyApp",
  "mergedContent": "logLevel: Warning\nserver: node1.prod.example.com\ntimeout: 30\n",
  "provenance": {
    "logLevel": {
      "scopeName": "Production",
      "precedence": 2,
      "value": "Warning",
      "overriddenBy": [
        {
          "scopeName": "Global",
          "precedence": 1,
          "value": "Info"
        }
      ]
    },
    "server": {
      "scopeName": "Node-Specific",
      "precedence": 4,
      "value": "node1.prod.example.com",
      "overriddenBy": [
        {
          "scopeName": "Production",
          "precedence": 2,
          "value": "prod.example.com"
        }
      ]
    },
    "timeout": {
      "scopeName": "Global",
      "precedence": 1,
      "value": 30
    }
  }
}
```

### Scope Precedence

Scopes are merged in precedence order (higher precedence overrides
lower):

- **Global** (precedence 1) - Base parameters for all nodes
- **Environment** (precedence 2) - Environment-specific overrides
  (dev/staging/prod)
- **Role** (precedence 3) - Role-specific settings (web server, database)
- **Node** (precedence 4) - Node-specific overrides (highest priority)

### Example Workflow

1. **Create scopes:**

   ```bash
   # Global scope
   curl -X POST https://server/api/v1/scopes \
     -d '{"name": "Global", "precedence": 1}'

   # Production environment
   curl -X POST https://server/api/v1/scopes \
     -d '{"name": "Production", "precedence": 2}'

   # Specific node
   curl -X POST https://server/api/v1/scopes \
     -d '{"name": "Node-WebServer1", "precedence": 3}'
   ```

2. **Upload parameters for each scope:**

   ```yaml
   # Global (precedence 1)
   logLevel: Info
   timeout: 30
   server: localhost
   ```

   ```yaml
   # Production (precedence 2)
   logLevel: Warning
   server: prod.example.com
   ```

   ```yaml
   # Node-WebServer1 (precedence 3)
   server: webserver1.prod.example.com
   ```

3. **Result when node requests configuration:**

   ```yaml
   logLevel: Warning        # From Production (precedence 2)
   timeout: 30              # From Global (precedence 1)
   server: webserver1.prod.example.com  # From Node (precedence 3)
   ```

### Version Retention

Clean up old parameter versions to save disk space:

```bash
curl -X POST https://server/api/v1/retention/parameters/cleanup \
  -H "Authorization: Bearer $ADMIN_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "keepVersions": 5,
    "keepDays": 30,
    "dryRun": false
  }'
```

This keeps the 5 most recent versions and any versions created in the
last 30 days. Active versions are never deleted.

For more details on parameter merging, see the
[OpenDsc.Parameters README](../OpenDsc.Parameters/README.md).

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
