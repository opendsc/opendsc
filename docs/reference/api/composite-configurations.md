# Composite configuration endpoints

`/api/v1/composite-configurations`

Manage composite configurations that combine multiple child configurations
into a single deployable unit. A composite configuration references other
configurations by name and version, letting you build layered or role-based
configuration sets. Like regular configurations, composite versions follow a
draft-then-publish workflow.

| Method   | Route                                     | Description                                       |
| :------- | :---------------------------------------- | :------------------------------------------------ |
| `GET`    | `/api/v1/composite-configurations/`       | List all composite configurations                 |
| `POST`   | `/api/v1/composite-configurations/`       | Create a composite configuration                  |
| `GET`    | `/api/v1/composite-configurations/{name}` | Get composite configuration details               |
| `DELETE` | `/api/v1/composite-configurations/{name}` | Delete a composite configuration and all versions |

## Composite configuration versions

| Method   | Route                                                                | Description                            |
| :------- | :------------------------------------------------------------------- | :------------------------------------- |
| `POST`   | `/api/v1/composite-configurations/{name}/versions`                   | Create a new composite version (draft) |
| `GET`    | `/api/v1/composite-configurations/{name}/versions`                   | List all composite versions            |
| `GET`    | `/api/v1/composite-configurations/{name}/versions/{version}`         | Get a specific version                 |
| `PUT`    | `/api/v1/composite-configurations/{name}/versions/{version}/publish` | Publish a draft version                |
| `DELETE` | `/api/v1/composite-configurations/{name}/versions/{version}`         | Delete a specific version              |

## Composite configuration children

| Method   | Route                                                                           | Description                  |
| :------- | :------------------------------------------------------------------------------ | :--------------------------- |
| `POST`   | `/api/v1/composite-configurations/{name}/versions/{version}/children`           | Add a child configuration    |
| `PUT`    | `/api/v1/composite-configurations/{name}/versions/{version}/children/{childId}` | Update a child configuration |
| `DELETE` | `/api/v1/composite-configurations/{name}/versions/{version}/children/{childId}` | Remove a child configuration |

## Composite configuration permissions

| Method   | Route                                                                               | Description                  |
| :------- | :---------------------------------------------------------------------------------- | :--------------------------- |
| `GET`    | `/api/v1/composite-configurations/{name}/permissions`                               | List permission grants       |
| `PUT`    | `/api/v1/composite-configurations/{name}/permissions`                               | Grant or update a permission |
| `DELETE` | `/api/v1/composite-configurations/{name}/permissions/{principalType}/{principalId}` | Revoke a permission          |
