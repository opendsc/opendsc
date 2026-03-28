# ---------------------------------------------------------------------------
# PSOpenDsc - PowerShell SDK for the OpenDsc Pull Server REST API
# ---------------------------------------------------------------------------

#region Module State

$script:Session = $null

#endregion

#region API Data Table

$script:ApiData = @{

    'Connect-OpenDsc'                                = @{
        URI    = '/api/v1/auth/login'
        Method = 'Post'
        Body   = @{ Username = 'username'; Password = 'password' }
    }
    'Disconnect-OpenDsc'                             = @{
        URI    = '/api/v1/auth/logout'
        Method = 'Post'
    }
    'Get-OpenDscCurrentUser'                         = @{
        URI    = '/api/v1/auth/me'
        Method = 'Get'
    }
    'Set-OpenDscPassword'                            = @{
        URI    = '/api/v1/auth/change-password'
        Method = 'Post'
        Body   = @{ CurrentPassword = 'currentPassword'; NewPassword = 'newPassword' }
    }
    'New-OpenDscToken'                               = @{
        URI    = '/api/v1/auth/tokens'
        Method = 'Post'
        Body   = @{ Name = 'name'; Scopes = 'scopes'; ExpiresAt = 'expiresAt' }
    }
    'Get-OpenDscToken'                               = @{
        URI    = '/api/v1/auth/tokens'
        Method = 'Get'
    }
    'Remove-OpenDscToken'                            = @{
        URI    = '/api/v1/auth/tokens/{id}'
        Method = 'Delete'
    }

    'Get-OpenDscNode'                                = @{
        URI    = '/api/v1/nodes'
        Method = 'Get'
    }
    'Register-OpenDscNode'                           = @{
        URI    = '/api/v1/nodes/register'
        Method = 'Post'
        Body   = @{
            RegistrationKey           = 'registrationKey'
            Fqdn                      = 'fqdn'
            ConfigurationSource       = 'configurationSource'
            ConfigurationMode         = 'configurationMode'
            ConfigurationModeInterval = 'configurationModeInterval'
            ReportCompliance          = 'reportCompliance'
        }
    }
    'Remove-OpenDscNode'                             = @{
        URI    = '/api/v1/nodes/{nodeId}'
        Method = 'Delete'
    }
    'Get-OpenDscNodeConfiguration'                   = @{
        URI    = '/api/v1/nodes/{nodeId}/configuration'
        Method = 'Get'
    }
    'Set-OpenDscNodeConfiguration'                   = @{
        URI    = '/api/v1/nodes/{nodeId}/configuration'
        Method = 'Put'
        Body   = @{
            ConfigurationName = 'configurationName'
            IsComposite       = 'isComposite'
            MajorVersion      = 'majorVersion'
            PrereleaseChannel = 'prereleaseChannel'
        }
    }
    'Remove-OpenDscNodeConfiguration'                = @{
        URI    = '/api/v1/nodes/{nodeId}/configuration'
        Method = 'Delete'
    }
    'Get-OpenDscNodeConfigurationChecksum'           = @{
        URI    = '/api/v1/nodes/{nodeId}/configuration/checksum'
        Method = 'Get'
    }
    'Save-OpenDscNodeConfigurationBundle'            = @{
        URI    = '/api/v1/nodes/{nodeId}/configuration/bundle'
        Method = 'Get'
    }
    'Set-OpenDscNodeLcmConfig'                       = @{
        URI    = '/api/v1/nodes/{nodeId}/lcm-config'
        Method = 'Put'
        Body   = @{
            ConfigurationMode         = 'configurationMode'
            ConfigurationModeInterval = 'configurationModeInterval'
            ReportCompliance          = 'reportCompliance'
        }
    }
    'Get-OpenDscNodeLcmConfig'                       = @{
        URI    = '/api/v1/nodes/{nodeId}/lcm-config'
        Method = 'Get'
    }
    'Set-OpenDscNodeLcmStatus'                       = @{
        URI    = '/api/v1/nodes/{nodeId}/lcm-status'
        Method = 'Put'
        Body   = @{ LcmStatus = 'lcmStatus' }
    }
    'Set-OpenDscNodeReportedConfig'                  = @{
        URI    = '/api/v1/nodes/{nodeId}/reported-config'
        Method = 'Put'
        Body   = @{
            ConfigurationMode         = 'configurationMode'
            ConfigurationModeInterval = 'configurationModeInterval'
            ReportCompliance          = 'reportCompliance'
        }
    }
    'Get-OpenDscNodeStatusHistory'                   = @{
        URI    = '/api/v1/nodes/{nodeId}/status-history'
        Method = 'Get'
    }
    'Get-OpenDscNodeTag'                             = @{
        URI    = '/api/v1/nodes/{nodeId}/tags'
        Method = 'Get'
    }
    'Add-OpenDscNodeTag'                             = @{
        URI    = '/api/v1/nodes/{nodeId}/tags'
        Method = 'Post'
        Body   = @{ ScopeValueId = 'scopeValueId' }
    }
    'Remove-OpenDscNodeTag'                          = @{
        URI    = '/api/v1/nodes/{nodeId}/tags/{scopeValueId}'
        Method = 'Delete'
    }
    'Get-OpenDscNodeParameterProvenance'             = @{
        URI    = '/api/v1/nodes/{nodeId}/parameters/provenance'
        Method = 'Get'
        Query  = @{ ConfigurationId = 'configurationId' }
    }
    'Get-OpenDscNodeParameterResolution'             = @{
        URI    = '/api/v1/nodes/{nodeId}/parameters/resolution'
        Method = 'Get'
        Query  = @{ ConfigurationId = 'configurationId' }
    }

    'Get-OpenDscConfiguration'                       = @{
        URI    = '/api/v1/configurations'
        Method = 'Get'
    }
    'Set-OpenDscConfiguration'                       = @{
        URI    = '/api/v1/configurations/{name}'
        Method = 'Patch'
        Body   = @{
            Description                = 'description'
            UseServerManagedParameters = 'useServerManagedParameters'
        }
    }
    'Remove-OpenDscConfiguration'                    = @{
        URI    = '/api/v1/configurations/{name}'
        Method = 'Delete'
    }
    'Get-OpenDscConfigurationVersion'                = @{
        URI    = '/api/v1/configurations/{name}/versions'
        Method = 'Get'
    }
    'Publish-OpenDscConfigurationVersion'            = @{
        URI    = '/api/v1/configurations/{name}/versions/{version}/publish'
        Method = 'Put'
    }
    'Remove-OpenDscConfigurationVersion'             = @{
        URI    = '/api/v1/configurations/{name}/versions/{version}'
        Method = 'Delete'
    }
    'Get-OpenDscConfigurationPermission'             = @{
        URI    = '/api/v1/configurations/{name}/permissions'
        Method = 'Get'
    }
    'Set-OpenDscConfigurationPermission'             = @{
        URI    = '/api/v1/configurations/{name}/permissions'
        Method = 'Put'
        Body   = @{
            PrincipalType = 'principalType'
            PrincipalId   = 'principalId'
            Level         = 'level'
        }
    }
    'Remove-OpenDscConfigurationPermission'          = @{
        URI    = '/api/v1/configurations/{name}/permissions/{principalType}/{principalId}'
        Method = 'Delete'
    }

    'Get-OpenDscCompositeConfiguration'              = @{
        URI    = '/api/v1/composite-configurations'
        Method = 'Get'
    }
    'New-OpenDscCompositeConfiguration'              = @{
        URI    = '/api/v1/composite-configurations'
        Method = 'Post'
        Body   = @{
            Name        = 'name'
            Description = 'description'
            EntryPoint  = 'entryPoint'
        }
    }
    'Remove-OpenDscCompositeConfiguration'           = @{
        URI    = '/api/v1/composite-configurations/{name}'
        Method = 'Delete'
    }
    'Get-OpenDscCompositeConfigurationVersion'       = @{
        URI    = '/api/v1/composite-configurations/{name}/versions'
        Method = 'Get'
    }
    'New-OpenDscCompositeConfigurationVersion'       = @{
        URI    = '/api/v1/composite-configurations/{name}/versions'
        Method = 'Post'
        Body   = @{
            Version           = 'version'
            PrereleaseChannel = 'prereleaseChannel'
        }
    }
    'Publish-OpenDscCompositeConfigurationVersion'   = @{
        URI    = '/api/v1/composite-configurations/{name}/versions/{version}/publish'
        Method = 'Put'
    }
    'Remove-OpenDscCompositeConfigurationVersion'    = @{
        URI    = '/api/v1/composite-configurations/{name}/versions/{version}'
        Method = 'Delete'
    }
    'Add-OpenDscCompositeConfigurationChild'         = @{
        URI    = '/api/v1/composite-configurations/{name}/versions/{version}/children'
        Method = 'Post'
        Body   = @{
            ChildConfigurationName = 'childConfigurationName'
            ActiveVersion          = 'activeVersion'
            Order                  = 'order'
        }
    }
    'Set-OpenDscCompositeConfigurationChild'         = @{
        URI    = '/api/v1/composite-configurations/{name}/versions/{version}/children/{childId}'
        Method = 'Put'
        Body   = @{
            ActiveVersion = 'activeVersion'
            Order         = 'order'
        }
    }
    'Remove-OpenDscCompositeConfigurationChild'      = @{
        URI    = '/api/v1/composite-configurations/{name}/versions/{version}/children/{childId}'
        Method = 'Delete'
    }
    'Get-OpenDscCompositeConfigurationPermission'    = @{
        URI    = '/api/v1/composite-configurations/{name}/permissions'
        Method = 'Get'
    }
    'Set-OpenDscCompositeConfigurationPermission'    = @{
        URI    = '/api/v1/composite-configurations/{name}/permissions'
        Method = 'Put'
        Body   = @{
            PrincipalType = 'principalType'
            PrincipalId   = 'principalId'
            Level         = 'level'
        }
    }
    'Remove-OpenDscCompositeConfigurationPermission' = @{
        URI    = '/api/v1/composite-configurations/{name}/permissions/{principalType}/{principalId}'
        Method = 'Delete'
    }

    'Set-OpenDscParameter'                           = @{
        URI    = '/api/v1/parameters/{scopeTypeId}/{configurationId}'
        Method = 'Put'
        Body   = @{
            ScopeValue    = 'scopeValue'
            Version       = 'version'
            Content       = 'content'
            ContentType   = 'contentType'
            IsPassthrough = 'isPassthrough'
        }
    }
    'Get-OpenDscParameterVersion'                    = @{
        URI    = '/api/v1/parameters/{scopeTypeId}/{configurationId}/versions'
        Method = 'Get'
        Query  = @{ ScopeValue = 'scopeValue' }
    }
    'Publish-OpenDscParameterVersion'                = @{
        URI    = '/api/v1/parameters/{scopeTypeId}/{configurationId}/versions/{version}/publish'
        Method = 'Put'
        Query  = @{ ScopeValue = 'scopeValue' }
    }
    'Remove-OpenDscParameterVersion'                 = @{
        URI    = '/api/v1/parameters/{scopeTypeId}/{configurationId}/versions/{version}'
        Method = 'Delete'
        Query  = @{ ScopeValue = 'scopeValue' }
    }
    'Get-OpenDscParameterMajorVersion'               = @{
        URI    = '/api/v1/parameters/{scopeTypeId}/{configurationId}/majors'
        Method = 'Get'
    }

    'Get-OpenDscScopeType'                           = @{
        URI    = '/api/v1/scope-types'
        Method = 'Get'
    }
    'New-OpenDscScopeType'                           = @{
        URI    = '/api/v1/scope-types'
        Method = 'Post'
        Body   = @{
            Name        = 'name'
            Description = 'description'
            ValueMode   = 'valueMode'
        }
    }
    'Set-OpenDscScopeType'                           = @{
        URI    = '/api/v1/scope-types/{id}'
        Method = 'Patch'
        Body   = @{ Description = 'description' }
    }
    'Remove-OpenDscScopeType'                        = @{
        URI    = '/api/v1/scope-types/{id}'
        Method = 'Delete'
    }
    'Set-OpenDscScopeTypeOrder'                      = @{
        URI    = '/api/v1/scope-types/reorder'
        Method = 'Post'
        Body   = @{ ScopeTypeIds = 'scopeTypeIds' }
    }
    'Enable-OpenDscScopeType'                        = @{
        URI    = '/api/v1/scope-types/{id}/enable'
        Method = 'Patch'
    }
    'Disable-OpenDscScopeType'                       = @{
        URI    = '/api/v1/scope-types/{id}/disable'
        Method = 'Patch'
    }

    'Get-OpenDscScopeValue'                          = @{
        URI    = '/api/v1/scope-types/{scopeTypeId}/values'
        Method = 'Get'
    }
    'New-OpenDscScopeValue'                          = @{
        URI    = '/api/v1/scope-types/{scopeTypeId}/values'
        Method = 'Post'
        Body   = @{
            Value       = 'value'
            Description = 'description'
        }
    }
    'Set-OpenDscScopeValue'                          = @{
        URI    = '/api/v1/scope-types/{scopeTypeId}/values/{id}'
        Method = 'Patch'
        Body   = @{ Description = 'description' }
    }
    'Remove-OpenDscScopeValue'                       = @{
        URI    = '/api/v1/scope-types/{scopeTypeId}/values/{id}'
        Method = 'Delete'
    }

    'Get-OpenDscReport'                              = @{
        URI    = '/api/v1/reports'
        Method = 'Get'
        Query  = @{ Skip = 'skip'; Take = 'take'; From = 'from'; To = 'to' }
    }
    'Get-OpenDscNodeReport'                          = @{
        URI    = '/api/v1/nodes/{nodeId}/reports'
        Method = 'Get'
        Query  = @{ Skip = 'skip'; Take = 'take'; From = 'from'; To = 'to' }
    }

    'Get-OpenDscUser'                                = @{
        URI    = '/api/v1/users'
        Method = 'Get'
    }
    'New-OpenDscUser'                                = @{
        URI    = '/api/v1/users'
        Method = 'Post'
        Body   = @{
            Username              = 'username'
            Email                 = 'email'
            Password              = 'password'
            AccountType           = 'accountType'
            RequirePasswordChange = 'requirePasswordChange'
        }
    }
    'Set-OpenDscUser'                                = @{
        URI    = '/api/v1/users/{id}'
        Method = 'Patch'
        Body   = @{
            Username = 'username'
            Email    = 'email'
            IsActive = 'isActive'
        }
    }
    'Remove-OpenDscUser'                             = @{
        URI    = '/api/v1/users/{id}'
        Method = 'Delete'
    }
    'Reset-OpenDscUserPassword'                      = @{
        URI    = '/api/v1/users/{id}/reset-password'
        Method = 'Post'
        Body   = @{ NewPassword = 'newPassword' }
    }
    'Unlock-OpenDscUser'                             = @{
        URI    = '/api/v1/users/{id}/unlock'
        Method = 'Post'
    }
    'Set-OpenDscUserRole'                            = @{
        URI    = '/api/v1/users/{id}/roles'
        Method = 'Put'
        Body   = @{ RoleIds = 'roleIds' }
    }

    'Get-OpenDscGroup'                               = @{
        URI    = '/api/v1/groups'
        Method = 'Get'
    }
    'New-OpenDscGroup'                               = @{
        URI    = '/api/v1/groups'
        Method = 'Post'
        Body   = @{
            Name        = 'name'
            Description = 'description'
        }
    }
    'Set-OpenDscGroup'                               = @{
        URI    = '/api/v1/groups/{id}'
        Method = 'Patch'
        Body   = @{
            Name        = 'name'
            Description = 'description'
        }
    }
    'Remove-OpenDscGroup'                            = @{
        URI    = '/api/v1/groups/{id}'
        Method = 'Delete'
    }
    'Get-OpenDscGroupMember'                         = @{
        URI    = '/api/v1/groups/{id}/members'
        Method = 'Get'
    }
    'Set-OpenDscGroupMember'                         = @{
        URI    = '/api/v1/groups/{id}/members'
        Method = 'Put'
        Body   = @{ UserIds = 'userIds' }
    }
    'Set-OpenDscGroupRole'                           = @{
        URI    = '/api/v1/groups/{id}/roles'
        Method = 'Put'
        Body   = @{ RoleIds = 'roleIds' }
    }

    'Get-OpenDscRole'                                = @{
        URI    = '/api/v1/roles'
        Method = 'Get'
    }
    'New-OpenDscRole'                                = @{
        URI    = '/api/v1/roles'
        Method = 'Post'
        Body   = @{
            Name        = 'name'
            Description = 'description'
            Permissions = 'permissions'
        }
    }
    'Set-OpenDscRole'                                = @{
        URI    = '/api/v1/roles/{id}'
        Method = 'Patch'
        Body   = @{
            Name        = 'name'
            Description = 'description'
            Permissions = 'permissions'
        }
    }
    'Remove-OpenDscRole'                             = @{
        URI    = '/api/v1/roles/{id}'
        Method = 'Delete'
    }

    'Get-OpenDscSetting'                             = @{
        URI    = '/api/v1/settings'
        Method = 'Get'
    }
    'Get-OpenDscPublicSetting'                       = @{
        URI    = '/api/v1/settings/public'
        Method = 'Get'
    }
    'Set-OpenDscSetting'                             = @{
        URI    = '/api/v1/settings'
        Method = 'Put'
        Body   = @{
            CertificateRotationInterval = 'certificateRotationInterval'
            StalenessMultiplier         = 'stalenessMultiplier'
        }
    }
    'Get-OpenDscLcmDefault'                          = @{
        URI    = '/api/v1/settings/lcm-defaults'
        Method = 'Get'
    }
    'Set-OpenDscLcmDefault'                          = @{
        URI    = '/api/v1/settings/lcm-defaults'
        Method = 'Put'
        Body   = @{
            DefaultConfigurationMode         = 'defaultConfigurationMode'
            DefaultConfigurationModeInterval = 'defaultConfigurationModeInterval'
            DefaultReportCompliance          = 'defaultReportCompliance'
        }
    }
    'Get-OpenDscValidationSetting'                   = @{
        URI    = '/api/v1/settings/validation'
        Method = 'Get'
    }
    'Set-OpenDscValidationSetting'                   = @{
        URI    = '/api/v1/settings/validation'
        Method = 'Put'
        Body   = @{
            RequireSemVer                    = 'requireSemVer'
            DefaultParameterValidationMode   = 'defaultParameterValidationMode'
            AllowConfigurationOverride       = 'allowConfigurationOverride'
            AllowParameterValidationOverride = 'allowParameterValidationOverride'
        }
    }
    'Get-OpenDscRetentionSetting'                    = @{
        URI    = '/api/v1/settings/retention'
        Method = 'Get'
    }
    'Set-OpenDscRetentionSetting'                    = @{
        URI    = '/api/v1/settings/retention'
        Method = 'Put'
        Body   = @{
            Enabled               = 'enabled'
            KeepVersions          = 'keepVersions'
            KeepDays              = 'keepDays'
            KeepReleaseVersions   = 'keepReleaseVersions'
            ScheduleIntervalHours = 'scheduleIntervalHours'
            ReportKeepCount       = 'reportKeepCount'
            ReportKeepDays        = 'reportKeepDays'
            StatusEventKeepCount  = 'statusEventKeepCount'
            StatusEventKeepDays   = 'statusEventKeepDays'
        }
    }

    'New-OpenDscRegistrationKey'                     = @{
        URI    = '/api/v1/admin/registration-keys'
        Method = 'Post'
        Body   = @{
            Description = 'description'
            ExpiresAt   = 'expiresAt'
            MaxUses     = 'maxUses'
        }
    }
    'Get-OpenDscRegistrationKey'                     = @{
        URI    = '/api/v1/admin/registration-keys'
        Method = 'Get'
    }
    'Set-OpenDscRegistrationKey'                     = @{
        URI    = '/api/v1/admin/registration-keys/{id}'
        Method = 'Put'
        Body   = @{ Description = 'description' }
    }
    'Remove-OpenDscRegistrationKey'                  = @{
        URI    = '/api/v1/admin/registration-keys/{id}'
        Method = 'Delete'
    }

    'Get-OpenDscConfigurationSetting'                = @{
        URI    = '/api/v1/configurations/{name}/settings'
        Method = 'Get'
    }
    'Set-OpenDscConfigurationSetting'                = @{
        URI    = '/api/v1/configurations/{name}/settings'
        Method = 'Put'
        Body   = @{
            RequireSemVer           = 'requireSemVer'
            ParameterValidationMode = 'parameterValidationMode'
        }
    }
    'Remove-OpenDscConfigurationSetting'             = @{
        URI    = '/api/v1/configurations/{name}/settings'
        Method = 'Delete'
    }
    'Get-OpenDscConfigurationRetention'              = @{
        URI    = '/api/v1/configurations/{name}/settings/retention'
        Method = 'Get'
    }
    'Set-OpenDscConfigurationRetention'              = @{
        URI    = '/api/v1/configurations/{name}/settings/retention'
        Method = 'Put'
        Body   = @{
            Enabled             = 'enabled'
            KeepVersions        = 'keepVersions'
            KeepDays            = 'keepDays'
            KeepReleaseVersions = 'keepReleaseVersions'
        }
    }
    'Remove-OpenDscConfigurationRetention'           = @{
        URI    = '/api/v1/configurations/{name}/settings/retention'
        Method = 'Delete'
    }

    'Invoke-OpenDscConfigurationCleanup'             = @{
        URI    = '/api/v1/retention/configurations/cleanup'
        Method = 'Post'
        Body   = @{
            KeepVersions        = 'keepVersions'
            KeepDays            = 'keepDays'
            KeepReleaseVersions = 'keepReleaseVersions'
            DryRun              = 'dryRun'
        }
    }
    'Invoke-OpenDscParameterCleanup'                 = @{
        URI    = '/api/v1/retention/parameters/cleanup'
        Method = 'Post'
        Body   = @{
            KeepVersions        = 'keepVersions'
            KeepDays            = 'keepDays'
            KeepReleaseVersions = 'keepReleaseVersions'
            DryRun              = 'dryRun'
        }
    }
    'Invoke-OpenDscCompositeConfigurationCleanup'    = @{
        URI    = '/api/v1/retention/composite-configurations/cleanup'
        Method = 'Post'
        Body   = @{
            KeepVersions        = 'keepVersions'
            KeepDays            = 'keepDays'
            KeepReleaseVersions = 'keepReleaseVersions'
            DryRun              = 'dryRun'
        }
    }
    'Invoke-OpenDscReportCleanup'                    = @{
        URI    = '/api/v1/retention/reports/cleanup'
        Method = 'Post'
        Body   = @{
            KeepCount = 'keepCount'
            KeepDays  = 'keepDays'
            DryRun    = 'dryRun'
        }
    }
    'Invoke-OpenDscStatusEventCleanup'               = @{
        URI    = '/api/v1/retention/status-events/cleanup'
        Method = 'Post'
        Body   = @{
            KeepCount = 'keepCount'
            KeepDays  = 'keepDays'
            DryRun    = 'dryRun'
        }
    }
    'Get-OpenDscRetentionRun'                        = @{
        URI    = '/api/v1/retention/runs'
        Method = 'Get'
        Query  = @{ Limit = 'limit'; From = 'from'; To = 'to' }
    }

    'Test-OpenDscHealth'                             = @{
        URI    = '/health'
        Method = 'Get'
    }
    'Test-OpenDscReady'                              = @{
        URI    = '/health/ready'
        Method = 'Get'
    }
}

#endregion

#region Private Helper Functions

function Test-OpenDscSession
{
    if ($null -eq $script:Session)
    {
        throw 'No OpenDsc session found. Run Connect-OpenDsc first.'
    }
}

function New-OpenDscUri
{
    param(
        [string]$Endpoint,
        [hashtable]$PathParams = @{}
    )

    $uri = $Endpoint
    foreach ($key in $PathParams.Keys)
    {
        if ($null -ne $PathParams[$key])
        {
            $uri = $uri -replace "\{$key\}", $PathParams[$key]
        }
    }

    $result = '{0}{1}' -f $script:Session.Server.TrimEnd('/'), $uri
    Write-Debug -Message "URI: $result"
    return $result
}

function New-OpenDscQueryString
{
    param(
        [string]$Uri,
        [hashtable]$QueryMapping,
        [hashtable]$BoundParameters
    )

    if (-not $QueryMapping -or $QueryMapping.Count -eq 0) { return $Uri }

    $parts = @()
    foreach ($paramName in $QueryMapping.Keys)
    {
        if ($BoundParameters.ContainsKey($paramName))
        {
            $value = $BoundParameters[$paramName]
            $parts += '{0}={1}' -f $QueryMapping[$paramName], [uri]::EscapeDataString($value)
        }
    }

    if ($parts.Count -gt 0)
    {
        $result = '{0}?{1}' -f $Uri, ($parts -join '&')
        Write-Debug -Message "Query URI: $result"
        return $result
    }

    return $Uri
}

function New-OpenDscBody
{
    param(
        [hashtable]$BodyMapping,
        [hashtable]$BoundParameters
    )

    if (-not $BodyMapping -or $BodyMapping.Count -eq 0) { return $null }

    $body = @{}
    foreach ($paramName in $BodyMapping.Keys)
    {
        if ($BoundParameters.ContainsKey($paramName))
        {
            $body[$BodyMapping[$paramName]] = $BoundParameters[$paramName]
        }
    }

    if ($body.Count -eq 0) { return $null }

    $json = $body | ConvertTo-Json -Depth 10 -Compress
    Write-Debug -Message "Body: $json"
    return $json
}

function Submit-OpenDscRequest
{
    param(
        [string]$Uri,
        [string]$Method,
        [string]$Body,
        [string]$OutFile,
        [string]$ContentType = 'application/json'
    )

    $splat = @{
        Uri         = $Uri
        Method      = $Method
        ContentType = $ContentType
    }

    if ($script:Session.WebSession)
    {
        $splat.WebSession = $script:Session.WebSession
    }
    else
    {
        $splat.Headers = $script:Session.Headers
    }

    if ($Body)
    {
        $splat.Body = $Body
    }

    if ($OutFile)
    {
        $splat.OutFile = $OutFile
    }

    Write-Debug -Message "Submit-OpenDscRequest: $Method $Uri"
    Write-Debug -Message ($splat.GetEnumerator() | Out-String)

    try
    {
        $result = Invoke-RestMethod @splat
        Write-Debug -Message "Raw response content:`n$($result | ConvertTo-Json -Depth 10 -ErrorAction SilentlyContinue)"
        return $result
    }
    catch
    {
        $statusCode = $null
        $errorBody = $null

        if ($_.Exception.Response)
        {
            $statusCode = [int]$_.Exception.Response.StatusCode

            try
            {
                $stream = $_.Exception.Response.GetResponseStream()
                $reader = [System.IO.StreamReader]::new($stream)
                $errorBody = $reader.ReadToEnd() | ConvertFrom-Json
                $reader.Close()
            }
            catch { }
        }

        if ($errorBody)
        {
            $message = if ($errorBody.title) { $errorBody.title } elseif ($errorBody.message) { $errorBody.message } else { $errorBody }
            throw "OpenDsc API error ($statusCode): $message"
        }

        throw $_
    }
}

function Submit-OpenDscMultipartRequest
{
    param(
        [string]$Uri,
        [hashtable]$FormData
    )

    $splat = @{
        Uri    = $Uri
        Method = 'Post'
        Form   = $FormData
    }

    if ($script:Session.WebSession)
    {
        $splat.WebSession = $script:Session.WebSession
    }
    else
    {
        $splat.Headers = $script:Session.Headers
    }

    Write-Debug -Message "Submit-OpenDscMultipartRequest: POST $Uri"
    Write-Debug -Message "Form keys: $($FormData.Keys -join ', ')"
    Write-Debug -Message ($splat.GetEnumerator() | Out-String)

    try
    {
        $result = Invoke-RestMethod @splat
        Write-Debug -Message "Raw response content:`n$($result | ConvertTo-Json -Depth 10 -ErrorAction SilentlyContinue)"
        return $result
    }
    catch
    {
        $statusCode = $null
        $errorBody = $null

        if ($_.Exception.Response)
        {
            $statusCode = [int]$_.Exception.Response.StatusCode

            try
            {
                $stream = $_.Exception.Response.GetResponseStream()
                $reader = [System.IO.StreamReader]::new($stream)
                $errorBody = $reader.ReadToEnd() | ConvertFrom-Json
                $reader.Close()
            }
            catch { }
        }

        if ($errorBody)
        {
            $message = if ($errorBody.title) { $errorBody.title } elseif ($errorBody.message) { $errorBody.message } else { $errorBody }
            throw "OpenDsc API error ($statusCode): $message"
        }

        throw $_
    }
}

#endregion

#region Connection & Auth

<#
    .SYNOPSIS
        Connects to an OpenDsc Pull Server.

    .DESCRIPTION
        Establishes a session to an OpenDsc Pull Server using a personal access token, API key,
        or username/password credential. The session is stored in module state and used by all
        subsequent commands.

    .PARAMETER Server
        The base URL of the OpenDsc Pull Server (e.g. https://dsc-server.example.com).

    .PARAMETER Token
        A personal access token (PAT) for bearer authentication.

    .PARAMETER Credential
        A PSCredential object for username/password authentication.

    .PARAMETER ApiKey
        An API key for X-API-Key header authentication.

    .EXAMPLE
        Connect-OpenDsc -Server 'https://dsc.example.com' -Token 'my-pat'

    .EXAMPLE
        Connect-OpenDsc -Server 'https://dsc.example.com' -Credential (Get-Credential)
#>
function Connect-OpenDsc
{
    [CmdletBinding(DefaultParameterSetName = 'Token')]
    param(
        [Parameter(Mandatory)]
        [string]$Server,

        [Parameter(ParameterSetName = 'Token', Mandatory)]
        [string]$Token,

        [Parameter(ParameterSetName = 'Credential', Mandatory)]
        [pscredential]$Credential,

        [Parameter(ParameterSetName = 'ApiKey', Mandatory)]
        [string]$ApiKey
    )
    process
    {
        $server = $Server.TrimEnd('/')
        $headers = @{}

        switch ($PSCmdlet.ParameterSetName)
        {
            'Token'
            {
                $headers['Authorization'] = "Bearer $Token"
            }
            'ApiKey'
            {
                $headers['X-API-Key'] = $ApiKey
            }
            'Credential'
            {
                $loginBody = @{
                    username = $Credential.UserName
                    password = $Credential.GetNetworkCredential().Password
                } | ConvertTo-Json -Compress

                $loginResult = Invoke-RestMethod -Uri "$server/api/v1/auth/login" `
                    -Method Post -Body $loginBody -ContentType 'application/json' `
                    -SessionVariable webSession
            }
        }

        $script:Session = [PSCustomObject]@{
            Server     = $server
            Headers    = $headers
            AuthType   = $PSCmdlet.ParameterSetName
            WebSession = if ($PSCmdlet.ParameterSetName -eq 'Credential') { $webSession } else { $null }
        }

        Write-Verbose "Connected to OpenDsc server at $server using $($PSCmdlet.ParameterSetName) authentication."
        $script:Session
    }
}

<#
    .SYNOPSIS
        Disconnects from the OpenDsc Pull Server.

    .DESCRIPTION
        Terminates the current OpenDsc session. If the session was established with credentials,
        the server-side logout endpoint is called before clearing local state.
#>
function Disconnect-OpenDsc
{
    [CmdletBinding()]
    param()
    process
    {
        if ($script:Session -and $script:Session.AuthType -eq 'Credential')
        {
            try
            {
                $r = $script:ApiData[$MyInvocation.MyCommand.Name]
                $uri = New-OpenDscUri -Endpoint $r.URI
                Submit-OpenDscRequest -Uri $uri -Method $r.Method
            }
            catch { }
        }
        $script:Session = $null
        Write-Verbose 'Disconnected from OpenDsc server.'
    }
}

<#
    .SYNOPSIS
        Gets the currently authenticated user.

    .DESCRIPTION
        Returns the user profile associated with the active OpenDsc session.
#>
function Get-OpenDscCurrentUser
{
    [CmdletBinding()]
    param()
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Changes the password for the currently authenticated user.

    .PARAMETER CurrentPassword
        The current password.

    .PARAMETER NewPassword
        The new password.
#>
function Set-OpenDscPassword
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$CurrentPassword,

        [Parameter(Mandatory)]
        [string]$NewPassword
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Creates a new personal access token.

    .PARAMETER Name
        A display name for the token.

    .PARAMETER Scopes
        The permission scopes to grant the token.

    .PARAMETER ExpiresAt
        The expiration date for the token.
#>
function New-OpenDscToken
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [ValidateSet(
            'server.settings.read',
            'server.settings.write',
            'users.manage',
            'groups.manage',
            'roles.manage',
            'registration-keys.manage',
            'nodes.read',
            'nodes.write',
            'nodes.delete',
            'nodes.assign-configuration',
            'reports.read',
            'reports.read-all',
            'retention.manage',
            'configurations.admin-override',
            'composite-configurations.admin-override',
            'parameters.admin-override',
            'scopes.admin-override'
        )]
        [string[]]$Scopes,

        [datetime]$ExpiresAt
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Lists all personal access tokens for the current user.
#>
function Get-OpenDscToken
{
    [CmdletBinding()]
    param()
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Revokes a personal access token.

    .PARAMETER Id
        The unique identifier of the token to revoke.
#>
function Remove-OpenDscToken
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ id = $Id }
        if ($PSCmdlet.ShouldProcess($Id, 'Revoke token'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

#endregion

#region Nodes

<#
    .SYNOPSIS
        Gets one or all registered nodes.

    .PARAMETER Id
        The unique identifier of a specific node. When omitted, all nodes are returned.

    .EXAMPLE
        Get-OpenDscNode

    .EXAMPLE
        Get-OpenDscNode -Id 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'
#>
function Get-OpenDscNode
{
    [CmdletBinding(DefaultParameterSetName = 'Query')]
    param(
        [Parameter(ParameterSetName = 'ID', Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        if ($PSCmdlet.ParameterSetName -eq 'ID')
        {
            $uri = New-OpenDscUri "$($r.URI)/$Id"
        }
        else
        {
            $uri = New-OpenDscUri $r.URI
        }
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Registers a new node with the Pull Server.

    .PARAMETER Fqdn
        The fully qualified domain name of the node.

    .PARAMETER RegistrationKey
        The shared registration key for initial enrollment.

    .PARAMETER ConfigurationSource
        The configuration source type (Local or Pull).

    .PARAMETER ConfigurationMode
        The configuration mode (Monitor or Remediate).

    .PARAMETER ConfigurationModeInterval
        The interval between configuration checks.

    .PARAMETER ReportCompliance
        Whether the node should report compliance status.
#>
function Register-OpenDscNode
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Fqdn,

        [Parameter(Mandatory)]
        [string]$RegistrationKey,

        [string]$ConfigurationSource,
        [string]$ConfigurationMode,
        [timespan]$ConfigurationModeInterval,
        [bool]$ReportCompliance
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes a registered node.

    .PARAMETER Id
        The unique identifier of the node to remove.
#>
function Remove-OpenDscNode
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ nodeId = $Id }
        if ($PSCmdlet.ShouldProcess($Id, 'Remove node'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Gets the configuration assignment for a node.

    .PARAMETER Id
        The unique identifier of the node.
#>
function Get-OpenDscNodeConfiguration
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ nodeId = $Id }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Assigns a configuration to a node.

    .PARAMETER Id
        The unique identifier of the node.

    .PARAMETER ConfigurationName
        The name of the configuration to assign.

    .PARAMETER IsComposite
        Whether the configuration is a composite configuration.

    .PARAMETER MajorVersion
        The major version constraint for the configuration.

    .PARAMETER PrereleaseChannel
        The prerelease channel to pin the node to.
#>
function Set-OpenDscNodeConfiguration
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id,

        [Parameter(Mandatory)]
        [string]$ConfigurationName,

        [switch]$IsComposite,
        [int]$MajorVersion,
        [string]$PrereleaseChannel
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -ne 'Id') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ nodeId = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes the configuration assignment from a node.

    .PARAMETER Id
        The unique identifier of the node.
#>
function Remove-OpenDscNodeConfiguration
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ nodeId = $Id }
        if ($PSCmdlet.ShouldProcess($Id, 'Remove node configuration assignment'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Gets the configuration checksum for a node.

    .PARAMETER Id
        The unique identifier of the node.
#>
function Get-OpenDscNodeConfigurationChecksum
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ nodeId = $Id }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Downloads the configuration bundle for a node.

    .PARAMETER Id
        The unique identifier of the node.

    .PARAMETER OutFile
        The local file path to save the bundle to.
#>
function Save-OpenDscNodeConfigurationBundle
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id,

        [Parameter(Mandatory)]
        [string]$OutFile
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ nodeId = $Id }) -Method $r.Method -OutFile $OutFile
    }
}

<#
    .SYNOPSIS
        Updates the desired LCM configuration for a node.

    .PARAMETER Id
        The unique identifier of the node.

    .PARAMETER ConfigurationMode
        The configuration mode (Monitor or Remediate).

    .PARAMETER ConfigurationModeInterval
        The interval between configuration checks.

    .PARAMETER ReportCompliance
        Whether the node should report compliance status.
#>
function Set-OpenDscNodeLcmConfig
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id,

        [string]$ConfigurationMode,
        [timespan]$ConfigurationModeInterval,
        [bool]$ReportCompliance
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -ne 'Id') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ nodeId = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Gets the desired LCM configuration for a node.

    .PARAMETER Id
        The unique identifier of the node.
#>
function Get-OpenDscNodeLcmConfig
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ nodeId = $Id }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Updates the LCM status reported by a node.

    .PARAMETER Id
        The unique identifier of the node.

    .PARAMETER LcmStatus
        The current LCM status string.
#>
function Set-OpenDscNodeLcmStatus
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id,

        [Parameter(Mandatory)]
        [string]$LcmStatus
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body @{ LcmStatus = $LcmStatus }
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ nodeId = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Updates the reported configuration state for a node.

    .PARAMETER Id
        The unique identifier of the node.

    .PARAMETER ConfigurationMode
        The reported configuration mode.

    .PARAMETER ConfigurationModeInterval
        The reported configuration check interval.

    .PARAMETER ReportCompliance
        The reported compliance reporting setting.
#>
function Set-OpenDscNodeReportedConfig
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id,

        [string]$ConfigurationMode,
        [timespan]$ConfigurationModeInterval,
        [bool]$ReportCompliance
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -ne 'Id') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ nodeId = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Gets the status history events for a node.

    .PARAMETER Id
        The unique identifier of the node.
#>
function Get-OpenDscNodeStatusHistory
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ nodeId = $Id }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Gets the scope value tags assigned to a node.

    .PARAMETER Id
        The unique identifier of the node.
#>
function Get-OpenDscNodeTag
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ nodeId = $Id }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Adds a scope value tag to a node.

    .PARAMETER Id
        The unique identifier of the node.

    .PARAMETER ScopeValueId
        The unique identifier of the scope value to tag.
#>
function Add-OpenDscNodeTag
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id,

        [Parameter(Mandatory)]
        [guid]$ScopeValueId
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body @{ ScopeValueId = $ScopeValueId }
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ nodeId = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes a scope value tag from a node.

    .PARAMETER Id
        The unique identifier of the node.

    .PARAMETER ScopeValueId
        The unique identifier of the scope value tag to remove.
#>
function Remove-OpenDscNodeTag
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id,

        [Parameter(Mandatory)]
        [guid]$ScopeValueId
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ nodeId = $Id; scopeValueId = $ScopeValueId }
        if ($PSCmdlet.ShouldProcess("$ScopeValueId on node $Id", 'Remove tag'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Gets the parameter provenance for a node showing which scope each parameter originates from.

    .PARAMETER Id
        The unique identifier of the node.

    .PARAMETER ConfigurationId
        Optional configuration identifier to filter results.
#>
function Get-OpenDscNodeParameterProvenance
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id,

        [guid]$ConfigurationId
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ nodeId = $Id }
        $uri = New-OpenDscQueryString $uri $r.Query $PSBoundParameters
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Gets the resolved parameters for a node after scope merging.

    .PARAMETER Id
        The unique identifier of the node.

    .PARAMETER ConfigurationId
        Optional configuration identifier to filter results.
#>
function Get-OpenDscNodeParameterResolution
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id,

        [guid]$ConfigurationId
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ nodeId = $Id }
        $uri = New-OpenDscQueryString $uri $r.Query $PSBoundParameters
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

#endregion

#region Configurations

<#
    .SYNOPSIS
        Gets one or all configurations.

    .PARAMETER Name
        The name of a specific configuration. When omitted, all configurations are returned.

    .EXAMPLE
        Get-OpenDscConfiguration

    .EXAMPLE
        Get-OpenDscConfiguration -Name 'my-app'
#>
function Get-OpenDscConfiguration
{
    [CmdletBinding(DefaultParameterSetName = 'Query')]
    param(
        [Parameter(ParameterSetName = 'Name', Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        if ($PSCmdlet.ParameterSetName -eq 'Name')
        {
            $uri = New-OpenDscUri "$($r.URI)/$Name"
        }
        else
        {
            $uri = New-OpenDscUri $r.URI
        }
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Creates a new configuration by uploading DSC configuration files.

    .PARAMETER Name
        The name for the new configuration.

    .PARAMETER Description
        An optional description.

    .PARAMETER EntryPoint
        The entry point file within the uploaded configuration files.

    .PARAMETER Version
        The initial version string.

    .PARAMETER UseServerManagedParameters
        Whether server-managed parameters are enabled for this configuration.

    .PARAMETER FilePath
        One or more local file paths to upload as the configuration content.
#>
function New-OpenDscConfiguration
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [string]$Description,
        [string]$EntryPoint,
        [string]$Version,
        [bool]$UseServerManagedParameters,

        [Parameter(Mandatory)]
        [string[]]$FilePath
    )
    process
    {
        Test-OpenDscSession
        $form = @{ name = $Name }
        if ($Description) { $form.description = $Description }
        if ($EntryPoint) { $form.entryPoint = $EntryPoint }
        if ($Version) { $form.version = $Version }
        if ($PSBoundParameters.ContainsKey('UseServerManagedParameters'))
        {
            $form.useServerManagedParameters = $UseServerManagedParameters
        }
        $fileItems = foreach ($fp in $FilePath) { Get-Item $fp }
        $form.files = $fileItems
        $uri = New-OpenDscUri '/api/v1/configurations'
        Submit-OpenDscMultipartRequest -Uri $uri -FormData $form
    }
}

<#
    .SYNOPSIS
        Updates configuration metadata.

    .PARAMETER Name
        The name of the configuration.

    .PARAMETER Description
        The updated description.

    .PARAMETER UseServerManagedParameters
        Whether server-managed parameters are enabled.
#>
function Set-OpenDscConfiguration
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name,

        [string]$Description,
        [bool]$UseServerManagedParameters
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -ne 'Name') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes a configuration and all its versions.

    .PARAMETER Name
        The name of the configuration to remove.
#>
function Remove-OpenDscConfiguration
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ name = $Name }
        if ($PSCmdlet.ShouldProcess($Name, 'Remove configuration'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Lists all versions of a configuration.

    .PARAMETER Name
        The name of the configuration.
#>
function Get-OpenDscConfigurationVersion
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Creates a new version of a configuration by uploading files.

    .PARAMETER Name
        The name of the configuration.

    .PARAMETER Version
        The version string for the new version.

    .PARAMETER EntryPoint
        The entry point file within the uploaded files.

    .PARAMETER PrereleaseChannel
        An optional prerelease channel label.

    .PARAMETER FilePath
        One or more local file paths to upload.
#>
function New-OpenDscConfigurationVersion
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Version,

        [string]$EntryPoint,
        [string]$PrereleaseChannel,

        [Parameter(Mandatory)]
        [string[]]$FilePath
    )
    process
    {
        Test-OpenDscSession
        $form = @{ version = $Version }
        if ($EntryPoint) { $form.entryPoint = $EntryPoint }
        if ($PrereleaseChannel) { $form.prereleaseChannel = $PrereleaseChannel }
        $fileItems = foreach ($fp in $FilePath) { Get-Item $fp }
        $form.files = $fileItems
        $uri = New-OpenDscUri "/api/v1/configurations/$Name/versions"
        Submit-OpenDscMultipartRequest -Uri $uri -FormData $form
    }
}

<#
    .SYNOPSIS
        Publishes a draft configuration version, making it available to nodes.

    .PARAMETER Name
        The name of the configuration.

    .PARAMETER Version
        The version string to publish.
#>
function Publish-OpenDscConfigurationVersion
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Version
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name; version = $Version }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Removes a specific version of a configuration.

    .PARAMETER Name
        The name of the configuration.

    .PARAMETER Version
        The version string to remove.
#>
function Remove-OpenDscConfigurationVersion
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Version
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ name = $Name; version = $Version }
        if ($PSCmdlet.ShouldProcess("$Name v$Version", 'Remove configuration version'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Gets the access permissions for a configuration.

    .PARAMETER Name
        The name of the configuration.
#>
function Get-OpenDscConfigurationPermission
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Sets an access permission on a configuration.

    .PARAMETER Name
        The name of the configuration.

    .PARAMETER PrincipalType
        The type of principal (User or Group).

    .PARAMETER PrincipalId
        The unique identifier of the principal.

    .PARAMETER Level
        The permission level to grant.
#>
function Set-OpenDscConfigurationPermission
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$PrincipalType,

        [Parameter(Mandatory)]
        [guid]$PrincipalId,

        [Parameter(Mandatory)]
        [string]$Level
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body @{
            PrincipalType = $PrincipalType
            PrincipalId   = $PrincipalId
            Level         = $Level
        }
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes an access permission from a configuration.

    .PARAMETER Name
        The name of the configuration.

    .PARAMETER PrincipalType
        The type of principal.

    .PARAMETER PrincipalId
        The unique identifier of the principal.
#>
function Remove-OpenDscConfigurationPermission
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$PrincipalType,

        [Parameter(Mandatory)]
        [guid]$PrincipalId
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ name = $Name; principalType = $PrincipalType; principalId = $PrincipalId }
        if ($PSCmdlet.ShouldProcess("$PrincipalType/$PrincipalId on $Name", 'Remove permission'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

#endregion

#region Composite Configurations

<#
    .SYNOPSIS
        Gets one or all composite configurations.

    .PARAMETER Name
        The name of a specific composite configuration. When omitted, all are returned.
#>
function Get-OpenDscCompositeConfiguration
{
    [CmdletBinding(DefaultParameterSetName = 'Query')]
    param(
        [Parameter(ParameterSetName = 'Name', Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        if ($PSCmdlet.ParameterSetName -eq 'Name')
        {
            $uri = New-OpenDscUri "$($r.URI)/$Name"
        }
        else
        {
            $uri = New-OpenDscUri $r.URI
        }
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Creates a new composite configuration.

    .PARAMETER Name
        The name for the composite configuration.

    .PARAMETER Description
        An optional description.

    .PARAMETER EntryPoint
        The entry point configuration name.
#>
function New-OpenDscCompositeConfiguration
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [string]$Description,
        [string]$EntryPoint
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes a composite configuration and all its versions.

    .PARAMETER Name
        The name of the composite configuration to remove.
#>
function Remove-OpenDscCompositeConfiguration
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ name = $Name }
        if ($PSCmdlet.ShouldProcess($Name, 'Remove composite configuration'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Lists all versions of a composite configuration.

    .PARAMETER Name
        The name of the composite configuration.
#>
function Get-OpenDscCompositeConfigurationVersion
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Creates a new version of a composite configuration.

    .PARAMETER Name
        The name of the composite configuration.

    .PARAMETER Version
        The version string.

    .PARAMETER PrereleaseChannel
        An optional prerelease channel label.
#>
function New-OpenDscCompositeConfigurationVersion
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Version,

        [string]$PrereleaseChannel
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -ne 'Name') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Publishes a draft composite configuration version.

    .PARAMETER Name
        The name of the composite configuration.

    .PARAMETER Version
        The version string to publish.
#>
function Publish-OpenDscCompositeConfigurationVersion
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Version
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name; version = $Version }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Removes a specific version of a composite configuration.

    .PARAMETER Name
        The name of the composite configuration.

    .PARAMETER Version
        The version string to remove.
#>
function Remove-OpenDscCompositeConfigurationVersion
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Version
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ name = $Name; version = $Version }
        if ($PSCmdlet.ShouldProcess("$Name v$Version", 'Remove composite configuration version'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Adds a child configuration to a composite configuration version.

    .PARAMETER Name
        The name of the composite configuration.

    .PARAMETER Version
        The version string.

    .PARAMETER ChildConfigurationName
        The name of the child configuration to add.

    .PARAMETER ActiveVersion
        The active version of the child configuration.

    .PARAMETER Order
        The sort order of the child within the composite.
#>
function Add-OpenDscCompositeConfigurationChild
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Version,

        [Parameter(Mandatory)]
        [string]$ChildConfigurationName,

        [string]$ActiveVersion,
        [int]$Order
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -notin 'Name', 'Version') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name; version = $Version }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Updates a child configuration entry in a composite configuration version.

    .PARAMETER Name
        The name of the composite configuration.

    .PARAMETER Version
        The version string.

    .PARAMETER ChildId
        The unique identifier of the child entry.

    .PARAMETER ActiveVersion
        The active version of the child configuration.

    .PARAMETER Order
        The sort order of the child.
#>
function Set-OpenDscCompositeConfigurationChild
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Version,

        [Parameter(Mandatory)]
        [guid]$ChildId,

        [string]$ActiveVersion,
        [int]$Order
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -notin 'Name', 'Version', 'ChildId') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name; version = $Version; childId = $ChildId }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes a child configuration from a composite configuration version.

    .PARAMETER Name
        The name of the composite configuration.

    .PARAMETER Version
        The version string.

    .PARAMETER ChildId
        The unique identifier of the child entry to remove.
#>
function Remove-OpenDscCompositeConfigurationChild
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Version,

        [Parameter(Mandatory)]
        [guid]$ChildId
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ name = $Name; version = $Version; childId = $ChildId }
        if ($PSCmdlet.ShouldProcess("$ChildId from $Name v$Version", 'Remove child configuration'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Gets the access permissions for a composite configuration.

    .PARAMETER Name
        The name of the composite configuration.
#>
function Get-OpenDscCompositeConfigurationPermission
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Sets an access permission on a composite configuration.

    .PARAMETER Name
        The name of the composite configuration.

    .PARAMETER PrincipalType
        The type of principal (User or Group).

    .PARAMETER PrincipalId
        The unique identifier of the principal.

    .PARAMETER Level
        The permission level to grant.
#>
function Set-OpenDscCompositeConfigurationPermission
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$PrincipalType,

        [Parameter(Mandatory)]
        [guid]$PrincipalId,

        [Parameter(Mandatory)]
        [string]$Level
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body @{
            PrincipalType = $PrincipalType
            PrincipalId   = $PrincipalId
            Level         = $Level
        }
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes an access permission from a composite configuration.

    .PARAMETER Name
        The name of the composite configuration.

    .PARAMETER PrincipalType
        The type of principal.

    .PARAMETER PrincipalId
        The unique identifier of the principal.
#>
function Remove-OpenDscCompositeConfigurationPermission
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$PrincipalType,

        [Parameter(Mandatory)]
        [guid]$PrincipalId
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ name = $Name; principalType = $PrincipalType; principalId = $PrincipalId }
        if ($PSCmdlet.ShouldProcess("$PrincipalType/$PrincipalId on $Name", 'Remove permission'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

#endregion

#region Parameters

<#
    .SYNOPSIS
        Creates or updates a parameter file for a scope type and configuration.

    .PARAMETER ScopeTypeId
        The unique identifier of the scope type.

    .PARAMETER ConfigurationId
        The unique identifier of the configuration.

    .PARAMETER Version
        The version string for the parameter file.

    .PARAMETER ScopeValue
        The scope value this parameter file applies to.

    .PARAMETER Content
        The parameter content as a JSON string.

    .PARAMETER ContentType
        The content type of the parameter data.

    .PARAMETER IsPassthrough
        Whether the parameter should be passed through without schema validation.
#>
function Set-OpenDscParameter
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [guid]$ScopeTypeId,

        [Parameter(Mandatory)]
        [guid]$ConfigurationId,

        [Parameter(Mandatory)]
        [string]$Version,

        [string]$ScopeValue,
        [string]$Content,
        [string]$ContentType,
        [bool]$IsPassthrough
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -notin 'ScopeTypeId', 'ConfigurationId') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ scopeTypeId = $ScopeTypeId; configurationId = $ConfigurationId }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Lists parameter versions for a scope type and configuration.

    .PARAMETER ScopeTypeId
        The unique identifier of the scope type.

    .PARAMETER ConfigurationId
        The unique identifier of the configuration.

    .PARAMETER ScopeValue
        Optional scope value to filter by.
#>
function Get-OpenDscParameterVersion
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [guid]$ScopeTypeId,

        [Parameter(Mandatory)]
        [guid]$ConfigurationId,

        [string]$ScopeValue
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ scopeTypeId = $ScopeTypeId; configurationId = $ConfigurationId }
        $uri = New-OpenDscQueryString $uri $r.Query $PSBoundParameters
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Publishes a draft parameter version.

    .PARAMETER ScopeTypeId
        The unique identifier of the scope type.

    .PARAMETER ConfigurationId
        The unique identifier of the configuration.

    .PARAMETER Version
        The version string to publish.

    .PARAMETER ScopeValue
        The scope value the parameter applies to.
#>
function Publish-OpenDscParameterVersion
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [guid]$ScopeTypeId,

        [Parameter(Mandatory)]
        [guid]$ConfigurationId,

        [Parameter(Mandatory)]
        [string]$Version,

        [string]$ScopeValue
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ scopeTypeId = $ScopeTypeId; configurationId = $ConfigurationId; version = $Version }
        $uri = New-OpenDscQueryString $uri $r.Query $PSBoundParameters
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Removes a specific parameter version.

    .PARAMETER ScopeTypeId
        The unique identifier of the scope type.

    .PARAMETER ConfigurationId
        The unique identifier of the configuration.

    .PARAMETER Version
        The version string to remove.

    .PARAMETER ScopeValue
        The scope value the parameter applies to.
#>
function Remove-OpenDscParameterVersion
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [guid]$ScopeTypeId,

        [Parameter(Mandatory)]
        [guid]$ConfigurationId,

        [Parameter(Mandatory)]
        [string]$Version,

        [string]$ScopeValue
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ scopeTypeId = $ScopeTypeId; configurationId = $ConfigurationId; version = $Version }
        $uri = New-OpenDscQueryString $uri $r.Query $PSBoundParameters
        if ($PSCmdlet.ShouldProcess("v$Version", 'Remove parameter version'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Lists the major versions available for a parameter.

    .PARAMETER ScopeTypeId
        The unique identifier of the scope type.

    .PARAMETER ConfigurationId
        The unique identifier of the configuration.
#>
function Get-OpenDscParameterMajorVersion
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [guid]$ScopeTypeId,

        [Parameter(Mandatory)]
        [guid]$ConfigurationId
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ scopeTypeId = $ScopeTypeId; configurationId = $ConfigurationId }) -Method $r.Method
    }
}

#endregion

#region Scope Types

<#
    .SYNOPSIS
        Gets one or all scope types.

    .PARAMETER Id
        The unique identifier of a specific scope type. When omitted, all are returned.
#>
function Get-OpenDscScopeType
{
    [CmdletBinding(DefaultParameterSetName = 'Query')]
    param(
        [Parameter(ParameterSetName = 'ID', Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        if ($PSCmdlet.ParameterSetName -eq 'ID')
        {
            $uri = New-OpenDscUri "$($r.URI)/$Id"
        }
        else
        {
            $uri = New-OpenDscUri $r.URI
        }
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Creates a new scope type.

    .PARAMETER Name
        The name of the scope type.

    .PARAMETER Description
        An optional description.

    .PARAMETER ValueMode
        The value mode for the scope type.
#>
function New-OpenDscScopeType
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [string]$Description,
        [string]$ValueMode
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Updates the description of a scope type.

    .PARAMETER Id
        The unique identifier of the scope type.

    .PARAMETER Description
        The new description.
#>
function Set-OpenDscScopeType
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id,

        [Parameter(Mandatory)]
        [string]$Description
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body @{ Description = $Description }
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes a scope type.

    .PARAMETER Id
        The unique identifier of the scope type to remove.
#>
function Remove-OpenDscScopeType
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ id = $Id }
        if ($PSCmdlet.ShouldProcess($Id, 'Remove scope type'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Sets the precedence order of scope types for parameter merging.

    .PARAMETER ScopeTypeIds
        An ordered array of scope type identifiers defining the merge precedence.
#>
function Set-OpenDscScopeTypeOrder
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [guid[]]$ScopeTypeIds
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Enables a scope type.

    .PARAMETER Id
        The unique identifier of the scope type to enable.
#>
function Enable-OpenDscScopeType
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Disables a scope type.

    .PARAMETER Id
        The unique identifier of the scope type to disable.
#>
function Disable-OpenDscScopeType
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method
    }
}

#endregion

#region Scope Values

<#
    .SYNOPSIS
        Gets one or all scope values for a scope type.

    .PARAMETER ScopeTypeId
        The unique identifier of the parent scope type.

    .PARAMETER Id
        The unique identifier of a specific scope value. When omitted, all values are returned.
#>
function Get-OpenDscScopeValue
{
    [CmdletBinding(DefaultParameterSetName = 'Query')]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$ScopeTypeId,

        [Parameter(ParameterSetName = 'ID', Mandatory)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        if ($PSCmdlet.ParameterSetName -eq 'ID')
        {
            $uri = New-OpenDscUri "$($r.URI)/$Id" @{ scopeTypeId = $ScopeTypeId }
        }
        else
        {
            $uri = New-OpenDscUri $r.URI @{ scopeTypeId = $ScopeTypeId }
        }
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Creates a new scope value under a scope type.

    .PARAMETER ScopeTypeId
        The unique identifier of the parent scope type.

    .PARAMETER Value
        The value string.

    .PARAMETER Description
        An optional description.
#>
function New-OpenDscScopeValue
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [guid]$ScopeTypeId,

        [Parameter(Mandatory)]
        [string]$Value,

        [string]$Description
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body @{ Value = $Value; Description = $Description }
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ scopeTypeId = $ScopeTypeId }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Updates the description of a scope value.

    .PARAMETER ScopeTypeId
        The unique identifier of the parent scope type.

    .PARAMETER Id
        The unique identifier of the scope value.

    .PARAMETER Description
        The new description.
#>
function Set-OpenDscScopeValue
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [guid]$ScopeTypeId,

        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id,

        [Parameter(Mandatory)]
        [string]$Description
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body @{ Description = $Description }
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ scopeTypeId = $ScopeTypeId; id = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes a scope value.

    .PARAMETER ScopeTypeId
        The unique identifier of the parent scope type.

    .PARAMETER Id
        The unique identifier of the scope value to remove.
#>
function Remove-OpenDscScopeValue
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [guid]$ScopeTypeId,

        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ scopeTypeId = $ScopeTypeId; id = $Id }
        if ($PSCmdlet.ShouldProcess($Id, 'Remove scope value'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

#endregion

#region Reports

<#
    .SYNOPSIS
        Gets one or all compliance reports.

    .PARAMETER Id
        The unique identifier of a specific report. When omitted, reports are listed.

    .PARAMETER Skip
        Number of records to skip for pagination.

    .PARAMETER Take
        Number of records to return.

    .PARAMETER From
        Start date filter.

    .PARAMETER To
        End date filter.
#>
function Get-OpenDscReport
{
    [CmdletBinding(DefaultParameterSetName = 'Query')]
    param(
        [Parameter(ParameterSetName = 'ID', Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id,

        [Parameter(ParameterSetName = 'Query')]
        [int]$Skip,

        [Parameter(ParameterSetName = 'Query')]
        [int]$Take,

        [Parameter(ParameterSetName = 'Query')]
        [datetime]$From,

        [Parameter(ParameterSetName = 'Query')]
        [datetime]$To
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        if ($PSCmdlet.ParameterSetName -eq 'ID')
        {
            $uri = New-OpenDscUri "$($r.URI)/$Id"
        }
        else
        {
            $uri = New-OpenDscUri $r.URI
            $uri = New-OpenDscQueryString $uri $r.Query $PSBoundParameters
        }
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Gets compliance reports for a specific node.

    .PARAMETER Id
        The unique identifier of the node.

    .PARAMETER Skip
        Number of records to skip for pagination.

    .PARAMETER Take
        Number of records to return.

    .PARAMETER From
        Start date filter.

    .PARAMETER To
        End date filter.
#>
function Get-OpenDscNodeReport
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [Alias('NodeId')]
        [guid]$Id,

        [int]$Skip,
        [int]$Take,
        [datetime]$From,
        [datetime]$To
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ nodeId = $Id }
        $uri = New-OpenDscQueryString $uri $r.Query $PSBoundParameters
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

#endregion

#region Users

<#
    .SYNOPSIS
        Gets one or all users.

    .PARAMETER Id
        The unique identifier of a specific user. When omitted, all users are returned.
#>
function Get-OpenDscUser
{
    [CmdletBinding(DefaultParameterSetName = 'Query')]
    param(
        [Parameter(ParameterSetName = 'ID', Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        if ($PSCmdlet.ParameterSetName -eq 'ID')
        {
            $uri = New-OpenDscUri "$($r.URI)/$Id"
        }
        else
        {
            $uri = New-OpenDscUri $r.URI
        }
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Creates a new user account.

    .PARAMETER Username
        The username for the new account.

    .PARAMETER Email
        The email address.

    .PARAMETER Password
        The initial password.

    .PARAMETER AccountType
        The account type (default: User).

    .PARAMETER RequirePasswordChange
        Whether the user must change their password on first login.
#>
function New-OpenDscUser
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Username,

        [Parameter(Mandatory)]
        [string]$Email,

        [Parameter(Mandatory)]
        [string]$Password,

        [string]$AccountType = 'User',
        [bool]$RequirePasswordChange = $true
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Updates a user account.

    .PARAMETER Id
        The unique identifier of the user.

    .PARAMETER Username
        The new username.

    .PARAMETER Email
        The new email address.

    .PARAMETER IsActive
        Whether the account is active.
#>
function Set-OpenDscUser
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id,

        [string]$Username,
        [string]$Email,
        [bool]$IsActive
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -ne 'Id') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes a user account.

    .PARAMETER Id
        The unique identifier of the user to remove.
#>
function Remove-OpenDscUser
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ id = $Id }
        if ($PSCmdlet.ShouldProcess($Id, 'Remove user'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Resets a user's password.

    .PARAMETER Id
        The unique identifier of the user.

    .PARAMETER NewPassword
        The new password to set.
#>
function Reset-OpenDscUserPassword
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id,

        [Parameter(Mandatory)]
        [string]$NewPassword
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body @{ NewPassword = $NewPassword }
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Unlocks a locked user account.

    .PARAMETER Id
        The unique identifier of the user to unlock.
#>
function Unlock-OpenDscUser
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Sets the role assignments for a user.

    .PARAMETER Id
        The unique identifier of the user.

    .PARAMETER RoleIds
        An array of role identifiers to assign.
#>
function Set-OpenDscUserRole
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id,

        [Parameter(Mandatory)]
        [guid[]]$RoleIds
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body @{ RoleIds = $RoleIds }
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method -Body $body
    }
}

#endregion

#region Groups

<#
    .SYNOPSIS
        Gets one or all groups.

    .PARAMETER Id
        The unique identifier of a specific group. When omitted, all groups are returned.
#>
function Get-OpenDscGroup
{
    [CmdletBinding(DefaultParameterSetName = 'Query')]
    param(
        [Parameter(ParameterSetName = 'ID', Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        if ($PSCmdlet.ParameterSetName -eq 'ID')
        {
            $uri = New-OpenDscUri "$($r.URI)/$Id"
        }
        else
        {
            $uri = New-OpenDscUri $r.URI
        }
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Creates a new group.

    .PARAMETER Name
        The name of the group.

    .PARAMETER Description
        An optional description.
#>
function New-OpenDscGroup
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [string]$Description
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Updates a group.

    .PARAMETER Id
        The unique identifier of the group.

    .PARAMETER Name
        The new name.

    .PARAMETER Description
        The new description.
#>
function Set-OpenDscGroup
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id,

        [string]$Name,
        [string]$Description
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -ne 'Id') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes a group.

    .PARAMETER Id
        The unique identifier of the group to remove.
#>
function Remove-OpenDscGroup
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ id = $Id }
        if ($PSCmdlet.ShouldProcess($Id, 'Remove group'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Gets the members of a group.

    .PARAMETER Id
        The unique identifier of the group.
#>
function Get-OpenDscGroupMember
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Sets the member list for a group.

    .PARAMETER Id
        The unique identifier of the group.

    .PARAMETER UserIds
        An array of user identifiers to set as members.
#>
function Set-OpenDscGroupMember
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id,

        [Parameter(Mandatory)]
        [guid[]]$UserIds
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body @{ UserIds = $UserIds }
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Sets the role assignments for a group.

    .PARAMETER Id
        The unique identifier of the group.

    .PARAMETER RoleIds
        An array of role identifiers to assign.
#>
function Set-OpenDscGroupRole
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id,

        [Parameter(Mandatory)]
        [guid[]]$RoleIds
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body @{ RoleIds = $RoleIds }
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method -Body $body
    }
}

#endregion

#region Roles

<#
    .SYNOPSIS
        Gets one or all roles.

    .PARAMETER Id
        The unique identifier of a specific role. When omitted, all roles are returned.
#>
function Get-OpenDscRole
{
    [CmdletBinding(DefaultParameterSetName = 'Query')]
    param(
        [Parameter(ParameterSetName = 'ID', Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        if ($PSCmdlet.ParameterSetName -eq 'ID')
        {
            $uri = New-OpenDscUri "$($r.URI)/$Id"
        }
        else
        {
            $uri = New-OpenDscUri $r.URI
        }
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Creates a new role.

    .PARAMETER Name
        The name of the role.

    .PARAMETER Description
        An optional description.

    .PARAMETER Permissions
        The permission strings to include in the role.
#>
function New-OpenDscRole
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [string]$Description,

        [Parameter(Mandatory)]
        [string[]]$Permissions
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Updates a role.

    .PARAMETER Id
        The unique identifier of the role.

    .PARAMETER Name
        The new name.

    .PARAMETER Description
        The new description.

    .PARAMETER Permissions
        The updated permission strings.
#>
function Set-OpenDscRole
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id,

        [string]$Name,
        [string]$Description,
        [string[]]$Permissions
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -ne 'Id') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes a role.

    .PARAMETER Id
        The unique identifier of the role to remove.
#>
function Remove-OpenDscRole
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ id = $Id }
        if ($PSCmdlet.ShouldProcess($Id, 'Remove role'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

#endregion

#region Settings

<#
    .SYNOPSIS
        Gets all server settings.
#>
function Get-OpenDscSetting
{
    [CmdletBinding()]
    param()
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Gets the public server settings visible without authentication.
#>
function Get-OpenDscPublicSetting
{
    [CmdletBinding()]
    param()
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Updates server settings.

    .PARAMETER CertificateRotationInterval
        The interval for automatic certificate rotation.

    .PARAMETER StalenessMultiplier
        The multiplier used to determine node staleness.
#>
function Set-OpenDscSetting
{
    [CmdletBinding()]
    param(
        [timespan]$CertificateRotationInterval,
        [double]$StalenessMultiplier
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Gets the default LCM settings applied to newly registered nodes.
#>
function Get-OpenDscLcmDefault
{
    [CmdletBinding()]
    param()
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Updates the default LCM settings for newly registered nodes.

    .PARAMETER DefaultConfigurationMode
        The default configuration mode (Monitor or Remediate).

    .PARAMETER DefaultConfigurationModeInterval
        The default interval between configuration checks.

    .PARAMETER DefaultReportCompliance
        Whether nodes report compliance by default.
#>
function Set-OpenDscLcmDefault
{
    [CmdletBinding()]
    param(
        [string]$DefaultConfigurationMode,
        [timespan]$DefaultConfigurationModeInterval,
        [bool]$DefaultReportCompliance
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Gets the validation settings.
#>
function Get-OpenDscValidationSetting
{
    [CmdletBinding()]
    param()
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Updates the validation settings.

    .PARAMETER RequireSemVer
        Whether semantic versioning is required for configuration versions.

    .PARAMETER DefaultParameterValidationMode
        The default parameter validation mode.

    .PARAMETER AllowConfigurationOverride
        Whether individual configurations can override validation settings.

    .PARAMETER AllowParameterValidationOverride
        Whether parameter validation can be overridden per configuration.
#>
function Set-OpenDscValidationSetting
{
    [CmdletBinding()]
    param(
        [bool]$RequireSemVer,
        [string]$DefaultParameterValidationMode,
        [bool]$AllowConfigurationOverride,
        [bool]$AllowParameterValidationOverride
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Gets the global retention settings.
#>
function Get-OpenDscRetentionSetting
{
    [CmdletBinding()]
    param()
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Updates the global retention settings.

    .PARAMETER Enabled
        Whether retention cleanup is enabled.

    .PARAMETER KeepVersions
        Number of versions to keep.

    .PARAMETER KeepDays
        Number of days to keep versions.

    .PARAMETER KeepReleaseVersions
        Whether to always keep release (non-prerelease) versions.

    .PARAMETER ScheduleIntervalHours
        Hours between automatic retention runs.

    .PARAMETER ReportKeepCount
        Maximum number of reports to retain.

    .PARAMETER ReportKeepDays
        Number of days to retain reports.

    .PARAMETER StatusEventKeepCount
        Maximum number of status events to retain.

    .PARAMETER StatusEventKeepDays
        Number of days to retain status events.
#>
function Set-OpenDscRetentionSetting
{
    [CmdletBinding()]
    param(
        [bool]$Enabled,
        [int]$KeepVersions,
        [int]$KeepDays,
        [bool]$KeepReleaseVersions,
        [int]$ScheduleIntervalHours,
        [int]$ReportKeepCount,
        [int]$ReportKeepDays,
        [int]$StatusEventKeepCount,
        [int]$StatusEventKeepDays
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

#endregion

#region Registration Keys

<#
    .SYNOPSIS
        Creates a new registration key for node enrollment.

    .PARAMETER Description
        An optional description for the key.

    .PARAMETER ExpiresAt
        When the key expires.

    .PARAMETER MaxUses
        Maximum number of times the key can be used.
#>
function New-OpenDscRegistrationKey
{
    [CmdletBinding()]
    param(
        [string]$Description,
        [datetime]$ExpiresAt,
        [int]$MaxUses
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Lists all registration keys.
#>
function Get-OpenDscRegistrationKey
{
    [CmdletBinding()]
    param()
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Updates a registration key.

    .PARAMETER Id
        The unique identifier of the registration key.

    .PARAMETER Description
        The new description.
#>
function Set-OpenDscRegistrationKey
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id,

        [Parameter(Mandatory)]
        [string]$Description
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body @{ Description = $Description }
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ id = $Id }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Removes a registration key.

    .PARAMETER Id
        The unique identifier of the registration key to remove.
#>
function Remove-OpenDscRegistrationKey
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [guid]$Id
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ id = $Id }
        if ($PSCmdlet.ShouldProcess($Id, 'Remove registration key'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

#endregion

#region Configuration Settings

<#
    .SYNOPSIS
        Gets the per-configuration setting overrides.

    .PARAMETER Name
        The name of the configuration.
#>
function Get-OpenDscConfigurationSetting
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Sets per-configuration setting overrides.

    .PARAMETER Name
        The name of the configuration.

    .PARAMETER RequireSemVer
        Whether semantic versioning is required.

    .PARAMETER ParameterValidationMode
        The parameter validation mode override.
#>
function Set-OpenDscConfigurationSetting
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name,

        [bool]$RequireSemVer,
        [string]$ParameterValidationMode
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -ne 'Name') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Clears per-configuration setting overrides, reverting to server defaults.

    .PARAMETER Name
        The name of the configuration.
#>
function Remove-OpenDscConfigurationSetting
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ name = $Name }
        if ($PSCmdlet.ShouldProcess($Name, 'Clear configuration setting overrides'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

<#
    .SYNOPSIS
        Gets the per-configuration retention overrides.

    .PARAMETER Name
        The name of the configuration.
#>
function Get-OpenDscConfigurationRetention
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name }) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Sets per-configuration retention overrides.

    .PARAMETER Name
        The name of the configuration.

    .PARAMETER Enabled
        Whether retention is enabled.

    .PARAMETER KeepVersions
        Number of versions to keep.

    .PARAMETER KeepDays
        Number of days to keep versions.

    .PARAMETER KeepReleaseVersions
        Whether to always keep release versions.
#>
function Set-OpenDscConfigurationRetention
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name,

        [bool]$Enabled,
        [int]$KeepVersions,
        [int]$KeepDays,
        [bool]$KeepReleaseVersions
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $bodyParams = @{}
        foreach ($key in $PSBoundParameters.Keys)
        {
            if ($key -ne 'Name') { $bodyParams[$key] = $PSBoundParameters[$key] }
        }
        $body = New-OpenDscBody $r.Body $bodyParams
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI @{ name = $Name }) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Clears per-configuration retention overrides, reverting to server defaults.

    .PARAMETER Name
        The name of the configuration.
#>
function Remove-OpenDscConfigurationRetention
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [string]$Name
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI @{ name = $Name }
        if ($PSCmdlet.ShouldProcess($Name, 'Clear configuration retention overrides'))
        {
            Submit-OpenDscRequest -Uri $uri -Method $r.Method
        }
    }
}

#endregion

#region Retention

<#
    .SYNOPSIS
        Runs a retention cleanup for configuration versions.

    .PARAMETER KeepVersions
        Number of versions to keep per configuration.

    .PARAMETER KeepDays
        Number of days to keep versions.

    .PARAMETER KeepReleaseVersions
        Whether to always keep release versions.

    .PARAMETER DryRun
        When specified, reports what would be deleted without actually deleting.
#>
function Invoke-OpenDscConfigurationCleanup
{
    [CmdletBinding()]
    param(
        [int]$KeepVersions = 10,
        [int]$KeepDays = 90,
        [bool]$KeepReleaseVersions = $true,
        [switch]$DryRun
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Runs a retention cleanup for parameter versions.

    .PARAMETER KeepVersions
        Number of versions to keep per parameter.

    .PARAMETER KeepDays
        Number of days to keep versions.

    .PARAMETER KeepReleaseVersions
        Whether to always keep release versions.

    .PARAMETER DryRun
        When specified, reports what would be deleted without actually deleting.
#>
function Invoke-OpenDscParameterCleanup
{
    [CmdletBinding()]
    param(
        [int]$KeepVersions = 10,
        [int]$KeepDays = 90,
        [bool]$KeepReleaseVersions = $true,
        [switch]$DryRun
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Runs a retention cleanup for composite configuration versions.

    .PARAMETER KeepVersions
        Number of versions to keep per composite configuration.

    .PARAMETER KeepDays
        Number of days to keep versions.

    .PARAMETER KeepReleaseVersions
        Whether to always keep release versions.

    .PARAMETER DryRun
        When specified, reports what would be deleted without actually deleting.
#>
function Invoke-OpenDscCompositeConfigurationCleanup
{
    [CmdletBinding()]
    param(
        [int]$KeepVersions = 10,
        [int]$KeepDays = 90,
        [bool]$KeepReleaseVersions = $true,
        [switch]$DryRun
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Runs a retention cleanup for compliance reports.

    .PARAMETER KeepCount
        Maximum number of reports to retain.

    .PARAMETER KeepDays
        Number of days to retain reports.

    .PARAMETER DryRun
        When specified, reports what would be deleted without actually deleting.
#>
function Invoke-OpenDscReportCleanup
{
    [CmdletBinding()]
    param(
        [int]$KeepCount = 1000,
        [int]$KeepDays = 30,
        [switch]$DryRun
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Runs a retention cleanup for node status events.

    .PARAMETER KeepCount
        Maximum number of status events to retain.

    .PARAMETER KeepDays
        Number of days to retain status events.

    .PARAMETER DryRun
        When specified, reports what would be deleted without actually deleting.
#>
function Invoke-OpenDscStatusEventCleanup
{
    [CmdletBinding()]
    param(
        [int]$KeepCount = 1000,
        [int]$KeepDays = 30,
        [switch]$DryRun
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $body = New-OpenDscBody $r.Body $PSBoundParameters
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method -Body $body
    }
}

<#
    .SYNOPSIS
        Gets the history of retention cleanup runs.

    .PARAMETER Limit
        Maximum number of runs to return.

    .PARAMETER From
        Start date filter.

    .PARAMETER To
        End date filter.
#>
function Get-OpenDscRetentionRun
{
    [CmdletBinding()]
    param(
        [int]$Limit,
        [datetime]$From,
        [datetime]$To
    )
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        $uri = New-OpenDscUri $r.URI
        $uri = New-OpenDscQueryString $uri $r.Query $PSBoundParameters
        Submit-OpenDscRequest -Uri $uri -Method $r.Method
    }
}

#endregion

#region Health

<#
    .SYNOPSIS
        Tests the health of the OpenDsc Pull Server.
#>
function Test-OpenDscHealth
{
    [CmdletBinding()]
    param()
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method
    }
}

<#
    .SYNOPSIS
        Tests whether the OpenDsc Pull Server is ready to accept requests.
#>
function Test-OpenDscReady
{
    [CmdletBinding()]
    param()
    process
    {
        Test-OpenDscSession
        $r = $script:ApiData[$MyInvocation.MyCommand.Name]
        Submit-OpenDscRequest -Uri (New-OpenDscUri $r.URI) -Method $r.Method
    }
}

#endregion

