Describe 'XML Element Resource' -Tag 'Windows', 'Linux', 'macOS' {
    BeforeAll {
        $publishDir = Join-Path $PSScriptRoot "..\..\artifacts\publish"
        if (Test-Path $publishDir) {
            $env:DSC_RESOURCE_PATH = $publishDir
        }

        $testDir = Join-Path $TestDrive 'xml-tests'
        New-Item -ItemType Directory -Path $testDir -Force | Out-Null
    }

    Context 'Discovery' -Tag 'Discovery' {
        It 'should be found by dsc' {
            $result = dsc resource list OpenDsc.Xml/Element | ConvertFrom-Json
            $result | Should -Not -BeNullOrEmpty
            $result.type | Should -Be 'OpenDsc.Xml/Element'
        }

        It 'should report correct capabilities' {
            $result = dsc resource list OpenDsc.Xml/Element | ConvertFrom-Json
            $result.capabilities | Should -Contain 'get'
            $result.capabilities | Should -Contain 'set'
            $result.capabilities | Should -Contain 'delete'
        }
    }

    Context 'Get Operation' -Tag 'Get' {
        It 'should return _exist=false for non-existent file' {
            $inputJson = @{
                path  = Join-Path $testDir 'nonexistent.xml'
                xPath = '/configuration/setting'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Xml/Element --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }

        It 'should return _exist=false for non-existent element' {
            $xmlPath = Join-Path $testDir 'test-get-1.xml'
            @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <settings>
        <value>123</value>
    </settings>
</configuration>
'@ | Set-Content -Path $xmlPath

            $inputJson = @{
                path  = $xmlPath
                xPath = '/configuration/nonexistent'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Xml/Element --input $inputJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false
        }

        It 'should read element text content' {
            $xmlPath = Join-Path $testDir 'test-get-2.xml'
            @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <settings>
        <level>Debug</level>
    </settings>
</configuration>
'@ | Set-Content -Path $xmlPath

            $inputJson = @{
                path  = $xmlPath
                xPath = '/configuration/settings/level'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Xml/Element --input $inputJson | ConvertFrom-Json
            $result.actualState.value | Should -Be 'Debug'
            $result.actualState._exist | Should -BeNullOrEmpty
        }

        It 'should read element attributes' {
            $xmlPath = Join-Path $testDir 'test-get-3.xml'
            @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <add key="Setting1" value="Value1" enabled="true" />
</configuration>
'@ | Set-Content -Path $xmlPath

            $inputJson = @{
                path  = $xmlPath
                xPath = '/configuration/add'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Xml/Element --input $inputJson | ConvertFrom-Json
            $result.actualState.attributes.key | Should -Be 'Setting1'
            $result.actualState.attributes.value | Should -Be 'Value1'
            $result.actualState.attributes.enabled | Should -Be 'true'
        }
    }

    Context 'Set Operation - Element Creation' -Tag 'Set' {
        It 'should fail when file does not exist' {
            $inputJson = @{
                path  = Join-Path $testDir 'nonexistent.xml'
                xPath = '/configuration/setting'
                value = 'test'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Xml/Element --input $inputJson 2>&1 | Out-Null
            $LASTEXITCODE | Should -Not -Be 0
        }

        It 'should create element with text content only' {
            $xmlPath = Join-Path $testDir 'test-set-1.xml'
            @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
</configuration>
'@ | Set-Content -Path $xmlPath

            $inputJson = @{
                path  = $xmlPath
                xPath = '/configuration/logging/level'
                value = 'Debug'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Xml/Element --input $inputJson | Out-Null

            $verifyJson = @{
                path  = $xmlPath
                xPath = '/configuration/logging/level'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Xml/Element --input $verifyJson | ConvertFrom-Json
            $result.actualState.value | Should -Be 'Debug'
        }

        It 'should create element with attributes' {
            $xmlPath = Join-Path $testDir 'test-set-2.xml'
            @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
</configuration>
'@ | Set-Content -Path $xmlPath

            $inputJson = @{
                path       = $xmlPath
                xPath      = '/configuration/add'
                attributes = @{
                    key   = 'DatabaseConnection'
                    value = 'Server=localhost'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Xml/Element --input $inputJson | Out-Null

            $verifyJson = @{
                path  = $xmlPath
                xPath = '/configuration/add'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Xml/Element --input $verifyJson | ConvertFrom-Json
            $result.actualState.attributes.key | Should -Be 'DatabaseConnection'
            $result.actualState.attributes.value | Should -Be 'Server=localhost'
        }

        It 'should create nested elements recursively' {
            $xmlPath = Join-Path $testDir 'test-set-3.xml'
            @'
<?xml version="1.0" encoding="UTF-8"?>
<root>
</root>
'@ | Set-Content -Path $xmlPath

            $inputJson = @{
                path  = $xmlPath
                xPath = '/root/level1/level2/level3/value'
                value = 'DeepValue'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Xml/Element --input $inputJson | Out-Null

            $verifyJson = @{
                path  = $xmlPath
                xPath = '/root/level1/level2/level3/value'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Xml/Element --input $verifyJson | ConvertFrom-Json
            $result.actualState.value | Should -Be 'DeepValue'
        }
    }

    Context 'Set Operation - Attribute Management' -Tag 'Set' {
        It 'should add attributes in additive mode (default)' {
            $xmlPath = Join-Path $testDir 'test-attr-1.xml'
            @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <setting version="1.0" enabled="false" />
</configuration>
'@ | Set-Content -Path $xmlPath

            $inputJson = @{
                path       = $xmlPath
                xPath      = '/configuration/setting'
                attributes = @{
                    version = '2.0'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Xml/Element --input $inputJson | Out-Null

            $verifyJson = @{
                path  = $xmlPath
                xPath = '/configuration/setting'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Xml/Element --input $verifyJson | ConvertFrom-Json
            $result.actualState.attributes.version | Should -Be '2.0'
            $result.actualState.attributes.enabled | Should -Be 'false'
        }

        It 'should remove unlisted attributes with _purge=true' {
            $xmlPath = Join-Path $testDir 'test-attr-2.xml'
            @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <setting version="1.0" enabled="false" deprecated="true" />
</configuration>
'@ | Set-Content -Path $xmlPath

            $inputJson = @{
                path       = $xmlPath
                xPath      = '/configuration/setting'
                attributes = @{
                    version = '2.0'
                    enabled = 'true'
                }
                _purge     = $true
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Xml/Element --input $inputJson | Out-Null

            $verifyJson = @{
                path  = $xmlPath
                xPath = '/configuration/setting'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Xml/Element --input $verifyJson | ConvertFrom-Json
            $result.actualState.attributes.version | Should -Be '2.0'
            $result.actualState.attributes.enabled | Should -Be 'true'
            $result.actualState.attributes.deprecated | Should -BeNullOrEmpty
        }

        It 'should handle combined value and attributes' {
            $xmlPath = Join-Path $testDir 'test-attr-3.xml'
            @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <setting />
</configuration>
'@ | Set-Content -Path $xmlPath

            $inputJson = @{
                path       = $xmlPath
                xPath      = '/configuration/setting'
                value      = 'TextContent'
                attributes = @{
                    type     = 'string'
                    required = 'true'
                }
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Xml/Element --input $inputJson | Out-Null

            $verifyJson = @{
                path  = $xmlPath
                xPath = '/configuration/setting'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Xml/Element --input $verifyJson | ConvertFrom-Json
            $result.actualState.value | Should -Be 'TextContent'
            $result.actualState.attributes.type | Should -Be 'string'
            $result.actualState.attributes.required | Should -Be 'true'
        }
    }

    Context 'Delete Operation' -Tag 'Delete' {
        It 'should remove element when _exist=false' {
            $xmlPath = Join-Path $testDir 'test-delete-1.xml'
            @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <setting>ToDelete</setting>
    <keep>Keep</keep>
</configuration>
'@ | Set-Content -Path $xmlPath

            $inputJson = @{
                path   = $xmlPath
                xPath  = '/configuration/setting'
                _exist = $false
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.Xml/Element --input $inputJson | Out-Null

            $verifyJson = @{
                path  = $xmlPath
                xPath = '/configuration/setting'
            } | ConvertTo-Json -Compress

            $result = dsc resource get -r OpenDsc.Xml/Element --input $verifyJson | ConvertFrom-Json
            $result.actualState._exist | Should -Be $false

            $verifyKeepJson = @{
                path  = $xmlPath
                xPath = '/configuration/keep'
            } | ConvertTo-Json -Compress

            $keepResult = dsc resource get -r OpenDsc.Xml/Element --input $verifyKeepJson | ConvertFrom-Json
            $keepResult.actualState.value | Should -Be 'Keep'
        }

        It 'should be idempotent when element does not exist' {
            $xmlPath = Join-Path $testDir 'test-delete-2.xml'
            @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
</configuration>
'@ | Set-Content -Path $xmlPath

            $inputJson = @{
                path   = $xmlPath
                xPath  = '/configuration/nonexistent'
                _exist = $false
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.Xml/Element --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0
        }

        It 'should handle deleting file that does not exist' {
            $inputJson = @{
                path   = Join-Path $testDir 'nonexistent-delete.xml'
                xPath  = '/configuration/setting'
                _exist = $false
            } | ConvertTo-Json -Compress

            dsc resource delete -r OpenDsc.Xml/Element --input $inputJson | Out-Null
            $LASTEXITCODE | Should -Be 0
        }
    }

    Context 'Encoding Preservation' {
        It 'should preserve UTF-8 encoding' {
            $xmlPath = Join-Path $testDir 'test-encoding.xml'
            @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <setting>Original</setting>
</configuration>
'@ | Set-Content -Path $xmlPath -Encoding UTF8

            $inputJson = @{
                path  = $xmlPath
                xPath = '/configuration/setting'
                value = 'Updated'
            } | ConvertTo-Json -Compress

            dsc resource set -r OpenDsc.Xml/Element --input $inputJson | Out-Null

            $content = Get-Content -Path $xmlPath -Raw
            $content | Should -Match 'encoding="UTF-8"'
        }
    }

    AfterEach {
        if (Test-Path $testDir) {
            Get-ChildItem -Path $testDir -Filter '*.xml' | Remove-Item -Force -ErrorAction SilentlyContinue
        }
    }
}
