# Health endpoints

`/health`

Health probes for monitoring and orchestration tools. The liveness endpoint
confirms the server process is running, while the readiness endpoint verifies
that the database is reachable. These endpoints don't require authentication
and are suitable for use with load balancers and container orchestrators.

| Method | Route           | Description                                              |
| :----- | :-------------- | :------------------------------------------------------- |
| `GET`  | `/health/`      | Liveness check — indicates the server process is running |
| `GET`  | `/health/ready` | Readiness check — verifies database connectivity         |
