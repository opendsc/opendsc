// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Lcm;

internal static class EventIds
{
    // Lifecycle Events: 1000-1099
    public const int ServiceStarting = 1000;
    public const int ServiceStopping = 1001;
    public const int CriticalError = 1002;
    public const int UnknownMode = 1003;

    // Configuration Events: 1100-1199
    public const int ConfigurationReloaded = 1100;
    public const int ConfigurationValidationError = 1101;
    public const int ConfigurationNotAvailable = 1102;
    public const int ConfigurationChangedAfterTest = 1103;
    public const int ConfigurationIntervalChanged = 1104;
    public const int ModeChangeDetected = 1105;
    public const int ModeSwitched = 1106;

    // Monitor Mode Events: 2000-2099
    public const int MonitorModeStarting = 2000;
    public const int MonitorCycleError = 2001;

    // Remediate Mode Events: 2100-2199
    public const int RemediateModeStarting = 2100;
    public const int ApplyingCorrections = 2101;
    public const int RemediateCycleError = 2102;
    public const int AllResourcesInDesiredState = 2103;

    // DSC Execution Events: 3000-3099
    public const int DscCommandExecuting = 3000;
    public const int DscCommandCompleted = 3001;
    public const int DscTestStarting = 3002;
    public const int DscSetStarting = 3003;
    public const int DscTestCompleted = 3010;
    public const int DscTestCompletedWithIssues = 3011;
    public const int DscSetCompleted = 3012;
    public const int DscSetCompletedWithIssues = 3013;
    public const int DscParseError = 3020;
    public const int DscMalformedJson = 3021;
    public const int DscErrorMessage = 3030;
    public const int DscWarningMessage = 3031;
    public const int DscInfoMessage = 3032;
    public const int DscDebugMessage = 3033;
    public const int DscTraceMessage = 3034;

    // Resource Status Events: 4000-4099
    public const int ResourceStatus = 4000;
    public const int ResourceNotInDesiredState = 4001;
    public const int SetOperationStatus = 4002;

    // Restart Requirement Events: 5000-5099
    public const int SystemRestartRequired = 5000;
    public const int ServiceRestartRequired = 5001;
    public const int ProcessRestartRequired = 5002;

    // Pull Server Events: 6000-6099
    public const int PullServerNotConfigured = 6000;
    public const int ConfigurationDownloadedFromServer = 6001;
    public const int ApiKeyRotated = 6002;
    public const int NodeIdPersisted = 6003;
    public const int FailedToPersistNodeId = 6004;
    public const int CertificateRotatedOnPullServer = 6005;
    public const int ConfigurationEntryPointNotFound = 6006;
    public const int ConfigurationExtractedFromBundle = 6007;

    // Certificate Management Events: 7000-7099
    public const int CertificateLoaded = 7000;
    public const int CertificateExpiringSoon = 7001;
    public const int FailedToLoadCertificate = 7002;
    public const int CertificateGenerated = 7003;
    public const int CertificateThumbprintNotConfigured = 7004;
    public const int CertificateNotFoundInStore = 7005;
    public const int CertificateLoadedFromStore = 7006;
    public const int CertificateRotated = 7007;
    public const int CertificateRotationNotSupported = 7008;
    public const int CertificatePathNotConfigured = 7009;
}
