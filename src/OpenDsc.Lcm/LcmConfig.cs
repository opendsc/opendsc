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
    /// Directory containing the DSC configuration documents (relative to the LCM config directory).
    /// </summary>
    [Required]
    public string ConfigurationDirectory { get; set; } = "config";

    /// <summary>
    /// Name of the main DSC configuration file within the ConfigurationDirectory.
    /// </summary>
    [Required]
    public string ConfigurationFileName { get; set; } = "config.dsc.yaml";

    /// <summary>
    /// Gets the full path to the DSC configuration directory.
    /// </summary>
    public string FullConfigurationDirectory => Path.Combine(ConfigPaths.GetLcmConfigDirectory(), ConfigurationDirectory);

    /// <summary>
    /// Gets the full path to the main DSC configuration file.
    /// </summary>
    public string FullConfigurationPath => Path.Combine(FullConfigurationDirectory, ConfigurationFileName);

    private TimeSpan _configurationModeInterval = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Interval between DSC operations (for Monitor and Remediate modes).
    /// Can be specified as a TimeSpan string like "00:15:00" for 15 minutes.
    /// </summary>
    [Required]
    public TimeSpan ConfigurationModeInterval
    {
        get => _configurationModeInterval;
        set
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Interval must be greater than 0");
            }

            _configurationModeInterval = value;
        }
    }

    /// <summary>
    /// Path to the DSC executable. If not specified, uses 'dsc' from PATH.
    /// </summary>
    public string? DscExecutablePath { get; set; }
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
