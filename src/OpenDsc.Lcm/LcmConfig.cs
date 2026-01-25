// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel.DataAnnotations;

namespace OpenDsc.Lcm;

/// <summary>
/// Configuration for the Local Configuration Manager (LCM) service.
/// </summary>
public class LcmConfig
{
    /// <summary>
    /// The mode of operation for the LCM service.
    /// </summary>
    [Required]
    public ConfigurationMode ConfigurationMode { get; set; } = ConfigurationMode.Monitor;

    /// <summary>
    /// The source of configuration. Pull retrieves from a server, Local uses a file.
    /// </summary>
    [Required]
    public ConfigurationSource ConfigurationSource { get; set; } = ConfigurationSource.Local;

    /// <summary>
    /// Full path to the main DSC configuration file. Used when ConfigurationSource is Local.
    /// </summary>
    public string ConfigurationPath { get; set; } = Path.Combine(ConfigPaths.GetLcmConfigDirectory(), "config", "main.dsc.yaml");

    /// <summary>
    /// Interval between DSC operations (for Monitor and Remediate modes).
    /// Can be specified as a TimeSpan string like "00:15:00" for 15 minutes.
    /// </summary>
    [Required]
    [MinTimeSpan("00:00:00.001")]
    public TimeSpan ConfigurationModeInterval { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Path to the DSC executable. If not specified, uses 'dsc' from PATH.
    /// </summary>
    public string? DscExecutablePath { get; set; }

    /// <summary>
    /// Pull server configuration. Required when ConfigurationSource is Pull.
    /// </summary>
    public PullServerSettings? PullServer { get; set; }
}

/// <summary>
/// Settings for connecting to a DSC pull server.
/// </summary>
public class PullServerSettings
{
    /// <summary>
    /// The URL of the pull server (e.g., https://dsc-server.example.com).
    /// </summary>
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// The node's unique identifier assigned during registration.
    /// </summary>
    public Guid? NodeId { get; set; }

    /// <summary>
    /// The API key for authentication with the pull server.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The registration key for initial node registration.
    /// </summary>
    public string? RegistrationKey { get; set; }

    /// <summary>
    /// Interval for rotating the API key. Set by the server during registration.
    /// </summary>
    public TimeSpan KeyRotationInterval { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Last time the API key was rotated.
    /// </summary>
    public DateTimeOffset? LastKeyRotation { get; set; }

    /// <summary>
    /// The cached checksum of the current configuration.
    /// </summary>
    public string? ConfigurationChecksum { get; set; }

    /// <summary>
    /// Whether to submit compliance reports to the server.
    /// </summary>
    public bool ReportCompliance { get; set; } = true;
}

/// <summary>
/// The source of configuration documents.
/// </summary>
public enum ConfigurationSource
{
    /// <summary>
    /// Use a local configuration file.
    /// </summary>
    Local,

    /// <summary>
    /// Pull configuration from a remote server.
    /// </summary>
    Pull
}

/// <summary>
/// LCM service operating modes.
/// </summary>
public enum ConfigurationMode
{
    /// <summary>
    /// Monitor mode: Run 'dsc config test' periodically.
    /// </summary>
    Monitor,

    /// <summary>
    /// Remediate mode: Run 'dsc config test' and apply corrections as needed.
    /// </summary>
    Remediate
}
