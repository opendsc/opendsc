// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Contracts.Lcm;

/// <summary>
/// LCM service operating modes.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ConfigurationMode>))]
public enum ConfigurationMode
{
    /// <summary>
    /// Monitor mode: run 'dsc config test' periodically and report drift.
    /// </summary>
    Monitor,

    /// <summary>
    /// Remediate mode: run 'dsc config test' and apply corrections as needed.
    /// </summary>
    Remediate
}
