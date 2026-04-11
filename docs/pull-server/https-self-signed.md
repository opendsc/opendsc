# Configuring HTTPS for OpenDSC Server (Self-Signed Certificate)

!!! note
    **This guide is intended for testing and development only.**
    For production deployments, use a CA-issued certificate (enterprise PKI
    or a public CA such as Let's Encrypt) and avoid self-signed certs unless
    you have a strong operational reason. OpenDSC Server uses
    ASP.NET Core/Kestrel for HTTPS and supports mutual TLS (mTLS) for node
    authentication. For most production scenarios, you should host OpenDSC
    Server with a **trusted CA certificate** (either from an enterprise CA or a
    public CA).

## Production Recommendation (CA-issued Certificates)

### Enterprise PKI (Recommended for internal deployments)

- Use your organization’s internal CA (e.g., AD CS) to issue a certificate for
  the OpenDSC Server hostname.
- Trust is automatic for domain-joined machines and eliminates manual trust
  steps.

Microsoft docs:

- [Deploying Active Directory Certificate Services (AD CS)][adcs]
- [Request a certificate using certreq][certreq]

### Public CA (Let’s Encrypt) / DNS-01 Challenge

- Use Let’s Encrypt for public-trusted certs.
- For internal-only hostnames, use **DNS-01 validation**, which works even when
  the service is not publicly reachable.
- DNS-01 requires creating a TXT record under `_acme-challenge.<hostname>`; many
  DNS providers support API-based updates.

Let’s Encrypt docs:

- [Let’s Encrypt Getting Started][letsencrypt]
- [ACME DNS-01 challenge][acme-dns]
- [Certbot DNS plugins (automation)][certbot]

!!! tip
    DNS-01 challenges can be automated by having your DNS provider support
    API updates. If you can’t change DNS programmatically, you can still do it
    manually, but renewals will require manual TXT record updates.

## Test/Dev (Self-Signed Certificate)

### 1) Generate a Self-Signed Certificate

#### Option A: PowerShell (Windows)

```powershell
$cert = New-SelfSignedCertificate -Subject "CN=opendsc.local" -CertStoreLocation "Cert:\LocalMachine\My" -KeyExportPolicy Exportable -NotAfter (Get-Date).AddYears(5)
$pwd = ConvertTo-SecureString -String "PfxPassword123!" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "C:\opendsc\certs\opendsc.pfx" -Password $pwd
```

### Option B: OpenSSL (Linux/macOS)

```sh
openssl req -x509 -nodes -newkey rsa:2048 -keyout opendsc-key.pem -out opendsc-cert.pem -days 1825 -subj "/CN=opendsc.local"
openssl pkcs12 -export -out opendsc.pfx -inkey opendsc-key.pem -in opendsc-cert.pem -passout pass:PfxPassword123!
```

> 🔐 **Tip:** Choose a strong password and keep it secret. Do not check it into
> source control.

### 2) Configure OpenDSC Server to Use the Certificate

Open `appsettings.json` in your OpenDSC Server configuration directory and add a
`Kestrel` section to point at your `.pfx` file.

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:443",
        "Certificate": {
          "Path": "C:\\opendsc\\certs\\opendsc.pfx",
          "Password": "PfxPassword123!"
        }
      }
    }
  }
}
```

### Linux/macOS Example (relative path)

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:443",
        "Certificate": {
          "Path": "/etc/opendsc/certs/opendsc.pfx",
          "Password": "PfxPassword123!"
        }
      }
    }
  }
}
```

> ✅ Note: OpenDSC Server loads configuration from:
>
> > 1. `appsettings.json` (in the executable folder) 2.
> > `appsettings.{Environment}.json` 3. `{configDir}/appsettings.json`
> > (platform-specific config dir) 4. Environment variables 5. Command line
> > arguments

### 3) (Optional) Use Environment Variables

Set the same values via environment variables instead of editing JSON.

```powershell
$env:ASPNETCORE_Kestrel__Endpoints__Https__Url = "https://0.0.0.0:443"
$env:ASPNETCORE_Kestrel__Endpoints__Https__Certificate__Path = "C:\opendsc\certs\opendsc.pfx"
$env:ASPNETCORE_Kestrel__Endpoints__Https__Certificate__Password = "PfxPassword123!"
```

### 4) Trust the Self-Signed Certificate (Client Browsers / API Users)

Browsers and API clients will reject a self-signed certificate by default. You
must import the certificate into the system trust store.

### Windows

1. Export the certificate (PEM/DER) from the PFX or from the Windows cert store.
2. Open `certlm.msc` → **Trusted Root Certification Authorities** →
   **Certificates**.
3. Import the certificate and complete the wizard.

### Linux (example using update-ca-trust)

```sh
sudo cp opendsc-cert.pem /etc/pki/ca-trust/source/anchors/
sudo update-ca-trust extract
```

### macOS

```sh
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain opendsc-cert.pem
```

### 5) Verify HTTPS

1. Start OpenDSC Server:
   - **Windows service** (recommended for production)
   - **Container** (if running in Docker/K8s)
   - **Development**: run `dotnet run --project
     src/OpenDsc.Server/OpenDsc.Server.csproj` and, optionally, run `dotnet
     dev-certs https --trust`.
2. Visit `https://<host>:443` in your browser.
3. You should see the OpenDSC Server UI (after trusting the cert).

### Troubleshooting

- **`ERR_CERT_AUTHORITY_INVALID`**: Your client does not trust the certificate.
  Import it into the trust store.
- **Port already in use**: Change the `Url` to a different port (e.g.,
  `https://0.0.0.0:8443`).
- **Certificate password errors**: Ensure the password matches the one used when
  exporting the `.pfx`.

## Security Reminder

Self-signed certificates are suitable for testing or internal environments only.
For production, use a CA-issued certificate (e.g., from Let’s Encrypt or your
internal PKI) and configure Kestrel or a reverse proxy to terminate TLS.

[adcs]: https://learn.microsoft.com/windows-server/identity/ad-ds/get-started/active-directory-certificate-services
[certreq]: https://learn.microsoft.com/windows-server/administration/windows-commands/certreq
[letsencrypt]: https://letsencrypt.org/getting-started/
[acme-dns]: https://letsencrypt.org/docs/challenge-types/#dns-01
[certbot]: https://certbot.eff.org/docs/using.html#dns-plugins
