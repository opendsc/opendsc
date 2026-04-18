# Authentication endpoints

`/api/v1/auth`

Manage user sessions and personal access tokens. These endpoints handle
sign-in and sign-out flows for the Blazor UI, expose the current user
identity, and provide token lifecycle management for automation scenarios.
See [authentication] for an overview of the supported authentication
mechanisms.

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

[authentication]: ../../concepts/pull-server/authentication.md
