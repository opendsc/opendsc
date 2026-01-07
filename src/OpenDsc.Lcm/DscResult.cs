// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OpenDsc.Lcm;

/// <summary>
/// Result from a DSC configuration operation.
/// </summary>
public class DscResult
{
    /// <summary>
    /// Exit code from the DSC process.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int ExitCode { get; set; }

    /// <summary>
    /// Metadata from the DSC operation.
    /// </summary>
    [JsonPropertyName("metadata")]
    public object? Metadata { get; set; }

    /// <summary>
    /// List of resource results.
    /// </summary>
    [JsonPropertyName("results")]
    [Required]
    public List<DscResourceResult>? Resources { get; set; }

    /// <summary>
    /// List of messages from DSC.
    /// </summary>
    [JsonPropertyName("messages")]
    [Required]
    public List<DscMessage>? Messages { get; set; }

    /// <summary>
    /// Whether the operation had errors.
    /// </summary>
    [JsonPropertyName("hadErrors")]
    [Required]
    public bool HadErrors { get; set; }

    /// <summary>
    /// Outputs from the DSC operation.
    /// </summary>
    [JsonPropertyName("outputs")]
    public object? Outputs { get; set; }

    /// <summary>
    /// Restart requirements.
    /// </summary>
    [JsonPropertyName("_restartRequired")]
    public List<DscRestartRequirement>? RestartRequired { get; set; }

    /// <summary>
    /// Additional properties from the DSC result.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalProperties { get; set; }

    /// <summary>
    /// Gets whether all resources are in desired state.
    /// This is computed from the Resources collection.
    /// Returns false if any resource has InDesiredState = false, or if InDesiredState is null (unknown state).
    /// </summary>
    public bool AllResourcesInDesiredState => Resources?.All(r => r.Result?.InDesiredState == true) ?? true;
}

/// <summary>
/// Result for an individual DSC resource.
/// </summary>
public class DscResourceResult
{
    /// <summary>
    /// The resource type.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The resource name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The detailed result of the resource operation.
    /// </summary>
    [JsonPropertyName("result")]
    public DscResourceOperationResult? Result { get; set; }

    /// <summary>
    /// Additional properties from the resource result.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalProperties { get; set; }
}

/// <summary>
/// The result details of a DSC resource operation.
/// </summary>
public class DscResourceOperationResult
{
    /// <summary>
    /// The desired state of the resource.
    /// </summary>
    [JsonPropertyName("desiredState")]
    public object? DesiredState { get; set; }

    /// <summary>
    /// The actual state of the resource.
    /// </summary>
    [JsonPropertyName("actualState")]
    public object? ActualState { get; set; }

    /// <summary>
    /// Whether the resource is in the desired state.
    /// </summary>
    [JsonPropertyName("inDesiredState")]
    public bool? InDesiredState { get; set; }

    /// <summary>
    /// Properties that differ between desired and actual state.
    /// </summary>
    [JsonPropertyName("differingProperties")]
    public string[]? DifferingProperties { get; set; }

    /// <summary>
    /// Additional properties from the operation result.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalProperties { get; set; }
}

/// <summary>
/// A message from DSC.
/// </summary>
public class DscMessage
{
    /// <summary>
    /// The timestamp of the message.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>
    /// The message level.
    /// </summary>
    [JsonPropertyName("level")]
    public string? Level { get; set; }

    /// <summary>
    /// The message fields.
    /// </summary>
    [JsonPropertyName("fields")]
    public DscMessageFields? Fields { get; set; }
}

/// <summary>
/// Fields within a DSC message.
/// </summary>
public class DscMessageFields
{
    /// <summary>
    /// The message text.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Restart requirement information.
/// </summary>
public class DscRestartRequirement
{
    /// <summary>
    /// The type of restart required.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Additional restart data.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalProperties { get; set; }
}
