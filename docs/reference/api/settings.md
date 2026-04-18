# Settings endpoints

`/api/v1/settings`

Manage server-wide settings including public server metadata, LCM defaults
pushed to nodes, validation rules for configuration uploads, and global
retention policies. The `/public` endpoint is unauthenticated and returns
settings that clients need before they can authenticate.

| Method | Route                                | Description                                             |
| :----- | :----------------------------------- | :------------------------------------------------------ |
| `GET`  | `/api/v1/settings/public`            | Get public server settings (no authentication required) |
| `GET`  | `/api/v1/settings/`                  | Get server settings                                     |
| `PUT`  | `/api/v1/settings/`                  | Update server settings                                  |
| `POST` | `/api/v1/settings/registration-keys` | Rotate the registration key                             |
| `GET`  | `/api/v1/settings/lcm-defaults`      | Get default LCM settings                                |
| `PUT`  | `/api/v1/settings/lcm-defaults`      | Update default LCM settings                             |

## Validation settings

| Method | Route                         | Description                |
| :----- | :---------------------------- | :------------------------- |
| `GET`  | `/api/v1/settings/validation` | Get validation settings    |
| `PUT`  | `/api/v1/settings/validation` | Update validation settings |

## Retention settings

| Method | Route                        | Description                    |
| :----- | :--------------------------- | :----------------------------- |
| `GET`  | `/api/v1/settings/retention` | Get global retention policy    |
| `PUT`  | `/api/v1/settings/retention` | Update global retention policy |
