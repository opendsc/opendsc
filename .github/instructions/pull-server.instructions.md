---
description: "Use when working on the Pull Server REST API, Blazor web UI, EF Core data layer, authentication handlers, or services. Covers mTLS, API endpoint conventions, Blazor/MudBlazor patterns, parameter merging, RBAC, database configuration, and LCM integration points."
applyTo: "src/OpenDsc.Server/**"
---

# Pull Server (OpenDsc.Server)

ASP.NET Core application combining a minimal REST API and Blazor Server web UI for centralized DSC configuration management.

## Key Components

| Path | Purpose |
|------|---------|
| `Program.cs` | Entry point: Blazor + minimal API + service registration |
| `Components/` | Blazor Server web UI (MudBlazor) |
| `Endpoints/` | Minimal API endpoint groups |
| `Data/` | EF Core database layer (SQLite, PostgreSQL, SQL Server) |
| `Authentication/` | `CertificateAuthHandler` (mTLS), `PersonalAccessTokenHandler` (PAT) |
| `Services/` | Business logic: parameter merging, schema validation, version retention, RBAC |
| `Entities/` | EF Core entity models |

## API Endpoint Registration

All endpoints registered via extension methods in `Program.cs`:

```csharp
app.MapAuthenticationEndpoints();          // /api/v1/auth/*
app.MapNodeEndpoints();                    // /api/v1/nodes/*
app.MapConfigurationEndpoints();           // /api/v1/configurations/*
app.MapCompositeConfigurationEndpoints();  // /api/v1/composite-configurations/*
app.MapParameterEndpoints();              // /api/v1/parameters/*
app.MapReportEndpoints();                 // /api/v1/reports/*
app.MapScopeTypeEndpoints();              // /api/v1/scope-types/*
app.MapScopeValueEndpoints();             // /api/v1/scope-values/*
app.MapNodeTagEndpoints();                // /api/v1/node-tags/*
app.MapRegistrationKeyEndpoints();        // /api/v1/registration-keys/*
app.MapRetentionEndpoints();              // /api/v1/retention/*
app.MapSettingsEndpoints();               // /api/v1/settings/*
app.MapValidationSettingsEndpoints();     // /api/v1/validation-settings/*
app.MapConfigurationSettingsEndpoints();  // /api/v1/configuration-settings/*
app.MapUserEndpoints();                   // /api/v1/users/*
app.MapGroupEndpoints();                  // /api/v1/groups/*
app.MapRoleEndpoints();                   // /api/v1/roles/*
app.MapHealthEndpoints();                 // /health, /health/ready
```

Each group lives in its own file in `Endpoints/`.

## Authentication

| Method | Used For |
|--------|---------|
| Registration Key | Initial node registration (shared secret) |
| mTLS (client certificate) | Node API operations via `CertificateAuthHandler` |
| Personal Access Token (PAT) | User/automation API access via `PersonalAccessTokenHandler` |
| Cookie/Session | Blazor web UI after password login |

**mTLS flow:**
1. LCM connects with client certificate during registration → server stores thumbprint, subject DN, expiration, issues `NodeId`
2. Subsequent requests → `CertificateAuthHandler` validates certificate thumbprint against DB record
3. Certificate rotation → `POST /api/v1/nodes/{nodeId}/rotate-certificate` → atomically updates, old certificate immediately invalidated

**Kestrel config**: `ClientCertificateMode.AllowCertificate` (not required — allows browser/UI traffic). Certificate validation enforced selectively in `CertificateAuthHandler` for node endpoints only.

Key files: [`Authentication/CertificateAuthHandler.cs`](../../src/OpenDsc.Server/Authentication/CertificateAuthHandler.cs), [`Entities/Node.cs`](../../src/OpenDsc.Server/Entities/Node.cs) (stores `CertificateThumbprint`, `CertificateSubject`, `CertificateNotAfter`), [`Endpoints/NodeEndpoints.cs`](../../src/OpenDsc.Server/Endpoints/NodeEndpoints.cs).

## Blazor Web UI (MudBlazor)

**All UI components use MudBlazor.** Do not use raw HTML forms or Bootstrap.

```
Components/
├── Layout/              # MudBlazor nav + theme
├── Pages/
│   ├── Dashboard.razor
│   ├── Nodes/
│   ├── Configurations/
│   ├── Parameters/
│   ├── Reports/
│   ├── Settings/
│   └── Admin/           # Users, Groups, Roles
└── Shared/              # Reusable components and dialogs
```

**Key conventions:**
- Use `MudDialog`, `MudTable`, `MudCard`, `MudAlert`, etc. — never custom dialog components
- Create/edit operations use dialogs (e.g., `CreateUserDialog.razor`, `EditGroupDialog.razor`)
- Pages use `[Authorize(Policy = "...")]` with policy names from `Program.cs` (e.g., `"nodes.read"`, `"configurations.write"`)
- `ThemeService` manages dark/light mode — inject in layout components
- API calls go through `ConfigurationApiClient` / `ParameterApiClient` service wrappers, not direct `HttpClient`
- Parameter pages include `ProvenanceVisualizationPanel` (merge lineage) and `VersionManagementPanel`

## Parameter Merging

Scope hierarchy — broad to narrow (narrow overrides):

```
Default → Region → Environment → Node
```

- `ScopeType` — defines scope hierarchy and precedence order
- `ScopeValue` — instances of a scope type (e.g., region "West", env "Prod")
- Nodes tagged via `NodeTag` entries linking them to scope values
- `IParameterMerger` / `ParameterMergeService` performs the merge
- `IParameterSchemaService` validates merged result against the configuration's parameter schema
- Schema version promotion: `ParameterCompatibilityService` checks existing parameter files for breaking changes, surfaces them in `ParameterMigrationDialog`

## RBAC

- `GroupClaimsTransformation` adds group claims to the principal
- `ResourceAuthorizationService` enforces resource-level access
- Authorization policies defined in `Program.cs` (e.g., `"nodes.read"`, `"configurations.write"`)

## Version Retention

`VersionRetentionService` enforces configurable retention policies per configuration/parameter.

## Database Configuration

```json
{
  "Database": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=opendsc.db"  // optional for SQLite; auto-generated if omitted
  }
}
```

Supported providers: `SQLite` (default), `SqlServer`, `PostgreSQL`. For SQLite, `ConnectionString` is optional — the server generates a path automatically. For other providers, `ConnectionString` is required.

Configuration is read by `DatabaseExtensions.AddServerDatabase()` using keys `Database:Provider` and `Database:ConnectionString`.

## LCM Integration Points

The LCM (`src/OpenDsc.Lcm/PullServerClient.cs`) communicates with these server endpoints:

| Operation | Endpoint |
|-----------|---------|
| Register node | `POST /api/v1/nodes/register` |
| Download configuration | `GET /api/v1/nodes/{nodeId}/configuration` |
| Check for changes | `GET /api/v1/nodes/{nodeId}/configuration/checksum` |
| Submit compliance report | `POST /api/v1/reports` |
| Rotate certificate | `POST /api/v1/nodes/{nodeId}/rotate-certificate` |

## API Documentation

- Interactive reference (development): `/scalar/v1`
- OpenAPI schema: `/openapi/v1.json`
- Blazor admin portal: `/`
