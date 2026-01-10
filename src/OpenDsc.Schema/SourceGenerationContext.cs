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
[JsonSerializable(typeof(DscResult), TypeInfoPropertyName = "DscResult")]
[JsonSerializable(typeof(DscResourceResult), TypeInfoPropertyName = "DscResourceResult")]
[JsonSerializable(typeof(DscGetOperationResult), TypeInfoPropertyName = "DscGetOperationResult")]
[JsonSerializable(typeof(DscTestOperationResult), TypeInfoPropertyName = "DscTestOperationResult")]
[JsonSerializable(typeof(DscSetOperationResult), TypeInfoPropertyName = "DscSetOperationResult")]
[JsonSerializable(typeof(DscMessage), TypeInfoPropertyName = "DscMessage")]
[JsonSerializable(typeof(DscTraceMessage), TypeInfoPropertyName = "DscTraceMessage")]
[JsonSerializable(typeof(DscTraceFields), TypeInfoPropertyName = "DscTraceFields")]
[JsonSerializable(typeof(DscMetadata), TypeInfoPropertyName = "DscMetadata")]
[JsonSerializable(typeof(MicrosoftDscMetadata), TypeInfoPropertyName = "MicrosoftDscMetadata")]
[JsonSerializable(typeof(SemanticVersion), TypeInfoPropertyName = "SemanticVersion")]
[JsonSerializable(typeof(DscRestartRequirement), TypeInfoPropertyName = "DscRestartRequirement")]
[JsonSerializable(typeof(DscProcessRestartInfo), TypeInfoPropertyName = "DscProcessRestartInfo")]
[JsonSerializable(typeof(DscExitCode), TypeInfoPropertyName = "DscExitCode")]
[JsonSerializable(typeof(DscOperation), TypeInfoPropertyName = "DscOperation")]
[JsonSerializable(typeof(DscExecutionKind), TypeInfoPropertyName = "DscExecutionKind")]
[JsonSerializable(typeof(DscSecurityContext), TypeInfoPropertyName = "DscSecurityContext")]
[JsonSerializable(typeof(DscMessageLevel), TypeInfoPropertyName = "DscMessageLevel")]
[JsonSerializable(typeof(DscTraceLevel), TypeInfoPropertyName = "DscTraceLevel")]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
