// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;

using OpenDsc.Contracts.DscFunctions;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Endpoints;

public static class DscFunctionEndpoints
{
    public static void MapDscFunctionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dsc-functions")
            .RequireAuthorization()
            .WithTags("DSC Functions");

        group.MapGet("/", GetFunctions)
            .WithSummary("List DSC functions")
            .WithDescription("Returns the catalog of available DSC configuration document functions.");

        group.MapPost("/evaluate", Evaluate)
            .WithSummary("Evaluate a DSC function")
            .WithDescription("Mock-evaluates a DSC function expression with the provided arguments and optional mock values.");
    }

    private static async Task<Ok<IReadOnlyList<DscFunctionInfo>>> GetFunctions(IDscFunctionService service, CancellationToken cancellationToken)
    {
        var functions = await service.GetFunctionsAsync(cancellationToken);
        return TypedResults.Ok(functions);
    }

    private static async Task<Ok<EvaluateDscFunctionResult>> Evaluate(
        EvaluateDscFunctionRequest request,
        IDscFunctionService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await service.EvaluateAsync(request, cancellationToken));
    }
}
