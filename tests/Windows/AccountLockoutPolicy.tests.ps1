if ($IsWindows)
{
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Windows Account Lockout Policy Resource' -Tag 'Windows' -Skip:(!$IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir)
        {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        # Store original lockout policy settings to restore later
        if ($script:isAdmin)
        {
            $script:originalPolicy = dsc resource get -r OpenDsc.Windows/AccountLockoutPolicy --input '{}' | ConvertFrom-Json
        }
    }

    AfterAll {
        # Restore original account lockout policy settings
        if ($script:isAdmin -and $script:originalPolicy)
        {
            $restoreInput = $script:originalPolicy.actualState | ConvertTo-Json -Compress
            dsc resource set -r OpenDsc.Windows/AccountLockoutPolicy --input $restoreInput | Out-Null
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Windows/AccountLockoutPolicy | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Windows/AccountLockoutPolicy'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Windows/AccountLockoutPolicy | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Not -Contain 'delete'
            $result.capabilities | Should -Not -Contain 'export'
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should retrieve current account lockout policy' {
            $inputJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/AccountLockoutPolicy --input $inputJson | ConvertFrom-Json
            $result.actualState | Should -Not -BeNullOrEmpty
            $result.actualState.lockoutThreshold | Should -BeGreaterThan -1
            $result.actualState.lockoutDurationMinutes | Should -BeGreaterThan -1
            $result.actualState.lockoutObservationWindowMinutes | Should -BeGreaterThan -1
        }

        It 'should work without admin rights for Get' -Skip:($script:isAdmin) {
            $inputJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress

            { dsc resource get -r OpenDsc.Windows/AccountLockoutPolicy --input $inputJson | Out-Null } | Should -Not -Throw
        }
    }

    Context 'Set Operation - Lockout Threshold' -Skip:(!$script:isAdmin) {
        It 'should set lockout threshold to 5' {
            $inputJson = @{
                lockoutThreshold                = 5
                lockoutDurationMinutes          = 30
                lockoutObservationWindowMinutes = 30
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AccountLockoutPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify with Get
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/AccountLockoutPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.lockoutThreshold | Should -Be 5
        }

        It 'should set lockout threshold to 0 (never lock out)' {
            $inputJson = @{
                lockoutThreshold = 0
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AccountLockoutPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/AccountLockoutPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.lockoutThreshold | Should -Be 0
        }
    }

    Context 'Set Operation - Lockout Duration' -Skip:(!$script:isAdmin) {
        It 'should set lockout duration to 30 minutes' {
            # First set threshold to non-zero
            $setupJson = @{
                lockoutThreshold = 5
            } | ConvertTo-Json -Compress
            dsc resource set -r OpenDsc.Windows/AccountLockoutPolicy --input $setupJson | Out-Null

            $inputJson = @{
                lockoutDurationMinutes          = 30
                lockoutObservationWindowMinutes = 30
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AccountLockoutPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/AccountLockoutPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.lockoutDurationMinutes | Should -Be 30
        }
    }

    Context 'Set Operation - Lockout Observation Window' -Skip:(!$script:isAdmin) {
        It 'should set observation window to 15 minutes (with matching duration)' {
            # Windows API requires observation window to be set together with duration
            # and observation window must be <= duration
            $inputJson = @{
                lockoutThreshold                = 5
                lockoutDurationMinutes          = 15
                lockoutObservationWindowMinutes = 15
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AccountLockoutPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/AccountLockoutPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.lockoutObservationWindowMinutes | Should -Be 15
            $getResult.actualState.lockoutDurationMinutes | Should -Be 15
        }
    }

    Context 'Set Operation - Multiple Properties' -Skip:(!$script:isAdmin) {
        It 'should set all lockout policy properties at once' {
            $inputJson = @{
                lockoutThreshold                = 10
                lockoutDurationMinutes          = 60
                lockoutObservationWindowMinutes = 60
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AccountLockoutPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify all properties
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/AccountLockoutPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.lockoutThreshold | Should -Be 10
            $getResult.actualState.lockoutDurationMinutes | Should -Be 60
            $getResult.actualState.lockoutObservationWindowMinutes | Should -Be 60
        }
    }

    Context 'Schema Validation' -Tag 'Schema' {
        It 'should reject lockout threshold > 999' {
            $invalidInput = @{
                lockoutThreshold = 1000
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AccountLockoutPolicy --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should reject observation window > duration when threshold is set' -Skip:(!$script:isAdmin) {
            $invalidInput = @{
                lockoutThreshold                = 5
                lockoutDurationMinutes          = 30
                lockoutObservationWindowMinutes = 60
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AccountLockoutPolicy --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Non-Elevated Access' {
        It 'should fail to set lockout policy without admin rights' -Skip:($script:isAdmin) {
            $inputJson = @{
                lockoutThreshold = 5
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AccountLockoutPolicy --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }
    }

    Context 'Recommended Security Settings' -Skip:(!$script:isAdmin) {
        It 'should support Microsoft recommended lockout policy' {
            # Microsoft Security Baseline: 10 failed attempts, 10 minute lockout
            $inputJson = @{
                lockoutThreshold                = 10
                lockoutDurationMinutes          = 10
                lockoutObservationWindowMinutes = 10
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/AccountLockoutPolicy --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0

            # Verify
            $verifyJson = '{}' | ConvertFrom-Json | ConvertTo-Json -Compress
            $getResult = dsc resource get -r OpenDsc.Windows/AccountLockoutPolicy --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.lockoutThreshold | Should -Be 10
            $getResult.actualState.lockoutDurationMinutes | Should -Be 10
            $getResult.actualState.lockoutObservationWindowMinutes | Should -Be 10
        }
    }
}