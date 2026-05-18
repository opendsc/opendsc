// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;

using OpenDsc.Contracts.Health;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Server.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/health")
            .WithTags("Health");

        group.MapGet("/", GetHealth)
            .WithSummary("Liveness check")
            .WithDescription("Returns the health status of the server.");

        group.MapGet("/ready", GetReadiness)
            .WithSummary("Readiness check")
            .WithDescription("Checks if the server is ready to accept requests, including database connectivity.");
    }

    private static Ok<HealthStatus> GetHealth()
    {
        return TypedResults.Ok(new HealthStatus
        {
            Status = "Healthy",
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    private static async Task<Results<Ok<ReadinessStatus>, StatusCodeHttpResult>> GetReadiness(
        IHealthService healthService,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await healthService.CanConnectAsync(cancellationToken))
            {
                return TypedResults.StatusCode(503);
            }

            return TypedResults.Ok(new ReadinessStatus
            {
                Status = "Ready",
                Database = "Connected",
                Timestamp = DateTimeOffset.UtcNow
            });
        }
        catch
        {
            return TypedResults.StatusCode(503);
        }
    }
}
