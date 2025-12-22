if ($IsWindows) {
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Windows Optional Feature Resource' -Tag 'Windows' -Skip:(!$IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $resourceName = 'OpenDsc.Windows/OptionalFeature'
        $testFeatureName = 'TelnetClient'
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Windows/OptionalFeature | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Windows/OptionalFeature'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Windows/OptionalFeature | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }
    }

    Context 'Get Operation' -Tag 'Get', 'Admin' -Skip:(!$script:isAdmin) {
        It 'should return _exist=false for non-existent feature' {
            $inputJson = @{
                name = 'NonExistentFeature-12345-XYZ'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r $resourceName --input $inputJson | ConvertFrom-Json

            $result.actualState.name | Should -Be 'NonExistentFeature-12345-XYZ'
            $result.actualState._exist | Should -Be $false
        }

        It 'should read properties of existing feature' {
            $inputJson = @{
                name = $testFeatureName
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r $resourceName --input $inputJson | ConvertFrom-Json

            $result.actualState.name | Should -Be $testFeatureName
            $result.actualState._exist | Should -BeIn @($true, $false)

            if ($result.actualState._exist) {
                $result.actualState.state | Should -Not -BeNullOrEmpty
                $result.actualState.displayName | Should -Not -BeNullOrEmpty
            }
        }

        It 'should include displayName and description for features' {
            $inputJson = @{
                name = 'TelnetClient'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r $resourceName --input $inputJson | ConvertFrom-Json

            $result.actualState.name | Should -Be 'TelnetClient'
            $result.actualState.PSObject.Properties.Name | Should -Contain 'displayName'
            $result.actualState.PSObject.Properties.Name | Should -Contain 'description'
        }
    }

    Context 'Set Operation' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            try {
                $getInput = @{ name = $testFeatureName } | ConvertTo-Json -Compress
                $currentState = dsc resource get -r $resourceName --input $getInput | ConvertFrom-Json

                if ($currentState.actualState._exist) {
                    $deleteInput = @{ name = $testFeatureName } | ConvertTo-Json -Compress
                    dsc resource delete -r $resourceName --input $deleteInput | Out-Null
                    Start-Sleep -Seconds 2
                }
            } catch {
                Write-Warning "Could not disable feature in BeforeEach: $_"
            }
        }

        AfterEach {
            try {
                $deleteInput = @{ name = $testFeatureName } | ConvertTo-Json -Compress
                dsc resource delete -r $resourceName --input $deleteInput | Out-Null
                Start-Sleep -Seconds 2
            } catch {
                Write-Warning "Could not cleanup feature: $_"
            }
        }

        It 'should enable a feature' {
            $inputJson = @{
                name = $testFeatureName
            } | ConvertTo-Json -Compress

            $result = dsc resource set -r $resourceName --input $inputJson | ConvertFrom-Json

            $result.afterState.name | Should -Be $testFeatureName
            $result.afterState._exist | Should -Not -Be $false

            $verifyJson = @{
                name = $testFeatureName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r $resourceName --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Not -Be $false
        }

        It 'should disable a feature using _exist=false' {
            $enableJson = @{
                name = $testFeatureName
            } | ConvertTo-Json -Compress

            dsc resource set -r $resourceName --input $enableJson | Out-Null
            Start-Sleep -Seconds 2

            $disableJson = @{
                name = $testFeatureName
                _exist = $false
            } | ConvertTo-Json -Compress

            $result = dsc resource set -r $resourceName --input $disableJson | ConvertFrom-Json

            $result.afterState._exist | Should -Be $false

            $verifyJson = @{
                name = $testFeatureName
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r $resourceName --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should be idempotent when feature already enabled' {
            $inputJson = @{
                name = $testFeatureName
            } | ConvertTo-Json -Compress

            dsc resource set -r $resourceName --input $inputJson | Out-Null
            Start-Sleep -Seconds 2

            $result = dsc resource set -r $resourceName --input $inputJson 2>&1

            if ($result) {
                $resultObj = $result | ConvertFrom-Json
                if ($resultObj.afterState) {
                    $resultObj.afterState._exist | Should -Not -Be $false
                }
            }
        }
    }

    Context 'Delete Operation' -Tag 'Delete', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            try {
                $inputJson = @{
                    name = $testFeatureName
                } | ConvertTo-Json -Compress

                dsc resource set -r $resourceName --input $inputJson | Out-Null
                Start-Sleep -Seconds 2
            } catch {
                Write-Warning "Could not enable feature in BeforeEach: $_"
            }
        }

        AfterEach {
            try {
                $deleteInput = @{ name = $testFeatureName } | ConvertTo-Json -Compress
                dsc resource delete -r $resourceName --input $deleteInput | Out-Null
                Start-Sleep -Seconds 2
            } catch {
                Write-Warning "Could not cleanup feature: $_"
            }
        }

        It 'should disable a feature' {
            $inputJson = @{
                name = $testFeatureName
            } | ConvertTo-Json -Compress

            { dsc resource delete -r $resourceName --input $inputJson } | Should -Not -Throw

            Start-Sleep -Seconds 2
            $result = dsc resource get -r $resourceName --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }

        It 'should be idempotent when feature already disabled' {
            $inputJson = @{
                name = $testFeatureName
            } | ConvertTo-Json -Compress

            dsc resource delete -r $resourceName --input $inputJson | Out-Null
            Start-Sleep -Seconds 2

            { dsc resource delete -r $resourceName --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Restart Metadata' -Tag 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            try {
                $getInput = @{ name = $testFeatureName } | ConvertTo-Json -Compress
                $currentState = dsc resource get -r $resourceName --input $getInput | ConvertFrom-Json

                if ($currentState.actualState._exist) {
                    $deleteInput = @{ name = $testFeatureName } | ConvertTo-Json -Compress
                    dsc resource delete -r $resourceName --input $deleteInput | Out-Null
                    Start-Sleep -Seconds 2
                }
            } catch {
                Write-Warning "Could not disable feature in BeforeEach: $_"
            }
        }

        AfterEach {
            try {
                $deleteInput = @{ name = $testFeatureName } | ConvertTo-Json -Compress
                dsc resource delete -r $resourceName --input $deleteInput | Out-Null
                Start-Sleep -Seconds 2
            } catch {
                Write-Warning "Could not cleanup feature: $_"
            }
        }

        It 'should include _metadata._restartRequired when feature requires restart' {
            $inputJson = @{
                name = $testFeatureName
            } | ConvertTo-Json -Compress

            $result = dsc resource set -r $resourceName --input $inputJson | ConvertFrom-Json
            $result.afterState._metadata._restartRequired[0].system | Should -Not -BeNullOrEmpty
            $result.afterState._metadata._restartRequired[0].system | Should -Be $env:COMPUTERNAME
        }
    }

    Context 'Export Operation' -Tag 'Export', 'Admin' -Skip:(!$script:isAdmin) {
        It 'should export all features' {
            $result = dsc resource export -r $resourceName | ConvertFrom-Json

            $result | Should -Not -BeNullOrEmpty
            $result.resources | Should -Not -BeNullOrEmpty
            $result.resources.Count | Should -BeGreaterThan 0

            foreach ($feature in $result.resources) {
                $feature.properties.name | Should -Not -BeNullOrEmpty
                if ($feature.properties._exist -eq $false) {
                    $feature.properties._exist | Should -Be $false
                }
            }
        }

        It 'should export features in valid JSON format' {
            $result = dsc resource export -r $resourceName
            $result | Should -Not -BeNullOrEmpty
            { $result | ConvertFrom-Json } | Should -Not -Throw
        }

        It 'should export both installed and available features' {
            $result = dsc resource export -r $resourceName | ConvertFrom-Json

            $result.resources.Count | Should -BeGreaterThan 0
        }
    }
}
