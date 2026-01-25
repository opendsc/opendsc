// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using OpenDsc.Schema;
using OpenDsc.Server.Contracts;
using OpenDsc.Server.Endpoints;

namespace OpenDsc.Server;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// Node contracts
[JsonSerializable(typeof(RegisterNodeRequest))]
[JsonSerializable(typeof(RegisterNodeResponse))]
[JsonSerializable(typeof(RotateKeyResponse))]
[JsonSerializable(typeof(NodeSummary))]
[JsonSerializable(typeof(List<NodeSummary>))]
[JsonSerializable(typeof(AssignConfigurationRequest))]
[JsonSerializable(typeof(ConfigurationChecksumResponse))]
// Configuration contracts
[JsonSerializable(typeof(CreateConfigurationRequest))]
[JsonSerializable(typeof(UpdateConfigurationRequest))]
[JsonSerializable(typeof(ConfigurationSummary))]
[JsonSerializable(typeof(List<ConfigurationSummary>))]
[JsonSerializable(typeof(ConfigurationDetails))]
// Report contracts
[JsonSerializable(typeof(SubmitReportRequest))]
[JsonSerializable(typeof(ReportSummary))]
[JsonSerializable(typeof(List<ReportSummary>))]
[JsonSerializable(typeof(ReportDetails))]
// Settings contracts
[JsonSerializable(typeof(ServerSettingsResponse))]
[JsonSerializable(typeof(UpdateServerSettingsRequest))]
[JsonSerializable(typeof(RotateRegistrationKeyResponse))]
[JsonSerializable(typeof(ErrorResponse))]
// Health contracts
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(ReadinessResponse))]
// Schema types
[JsonSerializable(typeof(DscResult))]
[JsonSerializable(typeof(DscOperation))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
