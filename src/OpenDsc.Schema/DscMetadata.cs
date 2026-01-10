// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using NuGet.Versioning;

namespace OpenDsc.Schema;

/// <summary>
/// Metadata from a DSC configuration operation.
/// </summary>
public sealed class DscMetadata
{
    /// <summary>
    /// Microsoft DSC metadata containing operation context.
    /// </summary>
    [JsonPropertyName("Microsoft.DSC")]
    public MicrosoftDscMetadata? MicrosoftDsc { get; set; }

    /// <summary>
    /// Additional metadata properties from other providers.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalProperties { get; set; }
}

/// <summary>
/// Microsoft DSC operation metadata.
/// </summary>
public sealed class MicrosoftDscMetadata
{
    /// <summary>
    /// The DSC version that performed the operation.
    /// </summary>
    [JsonConverter(typeof(SemanticVersionConverter))]
    public SemanticVersion? Version { get; set; }

    /// <summary>
    /// The operation that was performed (Get, Set, Test, Export).
    /// </summary>
    public DscOperation? Operation { get; set; }

    /// <summary>
    /// The execution type (Actual or WhatIf).
    /// </summary>
    public DscExecutionKind? ExecutionType { get; set; }

    /// <summary>
    /// The start timestamp in RFC3339 format.
    /// </summary>
    public DateTimeOffset? StartDatetime { get; set; }

    /// <summary>
    /// The end timestamp in RFC3339 format.
    /// </summary>
    public DateTimeOffset? EndDatetime { get; set; }

    /// <summary>
    /// The duration of the operation in ISO 8601 format.
    /// </summary>
    [JsonConverter(typeof(Iso8601DurationConverter))]
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// The security context (Current, Elevated, or Restricted).
    /// </summary>
    public DscSecurityContext? SecurityContext { get; set; }

    /// <summary>
    /// Restart requirements aggregated from resources.
    /// </summary>
    public List<DscRestartRequirement>? RestartRequired { get; set; }
}

/// <summary>
/// Process restart information.
/// </summary>
public sealed class DscProcessRestartInfo
{
    /// <summary>
    /// The process name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The process ID.
    /// </summary>
    public uint? Id { get; set; }
}
