// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.CompositeConfigurations;
using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Parameters;
using OpenDsc.Contracts.Reports;
using OpenDsc.Contracts.Settings;
using OpenDsc.Schema;
using OpenDsc.Server.Endpoints;
using OpenDsc.Server.Entities;
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
[JsonSerializable(typeof(UpdateLcmStatusRequest))]
[JsonSerializable(typeof(NodeSummary))]
[JsonSerializable(typeof(List<NodeSummary>))]
[JsonSerializable(typeof(NodeStatusEventSummary))]
[JsonSerializable(typeof(List<NodeStatusEventSummary>))]
[JsonSerializable(typeof(AssignConfigurationRequest))]
[JsonSerializable(typeof(ConfigurationChecksumResponse))]
// Configuration contracts
[JsonSerializable(typeof(Contracts.Configurations.ConfigurationSummary))]
[JsonSerializable(typeof(List<Contracts.Configurations.ConfigurationSummary>))]
[JsonSerializable(typeof(Contracts.Configurations.ConfigurationDetails))]
[JsonSerializable(typeof(ConfigurationVersionDetails))]
[JsonSerializable(typeof(List<ConfigurationVersionDetails>))]
[JsonSerializable(typeof(CreateConfigurationAdminRequest))]
[JsonSerializable(typeof(UpdateConfigurationAdminRequest))]
[JsonSerializable(typeof(CreateConfigurationVersionRequest))]
[JsonSerializable(typeof(CreateVersionFromExistingRequest))]
[JsonSerializable(typeof(UpdateConfigurationSettingsRequest))]
[JsonSerializable(typeof(SaveRetentionSettingsRequest))]
[JsonSerializable(typeof(ConfigurationSettingsSummary))]
[JsonSerializable(typeof(ConfigurationRetentionSummary))]
// Legacy configuration contracts (deprecated)
[JsonSerializable(typeof(CreateConfigurationRequest))]
[JsonSerializable(typeof(UpdateConfigurationRequest))]
// Composite configuration contracts
[JsonSerializable(typeof(CreateCompositeConfigurationRequest))]
[JsonSerializable(typeof(CreateCompositeConfigurationVersionRequest))]
[JsonSerializable(typeof(AddChildConfigurationRequest))]
[JsonSerializable(typeof(UpdateChildConfigurationRequest))]
[JsonSerializable(typeof(CompositeConfigurationSummary))]
[JsonSerializable(typeof(List<CompositeConfigurationSummary>))]
[JsonSerializable(typeof(CompositeConfigurationDetails))]
[JsonSerializable(typeof(CompositeConfigurationVersionDetails))]
[JsonSerializable(typeof(List<CompositeConfigurationVersionDetails>))]
[JsonSerializable(typeof(CompositeConfigurationItemDetails))]
[JsonSerializable(typeof(List<CompositeConfigurationItemDetails>))]
// Report contracts
[JsonSerializable(typeof(SubmitReportRequest))]
[JsonSerializable(typeof(ReportSummary))]
[JsonSerializable(typeof(List<ReportSummary>))]
[JsonSerializable(typeof(ReportDetails))]
// Settings contracts
[JsonSerializable(typeof(ServerSettingsResponse))]
[JsonSerializable(typeof(UpdateServerSettingsRequest))]
[JsonSerializable(typeof(CreateRegistrationKeyRequest))]
[JsonSerializable(typeof(UpdateRegistrationKeyRequest))]
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
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.ParameterVersionDetails))]
[JsonSerializable(typeof(List<OpenDsc.Contracts.Parameters.ParameterVersionDetails>))]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.CreateParameterRequest))]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.ParameterProvenanceDetails))]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.ParameterSourceInfo))]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.ScopeInfo))]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.ParameterResolutionDetails))]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.ScopeResolutionDetails))]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.MajorVersionSummary))]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.ValidationResult), TypeInfoPropertyName = "ParameterValidationResult")]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.ValidationError), TypeInfoPropertyName = "ParameterValidationError")]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.PublishResult), TypeInfoPropertyName = "ParameterPublishResult")]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.CompatibilityReport), TypeInfoPropertyName = "ParameterCompatibilityReport")]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.ParameterChange))]
[JsonSerializable(typeof(OpenDsc.Contracts.Parameters.ParameterFileMigrationStatus), TypeInfoPropertyName = "ParameterFileMigrationStatusModel")]
[JsonSerializable(typeof(ScopeValueInfo))]
[JsonSerializable(typeof(MergeResult))]
// Health contracts
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(ReadinessResponse))]
// Authentication contracts
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(CurrentUserResponse))]
[JsonSerializable(typeof(ChangePasswordRequest))]
[JsonSerializable(typeof(CreateTokenRequest))]
[JsonSerializable(typeof(CreateTokenResponse))]
[JsonSerializable(typeof(TokenMetadata))]
[JsonSerializable(typeof(List<TokenMetadata>))]
[JsonSerializable(typeof(OidcProviderInfo))]
[JsonSerializable(typeof(IEnumerable<OidcProviderInfo>))]
// User contracts
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(List<UserDto>))]
[JsonSerializable(typeof(UserDetailDto))]
[JsonSerializable(typeof(CreateUserRequest))]
[JsonSerializable(typeof(UpdateUserRequest))]
[JsonSerializable(typeof(ResetPasswordRequest))]
[JsonSerializable(typeof(SetRolesRequest))]
[JsonSerializable(typeof(RoleDto))]
[JsonSerializable(typeof(List<RoleDto>))]
[JsonSerializable(typeof(GroupDto))]
[JsonSerializable(typeof(List<GroupDto>))]
// Group contracts
[JsonSerializable(typeof(GroupSummaryDto))]
[JsonSerializable(typeof(List<GroupSummaryDto>))]
[JsonSerializable(typeof(GroupDetailDto))]
[JsonSerializable(typeof(CreateGroupRequest))]
[JsonSerializable(typeof(UpdateGroupRequest))]
[JsonSerializable(typeof(SetMembersRequest))]
[JsonSerializable(typeof(ExternalGroupMappingDto))]
[JsonSerializable(typeof(List<ExternalGroupMappingDto>))]
[JsonSerializable(typeof(CreateExternalGroupMappingRequest))]
// Role contracts
[JsonSerializable(typeof(RoleSummaryDto))]
[JsonSerializable(typeof(List<RoleSummaryDto>))]
[JsonSerializable(typeof(RoleDetailDto))]
[JsonSerializable(typeof(CreateRoleRequest))]
[JsonSerializable(typeof(UpdateRoleRequest))]
// Retention contracts
[JsonSerializable(typeof(CleanupRequest))]
[JsonSerializable(typeof(RecordCleanupRequest))]
[JsonSerializable(typeof(VersionRetentionResult))]
[JsonSerializable(typeof(List<VersionDeletionInfo>))]
[JsonSerializable(typeof(RetentionRunDto))]
[JsonSerializable(typeof(List<RetentionRunDto>))]
[JsonSerializable(typeof(RetentionSettingsDto))]
[JsonSerializable(typeof(UpdateRetentionSettingsRequest))]
// Parameter validation
[JsonSerializable(typeof(OpenDsc.Contracts.Configurations.ValidationError), TypeInfoPropertyName = "ConfigurationValidationError")]
[JsonSerializable(typeof(List<OpenDsc.Contracts.Configurations.ValidationError>), TypeInfoPropertyName = "ConfigurationListValidationError")]
[JsonSerializable(typeof(List<OpenDsc.Contracts.Parameters.ValidationError>), TypeInfoPropertyName = "ParameterListValidationError")]
[JsonSerializable(typeof(OpenDsc.Server.Services.ValidationResult), TypeInfoPropertyName = "ServerValidationResult")]
[JsonSerializable(typeof(OpenDsc.Contracts.Configurations.CompatibilityReport), TypeInfoPropertyName = "ConfigurationCompatibilityReport")]
[JsonSerializable(typeof(SchemaChange))]
[JsonSerializable(typeof(OpenDsc.Contracts.Configurations.ParameterFileMigrationStatus), TypeInfoPropertyName = "ConfigurationParameterFileMigrationStatus")]
[JsonSerializable(typeof(List<OpenDsc.Contracts.Configurations.ParameterFileMigrationStatus>), TypeInfoPropertyName = "ConfigurationListParameterFileMigrationStatus")]
[JsonSerializable(typeof(List<OpenDsc.Contracts.Parameters.ParameterFileMigrationStatus>), TypeInfoPropertyName = "ParameterListParameterFileMigrationStatus")]
[JsonSerializable(typeof(VersionUsageInfo))]
[JsonSerializable(typeof(OpenDsc.Contracts.Configurations.PublishResult), TypeInfoPropertyName = "ConfigurationPublishResult")]
// Schema types
[JsonSerializable(typeof(DscResult))]
[JsonSerializable(typeof(DscOperation))]
// Enums
[JsonSerializable(typeof(ScopeValueMode))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
