param(
    [string]$OutputPath = "src/OpenDsc.Client.Commands/GeneratedClientCoverageCmdlets.cs"
)

$ErrorActionPreference = 'Stop'

$serviceTypeNames = @(
    'OpenDsc.Client.Services.ConfigurationHttpService',
    'OpenDsc.Client.Services.CompositeConfigurationHttpService',
    'OpenDsc.Client.Services.NodeHttpService',
    'OpenDsc.Client.Services.HealthHttpService',
    'OpenDsc.Client.Services.SettingsHttpService',
    'OpenDsc.Client.Services.ScopeHttpService',
    'OpenDsc.Client.Services.ParameterHttpService',
    'OpenDsc.Client.Services.ReportHttpService',
    'OpenDsc.Client.Services.UserHttpService',
    'OpenDsc.Client.Services.GroupHttpService',
    'OpenDsc.Client.Services.RoleHttpService',
    'OpenDsc.Client.Services.RegistrationKeyHttpService'
)

function Get-CSharpTypeName {
    param([Type]$Type)

    if ($Type -eq [string]) { return 'string' }
    if ($Type -eq [bool]) { return 'bool' }
    if ($Type -eq [int]) { return 'int' }
    if ($Type -eq [long]) { return 'long' }
    if ($Type -eq [short]) { return 'short' }
    if ($Type -eq [byte]) { return 'byte' }
    if ($Type -eq [double]) { return 'double' }
    if ($Type -eq [float]) { return 'float' }
    if ($Type -eq [decimal]) { return 'decimal' }
    if ($Type -eq [System.Guid]) { return 'Guid' }
    if ($Type -eq [System.DateTime]) { return 'DateTime' }
    if ($Type -eq [System.DateTimeOffset]) { return 'DateTimeOffset' }
    if ($Type -eq [System.TimeSpan]) { return 'TimeSpan' }
    if ($Type -eq [System.IO.Stream]) { return 'Stream' }

    $nullable = [Nullable]::GetUnderlyingType($Type)
    if ($null -ne $nullable) {
        return "$(Get-CSharpTypeName -Type $nullable)?"
    }

    if ($Type.IsArray) {
        return "$(Get-CSharpTypeName -Type $Type.GetElementType())[]"
    }

    if ($Type.IsGenericType) {
        $defName = $Type.GetGenericTypeDefinition().FullName
        $defName = $defName.Substring(0, $defName.IndexOf('`')).Replace('+', '.')
        $args = $Type.GetGenericArguments() | ForEach-Object { Get-CSharpTypeName -Type $_ }
        return "$defName<$($args -join ', ')>"
    }

    return $Type.FullName.Replace('+', '.')
}

function Get-SourceVerb {
    param([string]$MethodName)

    $known = @('Get','Create','Update','Delete','Remove','Add','Set','Assign','Publish','Change','Save','Reset','Unlock','Rotate','Enable','Disable','Reorder','Register','Report','Submit','Validate','Check','Is','Can','Grant','Revoke','Download','Upload','Authenticate')
    foreach ($k in $known) {
        if ($MethodName.StartsWith($k, [StringComparison]::Ordinal)) {
            return $k
        }
    }

    return 'Invoke'
}

function Map-Verb {
    param([string]$SourceVerb)

    switch ($SourceVerb) {
        'Get' { 'Get' }
        'Create' { 'New' }
        'Update' { 'Set' }
        'Delete' { 'Remove' }
        'Remove' { 'Remove' }
        'Add' { 'Add' }
        'Set' { 'Set' }
        'Assign' { 'Set' }
        'Publish' { 'Publish' }
        'Change' { 'Set' }
        'Save' { 'Set' }
        'Reset' { 'Reset' }
        'Unlock' { 'Unlock' }
        'Rotate' { 'Update' }
        'Enable' { 'Enable' }
        'Disable' { 'Disable' }
        'Reorder' { 'Set' }
        'Register' { 'Register' }
        'Report' { 'Write' }
        'Submit' { 'Submit' }
        'Validate' { 'Test' }
        'Check' { 'Test' }
        'Is' { 'Test' }
        'Can' { 'Test' }
        'Grant' { 'Grant' }
        'Revoke' { 'Revoke' }
        'Download' { 'Get' }
        'Upload' { 'Add' }
        'Authenticate' { 'Test' }
        default { 'Invoke' }
    }
}

function Get-Initializer {
    param([Type]$Type)

    $typeName = Get-CSharpTypeName -Type $Type
    if ($typeName -eq 'string') { return 'string.Empty' }
    if ($Type.IsValueType -and $null -eq [Nullable]::GetUnderlyingType($Type)) { return 'default' }
    return 'null!'
}

function Normalize-Suffix {
    param(
        [string]$Domain,
        [string]$Suffix
    )

    if ($Suffix -eq $Domain -or $Suffix -eq ($Domain + 's')) {
        return ''
    }

    if ($Suffix.StartsWith($Domain, [StringComparison]::Ordinal)) {
        $tail = $Suffix.Substring($Domain.Length)
        if ($tail.Length -gt 0) {
            return $tail
        }
    }

    return $Suffix
}

dotnet build src/OpenDsc.Client/OpenDsc.Client.csproj -nologo | Out-Null

$clientAssemblyPath = Join-Path $PWD 'src/OpenDsc.Client/bin/Debug/net10.0/OpenDsc.Client.dll'
$contractsAssemblyPath = Join-Path $PWD 'src/OpenDsc.Contracts/bin/Debug/net10.0/OpenDsc.Contracts.dll'
[void][System.Reflection.Assembly]::LoadFrom($contractsAssemblyPath)
$clientAssembly = [System.Reflection.Assembly]::LoadFrom($clientAssemblyPath)

$sb = [System.Text.StringBuilder]::new()
[System.Collections.Generic.HashSet[string]]$usedCmdletNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
[void]$sb.AppendLine('// Copyright (c) Thomas Nieto - All Rights Reserved')
[void]$sb.AppendLine('// You may use, distribute and modify this code under the')
[void]$sb.AppendLine('// terms of the MIT license.')
[void]$sb.AppendLine()
[void]$sb.AppendLine('using System.Collections;')
[void]$sb.AppendLine('using System.Management.Automation;')
[void]$sb.AppendLine('using OpenDsc.Client.Services;')
[void]$sb.AppendLine()
[void]$sb.AppendLine('namespace OpenDsc.Client.Commands;')
[void]$sb.AppendLine()

foreach ($serviceTypeName in $serviceTypeNames) {
    $serviceType = $clientAssembly.GetType($serviceTypeName, $true)
    $domain = $serviceType.Name.Replace('HttpService', '')

    $methods = $serviceType.GetMethods([System.Reflection.BindingFlags]'Public, Instance, DeclaredOnly') |
        Where-Object { -not $_.IsSpecialName } |
        Sort-Object Name

    foreach ($method in $methods) {
        $methodStem = if ($method.Name.EndsWith('Async', [StringComparison]::Ordinal)) {
            $method.Name.Substring(0, $method.Name.Length - 5)
        }
        else {
            $method.Name
        }

        $sourceVerb = Get-SourceVerb -MethodName $methodStem
        $verb = Map-Verb -SourceVerb $sourceVerb
        $suffix = if ($methodStem.StartsWith($sourceVerb, [StringComparison]::Ordinal)) {
            $methodStem.Substring($sourceVerb.Length)
        }
        else {
            $methodStem
        }

        $suffix = Normalize-Suffix -Domain $domain -Suffix $suffix
        $noun = if ([string]::IsNullOrWhiteSpace($suffix)) { "DscServer$domain" } else { "DscServer$domain$suffix" }
        $cmdletName = "$verb-$noun"
        if (-not $usedCmdletNames.Add($cmdletName)) {
            $noun = "$noun$methodStem"
            $cmdletName = "$verb-$noun"
            $null = $usedCmdletNames.Add($cmdletName)
        }
        $className = "${domain}${methodStem}Command"

        [void]$sb.AppendLine("[Cmdlet(`"$verb`", `"$noun`")]")

        $returnType = $method.ReturnType
        if ($returnType.IsGenericType -and $returnType.GetGenericTypeDefinition().FullName -eq 'System.Threading.Tasks.Task`1') {
            $outputType = Get-CSharpTypeName -Type $returnType.GetGenericArguments()[0]
            [void]$sb.AppendLine("[OutputType(typeof($outputType))]")
        }

        [void]$sb.AppendLine("public sealed class $className : DscServerCommandBase")
        [void]$sb.AppendLine('{')

        $methodParams = @($method.GetParameters() | Where-Object { $_.ParameterType.FullName -ne 'System.Threading.CancellationToken' })
        for ($i = 0; $i -lt $methodParams.Count; $i++) {
            $p = $methodParams[$i]
            $propertyName = $p.Name.Substring(0, 1).ToUpper() + $p.Name.Substring(1)
            $paramType = Get-CSharpTypeName -Type $p.ParameterType
            $mandatory = if ($p.HasDefaultValue) { 'false' } else { 'true' }

            [void]$sb.AppendLine("    [Parameter(Mandatory = $mandatory, Position = $i)]")
            if ($paramType -eq 'string' -and $mandatory -eq 'true') {
                [void]$sb.AppendLine('    [ValidateNotNullOrEmpty]')
            }

            [void]$sb.AppendLine("    public $paramType $propertyName { get; set; } = $(Get-Initializer -Type $p.ParameterType);")
            [void]$sb.AppendLine()
        }

        [void]$sb.AppendLine('    protected override void ProcessRecord()')
        [void]$sb.AppendLine('    {')
        [void]$sb.AppendLine("        var service = GetRequiredService<$($serviceType.FullName.Replace('+', '.'))>();")

        $argList = @()
        foreach ($p in $methodParams) {
            $argList += ($p.Name.Substring(0, 1).ToUpper() + $p.Name.Substring(1))
        }

        if (($method.GetParameters().ParameterType.FullName -contains 'System.Threading.CancellationToken')) {
            $argList += 'CancellationToken.None'
        }

        $call = "service.$($method.Name)($($argList -join ', '))"

        if ($returnType.FullName -eq 'System.Threading.Tasks.Task') {
            [void]$sb.AppendLine("        $call.GetAwaiter().GetResult();")
        }
        elseif ($returnType.IsGenericType -and $returnType.GetGenericTypeDefinition().FullName -eq 'System.Threading.Tasks.Task`1') {
            [void]$sb.AppendLine("        var result = $call.GetAwaiter().GetResult();")
            [void]$sb.AppendLine('        WriteObject(result, enumerateCollection: true);')
        }
        else {
            [void]$sb.AppendLine("        var result = $call;")
            [void]$sb.AppendLine('        WriteObject(result);')
        }

        [void]$sb.AppendLine('    }')
        [void]$sb.AppendLine('}')
        [void]$sb.AppendLine()
    }
}

$outputFile = Join-Path $PWD $OutputPath
Set-Content -Path $outputFile -Value $sb.ToString() -Encoding UTF8
Write-Output "Generated: $outputFile"
