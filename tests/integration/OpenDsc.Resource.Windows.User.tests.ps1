# User creation requires administrative privileges.
#Requires -RunAsAdministrator

BeforeDiscovery {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($PSCommandPath) -replace '\.tests$'
    $buildProject = Join-Path $PSScriptRoot '..' '..' 'src' $projectName "$projectName.csproj"

    $global:outputDirectory = Join-Path $PSScriptRoot '..' '..' 'output'
    Write-Verbose "Output directory: $outputDirectory" -Verbose

    # TODO: Install .NET SDK if not present
    $arguments = @(
        'build', $buildProject,
        '-c', 'Release',
        '-o', $outputDirectory
    )
    & dotnet @arguments

    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to build the project: $buildProject"
    }
}

Describe 'OpenDsc.Resource.Windows.User resource tests' {

    BeforeAll {
        $oldPath = $env:Path
        $env:Path += [System.IO.Path]::PathSeparator + (Join-Path $PSScriptRoot '..' '..' 'output')
    }

    AfterAll {
        $env:Path = $oldPath
    }

    It 'Get works for administrator user' {
        $in = @{
            username = 'Administrator'
        } | ConvertTo-Json

        $out = windows-user config get --input $in | ConvertFrom-Json
        $out | Should -Not -BeNullOrEmpty
        $LASTEXITCODE | Should -Be 0
        $out.userName | Should -Be 'Administrator'
    }

    It 'Creates a new user' {
        $in = @{
            username                 = 'TestUser'
            fullName                 = 'Test User'
            description              = 'This is a test user.'
            password                 = 'P@ssw0rd!'
            disabled                 = $false
            passwordChangeRequired   = $true
            passwordNeverExpires     = $false
            passwordChangeNotAllowed = $false
        } | ConvertTo-Json

        $out = windows-user config set --input $in | ConvertFrom-Json
        $out | Should -Not -BeNullOrEmpty
        $LASTEXITCODE | Should -Be 0
        $out.userName | Should -Be 'TestUser'
        $out.fullName | Should -Be 'Test User'
        $out.description | Should -Be 'This is a test user.'
        $out.disabled | Should -Be $false
        $out.passwordChangeRequired | Should -Be $true
        $out.passwordNeverExpires | Should -Be $false
        $out.passwordChangeNotAllowed | Should -Be $false
    }

    It 'Should update the description' {
        $in = @{
            username    = 'TestUser'
            description = 'Updated description for Test User.'
        } | ConvertTo-Json

        $out = windows-user config set --input $in | ConvertFrom-Json
        $out | Should -Not -BeNullOrEmpty
        $LASTEXITCODE | Should -Be 0
        $out.userName | Should -Be 'TestUser'
        $out.description | Should -Be 'Updated description for Test User.'
    }

    It 'Should delete a user' {
        $in = @{
            username = 'TestUser'
        } | ConvertTo-Json

        windows-user config delete --input $in
        $LASTEXITCODE | Should -Be 0

        $out = windows-user config get --input $in | ConvertFrom-Json
        $out | Should -Not -BeNullOrEmpty
        $out._exist | Should -BeFalse
    }

    It 'Export works for all users' {
        $out = windows-user config export | ConvertFrom-Json
        $out | Should -Not -BeNullOrEmpty
        $LASTEXITCODE | Should -Be 0
        $out.Count | Should -BeGreaterThan 0
        $out[0].userName | Should -Not -BeNullOrEmpty
        $out[0].fullName | Should -Not -BeNullOrEmpty
        $out[0].description | Should -Not -BeNullOrEmpty
        $out[0].disabled | Should -Not -BeNullOrEmpty
        $out[0].passwordChangeRequired | Should -Not -BeNullOrEmpty
        $out[0].passwordNeverExpires | Should -Not -BeNullOrEmpty
        $out[0].passwordChangeNotAllowed | Should -Not -BeNullOrEmpty
        $out[0]._exist | Should -BeTrue
    }
}
