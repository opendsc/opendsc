# SQL Server Login resource tests

These tests require a SQL Server instance to run against.

## Prerequisites

1. SQL Server instance (any edition: Developer, Express, Standard, Enterprise)
2. The tests assume connection to a local default instance (`.` or `localhost`)
3. The user running the tests needs `sysadmin` or equivalent permissions

## Configuration

Set the `SQLSERVER_INSTANCE` environment variable to specify a non-default
instance:

```powershell
$env:SQLSERVER_INSTANCE = "localhost\SQLEXPRESS"
```

## Skipping tests

Tests are automatically skipped if:

- SQL Server is not accessible
