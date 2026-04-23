// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using AwesomeAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using OpenDsc.Lcm.Contracts;

using Xunit;

namespace OpenDsc.Lcm.Tests;

[Trait("Category", "Unit")]
public class CertificateManagerEdgeCaseTests
{
    private readonly Mock<IOptionsMonitor<LcmConfig>> _configMonitorMock;
    private readonly Mock<ILogger<CertificateManager>> _loggerMock;

    public CertificateManagerEdgeCaseTests()
    {
        _configMonitorMock = new Mock<IOptionsMonitor<LcmConfig>>();
        _loggerMock = new Mock<ILogger<CertificateManager>>();
    }

    [Fact]
    public void GetClientCertificate_WithNoPullServer_ReturnsNull()
    {
        var config = new LcmConfig
        {
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(config);

        var manager = new CertificateManager(_configMonitorMock.Object, _loggerMock.Object);

        var result = manager.GetClientCertificate();

        result.Should().BeNull();
    }

    [Fact]
    public void GetClientCertificate_WithManagedCertificateSource_ReturnsCertificateOrNull()
    {
        var tempCertPath = Path.Combine(Path.GetTempPath(), $"test-cert-{Guid.NewGuid()}.pfx");

        try
        {
            var config = new LcmConfig
            {
                ConfigurationSource = ConfigurationSource.Pull,
                PullServer = new PullServerSettings
                {
                    CertificateSource = CertificateSource.Managed,
                    CertificatePath = tempCertPath
                }
            };

            _configMonitorMock.Setup(x => x.CurrentValue).Returns(config);

            var manager = new CertificateManager(_configMonitorMock.Object, _loggerMock.Object);

            var result = manager.GetClientCertificate();

            // Result may be null if cert doesn't exist yet, or a valid certificate if created
            result.Should().BeAssignableTo<X509Certificate2>();
        }
        finally
        {
            if (File.Exists(tempCertPath))
            {
                try { File.Delete(tempCertPath); } catch { }
            }
        }
    }

    [Fact]
    public void GetClientCertificate_WithNullCertificatePath_HandlesGracefully()
    {
        var config = new LcmConfig
        {
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                CertificateSource = CertificateSource.Managed,
                CertificatePath = null
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(config);

        var manager = new CertificateManager(_configMonitorMock.Object, _loggerMock.Object);

        var action = () => manager.GetClientCertificate();

        // Should not throw - should handle null path gracefully
        action.Should().NotThrow();
    }

    [Fact]
    public void GetClientCertificate_WithEmptyCertificatePath_HandlesGracefully()
    {
        var config = new LcmConfig
        {
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                CertificateSource = CertificateSource.Managed,
                CertificatePath = string.Empty
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(config);

        var manager = new CertificateManager(_configMonitorMock.Object, _loggerMock.Object);

        var action = () => manager.GetClientCertificate();

        action.Should().NotThrow();
    }

    [Fact]
    public void ShouldRotateCertificate_WithNoPullServer_ReturnsFalse()
    {
        var config = new LcmConfig
        {
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(config);

        var manager = new CertificateManager(_configMonitorMock.Object, _loggerMock.Object);
        var pullServer = new PullServerSettings();

        var result = manager.ShouldRotateCertificate(null, pullServer);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRotateCertificate_WithNoCertificate_ReturnsFalse()
    {
        var config = new LcmConfig
        {
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                CertificateSource = CertificateSource.Managed,
                CertificateRotationInterval = TimeSpan.FromDays(30)
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(config);

        var manager = new CertificateManager(_configMonitorMock.Object, _loggerMock.Object);

        var result = manager.ShouldRotateCertificate(null, config.PullServer!);

        result.Should().BeFalse("a missing/null certificate should return false");
    }

    [Fact]
    public void ShouldRotateCertificate_WithExpiredCertificate_DependsOnRotationTime()
    {
        var config = new LcmConfig
        {
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                CertificateSource = CertificateSource.Managed,
                CertificateRotationInterval = TimeSpan.FromDays(1)
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(config);

        // Create an expired certificate
        // NotBefore + 1 day determines rotation time, not NotAfter
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={Environment.MachineName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var expiredCert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1));

        var manager = new CertificateManager(_configMonitorMock.Object, _loggerMock.Object);

        var result = manager.ShouldRotateCertificate(expiredCert, config.PullServer!);

        // Should be True because NotBefore(-2d) + 1d interval = 1 day ago, which is less than now
        result.Should().BeTrue("an old certificate's rotation interval should have passed");
    }

    [Fact]
    public void ShouldRotateCertificate_WithValidCertificateBeyondRotationInterval_ReturnsTrue()
    {
        var config = new LcmConfig
        {
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                CertificateSource = CertificateSource.Managed,
                CertificateRotationInterval = TimeSpan.FromDays(1)
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(config);

        // Create a certificate issued LONG ago (more than rotation interval)
        // NotBefore + rotationInterval must be in the past
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={Environment.MachineName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var oldCert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-60), DateTimeOffset.UtcNow.AddDays(365));

        var manager = new CertificateManager(_configMonitorMock.Object, _loggerMock.Object);

        var result = manager.ShouldRotateCertificate(oldCert, config.PullServer!);

        result.Should().BeTrue("a certificate beyond the rotation interval should trigger rotation");
    }

    [Fact]
    public void ShouldRotateCertificate_WithValidCertificateWithinRotationInterval_ReturnsFalse()
    {
        var config = new LcmConfig
        {
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                CertificateSource = CertificateSource.Managed,
                CertificateRotationInterval = TimeSpan.FromDays(90)
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(config);

        // Create a recently issued certificate
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={Environment.MachineName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var newCert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(365));

        var manager = new CertificateManager(_configMonitorMock.Object, _loggerMock.Object);

        var result = manager.ShouldRotateCertificate(newCert, config.PullServer!);

        result.Should().BeFalse("a valid certificate within rotation interval should not trigger rotation");
    }

    [Fact]
    public void ShouldRotateCertificate_WithNonManagedCertificateSource_ReturnsFalse()
    {
        var config = new LcmConfig
        {
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                CertificateSource = CertificateSource.Platform,
                CertificateRotationInterval = TimeSpan.FromDays(30)
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(config);

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={Environment.MachineName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(30));

        var manager = new CertificateManager(_configMonitorMock.Object, _loggerMock.Object);

        var result = manager.ShouldRotateCertificate(cert, config.PullServer!);

        result.Should().BeFalse("Platform certificate source should not trigger rotation");
    }

    [Fact]
    public void ShouldRotateCertificate_WithZeroRotationInterval_ReturnsTrue()
    {
        var config = new LcmConfig
        {
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                CertificateSource = CertificateSource.Managed,
                CertificateRotationInterval = TimeSpan.Zero
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(config);

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={Environment.MachineName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));

        var manager = new CertificateManager(_configMonitorMock.Object, _loggerMock.Object);

        var result = manager.ShouldRotateCertificate(cert, config.PullServer!);

        result.Should().BeTrue("a zero rotation interval should always trigger rotation");
    }
}
