if ($IsWindows)
{
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Windows Password Policy Resource' -Tag 'Windows' -Skip:(!$IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir)
        {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        # Store original password policy settings to restore later
        if ($script:isAdmin)
        {
            $script:originalPolicy = dsc resource get -r OpenDsc.Windows/PasswordPolicy --input '{}' | ConvertFrom-Json
        }
    }

    AfterAll {
        # Restore original password policy settings
        if ($script:isAdmin -and $script:originalPolicy)
        {
            $restoreInput = $script:originalPolicy.actualState | ConvertTo-Json -Compress
            dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $restoreInput | Out-Null
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Windows/PasswordPolicy | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Windows/PasswordPolicy'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Windows/PasswordPolicy | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Not -Contain 'delete'
            $result.capabilities | Should -Not -Contain 'export'
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should retrieve current password policy' {
            $inputJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/PasswordPolicy --input $inputJson | ConvertFrom-Json
            $result.actualState | Should -Not -BeNullOrEmpty
            $result.actualState.minimumPasswordLength | Should -BeGreaterThan -1
            $result.actualState.maximumPasswordAgeDays | Should -BeGreaterThan -1
            $result.actualState.minimumPasswordAgeDays | Should -BeGreaterThan -1
            $result.actualState.passwordHistoryLength | Should -BeGreaterThan -1
        }

        It 'should work without admin rights for Get' -Skip:($script:isAdmin) {
            $inputJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress

            { dsc resource get -r OpenDsc.Windows/PasswordPolicy --input $inputJson | Out-Null } | Should -Not -Throw
        }
    }

    Context 'Set Operation - Minimum Password Length' -Skip:(!$script:isAdmin) {
        It 'should set minimum password length to 8' {
            $inputJson = @{
                minimumPasswordLength = 8
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify with Get
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/PasswordPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.minimumPasswordLength | Should -Be 8
        }

        It 'should set minimum password length to 0 (no minimum)' {
            $inputJson = @{
                minimumPasswordLength = 0
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/PasswordPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.minimumPasswordLength | Should -Be 0
        }
    }

    Context 'Set Operation - Password Age' -Skip:(!$script:isAdmin) {
        It 'should set maximum password age to 90 days' {
            $inputJson = @{
                maximumPasswordAgeDays = 90
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/PasswordPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.maximumPasswordAgeDays | Should -Be 90
        }

        It 'should set maximum password age to 0 (never expires)' {
            $inputJson = @{
                maximumPasswordAgeDays = 0
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/PasswordPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.maximumPasswordAgeDays | Should -Be 0
        }

        It 'should set minimum password age to 1 day' {
            $inputJson = @{
                minimumPasswordAgeDays = 1
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/PasswordPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.minimumPasswordAgeDays | Should -Be 1
        }
    }

    Context 'Set Operation - Password History' -Skip:(!$script:isAdmin) {
        It 'should set password history length to 24' {
            $inputJson = @{
                passwordHistoryLength = 24
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/PasswordPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.passwordHistoryLength | Should -Be 24
        }

        It 'should set password history length to 0 (no history)' {
            $inputJson = @{
                passwordHistoryLength = 0
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/PasswordPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.passwordHistoryLength | Should -Be 0
        }
    }

    Context 'Set Operation - Multiple Properties' -Skip:(!$script:isAdmin) {
        It 'should set multiple password policy properties at once' {
            $inputJson = @{
                minimumPasswordLength  = 12
                maximumPasswordAgeDays = 60
                minimumPasswordAgeDays = 2
                passwordHistoryLength  = 10
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify all properties
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/PasswordPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.minimumPasswordLength | Should -Be 12
            $getResult.actualState.maximumPasswordAgeDays | Should -Be 60
            $getResult.actualState.minimumPasswordAgeDays | Should -Be 2
            $getResult.actualState.passwordHistoryLength | Should -Be 10
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should reject minimum password length > 14' {
            $invalidInput = @{
                minimumPasswordLength = 15
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should reject password history length > 24' {
            $invalidInput = @{
                passwordHistoryLength = 25
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Non-Elevated Access' {
        It 'should fail to set password policy without admin rights' -Skip:($script:isAdmin) {
            $inputJson = @{
                minimumPasswordLength = 8
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }
}
