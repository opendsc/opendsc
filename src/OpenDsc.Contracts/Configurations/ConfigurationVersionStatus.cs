// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Status of a configuration version.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ConfigurationVersionStatus>))]
public enum ConfigurationVersionStatus
{
    /// <summary>
    /// The configuration version is a draft and has not been published.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// The configuration version has been published and is available for use.
    /// </summary>
    Published = 1
}
