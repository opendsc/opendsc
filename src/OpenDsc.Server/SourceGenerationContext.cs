// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using OpenDsc.Schema;
using OpenDsc.Server.Contracts;
using OpenDsc.Server.Endpoints;
using OpenDsc.Server.Services;

namespace OpenDsc.Server;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// Node contracts
[JsonSerializable(typeof(RegisterNodeRequest))]
[JsonSerializable(typeof(RegisterNodeResponse))]
[JsonSerializable(typeof(RotateCertificateRequest))]
[JsonSerializable(typeof(RotateCertificateResponse))]
[JsonSerializable(typeof(NodeSummary))]
[JsonSerializable(typeof(List<NodeSummary>))]
[JsonSerializable(typeof(AssignConfigurationRequest))]
[JsonSerializable(typeof(ConfigurationChecksumResponse))]
// Configuration contracts
[JsonSerializable(typeof(ConfigurationSummaryDto))]
[JsonSerializable(typeof(List<ConfigurationSummaryDto>))]
[JsonSerializable(typeof(ConfigurationDetailsDto))]
[JsonSerializable(typeof(CreateConfigurationDto))]
[JsonSerializable(typeof(ConfigurationVersionDto))]
[JsonSerializable(typeof(List<ConfigurationVersionDto>))]
[JsonSerializable(typeof(CreateConfigurationVersionDto))]
// Legacy configuration contracts (deprecated)
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
[JsonSerializable(typeof(CreateRegistrationKeyRequest))]
[JsonSerializable(typeof(RegistrationKeyResponse))]
[JsonSerializable(typeof(List<RegistrationKeyResponse>))]
[JsonSerializable(typeof(ErrorResponse))]
// Scope contracts
[JsonSerializable(typeof(ScopeDto))]
[JsonSerializable(typeof(List<ScopeDto>))]
[JsonSerializable(typeof(CreateScopeRequest))]
[JsonSerializable(typeof(UpdateScopeRequest))]
[JsonSerializable(typeof(ReorderScopesRequest))]
[JsonSerializable(typeof(ScopeReorderItem))]
// Parameter contracts
[JsonSerializable(typeof(ParameterVersionDto))]
[JsonSerializable(typeof(List<ParameterVersionDto>))]
[JsonSerializable(typeof(CreateParameterRequest))]
[JsonSerializable(typeof(ParameterProvenanceDto))]
[JsonSerializable(typeof(ProvenanceInfo))]
[JsonSerializable(typeof(ScopeValueInfo))]
// Health contracts
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(ReadinessResponse))]
// Retention contracts
[JsonSerializable(typeof(CleanupRequest))]
[JsonSerializable(typeof(VersionRetentionResult))]
[JsonSerializable(typeof(List<VersionDeletionInfo>))]
// Schema types
[JsonSerializable(typeof(DscResult))]
[JsonSerializable(typeof(DscOperation))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
