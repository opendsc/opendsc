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
// Composite configuration contracts
[JsonSerializable(typeof(CreateCompositeConfigurationRequest))]
[JsonSerializable(typeof(CreateCompositeConfigurationVersionRequest))]
[JsonSerializable(typeof(AddChildConfigurationRequest))]
[JsonSerializable(typeof(UpdateChildConfigurationRequest))]
[JsonSerializable(typeof(CompositeConfigurationSummaryDto))]
[JsonSerializable(typeof(List<CompositeConfigurationSummaryDto>))]
[JsonSerializable(typeof(CompositeConfigurationDetailsDto))]
[JsonSerializable(typeof(CompositeConfigurationVersionDto))]
[JsonSerializable(typeof(List<CompositeConfigurationVersionDto>))]
[JsonSerializable(typeof(CompositeConfigurationItemDto))]
[JsonSerializable(typeof(List<CompositeConfigurationItemDto>))]
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
// Scope Type contracts
[JsonSerializable(typeof(ScopeTypeDto))]
[JsonSerializable(typeof(List<ScopeTypeDto>))]
[JsonSerializable(typeof(CreateScopeTypeRequest))]
[JsonSerializable(typeof(UpdateScopeTypeRequest))]
[JsonSerializable(typeof(ReorderScopeTypesRequest))]
// Scope Value contracts
[JsonSerializable(typeof(ScopeValueDto))]
[JsonSerializable(typeof(List<ScopeValueDto>))]
[JsonSerializable(typeof(CreateScopeValueRequest))]
[JsonSerializable(typeof(UpdateScopeValueRequest))]
// Node Tag contracts
[JsonSerializable(typeof(NodeTagDto))]
[JsonSerializable(typeof(List<NodeTagDto>))]
[JsonSerializable(typeof(AssignNodeTagRequest))]
// Parameter contracts
[JsonSerializable(typeof(ParameterFileDto))]
[JsonSerializable(typeof(List<ParameterFileDto>))]
[JsonSerializable(typeof(CreateParameterRequest))]
[JsonSerializable(typeof(ParameterProvenanceDto))]
[JsonSerializable(typeof(ParameterSourceInfo))]
[JsonSerializable(typeof(ScopeInfo))]
[JsonSerializable(typeof(ParameterProvenance))]
[JsonSerializable(typeof(Dictionary<string, ParameterProvenance>))]
[JsonSerializable(typeof(ScopeValueInfo))]
[JsonSerializable(typeof(MergeResult))]
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
