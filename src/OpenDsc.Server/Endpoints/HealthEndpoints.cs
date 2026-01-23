// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Data;

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

    private static Ok<HealthResponse> GetHealth()
    {
        return TypedResults.Ok(new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    private static async Task<Results<Ok<ReadinessResponse>, StatusCodeHttpResult>> GetReadiness(
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.CanConnectAsync(cancellationToken);

            return TypedResults.Ok(new ReadinessResponse
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

public sealed class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}

public sealed class ReadinessResponse
{
    public string Status { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
