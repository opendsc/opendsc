// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenDsc.Lcm;

/// <summary>
/// Manages certificates for mTLS authentication with the pull server.
/// </summary>
public partial class CertificateManager : ICertificateManager
{
    private readonly IOptionsMonitor<LcmConfig> _lcmMonitor;
    private readonly ILogger<CertificateManager> _logger;

    public CertificateManager(
        IOptionsMonitor<LcmConfig> lcmMonitor,
        ILogger<CertificateManager> logger)
    {
        _lcmMonitor = lcmMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Gets the client certificate based on configuration.
    /// </summary>
    public X509Certificate2? GetClientCertificate()
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null)
        {
            LogPullServerNotConfigured();
            return null;
        }

        var certificateSource = pullServer.CertificateSource;

        return certificateSource switch
        {
            CertificateSource.Managed => GetOrCreateManagedCertificate(pullServer),
            CertificateSource.Platform => LoadFromPlatformStore(pullServer),
            _ => null
        };
    }

    /// <summary>
    /// Gets or creates a managed self-signed certificate.
    /// </summary>
    private X509Certificate2? GetOrCreateManagedCertificate(PullServerSettings pullServer)
    {
        var certPath = pullServer.CertificatePath;

        if (string.IsNullOrWhiteSpace(certPath))
        {
            certPath = Path.Combine(ConfigPaths.GetLcmConfigDirectory(), "certs", "client.pfx");
            pullServer.CertificatePath = certPath;
        }

        var certDir = Path.GetDirectoryName(certPath);
        if (!string.IsNullOrWhiteSpace(certDir) && !Directory.Exists(certDir))
        {
            Directory.CreateDirectory(certDir);
        }

        if (File.Exists(certPath))
        {
            try
            {
                var existingCert = X509CertificateLoader.LoadPkcs12FromFile(
                    certPath,
                    pullServer.CertificatePassword,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

                if (existingCert.NotAfter > DateTimeOffset.UtcNow.AddDays(30))
                {
                    LogCertificateLoaded(existingCert.Thumbprint);
                    return existingCert;
                }

                LogCertificateExpiringSoon(existingCert.Thumbprint, existingCert.NotAfter);
                existingCert.Dispose();
            }
            catch (Exception ex)
            {
                LogFailedToLoadCertificate(certPath, ex);
            }
        }

        var newCert = GenerateSelfSignedCertificate();
        SaveCertificate(newCert, certPath, pullServer.CertificatePassword);
        pullServer.CertificateThumbprint = newCert.Thumbprint;
        LogCertificateGenerated(newCert.Thumbprint);

        return newCert;
    }

    /// <summary>
    /// Loads a certificate from the platform-specific certificate store.
    /// </summary>
    private X509Certificate2? LoadFromPlatformStore(PullServerSettings pullServer)
    {
        var thumbprint = pullServer.CertificateThumbprint;

        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            LogCertificateThumbprintNotConfigured();
            return null;
        }

        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);

        if (certs.Count == 0)
        {
            LogCertificateNotFoundInStore(thumbprint);
            return null;
        }

        var cert = certs[0];
        LogCertificateLoadedFromStore(cert.Thumbprint);
        return cert;
    }

    /// <summary>
    /// Generates a new self-signed certificate.
    /// </summary>
    private X509Certificate2 GenerateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);

        var subject = new X500DistinguishedName($"CN={Environment.MachineName}");
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.2")], // Client Authentication
                false));

        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var certBytes = cert.Export(X509ContentType.Pfx);
            return X509CertificateLoader.LoadPkcs12(certBytes, null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        return cert;
    }

    /// <summary>
    /// Saves a certificate to disk.
    /// </summary>
    private void SaveCertificate(X509Certificate2 cert, string path, string? password)
    {
        var certBytes = cert.Export(X509ContentType.Pfx, password);
        File.WriteAllBytes(path, certBytes);
    }

    /// <summary>
    /// Checks if the current certificate needs rotation.
    /// </summary>
    public bool ShouldRotateCertificate(X509Certificate2? cert, PullServerSettings pullServer)
    {
        if (cert is null)
        {
            return false;
        }

        if (pullServer.CertificateSource != CertificateSource.Managed)
        {
            return false;
        }

        var totalLifetime = cert.NotAfter - cert.NotBefore;
        var rotationThreshold = totalLifetime * 2 / 3;
        var rotationTime = cert.NotBefore + rotationThreshold;

        return DateTimeOffset.UtcNow >= rotationTime;
    }

    /// <summary>
    /// Rotates the managed certificate.
    /// </summary>
    public X509Certificate2? RotateCertificate(PullServerSettings pullServer)
    {
        if (pullServer.CertificateSource != CertificateSource.Managed)
        {
            LogCertificateRotationNotSupported(pullServer.CertificateSource);
            return null;
        }

        var certPath = pullServer.CertificatePath;
        if (string.IsNullOrWhiteSpace(certPath))
        {
            LogCertificatePathNotConfigured();
            return null;
        }

        var newCert = GenerateSelfSignedCertificate();
        SaveCertificate(newCert, certPath, pullServer.CertificatePassword);
        pullServer.CertificateThumbprint = newCert.Thumbprint;
        LogCertificateRotated(newCert.Thumbprint);

        return newCert;
    }

    [LoggerMessage(EventId = EventIds.PullServerNotConfigured, Level = LogLevel.Error, Message = "Pull server not configured")]
    private partial void LogPullServerNotConfigured();

    [LoggerMessage(EventId = EventIds.CertificateLoaded, Level = LogLevel.Information, Message = "Certificate loaded: {Thumbprint}")]
    private partial void LogCertificateLoaded(string thumbprint);

    [LoggerMessage(EventId = EventIds.CertificateExpiringSoon, Level = LogLevel.Warning, Message = "Certificate {Thumbprint} expiring soon: {ExpirationDate}")]
    private partial void LogCertificateExpiringSoon(string thumbprint, DateTime expirationDate);

    [LoggerMessage(EventId = EventIds.FailedToLoadCertificate, Level = LogLevel.Error, Message = "Failed to load certificate from {Path}")]
    private partial void LogFailedToLoadCertificate(string path, Exception ex);

    [LoggerMessage(EventId = EventIds.CertificateGenerated, Level = LogLevel.Information, Message = "Certificate generated: {Thumbprint}")]
    private partial void LogCertificateGenerated(string thumbprint);

    [LoggerMessage(EventId = EventIds.CertificateThumbprintNotConfigured, Level = LogLevel.Error, Message = "Certificate thumbprint not configured")]
    private partial void LogCertificateThumbprintNotConfigured();

    [LoggerMessage(EventId = EventIds.CertificateNotFoundInStore, Level = LogLevel.Error, Message = "Certificate not found in store: {Thumbprint}")]
    private partial void LogCertificateNotFoundInStore(string thumbprint);

    [LoggerMessage(EventId = EventIds.CertificateLoadedFromStore, Level = LogLevel.Information, Message = "Certificate loaded from store: {Thumbprint}")]
    private partial void LogCertificateLoadedFromStore(string thumbprint);

    [LoggerMessage(EventId = EventIds.CertificateRotated, Level = LogLevel.Information, Message = "Certificate rotated: {Thumbprint}")]
    private partial void LogCertificateRotated(string thumbprint);

    [LoggerMessage(EventId = EventIds.CertificateRotationNotSupported, Level = LogLevel.Warning, Message = "Certificate rotation not supported for source: {Source}")]
    private partial void LogCertificateRotationNotSupported(CertificateSource source);

    [LoggerMessage(EventId = EventIds.CertificatePathNotConfigured, Level = LogLevel.Error, Message = "Certificate path not configured")]
    private partial void LogCertificatePathNotConfigured();
}
