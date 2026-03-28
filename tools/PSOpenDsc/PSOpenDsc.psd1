@{
    RootModule        = 'PSOpenDsc.psm1'
    ModuleVersion     = '0.1.0'
    GUID              = 'a3e4b6c8-1d2f-4e5a-9b0c-7d8e6f1a2b3c'
    Author            = 'Thomas Nieto'
    Copyright         = '(c) 2026 Thomas Nieto. All rights reserved.'
    Description       = 'PowerShell SDK for the OpenDsc Pull Server REST API.'
    PowerShellVersion = '7.0'

    FunctionsToExport = @(
        # Connection & Auth
        'Connect-OpenDsc'
        'Disconnect-OpenDsc'
        'Get-OpenDscCurrentUser'
        'Set-OpenDscPassword'
        'New-OpenDscToken'
        'Get-OpenDscToken'
        'Remove-OpenDscToken'

        # Nodes
        'Get-OpenDscNode'
        'Register-OpenDscNode'
        'Remove-OpenDscNode'
        'Get-OpenDscNodeConfiguration'
        'Set-OpenDscNodeConfiguration'
        'Remove-OpenDscNodeConfiguration'
        'Get-OpenDscNodeConfigurationChecksum'
        'Save-OpenDscNodeConfigurationBundle'
        'Set-OpenDscNodeLcmConfig'
        'Get-OpenDscNodeLcmConfig'
        'Set-OpenDscNodeLcmStatus'
        'Set-OpenDscNodeReportedConfig'
        'Get-OpenDscNodeStatusHistory'
        'Get-OpenDscNodeTag'
        'Add-OpenDscNodeTag'
        'Remove-OpenDscNodeTag'
        'Get-OpenDscNodeParameterProvenance'
        'Get-OpenDscNodeParameterResolution'

        # Configurations
        'Get-OpenDscConfiguration'
        'New-OpenDscConfiguration'
        'Set-OpenDscConfiguration'
        'Remove-OpenDscConfiguration'
        'Get-OpenDscConfigurationVersion'
        'New-OpenDscConfigurationVersion'
        'Publish-OpenDscConfigurationVersion'
        'Remove-OpenDscConfigurationVersion'
        'Get-OpenDscConfigurationPermission'
        'Set-OpenDscConfigurationPermission'
        'Remove-OpenDscConfigurationPermission'

        # Composite Configurations
        'Get-OpenDscCompositeConfiguration'
        'New-OpenDscCompositeConfiguration'
        'Remove-OpenDscCompositeConfiguration'
        'Get-OpenDscCompositeConfigurationVersion'
        'New-OpenDscCompositeConfigurationVersion'
        'Publish-OpenDscCompositeConfigurationVersion'
        'Remove-OpenDscCompositeConfigurationVersion'
        'Add-OpenDscCompositeConfigurationChild'
        'Set-OpenDscCompositeConfigurationChild'
        'Remove-OpenDscCompositeConfigurationChild'
        'Get-OpenDscCompositeConfigurationPermission'
        'Set-OpenDscCompositeConfigurationPermission'
        'Remove-OpenDscCompositeConfigurationPermission'

        # Parameters
        'Set-OpenDscParameter'
        'Get-OpenDscParameterVersion'
        'Publish-OpenDscParameterVersion'
        'Remove-OpenDscParameterVersion'
        'Get-OpenDscParameterMajorVersion'

        # Scope Types
        'Get-OpenDscScopeType'
        'New-OpenDscScopeType'
        'Set-OpenDscScopeType'
        'Remove-OpenDscScopeType'
        'Set-OpenDscScopeTypeOrder'
        'Enable-OpenDscScopeType'
        'Disable-OpenDscScopeType'

        # Scope Values
        'Get-OpenDscScopeValue'
        'New-OpenDscScopeValue'
        'Set-OpenDscScopeValue'
        'Remove-OpenDscScopeValue'

        # Reports
        'Get-OpenDscReport'
        'Get-OpenDscNodeReport'

        # Users
        'Get-OpenDscUser'
        'New-OpenDscUser'
        'Set-OpenDscUser'
        'Remove-OpenDscUser'
        'Reset-OpenDscUserPassword'
        'Unlock-OpenDscUser'
        'Set-OpenDscUserRole'

        # Groups
        'Get-OpenDscGroup'
        'New-OpenDscGroup'
        'Set-OpenDscGroup'
        'Remove-OpenDscGroup'
        'Get-OpenDscGroupMember'
        'Set-OpenDscGroupMember'
        'Set-OpenDscGroupRole'

        # Roles
        'Get-OpenDscRole'
        'New-OpenDscRole'
        'Set-OpenDscRole'
        'Remove-OpenDscRole'

        # Settings
        'Get-OpenDscSetting'
        'Get-OpenDscPublicSetting'
        'Set-OpenDscSetting'
        'Get-OpenDscLcmDefault'
        'Set-OpenDscLcmDefault'
        'Get-OpenDscValidationSetting'
        'Set-OpenDscValidationSetting'
        'Get-OpenDscRetentionSetting'
        'Set-OpenDscRetentionSetting'

        # Registration Keys
        'New-OpenDscRegistrationKey'
        'Get-OpenDscRegistrationKey'
        'Set-OpenDscRegistrationKey'
        'Remove-OpenDscRegistrationKey'

        # Configuration Settings
        'Get-OpenDscConfigurationSetting'
        'Set-OpenDscConfigurationSetting'
        'Remove-OpenDscConfigurationSetting'
        'Get-OpenDscConfigurationRetention'
        'Set-OpenDscConfigurationRetention'
        'Remove-OpenDscConfigurationRetention'

        # Retention
        'Invoke-OpenDscConfigurationCleanup'
        'Invoke-OpenDscParameterCleanup'
        'Invoke-OpenDscCompositeConfigurationCleanup'
        'Invoke-OpenDscReportCleanup'
        'Invoke-OpenDscStatusEventCleanup'
        'Get-OpenDscRetentionRun'

        # Health
        'Test-OpenDscHealth'
        'Test-OpenDscReady'
    )

    CmdletsToExport   = @()
    VariablesToExport = @()
    AliasesToExport   = @()

    PrivateData       = @{
        PSData = @{
            Tags       = @('OpenDsc', 'DSC', 'Configuration', 'PullServer', 'REST', 'API')
            LicenseUri = 'https://github.com/opendsc/opendsc/blob/main/LICENSE'
            ProjectUri = 'https://github.com/opendsc/opendsc'
        }
    }
}
