if ($IsWindows) {
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Windows Service Resource' -Tag 'Windows' -Skip:(!$IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Windows/Service | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Windows/Service'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Windows/Service | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }

        Context 'Get Operation' -Tag 'Get' {
            It 'should return _exist=false for non-existent service' {
                $inputJson = @{
                    name = 'NonExistentService_12345'
                } | ConvertTo-Json -Compress

                $result = dsc resource get -r OpenDsc.Windows/Service --input $inputJson | ConvertFrom-Json
                $result.actualState._exist | Should -Be $false
                $result.actualState.name | Should -Be 'NonExistentService_12345'
            }

            It 'should read properties of existing service' {
                $inputJson = @{
                    name = 'wuauserv'
                } | ConvertTo-Json -Compress

                $result = dsc resource get -r OpenDsc.Windows/Service --input $inputJson | ConvertFrom-Json
                $result.actualState.name | Should -Be 'wuauserv'
                $result.actualState.displayName | Should -Not -BeNullOrEmpty
                $result.actualState.status | Should -Not -BeNullOrEmpty
                $result.actualState.startType | Should -Not -BeNullOrEmpty
            }

            It 'should retrieve service with dependencies' {
                $serviceWithDeps = Get-Service | Where-Object { $_.ServicesDependedOn.Count -gt 0 } | Select-Object -First 1

                if ($serviceWithDeps) {
                    $inputJson = @{
                        name = $serviceWithDeps.Name
                    } | ConvertTo-Json -Compress

                    $result = dsc resource get -r OpenDsc.Windows/Service --input $inputJson | ConvertFrom-Json
                    $result.actualState.dependencies | Should -Not -BeNullOrEmpty
                }
            }
        }

        Context 'Export Operation' -Tag 'Export' {
            It 'should export all services' {
                $result = dsc resource export -r OpenDsc.Windows/Service | ConvertFrom-Json

                $result | Should -Not -BeNullOrEmpty
                $result.resources | Should -Not -BeNullOrEmpty
                $result.resources.Count | Should -BeGreaterThan 0

                $firstService = $result.resources[0].properties
                $firstService.name | Should -Not -BeNullOrEmpty
                $firstService.status | Should -Not -BeNullOrEmpty
                $firstService.startType | Should -Not -BeNullOrEmpty
            }

            It 'should export known system services' {
                $result = dsc resource export -r OpenDsc.Windows/Service | ConvertFrom-Json

                $wuauserv = $result.resources | Where-Object { $_.properties.name -eq 'wuauserv' }
                $wuauserv | Should -Not -BeNullOrEmpty
                $wuauserv.properties.displayName | Should -Not -BeNullOrEmpty
            }
        }

        Context 'Schema Validation' -Tag 'Schema' {
            It 'should validate service name pattern (no slashes)' {
                $invalidInput = @{
                    name      = 'Invalid/Service'
                    path      = 'C:\Windows\System32\svchost.exe'
                    startType = 'Manual'
                } | ConvertTo-Json -Compress

                dsc resource set -r OpenDsc.Windows/Service --input $invalidInput 2>&1 | Out-Null
                $LASTEXITCODE | Should -Not -Be 0
            }

            It 'should accept valid service names' {
                $validNames = @('MyService', 'My-Service', 'My_Service', 'MyService123')

                foreach ($name in $validNames) {
                    $inputJson = @{
                        name = $name
                    } | ConvertTo-Json -Compress

                    $result = dsc resource get -r OpenDsc.Windows/Service --input $inputJson | ConvertFrom-Json
                    $result.actualState.name | Should -Be $name
                }
            }
        }

        Context 'Error Handling' {
            It 'should handle invalid JSON gracefully' {
                $invalidJson = '{ invalid json }'

                $invalidJson | dsc resource get -r OpenDsc.Windows/Service 2>&1 | Out-Null
                $LASTEXITCODE | Should -Not -Be 0
            }
        }

        Context 'Set Operation' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
            BeforeAll {
                $script:createdServices = @()
                $testServiceExe = Join-Path $PSScriptRoot '..\..\artifacts\TestService\TestService.exe'
                if (-not (Test-Path $testServiceExe)) {
                    throw 'Test service executable not found. Please run: .\tests\TestService\build.ps1'
                }
                $script:testServicePath = $testServiceExe
            }

            AfterAll {
                foreach ($serviceName in $script:createdServices) {
                    try {
                        $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
                        if ($service) {
                            if ($service.Status -ne 'Stopped') {
                                Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
                            }
                            $deleteInput = @{ name = $serviceName } | ConvertTo-Json -Compress
                            dsc resource delete -r OpenDsc.Windows/Service --input $deleteInput 2>$null
                        }
                    } catch {
                        # Ignore cleanup errors
                    }
                }
            }
            BeforeEach {
                $script:currentTestService = "DscTestService_$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
            }

            AfterEach {
                try {
                    $service = Get-Service -Name $script:currentTestService -ErrorAction SilentlyContinue
                    if ($service) {
                        if ($service.Status -ne 'Stopped') {
                            Stop-Service -Name $script:currentTestService -Force -ErrorAction SilentlyContinue
                        }
                        $deleteInput = @{ name = $script:currentTestService } | ConvertTo-Json -Compress
                        dsc resource delete -r OpenDsc.Windows/Service --input $deleteInput 2>$null
                    }
                } catch {
                    # Ignore cleanup errors
                }
            }

            It 'should create a service with required properties' {
                $inputJson = @{
                    name      = $script:currentTestService
                    path      = $script:testServicePath
                    startType = 'Manual'
                } | ConvertTo-Json -Compress

                $script:createdServices += $script:currentTestService
                $result = dsc resource set -r OpenDsc.Windows/Service --input $inputJson | ConvertFrom-Json

                $result.beforeState._exist | Should -Be $false
                $result.afterState.name | Should -Be $script:currentTestService
                $result.changedProperties | Should -Contain '_exist'

                $service = Get-Service -Name $script:currentTestService -ErrorAction SilentlyContinue
                $service | Should -Not -BeNullOrEmpty
            }

            It 'should set displayName property' {
                $displayName = 'Test Display Name'
                $inputJson = @{
                    name        = $script:currentTestService
                    path        = $script:testServicePath
                    displayName = $displayName
                    startType   = 'Manual'
                } | ConvertTo-Json -Compress

                $script:createdServices += $script:currentTestService
                dsc resource set -r OpenDsc.Windows/Service --input $inputJson | Out-Null

                $getInput = @{ name = $script:currentTestService } | ConvertTo-Json -Compress
                $result = dsc resource get -r OpenDsc.Windows/Service --input $getInput | ConvertFrom-Json
                $result.actualState.displayName | Should -Be $displayName
            }

            It 'should set description property' {
                $description = 'Test service description'
                $inputJson = @{
                    name        = $script:currentTestService
                    path        = $script:testServicePath
                    description = $description
                    startType   = 'Manual'
                } | ConvertTo-Json -Compress

                $script:createdServices += $script:currentTestService
                dsc resource set -r OpenDsc.Windows/Service --input $inputJson | Out-Null

                $getInput = @{ name = $script:currentTestService } | ConvertTo-Json -Compress
                $result = dsc resource get -r OpenDsc.Windows/Service --input $getInput | ConvertFrom-Json
                $result.actualState.description | Should -Be $description
            }

            It 'should set startType to Automatic' {
                $inputJson = @{
                    name      = $script:currentTestService
                    path      = $script:testServicePath
                    startType = 'Automatic'
                } | ConvertTo-Json -Compress

                $script:createdServices += $script:currentTestService
                dsc resource set -r OpenDsc.Windows/Service --input $inputJson | Out-Null

                $getInput = @{ name = $script:currentTestService } | ConvertTo-Json -Compress
                $result = dsc resource get -r OpenDsc.Windows/Service --input $getInput | ConvertFrom-Json
                $result.actualState.startType | Should -Be 'Automatic'
            }

            It 'should set startType to Manual' {
                $inputJson = @{
                    name      = $script:currentTestService
                    path      = $script:testServicePath
                    startType = 'Manual'
                } | ConvertTo-Json -Compress

                $script:createdServices += $script:currentTestService
                dsc resource set -r OpenDsc.Windows/Service --input $inputJson | Out-Null

                $getInput = @{ name = $script:currentTestService } | ConvertTo-Json -Compress
                $result = dsc resource get -r OpenDsc.Windows/Service --input $getInput | ConvertFrom-Json
                $result.actualState.startType | Should -Be 'Manual'
            }

            It 'should set startType to Disabled' {
                $inputJson = @{
                    name      = $script:currentTestService
                    path      = $script:testServicePath
                    startType = 'Disabled'
                } | ConvertTo-Json -Compress

                $script:createdServices += $script:currentTestService
                dsc resource set -r OpenDsc.Windows/Service --input $inputJson | Out-Null

                $getInput = @{ name = $script:currentTestService } | ConvertTo-Json -Compress
                $result = dsc resource get -r OpenDsc.Windows/Service --input $getInput | ConvertFrom-Json
                $result.actualState.startType | Should -Be 'Disabled'
            }

            It 'should update existing service properties' {
                $createInput = @{
                    name        = $script:currentTestService
                    path        = $script:testServicePath
                    displayName = 'Original Name'
                    startType   = 'Manual'
                } | ConvertTo-Json -Compress

                $script:createdServices += $script:currentTestService
                dsc resource set -r OpenDsc.Windows/Service --input $createInput | Out-Null

                $updateInput = @{
                    name        = $script:currentTestService
                    displayName = 'Updated Name'
                    description = 'Updated Description'
                    startType   = 'Automatic'
                } | ConvertTo-Json -Compress

                dsc resource set -r OpenDsc.Windows/Service --input $updateInput | Out-Null

                $getInput = @{ name = $script:currentTestService } | ConvertTo-Json -Compress
                $result = dsc resource get -r OpenDsc.Windows/Service --input $getInput | ConvertFrom-Json
                $result.actualState.displayName | Should -Be 'Updated Name'
                $result.actualState.description | Should -Be 'Updated Description'
                $result.actualState.startType | Should -Be 'Automatic'
            }

            It 'should fail when creating service without path' {
                $inputJson = @{
                    name      = $script:currentTestService
                    startType = 'Manual'
                } | ConvertTo-Json -Compress

                dsc resource set -r OpenDsc.Windows/Service --input $inputJson 2>&1 | Out-Null
                $LASTEXITCODE | Should -Not -Be 0
            }

            It 'should fail when creating service without startType' {
                $inputJson = @{
                    name = $script:currentTestService
                    path = $script:testServicePath
                } | ConvertTo-Json -Compress

                dsc resource set -r OpenDsc.Windows/Service --input $inputJson 2>&1 | Out-Null
                $LASTEXITCODE | Should -Not -Be 0
            }
        }

        Context 'Delete Operation' -Tag 'Delete', 'Admin' -Skip:(!$script:isAdmin) {
            It 'should delete an existing service' {
                $serviceName = "DscTestService_$([Guid]::NewGuid().ToString('N').Substring(0, 8))"

                $createInput = @{
                    name      = $serviceName
                    path      = $script:testServicePath
                    startType = 'Manual'
                } | ConvertTo-Json -Compress

                dsc resource set -r OpenDsc.Windows/Service --input $createInput | Out-Null

                $deleteInput = @{
                    name = $serviceName
                } | ConvertTo-Json -Compress

                dsc resource delete -r OpenDsc.Windows/Service --input $deleteInput

                $getInput = @{ name = $serviceName } | ConvertTo-Json -Compress
                $result = dsc resource get -r OpenDsc.Windows/Service --input $getInput | ConvertFrom-Json
                $result.actualState._exist | Should -Be $false
            }

            It 'should stop running service before deletion' {
                $serviceName = "DscTestService_$([Guid]::NewGuid().ToString('N').Substring(0, 8))"

                $createInput = @{
                    name      = $serviceName
                    path      = $script:testServicePath
                    startType = 'Manual'
                    status    = 'Running'
                } | ConvertTo-Json -Compress

                dsc resource set -r OpenDsc.Windows/Service --input $createInput | Out-Null

                Start-Sleep -Seconds 2

                $deleteInput = @{
                    name = $serviceName
                } | ConvertTo-Json -Compress

                dsc resource delete -r OpenDsc.Windows/Service --input $deleteInput

                $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
                $service | Should -BeNullOrEmpty
            }
        }

        Context 'Service Dependencies' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
            It 'should create service with dependencies' {
                $serviceName = "DscTestService_$([Guid]::NewGuid().ToString('N').Substring(0, 8))"

                $inputJson = @{
                    name         = $serviceName
                    path         = $script:testServicePath
                    startType    = 'Manual'
                    dependencies = @('Tcpip')
                } | ConvertTo-Json -Compress

                $script:createdServices += $serviceName
                dsc resource set -r OpenDsc.Windows/Service --input $inputJson | Out-Null

                $getInput = @{ name = $serviceName } | ConvertTo-Json -Compress
                $result = dsc resource get -r OpenDsc.Windows/Service --input $getInput | ConvertFrom-Json
                $result.actualState.dependencies | Should -Contain 'Tcpip'

                Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
                $deleteInput = @{ name = $serviceName } | ConvertTo-Json -Compress
                dsc resource delete -r OpenDsc.Windows/Service --input $deleteInput
            }

            It 'should update service dependencies' {
                $serviceName = "DscTestService_$([Guid]::NewGuid().ToString('N').Substring(0, 8))"

                $createInput = @{
                    name         = $serviceName
                    path         = $script:testServicePath
                    startType    = 'Manual'
                    dependencies = @('Tcpip')
                } | ConvertTo-Json -Compress

                $script:createdServices += $serviceName
                dsc resource set -r OpenDsc.Windows/Service --input $createInput | Out-Null

                $updateInput = @{
                    name         = $serviceName
                    dependencies = @('Dnscache')
                } | ConvertTo-Json -Compress

                dsc resource set -r OpenDsc.Windows/Service --input $updateInput | Out-Null

                $getInput = @{ name = $serviceName } | ConvertTo-Json -Compress
                $result = dsc resource get -r OpenDsc.Windows/Service --input $getInput | ConvertFrom-Json
                $result.actualState.dependencies | Should -Contain 'Dnscache'
                $result.actualState.dependencies | Should -Not -Contain 'Tcpip'

                Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
                $deleteInput = @{ name = $serviceName } | ConvertTo-Json -Compress
                dsc resource delete -r OpenDsc.Windows/Service --input $deleteInput
            }
        }

        Context 'Service Status Management' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
            It 'should start a service' {
                $serviceName = "DscTestService_$([Guid]::NewGuid().ToString('N').Substring(0, 8))"

                $createInput = @{
                    name      = $serviceName
                    path      = $script:testServicePath
                    startType = 'Manual'
                } | ConvertTo-Json -Compress

                $script:createdServices += $serviceName
                dsc resource set -r OpenDsc.Windows/Service --input $createInput | Out-Null

                $startInput = @{
                    name   = $serviceName
                    status = 'Running'
                } | ConvertTo-Json -Compress

                dsc resource set -r OpenDsc.Windows/Service --input $startInput | Out-Null

                $getInput = @{ name = $serviceName } | ConvertTo-Json -Compress
                $result = dsc resource get -r OpenDsc.Windows/Service --input $getInput | ConvertFrom-Json
                $result.actualState.status | Should -Be 'Running'

                Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
                $deleteInput = @{ name = $serviceName } | ConvertTo-Json -Compress
                dsc resource delete -r OpenDsc.Windows/Service --input $deleteInput
            }

            It 'should stop a service' {
                $serviceName = "DscTestService_$([Guid]::NewGuid().ToString('N').Substring(0, 8))"

                $createInput = @{
                    name      = $serviceName
                    path      = $script:testServicePath
                    startType = 'Manual'
                    status    = 'Running'
                } | ConvertTo-Json -Compress

                $script:createdServices += $serviceName
                dsc resource set -r OpenDsc.Windows/Service --input $createInput | Out-Null

                $stopInput = @{
                    name   = $serviceName
                    status = 'Stopped'
                } | ConvertTo-Json -Compress

                dsc resource set -r OpenDsc.Windows/Service --input $stopInput | Out-Null

                $getInput = @{ name = $serviceName } | ConvertTo-Json -Compress
                $result = dsc resource get -r OpenDsc.Windows/Service --input $getInput | ConvertFrom-Json
                $result.actualState.status | Should -Be 'Stopped'

                $deleteInput = @{ name = $serviceName } | ConvertTo-Json -Compress
                dsc resource delete -r OpenDsc.Windows/Service --input $deleteInput
            }

            It 'should create and start service in one operation' {
                $serviceName = "DscTestService_$([Guid]::NewGuid().ToString('N').Substring(0, 8))"

                $inputJson = @{
                    name      = $serviceName
                    path      = $script:testServicePath
                    startType = 'Manual'
                    status    = 'Running'
                } | ConvertTo-Json -Compress

                $script:createdServices += $serviceName
                dsc resource set -r OpenDsc.Windows/Service --input $inputJson | Out-Null

                $getInput = @{ name = $serviceName } | ConvertTo-Json -Compress
                $result = dsc resource get -r OpenDsc.Windows/Service --input $getInput | ConvertFrom-Json
                $result.actualState.status | Should -Be 'Running'

                Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
                $deleteInput = @{ name = $serviceName } | ConvertTo-Json -Compress
                dsc resource delete -r OpenDsc.Windows/Service --input $deleteInput
            }
        }
    }
}
