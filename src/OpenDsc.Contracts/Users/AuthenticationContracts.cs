// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

/// <summary>
/// Result of a successful user login operation.
/// </summary>
public sealed class LoginResult
{
    /// <summary>
    /// The unique identifier for the authenticated user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The username of the authenticated user.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the authenticated user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user must change their password on next login.
    /// </summary>
    public bool RequirePasswordChange { get; set; }
}

/// <summary>
/// Result of creating a new API token for a user.
/// </summary>
public sealed class TokenCreationResult
{
    /// <summary>
    /// The complete API token value. This is only returned on creation and cannot be retrieved later.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier for the created token.
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// The user-friendly name for the token.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A short prefix of the token used for identification in UI/logs (e.g., first 8 characters).
    /// </summary>
    public string TokenPrefix { get; set; } = string.Empty;

    /// <summary>
    /// The scopes/permissions granted to the token.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; set; } = [];

    /// <summary>
    /// The expiration date/time for the token, if applicable. Null if the token does not expire.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// The timestamp when the token was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Metadata about an existing API token (excluding the token value itself).
/// </summary>
public sealed class TokenMetadata
{
    /// <summary>
    /// The unique identifier for the token.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user-friendly name for the token.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A short prefix of the token used for identification in UI/logs (e.g., first 8 characters).
    /// </summary>
    public string TokenPrefix { get; set; } = string.Empty;

    /// <summary>
    /// The expiration date/time for the token, if applicable. Null if the token does not expire.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// The timestamp when the token was last used to authenticate a request, if applicable.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Whether the token has been revoked and can no longer be used for authentication.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// The scopes/permissions granted to the token.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; set; } = [];

    /// <summary>
    /// The timestamp when the token was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
