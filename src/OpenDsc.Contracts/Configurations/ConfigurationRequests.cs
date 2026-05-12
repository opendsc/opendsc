// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Platform-agnostic file upload abstraction used by configuration file operations.
/// </summary>
public sealed record FileUpload(
    string FileName,
    Stream Content,
    string? ContentType = null,
    long? Size = null);

/// <summary>
/// Request to create a new configuration with its initial version.
/// </summary>
public sealed class CreateConfigurationAdminRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntryPoint { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool UseServerManagedParameters { get; set; }
    public IReadOnlyList<FileUpload> Files { get; set; } = [];
}

/// <summary>
/// Request to update an existing configuration's metadata.
/// </summary>
public sealed class UpdateConfigurationAdminRequest
{
    public string? Description { get; set; }
    public bool? UseServerManagedParameters { get; set; }
}

/// <summary>
/// Request to create a new version of an existing configuration.
/// </summary>
public sealed class CreateConfigurationVersionRequest
{
    public string Version { get; set; } = string.Empty;
    public IReadOnlyList<FileUpload> Files { get; set; } = [];
    public string? EntryPoint { get; set; }
}

/// <summary>
/// Request to create a new version by copying an existing version's files.
/// </summary>
public sealed class CreateVersionFromExistingRequest
{
    public string SourceVersion { get; set; } = string.Empty;
    public string NewVersion { get; set; } = string.Empty;
}

/// <summary>
/// Request to update a configuration's version-management settings.
/// </summary>
public sealed class UpdateConfigurationSettingsRequest
{
    public bool? RequireSemVer { get; set; }
    public ParameterValidationMode? ParameterValidationMode { get; set; }
}

/// <summary>
/// Request to save a configuration-specific retention policy.
/// </summary>
public sealed class SaveRetentionSettingsRequest
{
    public bool? Enabled { get; set; }
    public int? KeepVersions { get; set; }
    public int? KeepDays { get; set; }
    public bool? KeepReleaseVersions { get; set; }
}

/// <summary>
/// Request to create a new version of a composite configuration by copying an existing version.
/// </summary>
public sealed class CreateCompositeVersionFromExistingRequest
{
    public string SourceVersion { get; set; } = string.Empty;
    public string NewVersion { get; set; } = string.Empty;
}
