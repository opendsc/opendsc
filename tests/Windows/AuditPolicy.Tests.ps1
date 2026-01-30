if ($IsWindows)
{
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Windows Audit Policy Resource' -Tag 'Windows' -Skip:(!$IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir)
        {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        # Common audit subcategory GUIDs for testing
        $script:FileSystemSubcategoryGuid = '0cce921d-69ae-11d9-bed3-505054503030'  # Audit_ObjectAccess_FileSystem
        $script:LogonSubcategoryGuid = '0cce9215-69ae-11d9-bed3-505054503030'       # Audit_Logon_Logon
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Windows/AuditPolicy | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Windows/AuditPolicy'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Windows/AuditPolicy | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
        }
    }

    Context 'Get Operation' -Tag 'Get' -Skip:(!$script:isAdmin) {
        It 'should read current audit policy setting' {
            $inputJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $inputJson | ConvertFrom-Json
            $result.actualState.subcategoryGuid | Should -Be $script:FileSystemSubcategoryGuid
            $result.actualState.setting | Should -BeIn @('None', 'Success', 'Failure', 'SuccessAndFailure')
        }

        It 'should handle different subcategory GUIDs' {
            $inputJson = @{
                subcategoryGuid = $script:LogonSubcategoryGuid
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $inputJson | ConvertFrom-Json
            $result.actualState.subcategoryGuid | Should -Be $script:LogonSubcategoryGuid
            $result.actualState.setting | Should -BeIn @('None', 'Success', 'Failure', 'SuccessAndFailure')
        }
    }

    Context 'Set Operation' -Tag 'Set' -Skip:(!$script:isAdmin) {
        BeforeEach {
            $inputJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
            } | ConvertTo-Json -Compress
            $script:originalSetting = (dsc resource get -r OpenDsc.Windows/AuditPolicy --input $inputJson | ConvertFrom-Json).actualState.setting
        }

        AfterEach {
            if ($null -ne $script:originalSetting)
            {
                $restoreJson = @{
                    subcategoryGuid = $script:FileSystemSubcategoryGuid
                    setting         = $script:originalSetting
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.Windows/AuditPolicy --input $restoreJson | Out-Null
            }
        }

        It 'should set audit policy to Success' {
            $inputJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
                setting         = 'Success'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.setting | Should -Be 'Success'
        }

        It 'should set audit policy to Failure' {
            $inputJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
                setting         = 'Failure'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.setting | Should -Be 'Failure'
        }

        It 'should set audit policy to SuccessAndFailure' {
            $inputJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
                setting         = 'SuccessAndFailure'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.setting | Should -Be 'SuccessAndFailure'
        }

        It 'should set audit policy to None' {
            $inputJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
                setting         = 'None'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.setting | Should -Be 'None'
        }
    }

    Context 'Delete Operation' -Tag 'Delete' -Skip:(!$script:isAdmin) {
        BeforeEach {
            $setJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
                setting         = 'Success'
            } | ConvertTo-Json -Compress
            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $setJson | Out-Null

            $inputJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
            } | ConvertTo-Json -Compress
            $script:originalSetting = (dsc resource get -r OpenDsc.Windows/AuditPolicy --input $inputJson | ConvertFrom-Json).actualState.setting
        }

        AfterEach {
            if ($null -ne $script:originalSetting)
            {
                $restoreJson = @{
                    subcategoryGuid = $script:FileSystemSubcategoryGuid
                    setting         = $script:originalSetting
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.Windows/AuditPolicy --input $restoreJson | Out-Null
            }
        }

        It 'should reset audit policy to None' {
            $inputJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
                _exist          = $false
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.Windows/AuditPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.setting | Should -Be 'None'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should validate subcategoryGuid format' {
            $invalidInput = @{
                subcategoryGuid = 'not-a-guid'
                setting         = 'Success'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should require subcategoryGuid' {
            $invalidInput = @{
                setting = 'Success'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept valid GUID format' {
            $validInput = @{
                subcategoryGuid = '0CCE921D-69AE-11D9-BED3-505054503030'
                setting         = 'Success'
            } | ConvertTo-Json -Compress

            # Should not fail on schema validation (may fail on permissions if not admin)
            $result = dsc resource set -r OpenDsc.Windows/AuditPolicy --input $validInput 2>&1
            if (!$script:isAdmin)
            {
                # If not admin, we expect failure but not due to schema validation
                $result | Should -Not -Match 'schema'
            }
        }
    }

    Context 'Non-Elevated Access' -Tag 'NonElevated' -Skip:($script:isAdmin) {
        It 'should fail gracefully when not running as administrator' {
            $inputJson = @{
                subcategoryGuid = $script:FileSystemSubcategoryGuid
            } | ConvertTo-Json -Compress

            dsc resource get -r OpenDsc.Windows/AuditPolicy --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }
}
