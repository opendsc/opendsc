// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Server.Contracts;

/// <summary>
/// Request to create or update a configuration.
/// </summary>
public sealed class CreateConfigurationRequest
{
    /// <summary>
    /// The name of the configuration.
    /// </summary>
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The DSC configuration document content.
    /// </summary>
    [JsonRequired]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Request to update a configuration's content.
/// </summary>
public sealed class UpdateConfigurationRequest
{
    /// <summary>
    /// The DSC configuration document content.
    /// </summary>
    [JsonRequired]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Summary information about a configuration.
/// </summary>
public sealed class ConfigurationSummary
{
    /// <summary>
    /// The configuration's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The configuration's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 checksum of the content.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// When the configuration was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the configuration was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}

/// <summary>
/// Full configuration details including content.
/// </summary>
public sealed class ConfigurationDetails
{
    /// <summary>
    /// The configuration's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The configuration's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The DSC configuration document content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 checksum of the content.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// When the configuration was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the configuration was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}
