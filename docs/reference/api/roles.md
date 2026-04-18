# Role endpoints

`/api/v1/roles`

Manage roles that define what users and groups can do on the Pull Server.
Roles bundle a set of permissions and can be assigned to individual users or
groups. The server includes built-in roles and supports creating custom roles
for fine-grained access control.

| Method   | Route                | Description                         |
| :------- | :------------------- | :---------------------------------- |
| `GET`    | `/api/v1/roles/`     | List all roles                      |
| `GET`    | `/api/v1/roles/{id}` | Get role details                    |
| `POST`   | `/api/v1/roles/`     | Create a custom role                |
| `PUT`    | `/api/v1/roles/{id}` | Update role details and permissions |
| `DELETE` | `/api/v1/roles/{id}` | Delete a custom role                |
