// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using OpenDsc.Schema;

using Xunit;

namespace OpenDsc.Lcm.IntegrationTests;

[Trait("Category", "Integration")]
public class PullServerClientTests : IClassFixture<LcmTestServerFactory>
{
    private readonly LcmTestServerFactory _factory;

    public PullServerClientTests(LcmTestServerFactory factory)
    {
        _factory = factory;
    }

    private static IOptionsMonitor<LcmConfig> CreateMonitor(LcmConfig config)
    {
        var monitor = new TestOptionsMonitor<LcmConfig>(config);
        return monitor;
    }

    [Fact]
    public async Task RegisterAsync_WithValidRegistrationKey_ReturnsNodeIdOnly()
    {
        var httpClient = _factory.CreateClient();
        var lcmConfig = new LcmConfig
        {
            PullServer = new PullServerSettings
            {
                ServerUrl = httpClient.BaseAddress!.ToString().TrimEnd('/'),
                RegistrationKey = "test-lcm-registration-key"
            }
        };
        var monitor = CreateMonitor(lcmConfig);
        var certManager = new NullCertificateManager();

        var client = new PullServerClient(
            httpClient,
            monitor,
            certManager,
            NullLogger<PullServerClient>.Instance);

        var result = await client.RegisterAsync();

        result.Should().NotBeNull();
        result!.NodeId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidRegistrationKey_ReturnsNull()
    {
        var httpClient = _factory.CreateClient();
        var lcmConfig = new LcmConfig
        {
            PullServer = new PullServerSettings
            {
                ServerUrl = httpClient.BaseAddress!.ToString().TrimEnd('/'),
                RegistrationKey = "invalid-key"
            }
        };
        var monitor = CreateMonitor(lcmConfig);
        var certificateManager = new NullCertificateManager();

        var client = new PullServerClient(
            httpClient,
            monitor,
            certificateManager,
            NullLogger<PullServerClient>.Instance);

        var result = await client.RegisterAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_UpdatesNodeIdInSettings()
    {
        var httpClient = _factory.CreateClient();
        var pullServerSettings = new PullServerSettings
        {
            ServerUrl = httpClient.BaseAddress!.ToString().TrimEnd('/'),
            RegistrationKey = "test-lcm-registration-key"
        };
        var lcmConfig = new LcmConfig
        {
            PullServer = pullServerSettings
        };
        var monitor = CreateMonitor(lcmConfig);
        var certificateManager = new NullCertificateManager();

        var client = new PullServerClient(
            httpClient,
            monitor,
            certificateManager,
            NullLogger<PullServerClient>.Instance);

        var result = await client.RegisterAsync();

        pullServerSettings.NodeId.Should().Be(result!.NodeId);
    }

    [Fact]
    public async Task GetConfigurationAsync_WithoutRegistration_ReturnsNull()
    {
        var httpClient = _factory.CreateClient();
        var lcmConfig = new LcmConfig
        {
            PullServer = new PullServerSettings
            {
                ServerUrl = httpClient.BaseAddress!.ToString(),
                RegistrationKey = "test-lcm-registration-key",
                NodeId = null
            }
        };
        var monitor = CreateMonitor(lcmConfig);
        var certificateManager = new CertificateManager(
            monitor,
            NullLogger<CertificateManager>.Instance);

        var client = new PullServerClient(
            httpClient,
            monitor,
            certificateManager,
            NullLogger<PullServerClient>.Instance);

        var result = await client.GetConfigurationAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConfigurationAsync_WithValidNodeId_ReturnsConfiguration()
    {
        var httpClient = _factory.CreateClient();
        var lcmConfig = new LcmConfig
        {
            PullServer = new PullServerSettings
            {
                ServerUrl = httpClient.BaseAddress!.ToString().TrimEnd('/'),
                RegistrationKey = "test-lcm-registration-key"
            }
        };
        var monitor = CreateMonitor(lcmConfig);
        var certificateManager = new NullCertificateManager();

        var client = new PullServerClient(
            httpClient,
            monitor,
            certificateManager,
            NullLogger<PullServerClient>.Instance);

        var registerResult = await client.RegisterAsync();
        registerResult.Should().NotBeNull();

        var db = _factory.Services.GetRequiredService<OpenDsc.Server.Data.ServerDbContext>();
        var node = await db.Nodes.FirstOrDefaultAsync(n => n.Id == registerResult!.NodeId);
        node.Should().NotBeNull();
        node!.ConfigurationName = "test-config";
        await db.SaveChangesAsync();

        var result = await client.GetConfigurationAsync();

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("resources: []");
    }

    [Fact]
    public async Task HasConfigurationChangedAsync_WithSameChecksum_ReturnsFalse()
    {
        var httpClient = _factory.CreateClient();
        var lcmConfig = new LcmConfig
        {
            PullServer = new PullServerSettings
            {
                ServerUrl = httpClient.BaseAddress!.ToString().TrimEnd('/'),
                RegistrationKey = "test-lcm-registration-key",
                ConfigurationChecksum = "test-checksum"
            }
        };
        var monitor = CreateMonitor(lcmConfig);
        var certificateManager = new NullCertificateManager();

        var client = new PullServerClient(
            httpClient,
            monitor,
            certificateManager,
            NullLogger<PullServerClient>.Instance);

        var registerResult = await client.RegisterAsync();
        registerResult.Should().NotBeNull();

        var db = _factory.Services.GetRequiredService<OpenDsc.Server.Data.ServerDbContext>();
        var node = await db.Nodes.FirstOrDefaultAsync(n => n.Id == registerResult!.NodeId);
        node.Should().NotBeNull();
        node!.ConfigurationName = "test-config";
        await db.SaveChangesAsync();

        var result = await client.HasConfigurationChangedAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RotateCertificateAsync_WithValidCertificate_ReturnsTrue()
    {
        var httpClient = _factory.CreateClient();
        var lcmConfig = new LcmConfig
        {
            PullServer = new PullServerSettings
            {
                ServerUrl = httpClient.BaseAddress!.ToString().TrimEnd('/'),
                RegistrationKey = "test-lcm-registration-key"
            }
        };
        var monitor = CreateMonitor(lcmConfig);
        var certificateManager = new NullCertificateManager();

        var client = new PullServerClient(
            httpClient,
            monitor,
            certificateManager,
            NullLogger<PullServerClient>.Instance);

        var registerResult = await client.RegisterAsync();
        registerResult.Should().NotBeNull();

        using var newCert = GenerateTestCertificate();

        var result = await client.RotateCertificateAsync(newCert);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitReportAsync_WithoutNodeId_DoesNotThrow()
    {
        var httpClient = _factory.CreateClient();
        var lcmConfig = new LcmConfig
        {
            PullServer = new PullServerSettings
            {
                ServerUrl = httpClient.BaseAddress!.ToString(),
                RegistrationKey = "test-lcm-registration-key",
                NodeId = null
            }
        };
        var monitor = CreateMonitor(lcmConfig);
        var certificateManager = new CertificateManager(
            monitor,
            NullLogger<CertificateManager>.Instance);

        var client = new PullServerClient(
            httpClient,
            monitor,
            certificateManager,
            NullLogger<PullServerClient>.Instance);
        var result = new DscResult();

        var act = async () => await client.SubmitReportAsync(DscOperation.Test, result);

        await act.Should().NotThrowAsync();
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public TestOptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class NullCertificateManager : ICertificateManager
    {
        public X509Certificate2? GetClientCertificate() => null;

        public X509Certificate2? RotateCertificate(PullServerSettings pullServer) => null;

        public bool ShouldRotateCertificate(X509Certificate2? currentCertificate, PullServerSettings pullServer) => false;
    }

    private static X509Certificate2 GenerateTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var subject = new X500DistinguishedName($"CN=TestCertificate-{Guid.NewGuid()}");
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.2")],
                false));

        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return cert;
    }

    [Fact]
    public void PullServerClient_HttpClient_ShouldBeConfiguredWithCertificate()
    {
        var testCert = GenerateTestCertificate();
        var certManager = new TestCertificateManager(testCert);
        var lcmConfig = new LcmConfig
        {
            PullServer = new PullServerSettings
            {
                ServerUrl = "https://localhost:5001",
                RegistrationKey = "test-key",
                CertificateSource = CertificateSource.Managed
            }
        };
        var monitor = CreateMonitor(lcmConfig);

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton<ICertificateManager>(certManager);
        services.AddSingleton<IOptionsMonitor<LcmConfig>>(monitor);
        services.AddHttpClient<PullServerClient>((sp, client) =>
        {
            var lcmMonitor = sp.GetRequiredService<IOptionsMonitor<LcmConfig>>();
            var pullServer = lcmMonitor.CurrentValue.PullServer;
            if (pullServer is not null && !string.IsNullOrWhiteSpace(pullServer.ServerUrl))
            {
                client.BaseAddress = new Uri(pullServer.ServerUrl);
            }
        })
        .ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var certificateManager = sp.GetRequiredService<ICertificateManager>();
            var cert = certificateManager.GetClientCertificate();

            var handler = new HttpClientHandler();
            if (cert is not null)
            {
                handler.ClientCertificates.Add(cert);
            }
            return handler;
        });

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient(nameof(PullServerClient));

        httpClient.Should().NotBeNull();
        httpClient.BaseAddress.Should().Be(new Uri("https://localhost:5001"));

        certManager.GetClientCertificateCalled.Should().BeTrue();
    }

    private sealed class TestCertificateManager : ICertificateManager
    {
        private readonly X509Certificate2 _certificate;

        public TestCertificateManager(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        public bool GetClientCertificateCalled { get; private set; }

        public X509Certificate2? GetClientCertificate()
        {
            GetClientCertificateCalled = true;
            return _certificate;
        }

        public X509Certificate2? RotateCertificate(PullServerSettings pullServer) => null;

        public bool ShouldRotateCertificate(X509Certificate2? currentCertificate, PullServerSettings pullServer) => false;
    }
}
