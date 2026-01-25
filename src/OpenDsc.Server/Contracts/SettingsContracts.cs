// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Contracts;

/// <summary>
/// Server settings response.
/// </summary>
public sealed class ServerSettingsResponse
{
    /// <summary>
    /// The registration key for new nodes.
    /// </summary>
    public string RegistrationKey { get; set; } = string.Empty;

    /// <summary>
    /// How often nodes should rotate their API keys.
    /// </summary>
    public TimeSpan KeyRotationInterval { get; set; }
}

/// <summary>
/// Request to update server settings.
/// </summary>
public sealed class UpdateServerSettingsRequest
{
    /// <summary>
    /// How often nodes should rotate their API keys.
    /// </summary>
    public TimeSpan? KeyRotationInterval { get; set; }
}

/// <summary>
/// Response after rotating a key.
/// </summary>
public sealed class RotateRegistrationKeyResponse
{
    /// <summary>
    /// The new registration key.
    /// </summary>
    public string RegistrationKey { get; set; } = string.Empty;
}

/// <summary>
/// Standard error response.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// Error message.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}
