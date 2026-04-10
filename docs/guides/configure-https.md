# Configure HTTPS with self-signed certificates

This guide walks you through configuring the OpenDSC Pull Server to use HTTPS
with a self-signed
certificate. This enables mutual TLS (mTLS) authentication between the server
and managed nodes.

## When to use this guide

Use this guide when you want to:

- Move beyond the default HTTP configuration for lab or staging environments.
- Enable mTLS authentication without a certificate authority (CA).
- Test the full node registration and certificate rotation workflow.

For production deployments with an enterprise CA, the process is similar but
you'll use
CA-issued certificates instead of self-signed ones.

## Generate a server certificate

Create a self-signed certificate for the Pull Server:

```powershell
$cert = New-SelfSignedCertificate `
    -DnsName 'localhost', $env:COMPUTERNAME, "$env:COMPUTERNAME.$env:USERDNSDOMAIN" `
    -CertStoreLocation 'Cert:\LocalMachine\My' `
    -FriendlyName 'OpenDSC Pull Server' `
    -NotAfter (Get-Date).AddYears(2) `
    -KeyUsage DigitalSignature, KeyEncipherment `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.1')

$cert.Thumbprint
```

Save the thumbprint — you'll need it for the server configuration.

## Configure Kestrel to use HTTPS

Update the Pull Server's `appsettings.json` to configure Kestrel with the
certificate. The
configuration file is located at `C:\Program Files\OpenDSC\appsettings.json`
(MSI install) or in
the server's output directory:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:5001",
        "Certificate": {
          "Subject": "localhost",
          "Store": "My",
          "Location": "LocalMachine",
          "AllowInvalid": true
        }
      }
    }
  }
}
```

Alternatively, bind by thumbprint:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:5001",
        "Certificate": {
          "Thumbprint": "<your-certificate-thumbprint>",
          "Store": "My",
          "Location": "LocalMachine"
        }
      }
    }
  }
}
```

Restart the service:

```powershell
Restart-Service OpenDscServer
```

## Verify HTTPS is working

Browse to `https://localhost:5001`. Your browser may warn about the self-signed
certificate — this
is expected.

```powershell
# Test with PowerShell (skip cert validation for self-signed)
Invoke-RestMethod -Uri 'https://localhost:5001/health' -SkipCertificateCheck
```

## Update the LCM configuration

Point the LCM at the HTTPS endpoint:

```powershell
$configPath = "$env:ProgramData\OpenDSC\LCM\appsettings.json"
$config = Get-Content $configPath | ConvertFrom-Json

$config.LCM.PullServer.ServerUrl = 'https://localhost:5001'

$config | ConvertTo-Json -Depth 5 | Set-Content -Path $configPath -Encoding UTF8
Restart-Service OpenDscLcm
```

## Remove the Testing environment override

If you previously set the `ASPNETCORE_ENVIRONMENT=Testing` override (as
described in the
getting started tutorial), remove it now:

```powershell
$serverRegPath = 'HKLM:\SYSTEM\CurrentControlSet\Services\OpenDscServer'
Remove-ItemProperty -Path $serverRegPath -Name Environment -ErrorAction SilentlyContinue
Restart-Service OpenDscServer
```
