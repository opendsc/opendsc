// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Lcm.Contracts;

/// <summary>
/// Request to register a new node with the pull server.
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

    /// <summary>
    /// Whether the node pulls its configuration from the server or manages it locally.
    /// </summary>
    public ConfigurationSource? ConfigurationSource { get; set; }

    /// <summary>
    /// The LCM operating mode (Monitor or Remediate).
    /// </summary>
    public ConfigurationMode? ConfigurationMode { get; set; }

    /// <summary>
    /// The interval between LCM operations.
    /// </summary>
    public TimeSpan? ConfigurationModeInterval { get; set; }

    /// <summary>
    /// Whether the node submits compliance reports to the server.
    /// </summary>
    public bool? ReportCompliance { get; set; }
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
/// The operational state of the LCM agent.
/// </summary>
public enum LcmStatus
{
    /// <summary>
    /// Status is unknown (node has not reported operational state).
    /// </summary>
    Unknown,

    /// <summary>
    /// LCM is idle, waiting for the next cycle.
    /// </summary>
    Idle,

    /// <summary>
    /// LCM is downloading configuration from the pull server.
    /// </summary>
    Downloading,

    /// <summary>
    /// LCM is running a DSC test operation.
    /// </summary>
    Testing,

    /// <summary>
    /// LCM is running a DSC set operation to remediate drift.
    /// </summary>
    Remediating,

    /// <summary>
    /// LCM encountered an unhandled error during the last cycle.
    /// </summary>
    Error
}

/// <summary>
/// Request to update the LCM operational status.
/// </summary>
public sealed class UpdateLcmStatusRequest
{
    /// <summary>
    /// The current operational state of the LCM agent.
    /// </summary>
    [JsonRequired]
    public LcmStatus LcmStatus { get; set; }
}

/// <summary>
/// Request to rotate a node's client certificate.
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
/// Response containing configuration checksum information.
/// </summary>
public sealed class ConfigurationChecksumResponse
{
    /// <summary>
    /// The SHA256 checksum of the configuration bundle.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// The entry point file name within the configuration bundle.
    /// </summary>
    public string EntryPoint { get; set; } = "main.dsc.yaml";

    /// <summary>
    /// The parameters file path within the bundle for use with --parameters-file,
    /// or null when parameters are not server-managed or this is a composite configuration.
    /// </summary>
    public string? ParametersFile { get; set; }
}
