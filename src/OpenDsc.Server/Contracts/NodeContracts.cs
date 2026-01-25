// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Server.Contracts;

/// <summary>
/// Request to register a new node.
/// </summary>
public sealed class RegisterNodeRequest
{
    /// <summary>
    /// The registration key for the server.
    /// </summary>
    [JsonRequired]
    public string RegistrationKey { get; set; } = string.Empty;

    /// <summary>
    /// The fully qualified domain name of the node.
    /// </summary>
    [JsonRequired]
    public string Fqdn { get; set; } = string.Empty;
}

/// <summary>
/// Response from node registration.
/// </summary>
public sealed class RegisterNodeResponse
{
    /// <summary>
    /// The unique identifier assigned to the node.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// The API key for the node to use for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// How often the node should rotate its API key.
    /// </summary>
    public TimeSpan KeyRotationInterval { get; set; }
}

/// <summary>
/// Response from key rotation.
/// </summary>
public sealed class RotateKeyResponse
{
    /// <summary>
    /// The new API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// How often the node should rotate its API key.
    /// </summary>
    public TimeSpan KeyRotationInterval { get; set; }
}

/// <summary>
/// Summary information about a node.
/// </summary>
public sealed class NodeSummary
{
    /// <summary>
    /// The node's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The node's fully qualified domain name.
    /// </summary>
    public string Fqdn { get; set; } = string.Empty;

    /// <summary>
    /// The name of the assigned configuration.
    /// </summary>
    public string? ConfigurationName { get; set; }

    /// <summary>
    /// The node's compliance status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the node last checked in.
    /// </summary>
    public DateTimeOffset? LastCheckIn { get; set; }

    /// <summary>
    /// When the node was registered.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Request to assign a configuration to a node.
/// </summary>
public sealed class AssignConfigurationRequest
{
    /// <summary>
    /// The name of the configuration to assign.
    /// </summary>
    [JsonRequired]
    public string ConfigurationName { get; set; } = string.Empty;
}

/// <summary>
/// Response with configuration checksum.
/// </summary>
public sealed class ConfigurationChecksumResponse
{
    /// <summary>
    /// The SHA256 checksum of the configuration.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;
}
