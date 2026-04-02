---
description: "Use when writing or reviewing xUnit tests for DSC resources, the LCM service, or the Pull Server. Covers test project locations, category conventions, and filter commands."
applyTo: "tests/**/*.cs"
---

# xUnit Test Conventions

## Test Project Structure

| Type | Folder pattern | Category trait | Run command |
|------|---------------|---------------|-------------|
| Unit | `tests/OpenDsc.*.Tests/` | `Unit` | `dotnet test --filter Category=Unit` |
| Integration | `tests/OpenDsc.*.IntegrationTests/` | `Integration` | `dotnet test --filter Category=Integration` |
| Functional | `tests/OpenDsc.*.FunctionalTests/` | `Functional` | `dotnet test --filter Category=Functional` |

**Functional tests** use Testcontainers and run against SQLite, PostgreSQL, and SQL Server. Located in `OpenDsc.Lcm.FunctionalTests` and `OpenDsc.Server.FunctionalTests`.

## Test Projects by Area

**DSC Resources** (`tests/OpenDsc.Resource.*.Tests/`):
- `OpenDsc.Resource.Windows.Tests`
- `OpenDsc.Resource.SqlServer.Tests`
- `OpenDsc.Resource.FileSystem.Tests`, `OpenDsc.Resource.Xml.Tests`, `OpenDsc.Resource.Json.Tests`
- `OpenDsc.Resource.Archive.Tests`, `OpenDsc.Resource.Posix.Tests`

Tests verify: schema generation (`GetSchema()` returns valid JSON), serialization/deserialization roundtrips, and resource logic.

**LCM** (`tests/OpenDsc.Lcm.*`):
- `OpenDsc.Lcm.Tests` — unit tests for `LcmWorker`, `DscExecutor`, `LcmConfig` parsing
- `OpenDsc.Lcm.IntegrationTests` — integration tests using real config files
- `OpenDsc.Lcm.FunctionalTests` — end-to-end tests with Testcontainers databases

**Pull Server** (`tests/OpenDsc.Server.*`):
- `OpenDsc.Server.Tests` — unit tests for endpoints, services, auth handlers
- `OpenDsc.Server.IntegrationTests` — tests with in-memory or SQLite DB
- `OpenDsc.Server.FunctionalTests` — cross-provider tests (SQLite, PostgreSQL, SQL Server via Testcontainers)

**Schema** (`tests/OpenDsc.Schema.Tests`): unit tests for the JSON schema generation library.

## Running Tests

```powershell
# All tests
dotnet test

# By category
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
dotnet test --filter Category=Functional

# Single project
dotnet test tests/OpenDsc.Resource.Windows.Tests/

# Build skips
.\build.ps1 -SkipUnitTests
.\build.ps1 -SkipIntegrationTests
.\build.ps1 -SkipFunctionalTests
```
