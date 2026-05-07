// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Summary information about a configuration (admin view).
/// </summary>
public sealed class ConfigurationSummary
{
    /// <summary>
    /// The configuration's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether server-managed parameters are enabled.
    /// </summary>
    public bool UseServerManagedParameters { get; set; }

    /// <summary>
    /// Number of versions available.
    /// </summary>
    public int VersionCount { get; set; }

    /// <summary>
    /// The latest version string, or null if no versions exist.
    /// </summary>
    public string? LatestVersion { get; set; }

    /// <summary>
    /// Whether any published version exists.
    /// </summary>
    public bool HasPublishedVersion { get; set; }

    /// <summary>
    /// When the configuration was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Full details about a configuration (admin view).
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
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether server-managed parameters are enabled.
    /// </summary>
    public bool UseServerManagedParameters { get; set; }

    /// <summary>
    /// The latest version string, or null if no versions exist.
    /// </summary>
    public string? LatestVersion { get; set; }

    /// <summary>
    /// When the configuration was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the configuration was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Details about a specific configuration version.
/// </summary>
public sealed class ConfigurationVersionDetails
{
    /// <summary>
    /// The version string.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The entry point filename within the configuration package.
    /// </summary>
    public string EntryPoint { get; set; } = string.Empty;

    /// <summary>
    /// The version's publication status.
    /// </summary>
    public ConfigurationVersionStatus Status { get; set; }

    /// <summary>
    /// Optional prerelease channel (e.g., "beta", "rc").
    /// </summary>
    public string? PrereleaseChannel { get; set; }

    /// <summary>
    /// Number of files in the version package.
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// When the version was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Who created the version.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Files contained in the version package.
    /// </summary>
    public List<ConfigurationFileDetails> Files { get; set; } = [];
}

/// <summary>
/// Details about a file within a configuration version package.
/// </summary>
public sealed class ConfigurationFileDetails
{
    /// <summary>
    /// The relative path of the file within the package.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// The MIME content type of the file.
    /// </summary>
    public string? ContentType { get; set; }
}

/// <summary>
/// Configuration-specific settings summary.
/// </summary>
public sealed class ConfigurationSettingsSummary
{
    /// <summary>
    /// Whether these settings override the server defaults.
    /// </summary>
    public bool IsOverridden { get; set; }

    /// <summary>
    /// Whether semantic versioning is required for version strings.
    /// </summary>
    public bool RequireSemVer { get; set; }

    /// <summary>
    /// Controls how parameter values are validated when nodes request configurations.
    /// </summary>
    public ParameterValidationMode ParameterValidationMode { get; set; }
}

/// <summary>
/// Configuration-specific retention policy summary.
/// </summary>
public sealed class ConfigurationRetentionSummary
{
    /// <summary>
    /// Whether these settings override the server-wide retention policy.
    /// </summary>
    public bool IsOverridden { get; set; }

    /// <summary>
    /// Whether version retention is enabled.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Maximum number of versions to keep.
    /// </summary>
    public int? KeepVersions { get; set; }

    /// <summary>
    /// Maximum age in days before a version is eligible for cleanup.
    /// </summary>
    public int? KeepDays { get; set; }

    /// <summary>
    /// Whether to keep all release (non-prerelease) versions regardless of other limits.
    /// </summary>
    public bool? KeepReleaseVersions { get; set; }
}
