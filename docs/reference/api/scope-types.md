# Scope type endpoints

`/api/v1/scope-types`

Manage scope types and their values. Scope types define the levels of the
parameter merging hierarchy — for example, "Environment", "Region", or
"Datacenter". Each scope type contains scope values (such as "Production" or
"US-East") that are tagged onto nodes to determine which parameter files
apply. The order of scope types controls merge precedence. See
[scope system] for background.

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

## Scope values

| Method   | Route                                           | Description                      |
| :------- | :---------------------------------------------- | :------------------------------- |
| `GET`    | `/api/v1/scope-types/{scopeTypeId}/values/`     | List all values for a scope type |
| `GET`    | `/api/v1/scope-types/{scopeTypeId}/values/{id}` | Get a specific scope value       |
| `POST`   | `/api/v1/scope-types/{scopeTypeId}/values/`     | Create a scope value             |
| `PUT`    | `/api/v1/scope-types/{scopeTypeId}/values/{id}` | Update a scope value             |
| `DELETE` | `/api/v1/scope-types/{scopeTypeId}/values/{id}` | Delete a scope value             |

[scope system]: ../../concepts/pull-server/scope-system.md
