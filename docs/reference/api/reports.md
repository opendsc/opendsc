# Report endpoints

`/api/v1/reports`

Query compliance reports submitted by LCM nodes. Reports capture the outcome
of each configuration check-in, including per-resource compliance status.
Use these endpoints to list and inspect reports across all nodes. To submit
reports or view reports for a specific node, see
[node reports].

| Method | Route                        | Description                  |
| :----- | :--------------------------- | :--------------------------- |
| `GET`  | `/api/v1/reports/`           | List all reports (paginated) |
| `GET`  | `/api/v1/reports/{reportId}` | Get report details           |

[node reports]: nodes.md#node-reports
