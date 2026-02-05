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
}

/// <summary>
/// Request to rotate a node's certificate.
/// </summary>
public sealed class RotateCertificateRequest
{
    /// <summary>
    /// The thumbprint of the new certificate.
    /// </summary>
    [JsonRequired]
    public string CertificateThumbprint { get; set; } = string.Empty;

    /// <summary>
    /// The subject DN of the new certificate.
    /// </summary>
    [JsonRequired]
    public string CertificateSubject { get; set; } = string.Empty;

    /// <summary>
    /// The expiration date of the new certificate.
    /// </summary>
    [JsonRequired]
    public DateTimeOffset CertificateNotAfter { get; set; }
}

/// <summary>
/// Response from certificate rotation.
/// </summary>
public sealed class RotateCertificateResponse
{
    /// <summary>
    /// Confirmation message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
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

    /// <summary>
    /// Whether this is a composite configuration.
    /// </summary>
    public bool IsComposite { get; set; }

    /// <summary>
    /// The specific version ID to pin (optional, defaults to latest published version).
    /// </summary>
    public Guid? VersionId { get; set; }
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
