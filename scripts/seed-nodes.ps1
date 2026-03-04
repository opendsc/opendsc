# seeds some fake nodes into an existing SQLite database used by the pull server
#
# Usage examples:
#   .\scripts\seed-nodes.ps1                    # use default database path and sample names
#   .\scripts\seed-nodes.ps1 -DatabasePath data.db -Count 5
#
param(
    [string]$DatabasePath = 'opendsc-server.db',
    [int]$Count = 3
)

if (-not (Get-Command sqlite3 -ErrorAction SilentlyContinue)) {
    Write-Error 'sqlite3 CLI not found in PATH. Install it or adjust your PATH before running this script.'
    exit 1
}

if (-not (Test-Path $DatabasePath)) {
    Write-Error "Database file '$DatabasePath' does not exist. Run the server once or point to the correct file."
    exit 1
}

Write-Host "Seeding $Count node(s) into database '$DatabasePath'..."

for ($i = 1; $i -le $Count; $i++) {
    $fqdn = "testnode$i.local"
    $thumb = [Guid]::NewGuid().ToString('N').Substring(0, 16).ToUpperInvariant()
    $subj = "CN=$fqdn"
    $notAfter = ([DateTimeOffset]::UtcNow.AddYears(1)).ToString('yyyy-MM-dd HH:mm:ss')
    $created = ([DateTimeOffset]::UtcNow).ToString('yyyy-MM-dd HH:mm:ss')

    # use SQLITE datetime literal format; Status 0 = Unknown
    $sql = @"
INSERT OR IGNORE INTO Nodes
(Id, Fqdn, ConfigurationName, CertificateThumbprint, CertificateSubject, CertificateNotAfter, LastCheckIn, Status, CreatedAt)
VALUES
('$( [Guid]::NewGuid())',
 '$fqdn',
 '',
 '$thumb',
 '$subj',
 '$notAfter',
 NULL,
 0,
 '$created');
"@

    sqlite3 $DatabasePath $sql
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to insert node $fqdn"
    } else {
        Write-Host "  seeded $fqdn" -ForegroundColor Green
    }
}

# display the verification command on a new line without confusing backslashes
Write-Host 'Done. You can verify by running:'
Write-Host "  sqlite3 $DatabasePath 'select * from Nodes;'"
