if ($IsWindows) {
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'Windows User Resource' -Tag 'Windows' -Skip:(!$IsWindows) {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $script:testUser1 = "DscTestUser_$(Get-Random -Maximum 9999)"
        $script:testUser2 = "DscTestUser_$(Get-Random -Maximum 9999)"
        $script:testPassword = "P@ssw0rd_$(Get-Random -Maximum 9999)!"
    }

    AfterAll {
        if ($script:isAdmin) {
            try {
                $context = [System.DirectoryServices.AccountManagement.PrincipalContext]::new('Machine')
                $user1 = [System.DirectoryServices.AccountManagement.UserPrincipal]::FindByIdentity($context, $script:testUser1)
                if ($user1) {
                    $user1.Delete()
                    $user1.Dispose()
                }
                $user2 = [System.DirectoryServices.AccountManagement.UserPrincipal]::FindByIdentity($context, $script:testUser2)
                if ($user2) {
                    $user2.Delete()
                    $user2.Dispose()
                }
                $context.Dispose()
            }
            catch { }
        }
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Windows/User | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            if ($result -is [array]) { $result = $result[0] }
            $result.type | Should -Be 'OpenDsc.Windows/User'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Windows/User | ConvertFrom-Json
            if ($result -is [array]) { $result = $result[0] }
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
            $result.capabilities | Should -Contain 'export'
        }

    }

    Context 'Get Operation - Non-Elevated' -Tag 'Get' {
        It 'should return _exist=false for non-existent user' {
            $inputJson = @{
                userName = 'NonExistUser99'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/User --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
            $result.actualState.userName | Should -Be 'NonExistUser99'
        }

        It 'should read properties of current user' {
            $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name.Split('\')[1]

            $inputJson = @{
                userName = $currentUser
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/User --input $inputJson | ConvertFrom-Json
            $result.actualState.userName | Should -Be $currentUser
            $result.actualState.disabled | Should -Not -BeNullOrEmpty
        }

        It 'should not return password in Get operation' {
            $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name.Split('\')[1]

            $inputJson = @{
                userName = $currentUser
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/User --input $inputJson | ConvertFrom-Json
            $result.actualState.PSObject.Properties.userName | Should -Not -Contain 'password'
        }
    }

    Context 'Get Operation - Elevated' -Tag 'Get', 'Admin' -Skip:(!$script:isAdmin) {
        It 'should read properties of existing user' {
            $context = [System.DirectoryServices.AccountManagement.PrincipalContext]::new('Machine')
            $user = [System.DirectoryServices.AccountManagement.UserPrincipal]::new($context)
            $user.SamAccountName = $script:testUser1
            $user.DisplayName = 'Test User Display'
            $user.Description = 'Test Description'
            $user.SetPassword($script:testPassword)
            $user.Save()
            $user.Dispose()
            $context.Dispose()

            $inputJson = @{
                userName = $script:testUser1
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Windows/User --input $inputJson | ConvertFrom-Json
            $result.actualState.userName | Should -Be $script:testUser1
            $result.actualState.fullName | Should -Be 'Test User Display'
            $result.actualState.description | Should -Be 'Test Description'
            $result.actualState.disabled | Should -Be $false
        }

        AfterEach {
            try {
                $context = [System.DirectoryServices.AccountManagement.PrincipalContext]::new('Machine')
                $user = [System.DirectoryServices.AccountManagement.UserPrincipal]::FindByIdentity($context, $script:testUser1)
                if ($user) {
                    $user.Delete()
                    $user.Dispose()
                }
                $context.Dispose()
            }
            catch { }
        }
    }

    Context 'Set Operation - Create User' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
        It 'should create a new user account' {
            $inputJson = @{
                userName = $script:testUser1
                password = $script:testPassword
                fullName = 'Test User Full Name'
                description = 'Created by DSC test'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/User --input $inputJson | Out-Null

            $verifyJson = @{
                userName = $script:testUser1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/User --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.userName | Should -Be $script:testUser1
            $getResult.actualState.fullName | Should -Be 'Test User Full Name'
            $getResult.actualState.description | Should -Be 'Created by DSC test'
        }

        It 'should create a disabled user' {
            $inputJson = @{
                userName = $script:testUser1
                password = $script:testPassword
                disabled = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/User --input $inputJson | Out-Null

            $verifyJson = @{
                userName = $script:testUser1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/User --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.disabled | Should -Be $true
        }

        It 'should fail to create user without password' {
            $inputJson = @{
                userName = $script:testUser1
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/User --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        AfterEach {
            try {
                $context = [System.DirectoryServices.AccountManagement.PrincipalContext]::new('Machine')
                $user = [System.DirectoryServices.AccountManagement.UserPrincipal]::FindByIdentity($context, $script:testUser1)
                if ($user) {
                    $user.Delete()
                    $user.Dispose()
                }
                $context.Dispose()
            }
            catch { }
        }
    }

    Context 'Set Operation - Update User' -Tag 'Set', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            $context = [System.DirectoryServices.AccountManagement.PrincipalContext]::new('Machine')
            $user = [System.DirectoryServices.AccountManagement.UserPrincipal]::new($context)
            $user.SamAccountName = $script:testUser1
            $user.DisplayName = 'Original Name'
            $user.Description = 'Original Description'
            $user.SetPassword($script:testPassword)
            $user.Save()
            $user.Dispose()
            $context.Dispose()
        }

        AfterEach {
            try {
                $context = [System.DirectoryServices.AccountManagement.PrincipalContext]::new('Machine')
                $user = [System.DirectoryServices.AccountManagement.UserPrincipal]::FindByIdentity($context, $script:testUser1)
                if ($user) {
                    $user.Delete()
                    $user.Dispose()
                }
                $context.Dispose()
            }
            catch { }
        }

        It 'should update user full name' {
            $inputJson = @{
                userName = $script:testUser1
                fullName = 'Updated Full Name'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/User --input $inputJson | Out-Null

            $verifyJson = @{
                userName = $script:testUser1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/User --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.fullName | Should -Be 'Updated Full Name'
        }

        It 'should update user description' {
            $inputJson = @{
                userName = $script:testUser1
                description = 'Updated Description'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/User --input $inputJson | Out-Null

            $verifyJson = @{
                userName = $script:testUser1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/User --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.description | Should -Be 'Updated Description'
        }

        It 'should disable user account' {
            $inputJson = @{
                userName = $script:testUser1
                disabled = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/User --input $inputJson | Out-Null

            $verifyJson = @{
                userName = $script:testUser1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/User --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.disabled | Should -Be $true
        }

        It 'should set password never expires' {
            $inputJson = @{
                userName = $script:testUser1
                passwordNeverExpires = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Windows/User --input $inputJson | Out-Null

            $verifyJson = @{
                userName = $script:testUser1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/User --input $verifyJson | ConvertFrom-Json
            $getResult.actualState.passwordNeverExpires | Should -Be $true
        }
    }

    Context 'Delete Operation' -Tag 'Delete', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeEach {
            $context = [System.DirectoryServices.AccountManagement.PrincipalContext]::new('Machine')
            $user = [System.DirectoryServices.AccountManagement.UserPrincipal]::new($context)
            $user.SamAccountName = $script:testUser1
            $user.SetPassword($script:testPassword)
            $user.Save()
            $user.Dispose()
            $context.Dispose()
        }

        It 'should delete an existing user' {
            $inputJson = @{
                userName = $script:testUser1
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.Windows/User --input $inputJson | Out-Null

            $verifyJson = @{
                userName = $script:testUser1
            } | ConvertTo-Json -Compress

            $getResult = dsc resource get -r OpenDsc.Windows/User --input $verifyJson | ConvertFrom-Json
            $getResult.actualState._exist | Should -Be $false
        }

        It 'should not error when deleting non-existent user' {
            $inputJson = @{
                userName = 'NonExist12345'
            } | ConvertTo-Json -Compress

            { dsc resource delete -r OpenDsc.Windows/User --input $inputJson } | Should -Not -Throw
        }
    }

    Context 'Export Operation' -Tag 'Export', 'Admin' -Skip:(!$script:isAdmin) {
        BeforeAll {
        $configuration = $env:BUILD_CONFIGURATION ?? 'Release'
            $context = [System.DirectoryServices.AccountManagement.PrincipalContext]::new('Machine')
            try {
                $existingUser1 = [System.DirectoryServices.AccountManagement.UserPrincipal]::FindByIdentity($context, $script:testUser1)
                if ($existingUser1) {
                    $existingUser1.Delete()
                    $existingUser1.Dispose()
                }
                $existingUser2 = [System.DirectoryServices.AccountManagement.UserPrincipal]::FindByIdentity($context, $script:testUser2)
                if ($existingUser2) {
                    $existingUser2.Delete()
                    $existingUser2.Dispose()
                }
            } catch { }

            $user1 = [System.DirectoryServices.AccountManagement.UserPrincipal]::new($context)
            $user1.SamAccountName = $script:testUser1
            $user1.DisplayName = 'Export Test 1'
            $user1.SetPassword($script:testPassword)
            $user1.Save()
            $user1.Dispose()

            $user2 = [System.DirectoryServices.AccountManagement.UserPrincipal]::new($context)
            $user2.SamAccountName = $script:testUser2
            $user2.DisplayName = 'Export Test 2'
            $user2.SetPassword($script:testPassword)
            $user2.Save()
            $user2.Dispose()

            $context.Dispose()
        }

        AfterAll {
            try {
                $context = [System.DirectoryServices.AccountManagement.PrincipalContext]::new('Machine')
                $user1 = [System.DirectoryServices.AccountManagement.UserPrincipal]::FindByIdentity($context, $script:testUser1)
                if ($user1) {
                    $user1.Delete()
                    $user1.Dispose()
                }
                $user2 = [System.DirectoryServices.AccountManagement.UserPrincipal]::FindByIdentity($context, $script:testUser2)
                if ($user2) {
                    $user2.Delete()
                    $user2.Dispose()
                }
                $context.Dispose()
            }
            catch { }
        }

        It 'should export all local users' {
            $result = dsc resource export -r OpenDsc.Windows/User | ConvertFrom-Json
            $result.resources | Should -Not -BeNullOrEmpty
            $result.resources.Count | Should -BeGreaterThan 0

            $exportedUsers = $result.resources.properties.userName
            $exportedUsers | Should -Contain $script:testUser1
            $exportedUsers | Should -Contain $script:testUser2
        }

        It 'should include user properties in export' {
            $result = dsc resource export -r OpenDsc.Windows/User | ConvertFrom-Json
            $testUser = $result.resources | Where-Object { $_.properties.userName -eq $script:testUser1 }

            $testUser | Should -Not -BeNullOrEmpty
            $testUser.properties.userName | Should -Be $script:testUser1
            $testUser.properties.fullName | Should -Be 'Export Test 1'
        }
    }
}
