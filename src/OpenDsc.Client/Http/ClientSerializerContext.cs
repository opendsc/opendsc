// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.CompositeConfigurations;
using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Parameters;
using OpenDsc.Contracts.Permissions;
using OpenDsc.Contracts.Reports;
using OpenDsc.Contracts.Retention;
using OpenDsc.Contracts.Settings;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Http;

[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(Guid?), TypeInfoPropertyName = "NullableGuid")]
[JsonSerializable(typeof(List<int>), TypeInfoPropertyName = "IntList")]
[JsonSerializable(typeof(List<string>), TypeInfoPropertyName = "StringList")]
[JsonSerializable(typeof(Dictionary<Guid, int>), TypeInfoPropertyName = "GuidIntDictionary")]
[JsonSerializable(typeof(HashSet<string>), TypeInfoPropertyName = "StringHashSet")]
// Configurations
[JsonSerializable(typeof(OpenDsc.Contracts.Configurations.ConfigurationDetails))]
[JsonSerializable(typeof(OpenDsc.Contracts.Configurations.ConfigurationSummary))]
[JsonSerializable(typeof(List<OpenDsc.Contracts.Configurations.ConfigurationSummary>), TypeInfoPropertyName = "ConfigurationSummaryList")]
[JsonSerializable(typeof(ConfigurationVersionDetails))]
[JsonSerializable(typeof(List<ConfigurationVersionDetails>), TypeInfoPropertyName = "ConfigurationVersionDetailsList")]
[JsonSerializable(typeof(ConfigurationSettingsSummary))]
[JsonSerializable(typeof(ConfigurationRetentionSummary))]
[JsonSerializable(typeof(VersionUsageInfo))]
[JsonSerializable(typeof(OpenDsc.Contracts.Configurations.PublishResult))]
[JsonSerializable(typeof(UpdateConfigurationSettingsRequest))]
[JsonSerializable(typeof(SaveRetentionSettingsRequest))]
[JsonSerializable(typeof(UpdateConfigurationAdminRequest))]
[JsonSerializable(typeof(CreateVersionFromExistingRequest))]
// Composite configurations
[JsonSerializable(typeof(CompositeConfigurationDetails))]
[JsonSerializable(typeof(CompositeConfigurationSummary))]
[JsonSerializable(typeof(List<CompositeConfigurationSummary>), TypeInfoPropertyName = "CompositeConfigurationSummaryList")]
[JsonSerializable(typeof(CompositeConfigurationVersionDetails))]
[JsonSerializable(typeof(List<CompositeConfigurationVersionDetails>), TypeInfoPropertyName = "CompositeConfigurationVersionDetailsList")]
[JsonSerializable(typeof(CompositeConfigurationItemDetails))]
[JsonSerializable(typeof(ChildConfigurationOption))]
[JsonSerializable(typeof(List<ChildConfigurationOption>), TypeInfoPropertyName = "ChildConfigurationOptionList")]
[JsonSerializable(typeof(CreateCompositeConfigurationRequest))]
[JsonSerializable(typeof(CreateCompositeConfigurationVersionRequest))]
[JsonSerializable(typeof(AddChildConfigurationRequest))]
[JsonSerializable(typeof(UpdateChildConfigurationRequest))]
[JsonSerializable(typeof(CreateCompositeVersionFromExistingRequest))]
// Nodes
[JsonSerializable(typeof(NodeSummary))]
[JsonSerializable(typeof(List<NodeSummary>), TypeInfoPropertyName = "NodeSummaryList")]
[JsonSerializable(typeof(NodeDetails))]
[JsonSerializable(typeof(NodeAssignmentSummary))]
[JsonSerializable(typeof(NodeTagSummary))]
[JsonSerializable(typeof(List<NodeTagSummary>), TypeInfoPropertyName = "NodeTagSummaryList")]
[JsonSerializable(typeof(NodeScopeValueSummary))]
[JsonSerializable(typeof(List<NodeScopeValueSummary>), TypeInfoPropertyName = "NodeScopeValueSummaryList")]
[JsonSerializable(typeof(NodeStatusEventSummary))]
[JsonSerializable(typeof(List<NodeStatusEventSummary>), TypeInfoPropertyName = "NodeStatusEventSummaryList")]
[JsonSerializable(typeof(NodeConfigurationManifest))]
[JsonSerializable(typeof(ConfigurationChecksumResponse))]
[JsonSerializable(typeof(ConfigurationOption))]
[JsonSerializable(typeof(List<ConfigurationOption>), TypeInfoPropertyName = "ConfigurationOptionList")]
[JsonSerializable(typeof(ConfigurationAssignmentOption))]
[JsonSerializable(typeof(List<ConfigurationAssignmentOption>), TypeInfoPropertyName = "ConfigurationAssignmentOptionList")]
[JsonSerializable(typeof(ScopeTypeSummary))]
[JsonSerializable(typeof(List<ScopeTypeSummary>), TypeInfoPropertyName = "ScopeTypeSummaryList")]
[JsonSerializable(typeof(ScopeValueSummary))]
[JsonSerializable(typeof(List<ScopeValueSummary>), TypeInfoPropertyName = "ScopeValueSummaryList")]
[JsonSerializable(typeof(AssignConfigurationRequest))]
[JsonSerializable(typeof(AddNodeTagRequest))]
[JsonSerializable(typeof(RemoveNodeTagRequest))]
[JsonSerializable(typeof(SetNodeScopeValueRequest))]
[JsonSerializable(typeof(UpdateNodeLcmConfigRequest))]
// LCM / node API
[JsonSerializable(typeof(RegisterNodeRequest))]
[JsonSerializable(typeof(RegisterNodeResponse))]
[JsonSerializable(typeof(UpdateLcmStatusRequest))]
[JsonSerializable(typeof(RotateCertificateRequest))]
[JsonSerializable(typeof(RotateCertificateResponse))]
[JsonSerializable(typeof(ReportNodeLcmConfigRequest))]
[JsonSerializable(typeof(NodeLcmConfigResponse))]
[JsonSerializable(typeof(PublicSettingsResponse))]
// Parameters
[JsonSerializable(typeof(ParameterVersionDetails))]
[JsonSerializable(typeof(List<ParameterVersionDetails>), TypeInfoPropertyName = "ParameterVersionDetailsList")]
[JsonSerializable(typeof(ParameterProvenanceDetails))]
[JsonSerializable(typeof(ParameterResolutionDetails))]
[JsonSerializable(typeof(MajorVersionSummary))]
[JsonSerializable(typeof(List<MajorVersionSummary>), TypeInfoPropertyName = "MajorVersionSummaryList")]
[JsonSerializable(typeof(CreateParameterRequest))]
[JsonSerializable(typeof(UpdateParameterRequest))]
// Permissions
[JsonSerializable(typeof(PermissionEntry))]
[JsonSerializable(typeof(List<PermissionEntry>), TypeInfoPropertyName = "PermissionEntryList")]
[JsonSerializable(typeof(GrantPermissionRequest))]
[JsonSerializable(typeof(RevokePermissionRequest))]
// Reports
[JsonSerializable(typeof(ReportSummary))]
[JsonSerializable(typeof(List<ReportSummary>), TypeInfoPropertyName = "ReportSummaryList")]
[JsonSerializable(typeof(ReportDetails))]
[JsonSerializable(typeof(SubmitReportRequest))]
// Retention
[JsonSerializable(typeof(RetentionRunSummary))]
[JsonSerializable(typeof(List<RetentionRunSummary>), TypeInfoPropertyName = "RetentionRunSummaryList")]
// Settings
[JsonSerializable(typeof(ServerSettingsSummary))]
[JsonSerializable(typeof(ServerLcmDefaultsSummary))]
[JsonSerializable(typeof(ValidationSettingsSummary))]
[JsonSerializable(typeof(RetentionSettingsSummary))]
[JsonSerializable(typeof(UpdateServerSettingsRequest))]
[JsonSerializable(typeof(UpdateServerLcmDefaultsRequest))]
[JsonSerializable(typeof(UpdateValidationSettingsRequest))]
[JsonSerializable(typeof(UpdateRetentionSettingsRequest))]
// Scope
[JsonSerializable(typeof(ScopeTypeDetails))]
[JsonSerializable(typeof(List<ScopeTypeDetails>), TypeInfoPropertyName = "ScopeTypeDetailsList")]
[JsonSerializable(typeof(ScopeValueDetails))]
[JsonSerializable(typeof(List<ScopeValueDetails>), TypeInfoPropertyName = "ScopeValueDetailsList")]
[JsonSerializable(typeof(ScopeTypeWithValuesDetails))]
[JsonSerializable(typeof(List<ScopeTypeWithValuesDetails>), TypeInfoPropertyName = "ScopeTypeWithValuesDetailsList")]
[JsonSerializable(typeof(ScopeNodeInfo))]
[JsonSerializable(typeof(List<ScopeNodeInfo>), TypeInfoPropertyName = "ScopeNodeInfoList")]
[JsonSerializable(typeof(ScopeParameterInfo))]
[JsonSerializable(typeof(List<ScopeParameterInfo>), TypeInfoPropertyName = "ScopeParameterInfoList")]
[JsonSerializable(typeof(ScopeSummaryResponse))]
[JsonSerializable(typeof(CreateScopeTypeRequest))]
[JsonSerializable(typeof(UpdateScopeTypeRequest))]
[JsonSerializable(typeof(ReorderScopeTypesRequest))]
[JsonSerializable(typeof(CreateScopeValueRequest))]
[JsonSerializable(typeof(UpdateScopeValueRequest))]
// Registration keys
[JsonSerializable(typeof(RegistrationKeyResponse))]
[JsonSerializable(typeof(List<RegistrationKeyResponse>), TypeInfoPropertyName = "RegistrationKeyResponseList")]
[JsonSerializable(typeof(CreateRegistrationKeyRequest))]
[JsonSerializable(typeof(UpdateRegistrationKeyRequest))]
// Users
[JsonSerializable(typeof(UserSummary))]
[JsonSerializable(typeof(List<UserSummary>), TypeInfoPropertyName = "UserSummaryList")]
[JsonSerializable(typeof(UserDetails))]
[JsonSerializable(typeof(CurrentUserDetails))]
[JsonSerializable(typeof(AuthenticationResult))]
[JsonSerializable(typeof(LoginResult))]
[JsonSerializable(typeof(CreateUserRequest))]
[JsonSerializable(typeof(UpdateUserRequest))]
[JsonSerializable(typeof(ResetPasswordRequest))]
[JsonSerializable(typeof(ChangePasswordRequest))]
[JsonSerializable(typeof(AssignRoleRequest))]
[JsonSerializable(typeof(RemoveRoleRequest))]
[JsonSerializable(typeof(SetUserRolesRequest))]
[JsonSerializable(typeof(LoginRequest))]
// Roles
[JsonSerializable(typeof(RoleSummary))]
[JsonSerializable(typeof(List<RoleSummary>), TypeInfoPropertyName = "RoleSummaryList")]
[JsonSerializable(typeof(RoleDetails))]
[JsonSerializable(typeof(CreateRoleRequest))]
[JsonSerializable(typeof(UpdateRoleRequest))]
[JsonSerializable(typeof(SetRoleGroupsRequest))]
// Groups
[JsonSerializable(typeof(GroupSummary))]
[JsonSerializable(typeof(List<GroupSummary>), TypeInfoPropertyName = "GroupSummaryList")]
[JsonSerializable(typeof(GroupDetails))]
[JsonSerializable(typeof(ExternalGroupMappingInfo))]
[JsonSerializable(typeof(List<ExternalGroupMappingInfo>), TypeInfoPropertyName = "ExternalGroupMappingInfoList")]
[JsonSerializable(typeof(CreateGroupRequest))]
[JsonSerializable(typeof(UpdateGroupRequest))]
[JsonSerializable(typeof(AddGroupMemberRequest))]
[JsonSerializable(typeof(RemoveGroupMemberRequest))]
[JsonSerializable(typeof(AssignGroupRoleRequest))]
[JsonSerializable(typeof(RemoveGroupRoleRequest))]
[JsonSerializable(typeof(SetGroupMembersRequest))]
[JsonSerializable(typeof(SetGroupRolesRequest))]
[JsonSerializable(typeof(CreateExternalGroupMappingRequest))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
internal sealed partial class ClientSerializerContext : JsonSerializerContext;
