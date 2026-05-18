// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Platform-agnostic file upload abstraction used by configuration file operations.
/// </summary>
public sealed class FileUpload
{
    /// <summary>
    /// The name of the file being uploaded.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The file content as a stream.
    /// </summary>
    public required Stream Content { get; set; }

    /// <summary>
    /// The MIME type of the file (optional).
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// The size of the file in bytes (optional).
    /// </summary>
    public long? Size { get; set; }
}

/// <summary>
/// Request to create a new configuration with its initial version.
/// </summary>
public sealed class CreateConfigurationAdminRequest
{
    /// <summary>
    /// The name of the configuration to create.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the configuration's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The main DSC file to execute (e.g., "main.dsc.yaml").
    /// </summary>
    public string EntryPoint { get; set; } = string.Empty;

    /// <summary>
    /// The initial version identifier for the configuration (e.g., "1.0.0").
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether parameters for this configuration are managed by the server (true) or locally by nodes (false).
    /// </summary>
    public bool UseServerManagedParameters { get; set; }

    /// <summary>
    /// The files to include in the configuration version.
    /// </summary>
    public IReadOnlyList<FileUpload> Files { get; set; } = [];
}

/// <summary>
/// Request to update an existing configuration's metadata.
/// </summary>
public sealed class UpdateConfigurationAdminRequest
{
    /// <summary>
    /// The updated description of the configuration's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether to enable or disable server-managed parameter mode.
    /// </summary>
    public bool? UseServerManagedParameters { get; set; }
}

/// <summary>
/// Request to create a new version of an existing configuration.
/// </summary>
public sealed class CreateConfigurationVersionRequest
{
    /// <summary>
    /// The version identifier for the new version (e.g., "1.1.0").
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The files to include in this version.
    /// </summary>
    public IReadOnlyList<FileUpload> Files { get; set; } = [];

    /// <summary>
    /// Optional alternative entry point for this version (overrides configuration-level entry point).
    /// </summary>
    public string? EntryPoint { get; set; }
}

/// <summary>
/// Request to create a new version by copying an existing version's files.
/// </summary>
public sealed class CreateVersionFromExistingRequest
{
    /// <summary>
    /// The version identifier to copy files from.
    /// </summary>
    public string SourceVersion { get; set; } = string.Empty;

    /// <summary>
    /// The version identifier for the new version.
    /// </summary>
    public string NewVersion { get; set; } = string.Empty;
}

/// <summary>
/// Request to update a configuration's version-management settings.
/// </summary>
public sealed class UpdateConfigurationSettingsRequest
{
    /// <summary>
    /// Whether to enforce semantic versioning (e.g., "1.2.3" format) for configuration versions.
    /// </summary>
    public bool? RequireSemVer { get; set; }

    /// <summary>
    /// The parameter validation mode for configurations (None, Warn, Strict).
    /// </summary>
    public ParameterValidationMode? ParameterValidationMode { get; set; }
}

/// <summary>
/// Request to save a configuration-specific retention policy.
/// </summary>
public sealed class SaveRetentionSettingsRequest
{
    /// <summary>
    /// Whether retention policy enforcement is enabled for this configuration.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// The number of most recent versions to keep.
    /// </summary>
    public int? KeepVersions { get; set; }

    /// <summary>
    /// The number of days to keep versions (oldest versions are deleted after this period).
    /// </summary>
    public int? KeepDays { get; set; }

    /// <summary>
    /// Whether to always keep tagged/release versions regardless of age.
    /// </summary>
    public bool? KeepReleaseVersions { get; set; }
}

/// <summary>
/// Request to create a new version of a composite configuration by copying an existing version.
/// </summary>
public sealed class CreateCompositeVersionFromExistingRequest
{
    /// <summary>
    /// The version identifier to copy composite configuration from.
    /// </summary>
    public string SourceVersion { get; set; } = string.Empty;

    /// <summary>
    /// The version identifier for the new composite configuration version.
    /// </summary>
    public string NewVersion { get; set; } = string.Empty;
}
