---
name: add-pull-server-endpoint
description: "WORKFLOW SKILL — Add a new API endpoint group to the Pull Server (OpenDsc.Server). USE FOR: adding new REST API resource groups with EF Core entities, Blazor admin pages for new data types, extending the API surface area. INVOKES: file search, read, create, and edit tools. DO NOT USE FOR: modifying existing endpoints (just edit directly); adding DSC resources (use /create-dsc-resource)."
---

# Add Pull Server Endpoint

## Overview

This skill adds a complete new entity + API endpoint group to `src/OpenDsc.Server`: EF Core entity, database migration, endpoint group file, `Program.cs` registration, authorization policies, optional Blazor admin page, and xUnit tests.

Read [`src/OpenDsc.Server/Endpoints/NodeEndpoints.cs`](../../../src/OpenDsc.Server/Endpoints/NodeEndpoints.cs) as the reference for conventions before starting.

## Required Inputs

1. **Entity name** (e.g., `Widget`, `AuditEntry`) — PascalCase singular
2. **Route prefix** (e.g., `/api/v1/widgets`)
3. **Operations needed**: GET list, GET by id, POST create, PUT/PATCH update, DELETE
4. **Authorization policies** (e.g., `"widgets.read"`, `"widgets.write"`)
5. **Needs Blazor admin page?** (yes/no)

## Step-by-Step Process

### Step 1 — Read existing endpoint and entity for reference

Read an existing endpoint group and entity to understand current conventions:
- [`src/OpenDsc.Server/Endpoints/NodeEndpoints.cs`](../../../src/OpenDsc.Server/Endpoints/NodeEndpoints.cs)
- [`src/OpenDsc.Server/Entities/Node.cs`](../../../src/OpenDsc.Server/Entities/Node.cs)
- [`src/OpenDsc.Server/Data/ServerDbContext.cs`](../../../src/OpenDsc.Server/Data/ServerDbContext.cs)

### Step 2 — Create entity

Create `src/OpenDsc.Server/Entities/{Name}.cs`:
- Add `Id`, `CreatedAt`, `UpdatedAt` properties (or copy from an existing entity)
- Apply any required data annotations or fluent configuration
- MIT license header required

Add `DbSet<{Name}>` to `ServerDbContext` in `Data/ServerDbContext.cs` and configure the entity in `OnModelCreating` if needed.

### Step 3 — No migration needed

The server currently uses `EnsureCreatedAsync` (see `Data/DatabaseExtensions.cs`) rather than EF Core migrations. Simply adding the `DbSet` and configuring the entity in `OnModelCreating` is sufficient — the schema will be applied automatically on next startup.

### Step 4 — Create endpoint group

Create `src/OpenDsc.Server/Endpoints/{Name}Endpoints.cs`:

```csharp
// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

public static class {Name}Endpoints
{
    public static IEndpointRouteBuilder Map{Name}Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/{route-prefix}")
            .WithTags("{Name}");

        group.MapGet("/", GetAll)
            .RequireAuthorization("{name}.read")
            .WithSummary("List all {names}")
            .WithDescription("Returns a list of all {names}.");
        group.MapGet("/{id:guid}", GetById)
            .RequireAuthorization("{name}.read")
            .WithSummary("Get {name} details")
            .WithDescription("Returns details for a specific {name}.");
        group.MapPost("/", Create)
            .RequireAuthorization("{name}.write")
            .WithSummary("Create {name}")
            .WithDescription("Creates a new {name}.");
        group.MapPut("/{id:guid}", Update)
            .RequireAuthorization("{name}.write")
            .WithSummary("Update {name}")
            .WithDescription("Updates an existing {name}.");
        group.MapDelete("/{id:guid}", Delete)
            .RequireAuthorization("{name}.write")
            .WithSummary("Delete {name}")
            .WithDescription("Deletes a {name}.");

        return app;
    }

    // ... handler methods (use minimal API style with typed parameters)
}
```

### Step 5 — Register in Program.cs

Add to [`src/OpenDsc.Server/Program.cs`](../../../src/OpenDsc.Server/Program.cs) alongside the other `Map...Endpoints()` calls:

```csharp
app.Map{Name}Endpoints();
```

### Step 6 — Add authorization policies (if new)

In `Program.cs` where other policies are defined, add:

```csharp
options.AddPolicy("{name}.read",  policy => policy.RequireRole("..."));
options.AddPolicy("{name}.write", policy => policy.RequireRole("..."));
```

### Step 7 — Create Blazor admin page (if needed)

Create `src/OpenDsc.Server/Components/Pages/{Name}/Index.razor`:
- `[Authorize(Policy = "{name}.read")]` at the top
- Use `MudTable`, `MudCard`, `MudButton` (MudBlazor only — no raw HTML forms)
- Create/edit operations in dialog components: `Create{Name}Dialog.razor`, `Edit{Name}Dialog.razor` in the same folder
- Add navigation entry in [`Components/Layout/NavMenu.razor`](../../../src/OpenDsc.Server/Components/Layout/NavMenu.razor)

### Step 8 — Write xUnit tests

Create `tests/OpenDsc.Server.Tests/Endpoints/{Name}EndpointsTests.cs`:
- Test each endpoint for happy-path behavior
- Verify 401 (unauthenticated) and 403 (wrong policy) are returned correctly
- Test validation errors return 400 with appropriate problem details
- Test 404 for missing entity

### Step 9 — Verify build

```powershell
dotnet build src/OpenDsc.Server/OpenDsc.Server.csproj
dotnet test tests/OpenDsc.Server.Tests/
```

## Conventions Checklist

- [ ] MIT license header in all new `.cs` files
- [ ] Endpoint group uses `MapGroup` with `.WithTags()`; individual routes use `.WithSummary()` and `.WithDescription()`
- [ ] All routes use `RequireAuthorization` with the correct policy name
- [ ] Handler methods are static local functions or static methods in the endpoint class
- [ ] Blazor pages use MudBlazor components exclusively
- [ ] No unused `using` directives
- [ ] xUnit tests cover auth scenarios (401, 403)
