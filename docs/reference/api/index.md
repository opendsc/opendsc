# Pull Server API reference

The OpenDSC Pull Server exposes a REST API at version `v1` for managing nodes,
configurations,
parameters, and compliance reports. All API endpoints are prefixed with
`/api/v1/` unless otherwise
noted.

## Scalar API Documentation

Scalar is an interactive API documentation and API tester. To enable it, set
the `ASPNETCORE_ENVIRONMENT` environment variable to `Development` and restart
the server.

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    ```

=== "Shell"

    ```sh
    export ASPNETCORE_ENVIRONMENT=Development
    ```

!!! note
    If OpenDSC is running as a Windows service, set `ASPNETCORE_ENVIRONMENT`
    at the service level or machine level and then restart the service.

    The service registry key is:
    `HKLM:\SYSTEM\CurrentControlSet\Services\OpenDscServer`

    Add or update the `Environment` value under that key as a
    `REG_MULTI_SZ` (multiple string) with:
    `ASPNETCORE_ENVIRONMENT=Development`

<!-- markdownlint-enable MD046 -->

Once in development mode, navigate to `/scalar/v1` for an interactive API
reference. The OpenAPI schema is available at `/openapi/v1.json`.

## Authentication

The Pull Server supports multiple authentication mechanisms depending on the
client type:

| Mechanism             | Used by              | Description                                                       |
| :-------------------- | :------------------- | :---------------------------------------------------------------- |
| Cookie / Session      | Browser (Blazor UI)  | Standard login with username and password                         |
| mTLS (Mutual TLS)     | LCM nodes            | Client certificate validated against the node registration record |
| Personal Access Token | Automation / scripts | Bearer token passed in the `Authorization` header                 |
| Registration Key      | New nodes            | Shared secret used only during initial node registration          |

## Endpoints

| Category                                                | Description                                         |
|:--------------------------------------------------------|:----------------------------------------------------|
| [Authentication](authentication.md)                     | Sessions, passwords, and personal access tokens     |
| [Users](users.md)                                       | User account management                             |
| [Groups](groups.md)                                     | Group membership and external SSO mappings          |
| [Roles](roles.md)                                       | Role-based access control                           |
| [Health](health.md)                                     | Liveness and readiness probes                       |
| [Nodes](nodes.md)                                       | Node registration, configuration, tags, and reports |
| [Configurations](configurations.md)                     | Configuration documents, versions, and permissions  |
| [Composite Configurations](composite-configurations.md) | Multi-configuration bundles                         |
| [Parameters](parameters.md)                             | Environment-specific parameter files                |
| [Scope Types](scope-types.md)                           | Parameter merging hierarchy and scope values        |
| [Reports](reports.md)                                   | Compliance report queries                           |
| [Settings](settings.md)                                 | Server-wide settings, validation, and retention     |
| [Registration Keys](registration-keys.md)               | Keys for initial node registration                  |
| [Retention](retention.md)                               | Version and report cleanup                          |
