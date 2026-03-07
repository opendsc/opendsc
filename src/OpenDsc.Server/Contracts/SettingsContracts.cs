// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Lcm.Contracts;

namespace OpenDsc.Server.Contracts;

/// <summary>
/// Server settings response.
/// </summary>
public sealed class ServerSettingsResponse
{
    /// <summary>
    /// How often nodes should rotate their certificates (informational).
    /// </summary>
    public TimeSpan CertificateRotationInterval { get; set; }

    /// <summary>
    /// Multiplier applied to a node's ConfigurationModeInterval to determine staleness threshold.
    /// </summary>
    public double StalenessMultiplier { get; set; }
}

/// <summary>
/// Request to update server settings.
/// </summary>
public sealed class UpdateServerSettingsRequest
{
    /// <summary>
    /// How often nodes should rotate their certificates (informational).
    /// </summary>
    public TimeSpan? CertificateRotationInterval { get; set; }

    /// <summary>
    /// Multiplier applied to a node's ConfigurationModeInterval to determine staleness threshold.
    /// </summary>
    public double? StalenessMultiplier { get; set; }
}

/// <summary>
/// Server-wide LCM default settings response.
/// </summary>
public sealed class ServerLcmDefaultsResponse
{
    /// <summary>
    /// Server-wide default LCM operating mode. Null means no server default is set.
    /// </summary>
    public ConfigurationMode? DefaultConfigurationMode { get; set; }

    /// <summary>
    /// Server-wide default LCM configuration mode interval. Null means no server default is set.
    /// </summary>
    public TimeSpan? DefaultConfigurationModeInterval { get; set; }

    /// <summary>
    /// Server-wide default compliance reporting setting. Null means no server default is set.
    /// </summary>
    public bool? DefaultReportCompliance { get; set; }
}

/// <summary>
/// Request to update server-wide LCM default settings. Null values clear the corresponding default.
/// </summary>
public sealed class UpdateServerLcmDefaultsRequest
{
    /// <summary>
    /// Server-wide default LCM operating mode. Set to null to clear the default.
    /// </summary>
    public ConfigurationMode? DefaultConfigurationMode { get; set; }

    /// <summary>
    /// Server-wide default LCM configuration mode interval. Set to null to clear the default.
    /// </summary>
    public TimeSpan? DefaultConfigurationModeInterval { get; set; }

    /// <summary>
    /// Server-wide default compliance reporting setting. Set to null to clear the default.
    /// </summary>
    public bool? DefaultReportCompliance { get; set; }
}

/// <summary>
/// Request to create a registration key.
/// </summary>
public sealed class CreateRegistrationKeyRequest
{
    /// <summary>
    /// When the key should expire (optional, defaults to 7 days from now).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Maximum number of times this key can be used (null = unlimited).
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// Optional description of the key's intended usage or purpose.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request to update a registration key.
/// </summary>
public sealed class UpdateRegistrationKeyRequest
{
    /// <summary>
    /// Description of the key's intended usage or purpose.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Response with registration key details.
/// </summary>
public sealed class RegistrationKeyResponse
{
    /// <summary>
    /// The unique identifier for the key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The registration key value (only returned on creation).
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// When the key expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// When the key was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Maximum number of uses (null = unlimited).
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// Current number of uses.
    /// </summary>
    public int CurrentUses { get; set; }

    /// <summary>
    /// Whether the key is revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Optional description of the key's intended usage or purpose.
    /// </summary>
    public string? Description { get; set; }
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
