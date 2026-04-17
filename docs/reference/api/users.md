# User endpoints

`/api/v1/users`

Manage Pull Server user accounts. Users authenticate through the Blazor UI
or personal access tokens and are assigned roles that control access to
configurations, parameters, and administrative operations.

| Method   | Route                               | Description                  |
|:---------|:------------------------------------|:-----------------------------|
| `GET`    | `/api/v1/users`                     | List all users               |
| `GET`    | `/api/v1/users/{id}`                | Get user details             |
| `POST`   | `/api/v1/users`                     | Create a user                |
| `PUT`    | `/api/v1/users/{id}`                | Update a user                |
| `DELETE` | `/api/v1/users/{id}`                | Delete a user                |
| `POST`   | `/api/v1/users/{id}/reset-password` | Reset user password          |
| `POST`   | `/api/v1/users/{id}/unlock`         | Unlock a locked user account |
| `GET`    | `/api/v1/users/{id}/roles`          | Get roles assigned to a user |
| `PUT`    | `/api/v1/users/{id}/roles`          | Set roles for a user         |
