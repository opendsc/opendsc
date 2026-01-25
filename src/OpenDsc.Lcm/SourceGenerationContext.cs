// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using OpenDsc.Schema;

namespace OpenDsc.Lcm;

/// <summary>
/// JSON source generation context for LCM pull server client types.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// Pull server client types
[JsonSerializable(typeof(RegisterNodeRequest))]
[JsonSerializable(typeof(RegisterNodeResponse))]
[JsonSerializable(typeof(RotateKeyResponse))]
[JsonSerializable(typeof(ConfigurationChecksumResponse))]
[JsonSerializable(typeof(SubmitReportRequest))]
// Schema types for DSC result parsing (also include here for HTTP client usage)
[JsonSerializable(typeof(DscResult))]
[JsonSerializable(typeof(DscResourceResult))]
[JsonSerializable(typeof(DscTestOperationResult))]
[JsonSerializable(typeof(DscSetOperationResult))]
[JsonSerializable(typeof(DscTraceMessage))]
[JsonSerializable(typeof(DscOperation))]
public partial class PullServerJsonContext : JsonSerializerContext
{
}
