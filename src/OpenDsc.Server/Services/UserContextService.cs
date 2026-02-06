// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;

namespace OpenDsc.Server.Services;

/// <summary>
/// Service for accessing current user context.
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Gets the current user ID from claims.
    /// </summary>
    /// <returns>User ID if authenticated, null otherwise.</returns>
    Guid? GetCurrentUserId();

    /// <summary>
    /// Gets the current username from claims.
    /// </summary>
    /// <returns>Username if authenticated, null otherwise.</returns>
    string? GetCurrentUsername();

    /// <summary>
    /// Gets the current HTTP context IP address.
    /// </summary>
    /// <returns>IP address or null.</returns>
    string? GetIpAddress();
}

/// <summary>
/// User context service implementation.
/// </summary>
public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    public Guid? GetCurrentUserId()
    {
        var claim = httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return claim != null && Guid.TryParse(claim, out var userId) ? userId : null;
    }

    public string? GetCurrentUsername()
    {
        return httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.Name)?.Value;
    }

    public string? GetIpAddress()
    {
        return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
