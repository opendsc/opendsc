// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Server.Contracts;

/// <summary>
/// Request to create a composite configuration.
/// </summary>
public sealed class CreateCompositeConfigurationRequest
{
    /// <summary>
    /// The name of the composite configuration.
    /// </summary>
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the composite configuration.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Entry point filename for the generated orchestrator configuration.
    /// </summary>
    public string EntryPoint { get; set; } = "main.dsc.yaml";

    /// <summary>
    /// Whether this composite configuration uses server-managed parameters.
    /// </summary>
    public bool IsServerManaged { get; set; } = true;
}

/// <summary>
/// Request to create a new version of a composite configuration.
/// </summary>
public sealed class CreateCompositeConfigurationVersionRequest
{
    /// <summary>
    /// The semantic version number (e.g., "1.0.0").
    /// </summary>
    [JsonRequired]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Optional prerelease channel (e.g., "beta", "alpha").
    /// </summary>
    public string? PrereleaseChannel { get; set; }
}

/// <summary>
/// Request to add a child configuration to a composite version.
/// </summary>
public sealed class AddChildConfigurationRequest
{
    /// <summary>
    /// The name of the child configuration to add.
    /// </summary>
    [JsonRequired]
    public string ChildConfigurationName { get; set; } = string.Empty;

    /// <summary>
    /// Optional specific version string to pin this child configuration to.
    /// If null, will use the latest published version.
    /// Example: "1.0.0", "2.1.3"
    /// </summary>
    public string? ActiveVersion { get; set; }

    /// <summary>
    /// The order/sequence number for this child in the composite.
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// Request to update a child configuration's settings.
/// </summary>
public sealed class UpdateChildConfigurationRequest
{
    /// <summary>
    /// Optional specific version string to pin this child configuration to.
    /// If null, will use the latest published version.
    /// Example: "1.0.0", "2.1.3"
    /// </summary>
    public string? ActiveVersion { get; set; }

    /// <summary>
    /// The order/sequence number for this child in the composite.
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// Summary information about a composite configuration.
/// </summary>
public sealed class CompositeConfigurationSummaryDto
{
    /// <summary>
    /// The composite configuration's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The composite configuration's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Entry point filename.
    /// </summary>
    public string EntryPoint { get; set; } = string.Empty;

    /// <summary>
    /// Number of versions.
    /// </summary>
    public int VersionCount { get; set; }

    /// <summary>
    /// The latest version string.
    /// </summary>
    public string? LatestVersion { get; set; }

    /// <summary>
    /// When the composite configuration was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Full details about a composite configuration.
/// </summary>
public sealed class CompositeConfigurationDetailsDto
{
    /// <summary>
    /// The composite configuration's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The composite configuration's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Entry point filename.
    /// </summary>
    public string EntryPoint { get; set; } = string.Empty;

    /// <summary>
    /// Whether this uses server-managed parameters.
    /// </summary>
    public bool IsServerManaged { get; set; }

    /// <summary>
    /// All versions of this composite configuration.
    /// </summary>
    public List<CompositeConfigurationVersionDto> Versions { get; set; } = [];

    /// <summary>
    /// When the composite configuration was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the composite configuration was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Information about a composite configuration version.
/// </summary>
public sealed class CompositeConfigurationVersionDto
{
    /// <summary>
    /// The version's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The semantic version string.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a draft version.
    /// </summary>
    public bool IsDraft { get; set; }

    /// <summary>
    /// Whether this version is archived.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Optional prerelease channel.
    /// </summary>
    public string? PrereleaseChannel { get; set; }

    /// <summary>
    /// Child configurations in this version.
    /// </summary>
    public List<CompositeConfigurationItemDto> Items { get; set; } = [];

    /// <summary>
    /// When the version was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Who created the version.
    /// </summary>
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Information about a child configuration within a composite.
/// </summary>
public sealed class CompositeConfigurationItemDto
{
    /// <summary>
    /// The item's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The child configuration's ID.
    /// </summary>
    public Guid ChildConfigurationId { get; set; }

    /// <summary>
    /// The child configuration's name.
    /// </summary>
    public string ChildConfigurationName { get; set; } = string.Empty;

    /// <summary>
    /// The pinned version string, or null for latest.
    /// Example: "1.0.0", "2.1.3"
    /// </summary>
    public string? ActiveVersion { get; set; }

    /// <summary>
    /// The order/sequence number.
    /// </summary>
    public int Order { get; set; }
}
