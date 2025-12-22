if ($IsWindows) {
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Windows Group Resource' -Tag 'Windows' -Skip:(!$IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $script:testGroup1 = "DscTestGroup_$(Get-Random -Maximum 9999)"
        $script:testGroup2 = "DscTestGroup_$(Get-Random -Maximum 9999)"
        $script:testUser1 = "DscTestUser_$(Get-Random -Maximum 9999)"
    }

    AfterAll {
        Get-LocalUser -Name DscTestUser* | Remove-LocalUser
        Get-LocalGroup -Name DscTestGroup* | Remove-LocalGroup
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Windows/Group | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            if ($result -is [array]) { $result = $result[0] }
            $result.type | Should -Be 'OpenDsc.Windows/Group'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Windows/Group | ConvertFrom-Json
            if ($result -is [array]) { $result = $result[0] }
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }

    }

    Context 'Get Operation - Non-Elevated' -Tag 'Get' {
        It 'should return _exist=false for non-existent group' {
            $inputJson = @{
                groupName = 'NonExistGroup99'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/Group --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.groupName | Should -Be 'NonExistGroup99'
        }

        It 'should read properties of built-in Administrators group' {
            $inputJson = @{
                groupName = 'Administrators'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/Group --input $inputJson | ConvertFrom-Json
            $result.actualState.groupName | Should -Be 'Administrators'
            $result.actualState.members | Should -Not -BeNullOrEmpty
        }

        It 'should read properties of built-in Users group' {
            $inputJson = @{
                groupName = 'Users'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/Group --input $inputJson | ConvertFrom-Json
            $result.actualState.groupName | Should -Be 'Users'
        }
    }

    Context 'Set Operation - Create Group' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
        AfterEach {
            try {
                Get-LocalGroup -Name $script:testGroup1 -ErrorAction SilentlyContinue | Remove-LocalGroup
                Get-LocalUser -Name $script:testUser1 -ErrorAction SilentlyContinue | Remove-LocalUser
            }
            catch { }
        }

        It 'should create a new group' {
            $inputJson = @{
                groupName = $script:testGroup1
                description = 'Test Group Description'
            } | ConvertTo-Json -Compress

            $result = dsc resource set -r OpenDsc.Windows/Group --input $inputJson | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty

            # Verify group was created
            $getJson = @{
                groupName = $script:testGroup1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/Group --input $getJson | ConvertFrom-Json
            $getResult.actualState.groupName | Should -Be $script:testGroup1
            $getResult.actualState.description | Should -Be 'Test Group Description'
        }

        It 'should create a group with members' {
            Get-LocalUser -Name $script:testUser1 -ErrorAction SilentlyContinue | Remove-LocalUser

            New-LocalUser -Name $script:testUser1 -Password (ConvertTo-SecureString "P@ssw0rd_$(Get-Random)!" -AsPlainText -Force)

            $inputJson = @{
                groupName = $script:testGroup1
                description = 'Test Group with Members'
                members = @($script:testUser1)
            } | ConvertTo-Json -Compress

            $result = dsc resource set -r OpenDsc.Windows/Group --input $inputJson | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty

            # Verify group has member
            $getJson = @{
                groupName = $script:testGroup1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/Group --input $getJson | ConvertFrom-Json
            $getResult.actualState.members | Should -Not -BeNullOrEmpty
            $getResult.actualState.members | Should -Contain $script:testUser1

            Remove-LocalUser -Name $script:testUser1
        }
    }

    Context 'Set Operation - Update Group' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            New-LocalGroup -Name $script:testGroup1 -Description 'Original Description'
        }

        AfterEach {
            try {
                Get-LocalGroup -Name $script:testGroup1 -ErrorAction SilentlyContinue | Remove-LocalGroup
                Get-LocalUser -Name $script:testUser1 -ErrorAction SilentlyContinue | Remove-LocalUser
            }
            catch { }
        }

        It 'should update group description' {
            $inputJson = @{
                groupName = $script:testGroup1
                description = 'Updated Description'
            } | ConvertTo-Json -Compress

            $result = dsc resource set -r OpenDsc.Windows/Group --input $inputJson | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty

            # Verify description was updated
            $getJson = @{
                groupName = $script:testGroup1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/Group --input $getJson | ConvertFrom-Json
            $getResult.actualState.description | Should -Be 'Updated Description'
        }

        It 'should add members without removing others (_purge=false)' {
            Get-LocalUser -Name $script:testUser1 -ErrorAction SilentlyContinue | Remove-LocalUser

            New-LocalUser -Name $script:testUser1 -Password (ConvertTo-SecureString "P@ssw0rd_$(Get-Random)!" -AsPlainText -Force)

            $inputJson = @{
                groupName = $script:testGroup1
                members = @($script:testUser1)
                _purge = $false
            } | ConvertTo-Json -Compress

            $result = dsc resource set -r OpenDsc.Windows/Group --input $inputJson | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty

            # Verify member was added
            $getJson = @{
                groupName = $script:testGroup1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/Group --input $getJson | ConvertFrom-Json
            $getResult.actualState.members | Should -Contain $script:testUser1

            Remove-LocalUser -Name $script:testUser1
        }

        It 'should enforce exact membership (_purge=true)' {
            Get-LocalUser -Name $script:testUser1 -ErrorAction SilentlyContinue | Remove-LocalUser

            New-LocalUser -Name $script:testUser1 -Password (ConvertTo-SecureString "P@ssw0rd_$(Get-Random)!" -AsPlainText -Force)
            Add-LocalGroupMember -Group $script:testGroup1 -Member $script:testUser1

            $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name.Split('\')[1]

            $inputJson = @{
                groupName = $script:testGroup1
                members = @($currentUser)
                _purge = $true
            } | ConvertTo-Json -Compress

            $result = dsc resource set -r OpenDsc.Windows/Group --input $inputJson | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty

            # Verify user1 was removed and currentUser is present
            $getJson = @{
                groupName = $script:testGroup1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/Group --input $getJson | ConvertFrom-Json
            $getResult.actualState.members | Should -Contain $currentUser
            $getResult.actualState.members | Should -Not -Contain $script:testUser1

            Remove-LocalUser -Name $script:testUser1
        }
    }

    Context 'Delete Operation' -Tag 'Delete', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            New-LocalGroup -Name $script:testGroup1 -Description 'Test Group to Delete'
        }

        AfterEach {
            try {
                Get-LocalGroup -Name $script:testGroup1 -ErrorAction SilentlyContinue | Remove-LocalGroup
            }
            catch { }
        }

        It 'should delete a group' {
            $inputJson = @{
                groupName = $script:testGroup1
                _exist = $false
            } | ConvertTo-Json -Compress

            { dsc resource delete -r OpenDsc.Windows/Group --input $inputJson } | Should -Not -Throw

            # Verify group was deleted
            $getJson = @{
                groupName = $script:testGroup1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/Group --input $getJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should not fail when deleting non-existent group' {
            # First delete the group
            $inputJson = @{
                groupName = $script:testGroup1
                _exist = $false
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.Windows/Group --input $inputJson | Out-Null

            # Try to delete again - should not throw
            { dsc resource delete -r OpenDsc.Windows/Group --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Export Operation' -Tag 'Export', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            New-LocalGroup -Name $script:testGroup1 -Description 'Test Export Group'
        }

        AfterEach {
            try {
                Get-LocalGroup -Name $script:testGroup1 -ErrorAction SilentlyContinue | Remove-LocalGroup
            }
            catch { }
        }

        It 'should export all groups' {
            $result = dsc resource export -r OpenDsc.Windows/Group | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.resources | Should -Not -BeNullOrEmpty
            $result.resources.Count | Should -BeGreaterThan 0

            # Should include our test group
            $testGroup = $result.resources | Where-Object { $_.properties.groupName -eq $script:testGroup1 }
            $testGroup | Should -Not -BeNullOrEmpty
            $testGroup.properties.groupName | Should -Be $script:testGroup1
            $testGroup.properties.description | Should -Be 'Test Export Group'
        }

        It 'should export built-in groups' {
            $result = dsc resource export -r OpenDsc.Windows/Group | ConvertFrom-Json

            # Should include Administrators group
            $adminsGroup = $result.resources | Where-Object { $_.properties.groupName -eq 'Administrators' }
            $adminsGroup | Should -Not -BeNullOrEmpty

            # Should include Users group
            $usersGroup = $result.resources | Where-Object { $_.properties.groupName -eq 'Users' }
            $usersGroup | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Member Format Support' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            if (-not (Get-LocalGroup -Name $script:testGroup1 -ErrorAction SilentlyContinue)) {
                New-LocalGroup -Name $script:testGroup1 -Description 'Test Group for Member Format Tests'
            }

            if (-not (Get-LocalUser -Name $script:testUser1 -ErrorAction SilentlyContinue)) {
                New-LocalUser -Name $script:testUser1 -Password (ConvertTo-SecureString "P@ssw0rd_$(Get-Random)!" -AsPlainText -Force)
            }
        }

        AfterEach {
            try {
                Get-LocalGroup -Name $script:testGroup1 -ErrorAction SilentlyContinue | Remove-LocalGroup
                Get-LocalUser -Name $script:testUser1 -ErrorAction SilentlyContinue | Remove-LocalUser
            }
            catch { }
        }

        It 'should add member using username format' {
            $inputJson = @{
                groupName = $script:testGroup1
                members = @($script:testUser1)
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Group --input $inputJson | Out-Null

            # Verify member was added
            $getJson = @{ groupName = $script:testGroup1 } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Group --input $getJson | ConvertFrom-Json
            $result.actualState.members | Should -Contain $script:testUser1
        }

        It 'should add member using SID format' {
            $user = Get-LocalUser -Name $script:testUser1
            $userSid = $user.SID.Value

            $inputJson = @{
                groupName = $script:testGroup1
                members = @($userSid)
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Group --input $inputJson | Out-Null

            # Verify member was added
            $getJson = @{ groupName = $script:testGroup1 } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Group --input $getJson | ConvertFrom-Json
            $result.actualState.members | Should -Contain $script:testUser1
        }

        It 'should add member using Domain\Username format' {
            $machineName = $env:COMPUTERNAME
            $domainUser = "$machineName\$($script:testUser1)"

            $inputJson = @{
                groupName = $script:testGroup1
                members = @($domainUser)
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Group --input $inputJson | Out-Null

            # Verify member was added
            $getJson = @{ groupName = $script:testGroup1 } | ConvertTo-Json -Compress
            $result = dsc resource get -r OpenDsc.Windows/Group --input $getJson | ConvertFrom-Json
            $result.actualState.members | Should -Contain $script:testUser1
        }

        It 'should add member using UPN format if user has UPN' {
            Set-ItResult -Skipped -Because "Local machine accounts don't support UPN property"
        }

        It 'should add member using DN format if user has DN' {
            Set-ItResult -Skipped -Because "Local users don't have DN format"
        }
    }

    Context 'Error Handling' {
        AfterEach {
            if ($script:isAdmin) {
                try {
                    # Give a moment for any locks to release
                    Start-Sleep -Milliseconds 100

                    $context = [System.DirectoryServices.AccountManagement.PrincipalContext]::new('Machine')

                    $group = [System.DirectoryServices.AccountManagement.GroupPrincipal]::FindByIdentity($context, $script:testGroup1)
                    if ($group) {
                        $group.Delete()
                        $group.Dispose()
                    }

                    $context.Dispose()
                }
                catch { }
            }
        }

        It 'should fail with invalid group name pattern' -Skip:(!$script:isAdmin) {
            $inputJson = @{
                groupName = 'Invalid/Group*Name'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Group --input $inputJson 2>&1
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should fail when adding non-existent member' -Skip:(!$script:isAdmin) {
            if (-not (Get-LocalGroup -Name $script:testGroup1 -ErrorAction SilentlyContinue)) {
                New-LocalGroup -Name $script:testGroup1 -Description 'Test Group for Error Test'
            }

            $inputJson = @{
                groupName = $script:testGroup1
                members = @('NonExistentUser99999')
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/Group --input $inputJson 2>&1
            $LASTEXITCODE | Should -Not -Be 0
        }
    }
}
