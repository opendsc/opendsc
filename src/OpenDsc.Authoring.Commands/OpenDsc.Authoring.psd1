@{
    ModuleVersion        = '0.5.1'
    GUID                 = 'b3d5a9f0-c7e2-4d1b-8f6a-2e9c5d7b1a3f'
    Author               = 'Thomas Nieto'
    Copyright            = '(c) Thomas Nieto. All rights reserved.'
    Description          = 'PowerShell cmdlets for Microsoft DSC authoring.'
    PowerShellVersion    = '7.6'
    CompatiblePSEditions = @('Core')
    RootModule           = 'OpenDsc.Authoring.Commands.dll'
    FormatsToProcess     = @('OpenDsc.Authoring.format.ps1xml')
    FunctionsToExport    = @()
    CmdletsToExport      = @(
        'ConvertFrom-DscConfigurationMof',
        'ConvertFrom-DscSchemaMof',
        'New-DscAdaptedResourceManifest',
        'New-DscResourceManifest',
        'New-DscPropertyOverride',
        'Update-DscAdaptedResourceManifest',
        'Import-DscAdaptedResourceManifest',
        'Import-DscResourceManifest'
    )
    VariablesToExport    = @()
    AliasesToExport      = @()
    PrivateData          = @{
        PSData = @{
            Tags       = @('DSC', 'MOF', 'DSCv3', 'Manifest')
            ProjectUri = 'https://opendsc.dev'
            LicenseUri = 'https://github.com/opendsc/opendsc/blob/main/LICENSE'
        }
    }
}
