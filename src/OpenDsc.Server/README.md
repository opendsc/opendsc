# OpenDSC Pull Server

A modern REST API-based pull server for DSC v3, providing centralized configuration management for distributed systems.

## Features

- **Configuration Management**: Store and distribute DSC configurations to registered nodes
- **Node Registration**: FQDN-based node identification with automatic API key generation
- **Compliance Reporting**: Collect and store compliance reports from LCM agents
- **API Key Rotation**: Secure, atomic key rotation for node authentication
- **Multi-Database Support**: SQLite (default), SQL Server, and PostgreSQL
- **Docker Ready**: Multi-stage Dockerfile and docker-compose configurations
- **Interactive API Documentation**: Built-in Scalar API reference at `/scalar/v1`

## Quick Start

### Running with Docker

```bash
# Start with SQLite (default)
docker-compose up -d

# Start with PostgreSQL
docker-compose --profile postgres up -d

# Start with SQL Server
docker-compose --profile sqlserver up -d
```

### Running Locally

```bash
# Build the server
dotnet build

# Run the server
dotnet run
```

## API Endpoints

### Health

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/health` | GET | None | Liveness check |
| `/health/ready` | GET | None | Readiness check (includes database) |

### Nodes

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/v1/nodes/register` | POST | Registration Key | Register or re-register a node |
| `/api/v1/nodes` | GET | Admin | List all nodes |
| `/api/v1/nodes/{nodeId}` | GET | Admin | Get node details |
| `/api/v1/nodes/{nodeId}` | DELETE | Admin | Delete a node |
| `/api/v1/nodes/{nodeId}/configuration` | GET | Node | Download assigned configuration |
| `/api/v1/nodes/{nodeId}/configuration` | PUT | Admin | Assign configuration to node |
| `/api/v1/nodes/{nodeId}/configuration/checksum` | GET | Node | Get configuration checksum |
| `/api/v1/nodes/{nodeId}/rotate-key` | POST | Node | Rotate API key |

### Configurations

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/v1/configurations` | GET | Admin | List all configurations |
| `/api/v1/configurations` | POST | Admin | Create a new configuration |
| `/api/v1/configurations/{name}` | GET | Admin | Get configuration details |
| `/api/v1/configurations/{name}` | PUT | Admin | Update configuration content |
| `/api/v1/configurations/{name}` | DELETE | Admin | Delete a configuration |

### Reports

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/v1/nodes/{nodeId}/reports` | POST | Node | Submit a compliance report |
| `/api/v1/nodes/{nodeId}/reports` | GET | Admin | Get reports for a node |
| `/api/v1/reports` | GET | Admin | List all reports |
| `/api/v1/reports/{reportId}` | GET | Admin | Get report details |

### Settings

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/v1/settings` | GET | Admin | Get server settings |
| `/api/v1/settings` | PUT | Admin | Update server settings |
| `/api/v1/settings/registration-key/rotate` | POST | Admin | Rotate registration key |

## Authentication

### Node Authentication

Nodes authenticate using the `Authorization: Bearer {apiKey}` header. The API key is obtained during registration.

### Admin Authentication

Administrators authenticate using the `X-Admin-Key: {apiKey}` header.

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `Database__Provider` | Database provider (SQLite, SqlServer, PostgreSQL) | SQLite |
| `Database__ConnectionString` | Database connection string | Data Source=opendsc-server.db |
| `ASPNETCORE_URLS` | Server URL(s) | http://localhost:5000 |

### appsettings.json

```json
{
  "Database": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=opendsc-server.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
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
    "PullServer": {
      "ServerUrl": "http://your-server:8080",
      "RegistrationKey": "your-registration-key",
      "ReportCompliance": true
    }
  }
}
```

## Security Considerations

1. **Use HTTPS in Production**: Always use TLS for production deployments
2. **Secure Registration Key**: The registration key should be kept confidential
3. **Admin API Key**: Store the admin API key securely
4. **Key Rotation**: Enable automatic key rotation for nodes
5. **Database Security**: Use proper database authentication and encryption

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Publishing

```bash
dotnet publish -c Release
```

## License

MIT License - See LICENSE file for details.
