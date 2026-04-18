# Configuration endpoints

`/api/v1/configurations`

Manage DSC configuration documents and their versions. A configuration is a
named container that holds one or more versioned DSC configuration files.
Versions follow a draft-then-publish workflow — new uploads start as drafts
and must be explicitly published before nodes can pull them. See
[configuration management] and [versioning] for background.

| Method   | Route                           | Description                                 |
| :------- | :------------------------------ | :------------------------------------------ |
| `GET`    | `/api/v1/configurations/`       | List all configurations                     |
| `POST`   | `/api/v1/configurations/`       | Create a configuration                      |
| `GET`    | `/api/v1/configurations/{name}` | Get configuration details                   |
| `PATCH`  | `/api/v1/configurations/{name}` | Update configuration settings               |
| `DELETE` | `/api/v1/configurations/{name}` | Delete a configuration and all its versions |

## Configuration versions

| Method   | Route                                                                | Description                        |
| :------- | :------------------------------------------------------------------- | :--------------------------------- |
| `GET`    | `/api/v1/configurations/{name}/versions`                             | List all versions                  |
| `POST`   | `/api/v1/configurations/{name}/versions`                             | Create a new configuration version |
| `PUT`    | `/api/v1/configurations/{name}/versions/{version}/publish`           | Publish a draft version            |
| `DELETE` | `/api/v1/configurations/{name}/versions/{version}`                   | Delete a specific version          |
| `GET`    | `/api/v1/configurations/{name}/versions/{version}/files/{*filePath}` | Download a file from a version     |

## Configuration permissions

| Method   | Route                                                                     | Description                  |
| :------- | :------------------------------------------------------------------------ | :--------------------------- |
| `GET`    | `/api/v1/configurations/{name}/permissions`                               | List permission grants       |
| `PUT`    | `/api/v1/configurations/{name}/permissions`                               | Grant or update a permission |
| `DELETE` | `/api/v1/configurations/{name}/permissions/{principalType}/{principalId}` | Revoke a permission          |

## Configuration settings

| Method   | Route                                              | Description                                  |
| :------- | :------------------------------------------------- | :------------------------------------------- |
| `GET`    | `/api/v1/configurations/{name}/settings`           | Get configuration settings                   |
| `PUT`    | `/api/v1/configurations/{name}/settings`           | Update configuration settings                |
| `DELETE` | `/api/v1/configurations/{name}/settings`           | Delete configuration settings                |
| `GET`    | `/api/v1/configurations/{name}/settings/retention` | Get per-configuration retention overrides    |
| `PUT`    | `/api/v1/configurations/{name}/settings/retention` | Set per-configuration retention overrides    |
| `DELETE` | `/api/v1/configurations/{name}/settings/retention` | Remove per-configuration retention overrides |

## Configuration parameters

| Method   | Route                                                                                | Description                                   |
| :------- | :----------------------------------------------------------------------------------- | :-------------------------------------------- |
| `PUT`    | `/api/v1/configurations/{name}/parameters`                                           | Upload parameter schema                       |
| `POST`   | `/api/v1/configurations/{name}/parameters/validate`                                  | Validate a parameter file against its schema  |
| `POST`   | `/api/v1/configurations/{name}/parameters/parameter-files`                           | Upload a parameter file for a scope           |
| `GET`    | `/api/v1/configurations/{name}/parameters/permissions`                               | List parameter schema permission grants       |
| `PUT`    | `/api/v1/configurations/{name}/parameters/permissions`                               | Grant or update a parameter schema permission |
| `DELETE` | `/api/v1/configurations/{name}/parameters/permissions/{principalType}/{principalId}` | Revoke a parameter schema permission          |

[configuration management]: ../../concepts/pull-server/configuration-management.md
[versioning]: ../../concepts/pull-server/versioning.md
