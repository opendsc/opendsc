# Retention endpoints

`/api/v1/retention`

Trigger cleanup of old versions and data according to the configured retention
policies. The Pull Server retains a configurable number of configuration
versions, parameter versions, composite configuration versions, compliance
reports, and LCM status events. Use these endpoints to run cleanup on demand
or inspect previous retention runs.

| Method | Route                                                | Description                                   |
| :----- | :--------------------------------------------------- | :-------------------------------------------- |
| `POST` | `/api/v1/retention/configurations/cleanup`           | Clean up old configuration versions           |
| `POST` | `/api/v1/retention/parameters/cleanup`               | Clean up old parameter versions               |
| `POST` | `/api/v1/retention/composite-configurations/cleanup` | Clean up old composite configuration versions |
| `POST` | `/api/v1/retention/reports/cleanup`                  | Clean up old compliance reports               |
| `POST` | `/api/v1/retention/status-events/cleanup`            | Clean up old LCM status events                |
| `GET`  | `/api/v1/retention/runs`                             | Get retention run history                     |
