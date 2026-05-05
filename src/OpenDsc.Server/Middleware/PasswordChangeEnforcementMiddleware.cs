// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using OpenDsc.Server.Data;

namespace OpenDsc.Server.Middleware;

public sealed partial class PasswordChangeEnforcementMiddleware(RequestDelegate next, ILogger<PasswordChangeEnforcementMiddleware> logger)
{
    private static readonly HashSet<string> ExemptApiPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/auth/login",
        "/api/v1/auth/logout",
        "/api/v1/auth/change-password",
        "/api/v1/auth/me",
    };

    public async Task InvokeAsync(HttpContext context, ServerDbContext db, IMemoryCache cache)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "/";

        if (IsExemptPath(path))
        {
            await next(context);
            return;
        }

        var userIdValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            await next(context);
            return;
        }

        var requiresChange = await cache.GetOrCreateAsync(
            $"pwd-change-{userId}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.RequirePasswordChange, u.PasswordHash })
                    .FirstOrDefaultAsync();
            });

        if (requiresChange == null || !requiresChange.RequirePasswordChange || requiresChange.PasswordHash == null)
        {
            await next(context);
            return;
        }

        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            LogPasswordChangeRequiredApiBlocked(userId, path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                code = "password_change_required",
                message = "You must change your password before accessing this resource.",
                redirectUrl = "/change-password"
            }));
            return;
        }

        LogPasswordChangeRequiredRedirect(userId, path);
        context.Response.Redirect("/change-password");
    }

    private static bool IsExemptPath(string path)
    {
        if (path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.Equals("/change-password", StringComparison.OrdinalIgnoreCase)) return true;
        return ExemptApiPaths.Contains(path);
    }

    [LoggerMessage(EventId = EventIds.PasswordChangeRequired, Level = LogLevel.Warning, Message = "User {UserId} blocked from API path {Path}: password change required")]
    private partial void LogPasswordChangeRequiredApiBlocked(Guid userId, string path);

    [LoggerMessage(EventId = EventIds.PasswordChangeRequiredRedirect, Level = LogLevel.Warning, Message = "User {UserId} redirected from {Path} to change-password")]
    private partial void LogPasswordChangeRequiredRedirect(Guid userId, string path);
}
