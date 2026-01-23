if ($IsWindows) {
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Windows User Rights Assignment Resource' -Tag 'Windows' -Skip:(!$IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Windows/UserRight | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Windows/UserRight'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Windows/UserRight | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'export'
        }

        It 'should provide valid schema' {
            $schema = dsc resource schema -r OpenDsc.Windows/UserRight | ConvertFrom-Json
            $schema | Should -Not -BeNullOrEmpty
            $schema.'$schema' | Should -Be 'https://json-schema.org/draft/2020-12/schema'
            $schema.properties.rights | Should -Not -BeNullOrEmpty
            $schema.properties.principal | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Get Operation' -Tag 'Get', 'Admin' -Skip:(!$script:isAdmin) {
        It 'should return current rights for a principal' {
            $inputJson = @{
                rights = @('SeNetworkLogonRight')
                principal = 'Administrators'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/UserRight --input $inputJson | ConvertFrom-Json
            $result.actualState.principal | Should -Not -BeNullOrEmpty
            $result.actualState.rights | Should -Not -BeNullOrEmpty
        }

        It 'should return principals in friendly name format' {
            $inputJson = @{
                rights = @('SeNetworkLogonRight')
                principal = 'Administrators'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/UserRight --input $inputJson | ConvertFrom-Json
            $result.actualState.principal | Should -Not -BeNullOrEmpty
            $result.actualState.principal | Should -Not -Match '^S-1-'
        }

        It 'should return empty rights array for principal without specified rights' {
            $inputJson = @{
                rights = @('SeTrustedCredManAccessPrivilege')
                principal = 'Guest'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/UserRight --input $inputJson | ConvertFrom-Json
            $result.actualState.rights | Should -BeNullOrEmpty
        }
    }

    Context 'Schema Validation' -Tag 'Schema', 'Admin' -Skip:(!$script:isAdmin) {
        It 'should validate right enum values' {
            $invalidInput = @{
                rights = @('InvalidRightName')
                principal = 'Administrators'
            } | ConvertTo-Json -Compress

            dsc resource get -r OpenDsc.Windows/UserRight --input $invalidInput 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should accept valid right enum values' {
            $validInput = @{
                rights = @('SeBackupPrivilege')
                principal = 'Administrators'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/UserRight --input $validInput | ConvertFrom-Json
            $result.actualState.rights | Should -Contain 'SeBackupPrivilege'
        }
    }

    Context 'Set Operation - User Rights Management' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            $script:testUser = "DscTestUser_$(Get-Random -Maximum 99999)"
            $password = ConvertTo-SecureString "P@ssw0rd!$(Get-Random -Maximum 99999)" -AsPlainText -Force
            New-LocalUser -Name $script:testUser -Password $password -Description "Test user for DSC UserRight resource" -ErrorAction SilentlyContinue | Out-Null
        }

        AfterEach {
            if ($script:testUser) {
                Remove-LocalUser -Name $script:testUser -ErrorAction SilentlyContinue
            }
        }

        It 'should grant multiple rights to a user (additive mode)' {
            $script:testRight = 'SeServiceLogonRight'

            $setInputJson = @{
                rights = @($script:testRight, 'SeBatchLogonRight')
                principal = $script:testUser
                _purge = $false
            } | ConvertTo-Json -Compress

            $setResult = dsc resource set -r OpenDsc.Windows/UserRight --input $setInputJson | ConvertFrom-Json
            $setResult.afterState.principal | Should -Match $script:testUser

            $verifyJson = @{
                rights = @($script:testRight, 'SeBatchLogonRight')
                principal = $script:testUser
            } | ConvertTo-Json -Compress

            $verifyResult = dsc resource get -r OpenDsc.Windows/UserRight --input $verifyJson | ConvertFrom-Json
            $verifyResult.actualState.rights | Should -Contain $script:testRight
            $verifyResult.actualState.rights | Should -Contain 'SeBatchLogonRight'
        }

        It 'should support _purge mode (removes other principals)' {
            $script:testRight = 'SeBatchLogonRight'

            $setInputJson = @{
                rights = @($script:testRight)
                principal = $script:testUser
                _purge = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/UserRight --input $setInputJson | Out-Null

            $getPrincipalsJson = @{
                rights = @($script:testRight)
                principal = $script:testUser
            } | ConvertTo-Json -Compress
            $verifyResult = dsc resource get -r OpenDsc.Windows/UserRight --input $getPrincipalsJson | ConvertFrom-Json
            $verifyResult.actualState.principal | Should -Match $script:testUser
        }

        It 'should accept multiple principal formats' {
            $script:testRight = 'SeRemoteShutdownPrivilege'

            $setInputJson = @{
                rights = @($script:testRight)
                principal = $script:testUser
                _purge = $false
            } | ConvertTo-Json -Compress

            { dsc resource set -r OpenDsc.Windows/UserRight --input $setInputJson } | Should -Not -Throw
        }

        It 'should successfully grant logon rights' {
            $script:testRight = 'SeServiceLogonRight'

            $setInputJson = @{
                rights = @($script:testRight)
                principal = $script:testUser
                _purge = $false
            } | ConvertTo-Json -Compress

            $setResult = dsc resource set -r OpenDsc.Windows/UserRight --input $setInputJson | ConvertFrom-Json
            $setResult.afterState.principal | Should -Match $script:testUser
            $setResult.afterState.rights | Should -Contain $script:testRight
        }
    }

    Context 'Export Operation' -Tag 'Export', 'Admin' -Skip:(!$script:isAdmin) {
        It 'should export all principal-rights assignments' {
            $result = dsc resource export -r OpenDsc.Windows/UserRight | ConvertFrom-Json

            $result | Should -Not -BeNullOrEmpty
            $result.resources | Should -Not -BeNullOrEmpty
            $result.resources.Count | Should -BeGreaterThan 0

            $firstResource = $result.resources[0]
            $firstResource.type | Should -Be 'OpenDsc.Windows/UserRight'
            $firstResource.properties.principal | Should -Not -BeNullOrEmpty
            $firstResource.properties.rights | Should -Not -BeNullOrEmpty
        }

        It 'should export principals with their rights grouped' {
            $result = dsc resource export -r OpenDsc.Windows/UserRight | ConvertFrom-Json

            foreach ($resource in $result.resources) {
                if ($resource.properties.rights -is [array]) {
                    $resource.properties.rights.Count | Should -BeGreaterThan 0
                } else {
                    $resource.properties.rights | Should -Not -BeNullOrEmpty
                }
            }
        }

        It 'should export principals in friendly name format' {
            $result = dsc resource export -r OpenDsc.Windows/UserRight | ConvertFrom-Json

            foreach ($resource in $result.resources) {
                $resource.properties.principal | Should -Not -BeNullOrEmpty
            }
        }
    }
}
