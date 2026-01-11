// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using NuGet.Versioning;

namespace OpenDsc.Schema;

/// <summary>
/// JSON source generation context for DSC schema types.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DscResult))]
[JsonSerializable(typeof(DscResourceResult))]
[JsonSerializable(typeof(DscGetOperationResult))]
[JsonSerializable(typeof(DscTestOperationResult))]
[JsonSerializable(typeof(DscSetOperationResult))]
[JsonSerializable(typeof(DscMessage))]
[JsonSerializable(typeof(DscTraceMessage))]
[JsonSerializable(typeof(DscTraceFields))]
[JsonSerializable(typeof(DscMetadata))]
[JsonSerializable(typeof(MicrosoftDscMetadata))]
[JsonSerializable(typeof(SemanticVersion))]
[JsonSerializable(typeof(DscRestartRequirement))]
[JsonSerializable(typeof(DscProcessRestartInfo))]
[JsonSerializable(typeof(DscExitCode))]
[JsonSerializable(typeof(DscOperation))]
[JsonSerializable(typeof(DscExecutionKind))]
[JsonSerializable(typeof(DscSecurityContext))]
[JsonSerializable(typeof(DscMessageLevel))]
[JsonSerializable(typeof(DscTraceLevel))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
