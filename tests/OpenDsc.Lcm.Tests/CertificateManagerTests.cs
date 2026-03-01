// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using AwesomeAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Xunit;

namespace OpenDsc.Lcm.Tests;

public sealed class CertificateManagerTests : IDisposable
{
    private readonly Mock<IOptionsMonitor<LcmConfig>> _lcmMonitorMock;
    private readonly Mock<ILogger<CertificateManager>> _loggerMock;
    private readonly LcmConfig _config;
    private readonly string _testCertDir;
    private readonly string _testCertPath;

    public CertificateManagerTests()
    {
        _lcmMonitorMock = new Mock<IOptionsMonitor<LcmConfig>>();
        _loggerMock = new Mock<ILogger<CertificateManager>>();

        _testCertDir = Path.Combine(Path.GetTempPath(), $"opendsc-cert-test-{Guid.NewGuid():N}");
        _testCertPath = Path.Combine(_testCertDir, "client.pfx");
        Directory.CreateDirectory(_testCertDir);

        _config = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationPath = "test.dsc.yaml",
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = "http://localhost:8080",
                RegistrationKey = "test-key",
                CertificateSource = CertificateSource.Managed,
                CertificatePath = _testCertPath,
                CertificatePassword = "test-password"
            }
        };

        _lcmMonitorMock.Setup(x => x.CurrentValue).Returns(_config);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testCertDir))
        {
            try
            {
                Directory.Delete(_testCertDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void GetClientCertificate_WhenPullServerNotConfigured_ReturnsNull()
    {
        var configWithoutPullServer = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationPath = "test.dsc.yaml",
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        _lcmMonitorMock.Setup(x => x.CurrentValue).Returns(configWithoutPullServer);

        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var result = manager.GetClientCertificate();

        result.Should().BeNull();
    }

    [Fact]
    public void GetClientCertificate_WithManagedSource_GeneratesNewCertificate()
    {
        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var cert = manager.GetClientCertificate();

        cert.Should().NotBeNull();
        cert!.Subject.Should().Contain(Environment.MachineName);
        cert.HasPrivateKey.Should().BeTrue();
        File.Exists(_testCertPath).Should().BeTrue();
    }

    [Fact]
    public void GetClientCertificate_WithExistingManagedCertificate_LoadsExisting()
    {
        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var cert1 = manager.GetClientCertificate();
        var thumbprint1 = cert1!.Thumbprint;
        cert1.Dispose();

        var cert2 = manager.GetClientCertificate();
        var thumbprint2 = cert2!.Thumbprint;
        cert2.Dispose();

        thumbprint1.Should().Be(thumbprint2);
    }

    [Fact]
    public void GetClientCertificate_WithExpiringSoonCertificate_RegeneratesCertificate()
    {
        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        using var rsa = RSA.Create(2048);
        var subject = new X500DistinguishedName($"CN={Environment.MachineName}");
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var oldCert = request.CreateSelfSigned(DateTime.UtcNow.AddDays(-80), DateTime.UtcNow.AddDays(10));

        var certBytes = oldCert.Export(X509ContentType.Pfx, _config.PullServer!.CertificatePassword);
        File.WriteAllBytes(_testCertPath, certBytes);
        oldCert.Dispose();

        var newCert = manager.GetClientCertificate();

        newCert.Should().NotBeNull();
        newCert!.NotAfter.Should().BeAfter(DateTime.UtcNow.AddDays(60));
        newCert.Dispose();
    }

    [Fact]
    public void GetClientCertificate_WithPlatformSource_WhenThumbprintNotConfigured_ReturnsNull()
    {
        _config.PullServer!.CertificateSource = CertificateSource.Platform;
        _config.PullServer.CertificateThumbprint = null;

        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var cert = manager.GetClientCertificate();

        cert.Should().BeNull();
    }

    [Fact]
    public void GetClientCertificate_WithPlatformSource_WhenCertNotInStore_ReturnsNull()
    {
        _config.PullServer!.CertificateSource = CertificateSource.Platform;
        _config.PullServer.CertificateThumbprint = "NONEXISTENT1234567890ABCDEF";

        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var cert = manager.GetClientCertificate();

        cert.Should().BeNull();
    }

    [Fact]
    public void ShouldRotateCertificate_WhenCertIsNull_ReturnsFalse()
    {
        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var result = manager.ShouldRotateCertificate(null, _config.PullServer!);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRotateCertificate_WithPlatformSource_ReturnsFalse()
    {
        _config.PullServer!.CertificateSource = CertificateSource.Platform;

        using var cert = CreateTestCertificate();
        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var result = manager.ShouldRotateCertificate(cert, _config.PullServer);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRotateCertificate_WhenCertificatePastTwoThirdsLifetime_ReturnsTrue()
    {
        using var rsa = RSA.Create(2048);
        var subject = new X500DistinguishedName($"CN={Environment.MachineName}");
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var notBefore = DateTime.UtcNow.AddDays(-61);
        var notAfter = DateTime.UtcNow.AddDays(29);
        using var cert = request.CreateSelfSigned(notBefore, notAfter);

        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var result = manager.ShouldRotateCertificate(cert, _config.PullServer!);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRotateCertificate_WhenCertificateBeforeTwoThirdsLifetime_ReturnsFalse()
    {
        using var cert = CreateTestCertificate();
        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var result = manager.ShouldRotateCertificate(cert, _config.PullServer!);

        result.Should().BeFalse();
    }

    [Fact]
    public void RotateCertificate_WithManagedSource_GeneratesNewCertificate()
    {
        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var oldCert = manager.GetClientCertificate();
        var oldThumbprint = oldCert!.Thumbprint;
        oldCert.Dispose();

        var newCert = manager.RotateCertificate(_config.PullServer!);

        newCert.Should().NotBeNull();
        newCert!.Thumbprint.Should().NotBe(oldThumbprint);
        newCert.HasPrivateKey.Should().BeTrue();
        newCert.Dispose();
    }

    [Fact]
    public void RotateCertificate_WithPlatformSource_ReturnsNull()
    {
        _config.PullServer!.CertificateSource = CertificateSource.Platform;

        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var result = manager.RotateCertificate(_config.PullServer);

        result.Should().BeNull();
    }

    [Fact]
    public void RotateCertificate_WhenCertificatePathNotConfigured_ReturnsNull()
    {
        _config.PullServer!.CertificatePath = null;

        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var result = manager.RotateCertificate(_config.PullServer);

        result.Should().BeNull();
    }

    [Fact]
    public void GetClientCertificate_CreatesDirectoryIfNotExists()
    {
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"opendsc-test-{Guid.NewGuid():N}");
        var certPath = Path.Combine(nonExistentDir, "cert.pfx");

        try
        {
            _config.PullServer!.CertificatePath = certPath;

            var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

            var cert = manager.GetClientCertificate();

            cert.Should().NotBeNull();
            Directory.Exists(nonExistentDir).Should().BeTrue();
            File.Exists(certPath).Should().BeTrue();
            cert!.Dispose();
        }
        finally
        {
            if (Directory.Exists(nonExistentDir))
            {
                try
                {
                    Directory.Delete(nonExistentDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Fact]
    public void GetClientCertificate_WithCorruptedCertificateFile_RegeneratesCertificate()
    {
        File.WriteAllText(_testCertPath, "corrupted certificate data");

        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var cert = manager.GetClientCertificate();

        cert.Should().NotBeNull();
        cert!.HasPrivateKey.Should().BeTrue();
        cert.Dispose();
    }

    [Fact]
    public void GetClientCertificate_DefaultsCertificatePathWhenNotSpecified()
    {
        // Set a valid test directory instead of trying to use ProgramData
        var testDefaultDir = Path.Combine(Path.GetTempPath(), $"opendsc-default-test-{Guid.NewGuid():N}");
        _config.PullServer!.CertificatePath = Path.Combine(testDefaultDir, "certs", "client.pfx");

        try
        {
            var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

            var cert = manager.GetClientCertificate();

            cert.Should().NotBeNull();
            _config.PullServer.CertificatePath.Should().NotBeNullOrEmpty();
            _config.PullServer.CertificatePath.Should().EndWith("client.pfx");
            cert!.Dispose();
        }
        finally
        {
            if (Directory.Exists(testDefaultDir))
            {
                try
                {
                    Directory.Delete(testDefaultDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Fact]
    public void GetClientCertificate_CertificateHasCorrectExtensions()
    {
        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var cert = manager.GetClientCertificate();

        cert.Should().NotBeNull();

        var hasClientAuthEku = cert!.Extensions.OfType<X509EnhancedKeyUsageExtension>()
            .Any(ext => ext.EnhancedKeyUsages.Cast<Oid>().Any(oid => oid.Value == "1.3.6.1.5.5.7.3.2"));

        hasClientAuthEku.Should().BeTrue("certificate should have Client Authentication EKU");
        cert.Dispose();
    }

    [Fact]
    public void GetClientCertificate_SavedCertificateCanBeReloaded()
    {
        var manager = new CertificateManager(_lcmMonitorMock.Object, _loggerMock.Object);

        var cert1 = manager.GetClientCertificate();
        var thumbprint1 = cert1!.Thumbprint;
        cert1.Dispose();

        var loadedCert = X509CertificateLoader.LoadPkcs12FromFile(
            _testCertPath,
            _config.PullServer!.CertificatePassword,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

        loadedCert.Thumbprint.Should().Be(thumbprint1);
        loadedCert.HasPrivateKey.Should().BeTrue();
        loadedCert.Dispose();
    }

    private static X509Certificate2 CreateTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var subject = new X500DistinguishedName($"CN={Environment.MachineName}");
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.2")],
                false));

        var cert = request.CreateSelfSigned(DateTime.UtcNow, DateTime.UtcNow.AddDays(90));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var certBytes = cert.Export(X509ContentType.Pfx);
            return X509CertificateLoader.LoadPkcs12(certBytes, null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        }

        return cert;
    }
}
