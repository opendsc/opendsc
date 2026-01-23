// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource.Windows.UserRight;

// This enum defines Windows user rights constants that can be assigned to principals
// via Local Security Authority (LSA) Policy. Each constant follows the Windows SDK
// naming convention (Se*Privilege or Se*Right).
public enum UserRight
{
    SeNetworkLogonRight,
    SeBatchLogonRight,
    SeServiceLogonRight,
    SeInteractiveLogonRight,
    SeRemoteInteractiveLogonRight,
    SeDenyNetworkLogonRight,
    SeDenyBatchLogonRight,
    SeDenyServiceLogonRight,
    SeDenyInteractiveLogonRight,
    SeDenyRemoteInteractiveLogonRight,
    SeTrustedCredManAccessPrivilege,
    SeCreateTokenPrivilege,
    SeAssignPrimaryTokenPrivilege,
    SeLockMemoryPrivilege,
    SeIncreaseQuotaPrivilege,
    SeMachineAccountPrivilege,
    SeTcbPrivilege,
    SeSecurityPrivilege,
    SeTakeOwnershipPrivilege,
    SeLoadDriverPrivilege,
    SeSystemProfilePrivilege,
    SeSystemtimePrivilege,
    SeProfileSingleProcessPrivilege,
    SeIncreaseBasePriorityPrivilege,
    SeCreatePagefilePrivilege,
    SeCreatePermanentPrivilege,
    SeBackupPrivilege,
    SeRestorePrivilege,
    SeShutdownPrivilege,
    SeDebugPrivilege,
    SeAuditPrivilege,
    SeSystemEnvironmentPrivilege,
    SeChangeNotifyPrivilege,
    SeRemoteShutdownPrivilege,
    SeUndockPrivilege,
    SeSyncAgentPrivilege,
    SeEnableDelegationPrivilege,
    SeManageVolumePrivilege,
    SeImpersonatePrivilege,
    SeCreateGlobalPrivilege,
    SeIncreaseWorkingSetPrivilege,
    SeTimeZonePrivilege,
    SeCreateSymbolicLinkPrivilege,
    SeDelegateSessionUserImpersonatePrivilege,
    SeRelabelPrivilege
}
