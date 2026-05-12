// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server;

internal static class EventIds
{
    // Configuration Service Events: 2000-2099
    public const int ConfigurationNameRequired = 2001;
    public const int FilesRequired = 2002;
    public const int ConfigurationAlreadyExists = 2003;
    public const int EntryPointNotFoundInUploadedFiles = 2004;
    public const int ErrorCreatingConfiguration = 2005;
    public const int ConfigurationNotFound = 2006;
    public const int VersionAlreadyExists = 2007;
    public const int ErrorCreatingVersion = 2008;
    public const int SourceVersionNotFound = 2009;
    public const int ErrorCreatingVersionFromExisting = 2010;
    public const int SourceVersionDirectoryNotFound = 2011;
    public const int SourceFileNotFound = 2012;
    public const int CannotAddFilesToPublishedVersion = 2013;
    public const int FileAlreadyExistsInVersion = 2014;
    public const int ErrorAddingFilesToVersion = 2015;
    public const int ErrorDeletingConfiguration = 2019;
    public const int VersionNotFound = 2020;
    public const int ErrorDeletingVersion = 2021;
    public const int CannotDeleteFilesFromPublishedVersion = 2022;
    public const int FileNotFoundInVersion = 2023;
    public const int ErrorDeletingFileFromVersion = 2024;
    public const int CannotChangeEntryPointOfPublishedVersion = 2025;
    public const int EntryPointFileNotFoundInVersion = 2026;
    public const int ErrorChangingEntryPoint = 2027;
    public const int FileNotFound = 2028;
    public const int ErrorDownloadingFile = 2029;
    public const int ErrorSavingFile = 2030;

    // Parameter Service Events: 3000-3099
    public const int ErrorCreatingUpdatingParameter = 3001;
    public const int ErrorPublishingParameterVersion = 3002;
    public const int ErrorDeletingParameterVersion = 3003;
    public const int ErrorGettingParameterProvenance = 3004;
    public const int ErrorUpdatingDraftParameter = 3005;
    public const int ErrorReadingParameterContent = 3006;

    // Node Service Events: 4000-4099
    public const int NoConfigurationFoundForNode = 4010;

    // Version Retention Service Events: 5000-5099
    public const int VersionInActiveUseKeeping = 5001;
    public const int DeletedConfigurationVersion = 5003;
    public const int DeletedParameterVersion = 5004;
    public const int CleanupCompleted = 5005;

    // Retention Background Service Events: 6000-6099
    public const int RetentionServiceStarting = 6001;
    public const int RetentionServiceRunning = 6002;
    public const int RetentionServiceCompleted = 6003;
    public const int RetentionServiceFailed = 6004;
    public const int RetentionServiceStopping = 6005;

    // Certificate Auth Handler Events: 7000-7099
    public const int CertAuthTestModeBypass = 7001;
    public const int CertAuthCertificateExpired = 7002;
    public const int CertAuthChainValidationFailed = 7003;
    public const int CertAuthNodeNotFound = 7004;
    public const int CertAuthRegisteredCertificateExpired = 7005;
    public const int CertAuthSuccess = 7006;

    // Personal Access Token Handler Events: 7100-7199
    public const int PatHandlerInvalidToken = 7101;
    public const int PatHandlerSuccess = 7102;

    // Personal Access Token Service Events: 7200-7299
    public const int PatCreated = 7201;
    public const int PatValidationFailed = 7202;
    public const int PatRevoked = 7203;

    // Resource Authorization Service Events: 7300-7399
    public const int PermissionGrantedConfiguration = 7301;
    public const int PermissionRevokedConfiguration = 7302;
    public const int PermissionGrantedCompositeConfiguration = 7303;
    public const int PermissionRevokedCompositeConfiguration = 7304;
    public const int PermissionGrantedParameter = 7305;
    public const int PermissionRevokedParameter = 7306;

    // Group Claims Transformation Events: 7400-7499
    public const int ClaimsTransformed = 7401;

    // Password Change Enforcement Middleware Events: 7500-7599
    public const int PasswordChangeRequired = 7501;
    public const int PasswordChangeRequiredRedirect = 7502;

    // Parameter Merge Service Events: 8000-8099
    public const int MergingParameters = 8001;
    public const int NoParameterSourcesFound = 8002;
    public const int ParameterMergeComplete = 8003;

    // Parameter Schema Service Events: 8200-8299
    public const int SchemaChangesDetected = 8201;
    public const int BreakingSchemaChangesDetected = 8202;

    // Parameter Validator Events: 8300-8399
    public const int ParameterValidationFailed = 8301;

    // Parameter Compatibility Service Events: 8400-8499
    public const int CompatibilityBreakingChangesDetected = 8401;
    public const int SchemaComparisonComplete = 8402;

    // OIDC User Provisioning Events: 9000-9099
    public const int OidcUserProvisioned = 9001;
}
