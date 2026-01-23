// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Contracts;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Endpoints;

public static class ConfigurationEndpoints
{
    public static void MapConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/configurations")
            .RequireAuthorization("Admin")
            .WithTags("Configurations");

        group.MapGet("/", GetConfigurations)
            .WithSummary("List configurations")
            .WithDescription("Returns a list of all DSC configurations.");

        group.MapPost("/", CreateConfiguration)
            .WithSummary("Create configuration")
            .WithDescription("Creates a new DSC configuration.");

        group.MapGet("/{name}", GetConfiguration)
            .WithSummary("Get configuration")
            .WithDescription("Returns the content of a specific configuration.");

        group.MapPut("/{name}", UpdateConfiguration)
            .WithSummary("Update configuration")
            .WithDescription("Updates the content of an existing configuration.");

        group.MapDelete("/{name}", DeleteConfiguration)
            .WithSummary("Delete configuration")
            .WithDescription("Deletes a configuration and unassigns it from any nodes.");
    }

    private static async Task<Ok<List<ConfigurationSummary>>> GetConfigurations(
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var configs = await db.Configurations
            .AsNoTracking()
            .Select(c => new ConfigurationSummary
            {
                Id = c.Id,
                Name = c.Name,
                Checksum = c.Checksum,
                CreatedAt = c.CreatedAt,
                ModifiedAt = c.ModifiedAt
            })
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(configs);
    }

    private static async Task<Results<Created<ConfigurationSummary>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> CreateConfiguration(
        CreateConfigurationRequest request,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Configuration name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Configuration content is required." });
        }

        var exists = await db.Configurations.AnyAsync(c => c.Name == request.Name, cancellationToken);
        if (exists)
        {
            return TypedResults.Conflict(new ErrorResponse { Error = "Configuration with this name already exists." });
        }

        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Content = request.Content,
            Checksum = ComputeChecksum(request.Content),
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Configurations.Add(config);
        await db.SaveChangesAsync(cancellationToken);

        var summary = new ConfigurationSummary
        {
            Id = config.Id,
            Name = config.Name,
            Checksum = config.Checksum,
            CreatedAt = config.CreatedAt,
            ModifiedAt = config.ModifiedAt
        };

        return TypedResults.Created($"/api/v1/configurations/{config.Name}", summary);
    }

    private static async Task<Results<Ok<ConfigurationDetails>, NotFound<ErrorResponse>>> GetConfiguration(
        string name,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var config = await db.Configurations
            .AsNoTracking()
            .Where(c => c.Name == name)
            .Select(c => new ConfigurationDetails
            {
                Id = c.Id,
                Name = c.Name,
                Content = c.Content,
                Checksum = c.Checksum,
                CreatedAt = c.CreatedAt,
                ModifiedAt = c.ModifiedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (config is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Configuration not found." });
        }

        return TypedResults.Ok(config);
    }

    private static async Task<Results<Ok<ConfigurationSummary>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> UpdateConfiguration(
        string name,
        UpdateConfigurationRequest request,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Configuration content is required." });
        }

        var config = await db.Configurations.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
        if (config is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Configuration not found." });
        }

        config.Content = request.Content;
        config.Checksum = ComputeChecksum(request.Content);
        config.ModifiedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var summary = new ConfigurationSummary
        {
            Id = config.Id,
            Name = config.Name,
            Checksum = config.Checksum,
            CreatedAt = config.CreatedAt,
            ModifiedAt = config.ModifiedAt
        };

        return TypedResults.Ok(summary);
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>> DeleteConfiguration(
        string name,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var config = await db.Configurations.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
        if (config is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Configuration not found." });
        }

        db.Configurations.Remove(config);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static string ComputeChecksum(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
