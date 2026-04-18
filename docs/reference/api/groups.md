# Group endpoints

`/api/v1/groups`

Manage groups for organizing users and assigning roles collectively. Groups
simplify access control by letting you grant permissions to a set of users at
once instead of individually. External group mappings allow single sign-on
(SSO) providers to automatically map external identity groups to Pull Server
groups.

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
