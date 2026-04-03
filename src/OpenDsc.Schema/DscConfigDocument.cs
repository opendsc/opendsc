// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Schema;

/// <summary>
/// A DSC v3 configuration document.
/// </summary>
public sealed class DscConfigDocument
{
    /// <summary>
    /// The JSON schema URI for this configuration document.
    /// </summary>
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = "https://aka.ms/dsc/schemas/v3/bundled/config/document.json";

    /// <summary>
    /// The resource instances declared in this configuration.
    /// </summary>
    [JsonRequired]
    public IReadOnlyList<DscConfigResource> Resources { get; set; } = [];
}
