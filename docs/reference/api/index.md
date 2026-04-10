# Pull Server API reference

The OpenDSC Pull Server exposes a REST API at version `v1` for managing nodes,
configurations,
parameters, and compliance reports. All API endpoints are prefixed with
`/api/v1/` unless otherwise
noted.

**API documentation:** In development mode, navigate to `/scalar/v1` for an
interactive API
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

## Authentication endpoints

`/api/v1/auth`

| Method   | Route                          | Description                                      |
| :------- | :----------------------------- | :----------------------------------------------- |
| `POST`   | `/api/v1/auth/login`           | Sign in with username and password               |
| `POST`   | `/api/v1/auth/logout`          | Sign out and end the current session             |
| `GET`    | `/api/v1/auth/logout-redirect` | Sign out and redirect to the login page          |
| `GET`    | `/api/v1/auth/me`              | Return the current authenticated user            |
| `POST`   | `/api/v1/auth/change-password` | Change the current user's password               |
| `POST`   | `/api/v1/auth/tokens`          | Create a Personal Access Token (PAT)             |
| `GET`    | `/api/v1/auth/tokens`          | List Personal Access Tokens for the current user |
| `DELETE` | `/api/v1/auth/tokens/{id}`     | Revoke a Personal Access Token                   |

## User endpoints

`/api/v1/users`

| Method   | Route                               | Description                  |
| :------- | :---------------------------------- | :--------------------------- |
| `GET`    | `/api/v1/users/`                    | List all users               |
| `GET`    | `/api/v1/users/{id}`                | Get user details             |
| `POST`   | `/api/v1/users/`                    | Create a user                |
| `PUT`    | `/api/v1/users/{id}`                | Update a user                |
| `DELETE` | `/api/v1/users/{id}`                | Delete a user                |
| `POST`   | `/api/v1/users/{id}/reset-password` | Reset user password          |
| `POST`   | `/api/v1/users/{id}/unlock`         | Unlock a locked user account |
| `GET`    | `/api/v1/users/{id}/roles`          | Get roles assigned to a user |
| `PUT`    | `/api/v1/users/{id}/roles`          | Set roles for a user         |

## Group endpoints

`/api/v1/groups`

| Method   | Route                                   | Description                        |
| :------- | :-------------------------------------- | :--------------------------------- |
| `GET`    | `/api/v1/groups/`                       | List all groups                    |
| `GET`    | `/api/v1/groups/{id}`                   | Get group details                  |
| `POST`   | `/api/v1/groups/`                       | Create a group                     |
| `PUT`    | `/api/v1/groups/{id}`                   | Update a group                     |
| `DELETE` | `/api/v1/groups/{id}`                   | Delete a group                     |
| `GET`    | `/api/v1/groups/{id}/members`           | Get group members                  |
| `PUT`    | `/api/v1/groups/{id}/members`           | Set group members                  |
| `GET`    | `/api/v1/groups/{id}/roles`             | Get roles assigned to a group      |
| `PUT`    | `/api/v1/groups/{id}/roles`             | Set roles for a group              |
| `GET`    | `/api/v1/groups/external-mappings`      | List external group mappings (SSO) |
| `POST`   | `/api/v1/groups/external-mappings`      | Create an external group mapping   |
| `DELETE` | `/api/v1/groups/external-mappings/{id}` | Delete an external group mapping   |

## Role endpoints

`/api/v1/roles`

| Method   | Route                | Description                         |
| :------- | :------------------- | :---------------------------------- |
| `GET`    | `/api/v1/roles/`     | List all roles                      |
| `GET`    | `/api/v1/roles/{id}` | Get role details                    |
| `POST`   | `/api/v1/roles/`     | Create a custom role                |
| `PUT`    | `/api/v1/roles/{id}` | Update role details and permissions |
| `DELETE` | `/api/v1/roles/{id}` | Delete a custom role                |

## Health endpoints

`/health`

These endpoints don't require authentication.

| Method | Route           | Description                                              |
| :----- | :-------------- | :------------------------------------------------------- |
| `GET`  | `/health/`      | Liveness check — indicates the server process is running |
| `GET`  | `/health/ready` | Readiness check — verifies database connectivity         |

## Node endpoints

`/api/v1/nodes`

| Method   | Route                                       | Description                                      |
| :------- | :------------------------------------------ | :----------------------------------------------- |
| `POST`   | `/api/v1/nodes/register`                    | Register a node with mTLS certificate            |
| `GET`    | `/api/v1/nodes/`                            | List all registered nodes                        |
| `GET`    | `/api/v1/nodes/{nodeId}`                    | Get node details                                 |
| `DELETE` | `/api/v1/nodes/{nodeId}`                    | Delete a node                                    |
| `POST`   | `/api/v1/nodes/{nodeId}/rotate-certificate` | Rotate the node's mTLS certificate               |
| `PUT`    | `/api/v1/nodes/{nodeId}/lcm-status`         | Update the node's LCM operational status         |
| `GET`    | `/api/v1/nodes/{nodeId}/status-history`     | Get the node's LCM and compliance status history |
| `GET`    | `/api/v1/nodes/{nodeId}/lcm-config`         | Get the desired LCM configuration for a node     |
| `PUT`    | `/api/v1/nodes/{nodeId}/lcm-config`         | Update the desired LCM configuration for a node  |
| `PUT`    | `/api/v1/nodes/{nodeId}/reported-config`    | Report the current LCM configuration from a node |

### Node configuration

| Method   | Route                                           | Description                                  |
| :------- | :---------------------------------------------- | :------------------------------------------- |
| `GET`    | `/api/v1/nodes/{nodeId}/configuration`          | Download the assigned configuration document |
| `PUT`    | `/api/v1/nodes/{nodeId}/configuration`          | Assign a configuration by name               |
| `DELETE` | `/api/v1/nodes/{nodeId}/configuration`          | Unassign the current configuration           |
| `GET`    | `/api/v1/nodes/{nodeId}/configuration/checksum` | Get the configuration checksum               |
| `GET`    | `/api/v1/nodes/{nodeId}/configuration/bundle`   | Download the configuration bundle (ZIP)      |

### Node tags

| Method   | Route                                        | Description                             |
| :------- | :------------------------------------------- | :-------------------------------------- |
| `GET`    | `/api/v1/nodes/{nodeId}/tags/`               | Get scope value tags assigned to a node |
| `POST`   | `/api/v1/nodes/{nodeId}/tags/`               | Assign a scope value tag to a node      |
| `DELETE` | `/api/v1/nodes/{nodeId}/tags/{scopeValueId}` | Remove a scope value tag from a node    |

### Node reports

| Method | Route                             | Description                                    |
| :----- | :-------------------------------- | :--------------------------------------------- |
| `POST` | `/api/v1/nodes/{nodeId}/reports/` | Submit a compliance report                     |
| `GET`  | `/api/v1/nodes/{nodeId}/reports/` | Get reports for a node (paginated, filterable) |

### Node parameters

| Method | Route                                          | Description                                    |
| :----- | :--------------------------------------------- | :--------------------------------------------- |
| `GET`  | `/api/v1/nodes/{nodeId}/parameters/provenance` | Get parameter provenance showing scope lineage |
| `GET`  | `/api/v1/nodes/{nodeId}/parameters/resolution` | Get parameter version resolution per scope     |

## Configuration endpoints

`/api/v1/configurations`

| Method   | Route                           | Description                                 |
| :------- | :------------------------------ | :------------------------------------------ |
| `GET`    | `/api/v1/configurations/`       | List all configurations                     |
| `POST`   | `/api/v1/configurations/`       | Create a configuration                      |
| `GET`    | `/api/v1/configurations/{name}` | Get configuration details                   |
| `PATCH`  | `/api/v1/configurations/{name}` | Update configuration settings               |
| `DELETE` | `/api/v1/configurations/{name}` | Delete a configuration and all its versions |

### Configuration versions

| Method   | Route                                                                | Description                        |
| :------- | :------------------------------------------------------------------- | :--------------------------------- |
| `GET`    | `/api/v1/configurations/{name}/versions`                             | List all versions                  |
| `POST`   | `/api/v1/configurations/{name}/versions`                             | Create a new configuration version |
| `PUT`    | `/api/v1/configurations/{name}/versions/{version}/publish`           | Publish a draft version            |
| `DELETE` | `/api/v1/configurations/{name}/versions/{version}`                   | Delete a specific version          |
| `GET`    | `/api/v1/configurations/{name}/versions/{version}/files/{*filePath}` | Download a file from a version     |

### Configuration permissions

| Method   | Route                                                                     | Description                  |
| :------- | :------------------------------------------------------------------------ | :--------------------------- |
| `GET`    | `/api/v1/configurations/{name}/permissions`                               | List permission grants       |
| `PUT`    | `/api/v1/configurations/{name}/permissions`                               | Grant or update a permission |
| `DELETE` | `/api/v1/configurations/{name}/permissions/{principalType}/{principalId}` | Revoke a permission          |

### Configuration settings

| Method   | Route                                              | Description                                  |
| :------- | :------------------------------------------------- | :------------------------------------------- |
| `GET`    | `/api/v1/configurations/{name}/settings`           | Get configuration settings                   |
| `PUT`    | `/api/v1/configurations/{name}/settings`           | Update configuration settings                |
| `DELETE` | `/api/v1/configurations/{name}/settings`           | Delete configuration settings                |
| `GET`    | `/api/v1/configurations/{name}/settings/retention` | Get per-configuration retention overrides    |
| `PUT`    | `/api/v1/configurations/{name}/settings/retention` | Set per-configuration retention overrides    |
| `DELETE` | `/api/v1/configurations/{name}/settings/retention` | Remove per-configuration retention overrides |

### Configuration parameters

| Method   | Route                                                                                | Description                                   |
| :------- | :----------------------------------------------------------------------------------- | :-------------------------------------------- |
| `PUT`    | `/api/v1/configurations/{name}/parameters`                                           | Upload parameter schema                       |
| `POST`   | `/api/v1/configurations/{name}/parameters/validate`                                  | Validate a parameter file against its schema  |
| `POST`   | `/api/v1/configurations/{name}/parameters/parameter-files`                           | Upload a parameter file for a scope           |
| `GET`    | `/api/v1/configurations/{name}/parameters/permissions`                               | List parameter schema permission grants       |
| `PUT`    | `/api/v1/configurations/{name}/parameters/permissions`                               | Grant or update a parameter schema permission |
| `DELETE` | `/api/v1/configurations/{name}/parameters/permissions/{principalType}/{principalId}` | Revoke a parameter schema permission          |

## Composite configuration endpoints

`/api/v1/composite-configurations`

| Method   | Route                                     | Description                                       |
| :------- | :---------------------------------------- | :------------------------------------------------ |
| `GET`    | `/api/v1/composite-configurations/`       | List all composite configurations                 |
| `POST`   | `/api/v1/composite-configurations/`       | Create a composite configuration                  |
| `GET`    | `/api/v1/composite-configurations/{name}` | Get composite configuration details               |
| `DELETE` | `/api/v1/composite-configurations/{name}` | Delete a composite configuration and all versions |

### Composite configuration versions

| Method   | Route                                                                | Description                            |
| :------- | :------------------------------------------------------------------- | :------------------------------------- |
| `POST`   | `/api/v1/composite-configurations/{name}/versions`                   | Create a new composite version (draft) |
| `GET`    | `/api/v1/composite-configurations/{name}/versions`                   | List all composite versions            |
| `GET`    | `/api/v1/composite-configurations/{name}/versions/{version}`         | Get a specific version                 |
| `PUT`    | `/api/v1/composite-configurations/{name}/versions/{version}/publish` | Publish a draft version                |
| `DELETE` | `/api/v1/composite-configurations/{name}/versions/{version}`         | Delete a specific version              |

### Composite configuration children

| Method   | Route                                                                           | Description                  |
| :------- | :------------------------------------------------------------------------------ | :--------------------------- |
| `POST`   | `/api/v1/composite-configurations/{name}/versions/{version}/children`           | Add a child configuration    |
| `PUT`    | `/api/v1/composite-configurations/{name}/versions/{version}/children/{childId}` | Update a child configuration |
| `DELETE` | `/api/v1/composite-configurations/{name}/versions/{version}/children/{childId}` | Remove a child configuration |

### Composite configuration permissions

| Method   | Route                                                                               | Description                  |
| :------- | :---------------------------------------------------------------------------------- | :--------------------------- |
| `GET`    | `/api/v1/composite-configurations/{name}/permissions`                               | List permission grants       |
| `PUT`    | `/api/v1/composite-configurations/{name}/permissions`                               | Grant or update a permission |
| `DELETE` | `/api/v1/composite-configurations/{name}/permissions/{principalType}/{principalId}` | Revoke a permission          |

## Parameter endpoints

`/api/v1/parameters`

| Method   | Route                                                                           | Description                              |
| :------- | :------------------------------------------------------------------------------ | :--------------------------------------- |
| `PUT`    | `/api/v1/parameters/{scopeTypeId}/{configurationId}`                            | Create or update a parameter file        |
| `GET`    | `/api/v1/parameters/{scopeTypeId}/{configurationId}/versions`                   | List all parameter file versions         |
| `PUT`    | `/api/v1/parameters/{scopeTypeId}/{configurationId}/versions/{version}/publish` | Publish a parameter version              |
| `DELETE` | `/api/v1/parameters/{scopeTypeId}/{configurationId}/versions/{version}`         | Delete a parameter version               |
| `GET`    | `/api/v1/parameters/{scopeTypeId}/{configurationId}/majors`                     | List all major versions                  |
| `GET`    | `/api/v1/parameters/{scopeTypeId}/{configurationId}/majors/{major}`             | Get active parameter for a major version |

## Scope type endpoints

`/api/v1/scope-types`

Scope types define the parameter merging hierarchy. See
[scope system](../../concepts/pull-server/scope-system.md) for background.

| Method   | Route                              | Description                                |
| :------- | :--------------------------------- | :----------------------------------------- |
| `GET`    | `/api/v1/scope-types/`             | List all scope types ordered by precedence |
| `GET`    | `/api/v1/scope-types/{id}`         | Get a specific scope type                  |
| `POST`   | `/api/v1/scope-types/`             | Create a scope type                        |
| `PUT`    | `/api/v1/scope-types/{id}`         | Update a scope type                        |
| `PUT`    | `/api/v1/scope-types/reorder`      | Atomically reorder all scope types         |
| `DELETE` | `/api/v1/scope-types/{id}`         | Delete a scope type                        |
| `PATCH`  | `/api/v1/scope-types/{id}/enable`  | Enable a system scope type                 |
| `PATCH`  | `/api/v1/scope-types/{id}/disable` | Disable a system scope type                |

### Scope values

| Method   | Route                                           | Description                      |
| :------- | :---------------------------------------------- | :------------------------------- |
| `GET`    | `/api/v1/scope-types/{scopeTypeId}/values/`     | List all values for a scope type |
| `GET`    | `/api/v1/scope-types/{scopeTypeId}/values/{id}` | Get a specific scope value       |
| `POST`   | `/api/v1/scope-types/{scopeTypeId}/values/`     | Create a scope value             |
| `PUT`    | `/api/v1/scope-types/{scopeTypeId}/values/{id}` | Update a scope value             |
| `DELETE` | `/api/v1/scope-types/{scopeTypeId}/values/{id}` | Delete a scope value             |

## Report endpoints

`/api/v1/reports`

| Method | Route                        | Description                  |
| :----- | :--------------------------- | :--------------------------- |
| `GET`  | `/api/v1/reports/`           | List all reports (paginated) |
| `GET`  | `/api/v1/reports/{reportId}` | Get report details           |

Node-scoped report submission is listed under [Node reports](#node-reports).

## Settings endpoints

`/api/v1/settings`

| Method | Route                                | Description                                             |
| :----- | :----------------------------------- | :------------------------------------------------------ |
| `GET`  | `/api/v1/settings/public`            | Get public server settings (no authentication required) |
| `GET`  | `/api/v1/settings/`                  | Get server settings                                     |
| `PUT`  | `/api/v1/settings/`                  | Update server settings                                  |
| `POST` | `/api/v1/settings/registration-keys` | Rotate the registration key                             |
| `GET`  | `/api/v1/settings/lcm-defaults`      | Get default LCM settings                                |
| `PUT`  | `/api/v1/settings/lcm-defaults`      | Update default LCM settings                             |

### Validation settings

| Method | Route                         | Description                |
| :----- | :---------------------------- | :------------------------- |
| `GET`  | `/api/v1/settings/validation` | Get validation settings    |
| `PUT`  | `/api/v1/settings/validation` | Update validation settings |

### Retention settings

| Method | Route                        | Description                    |
| :----- | :--------------------------- | :----------------------------- |
| `GET`  | `/api/v1/settings/retention` | Get global retention policy    |
| `PUT`  | `/api/v1/settings/retention` | Update global retention policy |

## Registration key endpoints

`/api/v1/admin/registration-keys`

| Method   | Route                                     | Description                           |
| :------- | :---------------------------------------- | :------------------------------------ |
| `POST`   | `/api/v1/admin/registration-keys/`        | Create a registration key             |
| `GET`    | `/api/v1/admin/registration-keys/`        | List registration keys                |
| `PUT`    | `/api/v1/admin/registration-keys/{keyId}` | Update a registration key description |
| `DELETE` | `/api/v1/admin/registration-keys/{keyId}` | Revoke a registration key             |

## Retention endpoints

`/api/v1/retention`

| Method | Route                                                | Description                                   |
| :----- | :--------------------------------------------------- | :-------------------------------------------- |
| `POST` | `/api/v1/retention/configurations/cleanup`           | Clean up old configuration versions           |
| `POST` | `/api/v1/retention/parameters/cleanup`               | Clean up old parameter versions               |
| `POST` | `/api/v1/retention/composite-configurations/cleanup` | Clean up old composite configuration versions |
| `POST` | `/api/v1/retention/reports/cleanup`                  | Clean up old compliance reports               |
| `POST` | `/api/v1/retention/status-events/cleanup`            | Clean up old LCM status events                |
| `GET`  | `/api/v1/retention/runs`                             | Get retention run history                     |

## PowerShell examples

Use `Invoke-RestMethod` to interact with the API:

```powershell
# Authenticate and store the session
$body = @{ username = 'admin'; password = 'P@ssw0rd' } | ConvertTo-Json
Invoke-RestMethod -Uri 'https://pull-server:5001/api/v1/auth/login' `
    -Method Post -Body $body -ContentType 'application/json' `
    -SessionVariable session

# List all registered nodes
Invoke-RestMethod -Uri 'https://pull-server:5001/api/v1/nodes/' `
    -WebSession $session

# Get a specific configuration
Invoke-RestMethod -Uri 'https://pull-server:5001/api/v1/configurations/web-servers' `
    -WebSession $session

# Create a Personal Access Token for automation
$tokenBody = @{ description = 'CI pipeline'; expiresInDays = 90 } | ConvertTo-Json
$token = Invoke-RestMethod -Uri 'https://pull-server:5001/api/v1/auth/tokens' `
    -Method Post -Body $tokenBody -ContentType 'application/json' `
    -WebSession $session

# Use the PAT in subsequent requests
$headers = @{ Authorization = "Bearer $($token.token)" }
Invoke-RestMethod -Uri 'https://pull-server:5001/api/v1/nodes/' -Headers $headers
```
