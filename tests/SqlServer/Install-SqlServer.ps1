# Copyright (c) Thomas Nieto - All Rights Reserved
# You may use, distribute and modify this code under the
# terms of the MIT license.

<#
.SYNOPSIS
    Installs SQL Server for CI testing.

.DESCRIPTION

    This script installs SQL Server Developer edition on Windows or Linux
    when running in GitHub Actions. It is designed to be dot-sourced from
    Pester test BeforeAll blocks.

    On Windows, it uses winget to install SQL Server 2022 Developer.
    On Linux (Ubuntu), it uses apt to install SQL Server from Microsoft's
    repository following the official documentation.

.PARAMETER SaPassword
    The password for the SQL Server sa account on Linux.
    Default: 'P@ssw0rd123!'

.NOTES
    This script should be dot-sourced from test files:
    . $PSScriptRoot/Install-SqlServer.ps1
    Initialize-SqlServerForTests
#>

$script:SqlServerSaPassword = 'P@ssw0rd123!'

function Install-SqlServerWindows
{
    Write-Host 'Installing SQL Server 2022 Developer on Windows...'

    # Check if SQL Server is already installed
    $sqlService = Get-Service -Name 'MSSQLSERVER' -ErrorAction SilentlyContinue
    if ($sqlService)
    {
        Write-Host 'SQL Server is already installed.'
        return $true
    }

    # Install SQL Server using winget
    try
    {
        # winget may not be in PATH by default in GitHub Actions
        $wingetPath = Get-Command winget -ErrorAction SilentlyContinue
        if (-not $wingetPath)
        {
            Write-Warning 'winget not found in PATH'
            return $false
        }

        # Install SQL Server 2022 Developer
        # Use --accept-package-agreements and --accept-source-agreements to avoid prompts
        $result = winget install Microsoft.SQLServer.2022.Developer --accept-package-agreements --accept-source-agreements --silent 2>&1
        Write-Host $result

        # Wait for service to be available
        $maxRetries = 30
        $retryCount = 0
        while ($retryCount -lt $maxRetries)
        {
            $sqlService = Get-Service -Name 'MSSQLSERVER' -ErrorAction SilentlyContinue
            if ($sqlService -and $sqlService.Status -eq 'Running')
            {
                Write-Host 'SQL Server service is running.'
                return $true
            }

            Start-Sleep -Seconds 5
            $retryCount++
            Write-Host "Waiting for SQL Server service... (attempt $retryCount/$maxRetries)"
        }

        # Try starting the service if it exists but isn't running
        $sqlService = Get-Service -Name 'MSSQLSERVER' -ErrorAction SilentlyContinue
        if ($sqlService -and $sqlService.Status -ne 'Running')
        {
            Start-Service -Name 'MSSQLSERVER' -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 10
        }

        $sqlService = Get-Service -Name 'MSSQLSERVER' -ErrorAction SilentlyContinue
        return ($sqlService -and $sqlService.Status -eq 'Running')
    }
    catch
    {
        Write-Warning "Failed to install SQL Server: $_"
        return $false
    }
}

function Install-SqlServerLinux
{
    param (
        [string]$SaPassword = $script:SqlServerSaPassword
    )

    Write-Host 'Installing SQL Server on Linux...'

    # Check if SQL Server is already installed
    $mssqlStatus = bash -c 'systemctl is-active mssql-server 2>/dev/null' 2>$null
    if ($mssqlStatus -eq 'active')
    {
        Write-Host 'SQL Server is already running.'
        return $true
    }

    try
    {
        # Get Ubuntu version
        $ubuntuVersion = bash -c 'lsb_release -rs 2>/dev/null' 2>$null
        if (-not $ubuntuVersion)
        {
            $ubuntuVersion = '22.04'  # Default to 22.04
        }

        Write-Host "Detected Ubuntu version: $ubuntuVersion"

        # Ubuntu 24.04 uses different GPG key method and SQL Server 2025
        if ($ubuntuVersion -like '24.*')
        {
            Write-Host 'Using Ubuntu 24.04 installation method with SQL Server 2025...'

            # Download the public key, convert from ASCII to GPG format
            Write-Host 'Importing Microsoft GPG key...'
            bash -c 'curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg'

            # Register SQL Server 2025 repository for Ubuntu 24.04
            Write-Host 'Registering SQL Server 2025 repository...'
            bash -c 'curl -fsSL https://packages.microsoft.com/config/ubuntu/24.04/mssql-server-2025.list | sudo tee /etc/apt/sources.list.d/mssql-server-2025.list'
        }
        else
        {
            # For Ubuntu 22.04 and earlier, use existing method with SQL Server 2022
            Write-Host 'Using standard installation method with SQL Server 2022...'

            # Import Microsoft GPG key
            Write-Host 'Importing Microsoft GPG key...'
            bash -c 'curl https://packages.microsoft.com/keys/microsoft.asc | sudo tee /etc/apt/trusted.gpg.d/microsoft.asc'

            # Register SQL Server 2022 repository based on Ubuntu version
            Write-Host 'Registering SQL Server 2022 repository...'
            $repoUrl = "https://packages.microsoft.com/config/ubuntu/$ubuntuVersion/mssql-server-2022.list"
            bash -c "curl -fsSL $repoUrl | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list"
        }

        # Update packages and install SQL Server
        Write-Host 'Installing mssql-server package...'
        bash -c 'sudo apt-get update'
        bash -c 'sudo ACCEPT_EULA=Y apt-get install -y mssql-server'

        # Configure SQL Server with sa password and Developer edition
        Write-Host 'Configuring SQL Server...'
        $setupCmd = "sudo ACCEPT_EULA=Y MSSQL_SA_PASSWORD='$SaPassword' MSSQL_PID=Developer /opt/mssql/bin/mssql-conf -n setup"
        bash -c $setupCmd

        # Verify service is running
        Start-Sleep -Seconds 5
        $mssqlStatus = bash -c 'systemctl is-active mssql-server 2>/dev/null' 2>$null

        if ($mssqlStatus -eq 'active')
        {
            Write-Host 'SQL Server is running on Linux.'
            return $true
        }
        else
        {
            Write-Warning "SQL Server service status: $mssqlStatus"
            bash -c 'sudo systemctl status mssql-server --no-pager' 2>$null
            return $false
        }
    }
    catch
    {
        Write-Warning "Failed to install SQL Server on Linux: $_"
        return $false
    }
}

function Initialize-SqlServerForTests
{
    <#
    .SYNOPSIS
        Initializes SQL Server for testing, installing if necessary in CI.

    .DESCRIPTION
        This function checks for SQL Server availability and installs it
        when running in GitHub Actions. It sets script-scoped variables
        that can be used by tests.

    .OUTPUTS
        Sets the following script-scoped variables:
        - $script:sqlServerInstance - The SQL Server instance name
        - $script:sqlServerAvailable - Whether SQL Server is available
    #>

    # Default instance name
    $script:sqlServerInstance = if ($env:SQLSERVER_INSTANCE) { $env:SQLSERVER_INSTANCE } else { '.' }
    $script:sqlServerAvailable = $false

    if ($env:GITHUB_ACTIONS)
    {
        Write-Verbose 'Running in GitHub Actions - checking for SQL Server installation...' -Verbose

        if ($IsWindows)
        {
            $installed = Install-SqlServerWindows
            if (-not $installed)
            {
                Write-Warning 'Failed to install SQL Server on Windows in GitHub Actions.'
            }
        }
        elseif ($IsLinux)
        {
            $installed = Install-SqlServerLinux
            if ($installed)
            {
                # On Linux, we need to use SQL authentication
                $sqlServerInstance = 'localhost'
                $env:SQLSERVER_USE_SQL_AUTH = 'true'
                $env:SQLSERVER_SA_PASSWORD = $script:SqlServerSaPassword
            }
            else
            {
                Write-Warning 'Failed to install SQL Server on Linux in GitHub Actions.'
            }
        }
        else
        {
            Write-Warning 'Unsupported OS for SQL Server installation in GitHub Actions.'
        }
    }

    # Test SQL Server connectivity
    try
    {
        $connectionString = "Server=$sqlServerInstance;Connection Timeout=10"

        if ($env:SQLSERVER_USE_SQL_AUTH -eq 'true' -and $env:SQLSERVER_SA_PASSWORD)
        {
            # SQL Authentication for Linux
            $connectionString += ";User Id=sa;Password=$env:SQLSERVER_SA_PASSWORD;TrustServerCertificate=True"
        }
        else
        {
            # Windows Authentication
            $connectionString += ";Integrated Security=True"
        }

        $conn = New-Object System.Data.SqlClient.SqlConnection
        $conn.ConnectionString = $connectionString
        $conn.Open()
        $conn.Close()
        $script:sqlServerAvailable = $true

        Write-Host "SQL Server is available at '$sqlServerInstance'"

        return $script:sqlServerAvailable
    }
    catch
    {
        Write-Warning "SQL Server not available at '$sqlServerInstance'. Skipping SQL Server tests. Error: $_"
    }
}
