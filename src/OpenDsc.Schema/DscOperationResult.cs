// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDsc.Schema;

/// <summary>
/// Result from a DSC resource get operation.
/// </summary>
public sealed class DscGetOperationResult
{
    /// <summary>
    /// The actual state of the resource.
    /// </summary>
    [JsonRequired]
    public JsonElement ActualState { get; set; }
}

/// <summary>
/// Result from a DSC resource test operation.
/// </summary>
public sealed class DscTestOperationResult
{
    /// <summary>
    /// The desired state of the resource.
    /// </summary>
    [JsonRequired]
    public JsonElement DesiredState { get; set; }

    /// <summary>
    /// The actual state of the resource.
    /// </summary>
    [JsonRequired]
    public JsonElement ActualState { get; set; }

    /// <summary>
    /// Whether the resource is in the desired state.
    /// </summary>
    [JsonRequired]
    public bool InDesiredState { get; set; }

    /// <summary>
    /// Properties that differ between desired and actual state.
    /// </summary>
    public string[]? DifferingProperties { get; set; }
}

/// <summary>
/// Result from a DSC resource set operation.
/// </summary>
public sealed class DscSetOperationResult
{
    /// <summary>
    /// The state of the resource before the set operation.
    /// </summary>
    [JsonRequired]
    public JsonElement BeforeState { get; set; }

    /// <summary>
    /// The state of the resource after the set operation.
    /// </summary>
    [JsonRequired]
    public JsonElement AfterState { get; set; }

    /// <summary>
    /// Properties that were changed by the set operation.
    /// </summary>
    public string[]? ChangedProperties { get; set; }
}
