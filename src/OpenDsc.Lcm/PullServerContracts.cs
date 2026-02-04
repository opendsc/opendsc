// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Lcm;

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
/// Response containing configuration checksum.
/// </summary>
public sealed class ConfigurationChecksumResponse
{
    /// <summary>
    /// The SHA256 checksum of the configuration.
    /// </summary>
    public string? Checksum { get; set; }
}

/// <summary>
/// Request to submit a compliance report.
/// </summary>
public sealed class SubmitReportRequest
{
    /// <summary>
    /// The DSC operation that generated this report.
    /// </summary>
    [JsonRequired]
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The full DSC result as JSON.
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// When the report was generated.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
