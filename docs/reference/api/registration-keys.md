# Registration key endpoints

`/api/v1/admin/registration-keys`

Manage registration keys used by new LCM nodes to authenticate during initial
registration. A registration key is a shared secret that a node presents once
to prove it is authorized to join the Pull Server. After registration, nodes
authenticate with mTLS certificates. Keys can be revoked to prevent further
registrations.

| Method   | Route                                     | Description                           |
| :------- | :---------------------------------------- | :------------------------------------ |
| `POST`   | `/api/v1/admin/registration-keys/`        | Create a registration key             |
| `GET`    | `/api/v1/admin/registration-keys/`        | List registration keys                |
| `PUT`    | `/api/v1/admin/registration-keys/{keyId}` | Update a registration key description |
| `DELETE` | `/api/v1/admin/registration-keys/{keyId}` | Revoke a registration key             |
