// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

using AwesomeAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;
using Moq.Protected;

using OpenDsc.Schema;

using Xunit;

namespace OpenDsc.Lcm.Tests;

public sealed class PullServerClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<PullServerClient>> _loggerMock;
    private readonly Mock<IOptionsMonitor<LcmConfig>> _configMonitorMock;
    private readonly Mock<ICertificateManager> _certificateManagerMock;
    private readonly HttpClient _httpClient;
    private readonly LcmConfig _config;

    public PullServerClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<PullServerClient>>();
        _configMonitorMock = new Mock<IOptionsMonitor<LcmConfig>>();
        _certificateManagerMock = new Mock<ICertificateManager>();

        _config = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationPath = "test.dsc.yaml",
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = "http://localhost:8080",
                RegistrationKey = "test-registration-key",
                NodeId = Guid.NewGuid(),
                ConfigurationChecksum = "abc123",
                CertificateSource = CertificateSource.Managed
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(_config);

        _certificateManagerMock.Setup(x => x.GetClientCertificate()).Returns((X509Certificate2?)null);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
    }

    [Fact]
    public async Task RegisterAsync_WithValidKey_ReturnsRegistrationResult()
    {
        var response = new RegisterNodeResponse
        {
            NodeId = Guid.NewGuid()
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.AbsolutePath == "/api/v1/nodes/register"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(response, options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
            });

        var client = new PullServerClient(_httpClient, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.RegisterAsync();

        result.Should().NotBeNull();
        result!.NodeId.Should().Be(response.NodeId);
    }

    [Fact]
    public async Task RegisterAsync_WhenPullServerNotConfigured_ReturnsNull()
    {
        var configWithoutPullServer = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationPath = "test.dsc.yaml",
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithoutPullServer);

        var client = new PullServerClient(_httpClient, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.RegisterAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WhenServerReturnsError_ReturnsNull()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":\"Invalid registration key\"}", Encoding.UTF8, "application/json")
            });

        var client = new PullServerClient(_httpClient, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.RegisterAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task RotateCertificateAsync_WithValidCertificate_ReturnsTrue()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.AbsolutePath.Contains("/rotate-certificate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        var client = new PullServerClient(_httpClient, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        // Create a test certificate for rotation
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            $"CN={Environment.MachineName}",
            rsa,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90));
        var result = await client.RotateCertificateAsync(cert, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetConfigurationAsync_ReturnsConfigurationContent()
    {
        var configContent = "# DSC Configuration\nresources:\n  - name: test";

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.AbsolutePath.Contains("/configuration")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(configContent, Encoding.UTF8, "application/yaml")
            });

        var client = new PullServerClient(_httpClient, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.GetConfigurationAsync();

        result.Should().Be(configContent);
    }

    [Fact]
    public async Task GetConfigurationAsync_WhenNotConfigured_ReturnsNull()
    {
        var configWithoutPullServer = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationPath = "test.dsc.yaml",
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithoutPullServer);

        var client = new PullServerClient(_httpClient, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.GetConfigurationAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConfigurationChecksumAsync_ReturnsChecksum()
    {
        var response = new ConfigurationChecksumResponse
        {
            Checksum = "new-checksum-456"
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.AbsolutePath.Contains("/checksum")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(response, options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
            });

        var client = new PullServerClient(_httpClient, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.GetConfigurationChecksumAsync();

        result.Should().Be(response.Checksum);
    }

    [Fact]
    public async Task HasConfigurationChangedAsync_WhenChecksumDiffers_ReturnsTrue()
    {
        var response = new ConfigurationChecksumResponse
        {
            Checksum = "different-checksum"
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(response, options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
            });

        var client = new PullServerClient(_httpClient, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.HasConfigurationChangedAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasConfigurationChangedAsync_WhenChecksumMatches_ReturnsFalse()
    {
        var response = new ConfigurationChecksumResponse
        {
            Checksum = _config.PullServer!.ConfigurationChecksum!
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(response, options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
            });

        var client = new PullServerClient(_httpClient, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.HasConfigurationChangedAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitReportAsync_SendsReportToServer()
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.AbsolutePath.Contains("/reports")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        var client = new PullServerClient(_httpClient, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = new DscResult
        {
            Messages = [],
            Results = [],
            HadErrors = false
        };

        await client.SubmitReportAsync(DscOperation.Test, result);

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.AbsolutePath.Contains("/reports")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SubmitReportAsync_WhenNotConfigured_DoesNotSend()
    {
        var configWithoutPullServer = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationPath = "test.dsc.yaml",
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithoutPullServer);

        var client = new PullServerClient(_httpClient, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = new DscResult
        {
            Messages = [],
            Results = [],
            HadErrors = false
        };

        await client.SubmitReportAsync(DscOperation.Test, result);

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public void HttpClient_ShouldBeConfiguredWithClientCertificate_WhenCertificateIsAvailable()
    {
        var services = new ServiceCollection();
        var testCert = GenerateTestCertificate();
        var certManagerMock = new Mock<ICertificateManager>();
        certManagerMock.Setup(x => x.GetClientCertificate()).Returns(testCert);

        var config = new LcmConfig
        {
            PullServer = new PullServerSettings
            {
                ServerUrl = "https://localhost:5001",
                CertificateSource = CertificateSource.Managed
            }
        };
        var configMonitorMock = new Mock<IOptionsMonitor<LcmConfig>>();
        configMonitorMock.Setup(x => x.CurrentValue).Returns(config);

        services.AddSingleton(certManagerMock.Object);
        services.AddSingleton(configMonitorMock.Object);
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
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(PullServerClient));

        httpClient.Should().NotBeNull();
        httpClient.BaseAddress.Should().Be(new Uri("https://localhost:5001"));

        certManagerMock.Verify(x => x.GetClientCertificate(), Times.Once);
    }

    private static X509Certificate2 GenerateTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=test-node",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                false));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.2")],
                false));

        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1));

        return X509CertificateLoader.LoadPkcs12(
            cert.Export(X509ContentType.Pfx),
            null,
            X509KeyStorageFlags.Exportable);
    }
}
