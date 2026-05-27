// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;

using OpenDsc.Contracts.ResourceManifests;
using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Authorization;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Endpoints;

public static class ResourceManifestEndpoints
{
    public static void MapResourceManifestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/resource-manifests")
            .RequireAuthorization()
            .WithTags("Resource Manifests");

        group.MapGet("/", GetAll)
            .RequireAuthorization(ServerPermissions.ResourceManifestsRead)
            .WithSummary("List resource manifests")
            .WithDescription("Returns a summary list of all registered DSC resource types.");

        group.MapGet("/{id:guid}", GetById)
            .RequireAuthorization(ServerPermissions.ResourceManifestsRead)
            .WithSummary("Get resource manifest details")
            .WithDescription("Returns details for a registered resource type, including all versions.");

        group.MapGet("/by-type/{typeName}", GetByTypeName)
            .RequireAuthorization(ServerPermissions.ResourceManifestsRead)
            .WithSummary("Get resource manifest by type name")
            .WithDescription("Returns details for a registered resource type by its fully qualified type name.");

        group.MapGet("/{id:guid}/versions/{version}/schema", GetVersionSchema)
            .RequireAuthorization(ServerPermissions.ResourceManifestsRead)
            .WithSummary("Get instance schema for a manifest version")
            .WithDescription("Returns the JSON schema used to validate a resource instance for a specific version.");

        group.MapGet("/{id:guid}/versions/{version}", GetVersionDetails)
            .RequireAuthorization(ServerPermissions.ResourceManifestsRead)
            .WithSummary("Get manifest version details")
            .WithDescription("Returns the full manifest JSON and instance schema for a specific version.");

        group.MapPost("/", Import)
            .RequireAuthorization(ServerPermissions.ResourceManifestsWrite)
            .WithSummary("Import a resource manifest")
            .WithDescription("Imports a DSC resource manifest from its raw JSON. Creates a new type record or adds a new version.");

        group.MapPost("/discover", Discover)
            .RequireAuthorization(ServerPermissions.ResourceManifestsWrite)
            .WithSummary("Discover manifests from DSC CLI")
            .WithDescription("Runs 'dsc resource list' and imports all discovered manifests.");

        group.MapDelete("/{id:guid}", DeleteManifest)
            .RequireAuthorization(ServerPermissions.ResourceManifestsWrite)
            .WithSummary("Delete a resource manifest type")
            .WithDescription("Deletes a registered resource type and all its versions.");

        group.MapDelete("/versions/{versionId:guid}", DeleteVersion)
            .RequireAuthorization(ServerPermissions.ResourceManifestsWrite)
            .WithSummary("Delete a manifest version")
            .WithDescription("Deletes a specific version of a registered resource manifest.");
    }

    private static async Task<Ok<IReadOnlyList<ResourceManifestSummary>>> GetAll(
        IResourceManifestService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await service.GetAllAsync(cancellationToken));
    }

    private static async Task<Results<Ok<ResourceManifestDetails>, NotFound<ErrorResponse>>> GetById(
        Guid id,
        IResourceManifestService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        if (result is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = $"Resource manifest '{id}' not found." });
        }

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<ResourceManifestDetails>, NotFound<ErrorResponse>>> GetByTypeName(
        string typeName,
        IResourceManifestService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByTypeNameAsync(typeName, cancellationToken);
        if (result is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = $"Resource manifest '{typeName}' not found." });
        }

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<ResourceManifestVersionDetails>, NotFound<ErrorResponse>>> GetVersionDetails(
        Guid id,
        string version,
        IResourceManifestService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetVersionDetailsAsync(id, version, cancellationToken);
        if (result is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = $"Version '{version}' not found for manifest '{id}'." });
        }

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<string>, NotFound<ErrorResponse>>> GetVersionSchema(
        Guid id,
        string version,
        IResourceManifestService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetVersionDetailsAsync(id, version, cancellationToken);
        if (result is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = $"Version '{version}' not found for manifest '{id}'." });
        }

        if (string.IsNullOrWhiteSpace(result.InstanceSchemaJson))
        {
            return TypedResults.NotFound(new ErrorResponse { Error = $"No embedded instance schema available for version '{version}'." });
        }

        return TypedResults.Ok(result.InstanceSchemaJson);
    }

    private static async Task<Results<Ok<ResourceManifestVersionDetails>, BadRequest<ErrorResponse>>> Import(
        ImportResourceManifestRequest request,
        IResourceManifestService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.ImportAsync(request.ManifestJson, cancellationToken: cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Ok<DiscoverResponse>, BadRequest<ErrorResponse>>> Discover(
        IResourceManifestService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var count = await service.DiscoverFromDscCliAsync(cancellationToken);
            return TypedResults.Ok(new DiscoverResponse { ImportedCount = count });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>> DeleteManifest(
        Guid id,
        IResourceManifestService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteManifestAsync(id, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>> DeleteVersion(
        Guid versionId,
        IResourceManifestService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteVersionAsync(versionId, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = ex.Message });
        }
    }
}

public sealed class DiscoverResponse
{
    public required int ImportedCount { get; init; }
}
