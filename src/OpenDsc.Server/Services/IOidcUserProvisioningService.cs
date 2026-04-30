// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

/// <summary>
/// Provisions or retrieves a user for an external OIDC login.
/// </summary>
public interface IOidcUserProvisioningService
{
    /// <summary>
    /// Returns the existing local user linked to the given provider and subject,
    /// or creates a new user (just-in-time provisioning) if no match is found.
    /// </summary>
    Task<User> ProvisionOrGetUserAsync(
        string provider,
        string providerKey,
        string? displayName,
        string? email,
        string? preferredUsername,
        CancellationToken cancellationToken = default);
}
