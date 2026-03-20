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
        }
    }

    Context 'Get Operation' -Tag 'Get' -Skip:(!$script:isAdmin) {
        It 'should read current audit policy setting' {
            $inputJson = @{
                subcategory = 'File System'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $inputJson | ConvertFrom-Json
            $result.actualState.subcategory | Should -Be 'File System'
            # Setting should be enum string values
            $result.actualState.setting | Should -BeIn @('None', 'Success', 'Failure', 'SuccessAndFailure')
        }

        It 'should handle different subcategory names' {
            $inputJson = @{
                subcategory = 'Logon'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $inputJson | ConvertFrom-Json
            $result.actualState.subcategory | Should -Be 'Logon'
            $result.actualState.setting | Should -BeIn @('None', 'Success', 'Failure', 'SuccessAndFailure')
        }
    }

    Context 'Set Operation' -Tag 'Set' -Skip:(!$script:isAdmin) {
        BeforeEach {
            # Save original setting
            $inputJson = @{
                subcategory = 'File System'
            } | ConvertTo-Json -Compress
            $script:originalSetting = (dsc resource get -r OpenDsc.Windows/AuditPolicy --input $inputJson | ConvertFrom-Json).actualState.setting

            # Set to a known state (None) before each test
            $resetJson = @{
                subcategory = 'File System'
                setting     = 'None'
            } | ConvertTo-Json -Compress
            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $resetJson | Out-Null
        }

        AfterEach {
            if ($null -ne $script:originalSetting)
            {
                $restoreJson = @{
                    subcategory = 'File System'
                    setting     = $script:originalSetting
                } | ConvertTo-Json -Compress
                dsc resource set -r OpenDsc.Windows/AuditPolicy --input $restoreJson | Out-Null
            }
        }

        It 'should set audit policy to Success' {
            $inputJson = @{
                subcategory = 'File System'
                setting     = 'Success'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                subcategory = 'File System'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.setting | Should -Be 'Success'
        }

        It 'should set audit policy to Failure' {
            $inputJson = @{
                subcategory = 'File System'
                setting     = 'Failure'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                subcategory = 'File System'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.setting | Should -Be 'Failure'
        }

        It 'should set audit policy to Success and Failure' {
            $inputJson = @{
                subcategory = 'File System'
                setting     = 'SuccessAndFailure'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                subcategory = 'File System'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.setting | Should -Be 'SuccessAndFailure'
        }

        It 'should disable audit policy (None)' {
            $inputJson = @{
                subcategory = 'File System'
                setting     = 'None'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            $verifyJson = @{
                subcategory = 'File System'
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/AuditPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.setting | Should -Be 'None'
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should reject invalid subcategory name' {
            $invalidInput = @{
                subcategory = 'Invalid Subcategory Name'
                setting     = 'Success'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should require subcategory' {
            $invalidInput = @{
                setting = 'Success'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AuditPolicy --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept valid subcategory name' {
            $validInput = @{
                subcategory = 'File System'
                setting     = 'Success'
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
                subcategory = 'File System'
            } | ConvertTo-Json -Compress

            dsc resource get -r OpenDsc.Windows/AuditPolicy --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }
}
