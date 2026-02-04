# OpenDSC Pull Server

A modern REST API-based pull server for DSC v3, providing centralized
configuration management for distributed systems.

## Features

- **Configuration Management**: Store and distribute DSC configurations
  to registered nodes
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
| `/api/v1/nodes/{nodeId}/configuration` | GET | Node | Download assigned configuration |
| `/api/v1/nodes/{nodeId}/configuration` | PUT | Admin | Assign configuration to node |
| `/api/v1/nodes/{nodeId}/configuration/checksum` | GET | Node | Get configuration checksum |
| `/api/v1/nodes/{nodeId}/rotate-certificate` | POST | Node | Rotate client certificate |

### Configurations

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/configurations` | GET | Admin | List all configurations |
| `/api/v1/configurations` | POST | Admin | Create a new configuration |
| `/api/v1/configurations/{name}` | GET | Admin | Get configuration details |
| `/api/v1/configurations/{name}` | PUT | Admin | Update configuration content |
| `/api/v1/configurations/{name}` | DELETE | Admin | Delete a configuration |

### Reports

| Endpoint | Method | Auth | Description |
| :-------- | :----- | :--- | :---------- |
| `/api/v1/nodes/{nodeId}/reports` | POST | Node | Submit a compliance report |
| `/api/v1/nodes/{nodeId}/reports` | GET | Admin | Get reports for a node |
| `/api/v1/reports` | GET | Admin | List all reports |
| `/api/v1/reports/{reportId}` | GET | Admin | Get report details |

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
