# Node endpoints

`/api/v1/nodes`

Manage registered LCM nodes and their lifecycle. A node represents a machine
running the Local Configuration Manager (LCM) that has registered with the
Pull Server using mTLS. These endpoints cover registration, certificate
rotation, configuration assignment, compliance reporting, scope tagging, and
LCM status tracking.

| Method   | Route                                       | Description                                      |
| :------- | :------------------------------------------ | :----------------------------------------------- |
| `POST`   | `/api/v1/nodes/register`                    | Register a node with mTLS certificate            |
| `GET`    | `/api/v1/nodes/`                            | List all registered nodes                        |
| `GET`    | `/api/v1/nodes/{nodeId}`                    | Get node details                                 |
| `DELETE` | `/api/v1/nodes/{nodeId}`                    | Delete a node                                    |
| `POST`   | `/api/v1/nodes/{nodeId}/rotate-certificate` | Rotate the node's mTLS certificate               |
| `PUT`    | `/api/v1/nodes/{nodeId}/lcm-status`         | Update the node's LCM operational status         |
| `GET`    | `/api/v1/nodes/{nodeId}/status-history`     | Get the node's LCM and compliance status history |
| `GET`    | `/api/v1/nodes/{nodeId}/lcm-config`         | Get the desired LCM configuration for a node     |
| `PUT`    | `/api/v1/nodes/{nodeId}/lcm-config`         | Update the desired LCM configuration for a node  |
| `PUT`    | `/api/v1/nodes/{nodeId}/reported-config`    | Report the current LCM configuration from a node |

## Node configuration

| Method   | Route                                           | Description                                  |
| :------- | :---------------------------------------------- | :------------------------------------------- |
| `GET`    | `/api/v1/nodes/{nodeId}/configuration`          | Download the assigned configuration document |
| `PUT`    | `/api/v1/nodes/{nodeId}/configuration`          | Assign a configuration by name               |
| `DELETE` | `/api/v1/nodes/{nodeId}/configuration`          | Unassign the current configuration           |
| `GET`    | `/api/v1/nodes/{nodeId}/configuration/checksum` | Get the configuration checksum               |
| `GET`    | `/api/v1/nodes/{nodeId}/configuration/bundle`   | Download the configuration bundle (ZIP)      |

## Node tags

| Method   | Route                                        | Description                             |
| :------- | :------------------------------------------- | :-------------------------------------- |
| `GET`    | `/api/v1/nodes/{nodeId}/tags/`               | Get scope value tags assigned to a node |
| `POST`   | `/api/v1/nodes/{nodeId}/tags/`               | Assign a scope value tag to a node      |
| `DELETE` | `/api/v1/nodes/{nodeId}/tags/{scopeValueId}` | Remove a scope value tag from a node    |

## Node reports

| Method | Route                             | Description                                    |
| :----- | :-------------------------------- | :--------------------------------------------- |
| `POST` | `/api/v1/nodes/{nodeId}/reports/` | Submit a compliance report                     |
| `GET`  | `/api/v1/nodes/{nodeId}/reports/` | Get reports for a node (paginated, filterable) |

## Node parameters

| Method | Route                                          | Description                                    |
| :----- | :--------------------------------------------- | :--------------------------------------------- |
| `GET`  | `/api/v1/nodes/{nodeId}/parameters/provenance` | Get parameter provenance showing scope lineage |
| `GET`  | `/api/v1/nodes/{nodeId}/parameters/resolution` | Get parameter version resolution per scope     |
