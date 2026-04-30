# Parameter endpoints

`/api/v1/parameters`

Manage parameter files that supply environment-specific values to
configurations. Parameters are scoped by scope type and configuration,
allowing different values at each level of the hierarchy (for example, global
defaults, per-environment, or per-node overrides). See
[parameter merging] for how the Pull Server resolves values across
scopes.

| Method   | Route                                                                           | Description                              |
| :------- | :------------------------------------------------------------------------------ | :--------------------------------------- |
| `PUT`    | `/api/v1/parameters/{scopeTypeId}/{configurationId}`                            | Create or update a parameter file        |
| `GET`    | `/api/v1/parameters/{scopeTypeId}/{configurationId}/versions`                   | List all parameter file versions         |
| `PUT`    | `/api/v1/parameters/{scopeTypeId}/{configurationId}/versions/{version}/publish` | Publish a parameter version              |
| `DELETE` | `/api/v1/parameters/{scopeTypeId}/{configurationId}/versions/{version}`         | Delete a parameter version               |
| `GET`    | `/api/v1/parameters/{scopeTypeId}/{configurationId}/majors`                     | List all major versions                  |
| `GET`    | `/api/v1/parameters/{scopeTypeId}/{configurationId}/majors/{major}`             | Get active parameter for a major version |

[parameter merging]: ../../concepts/pull-server/parameter-merging.md
