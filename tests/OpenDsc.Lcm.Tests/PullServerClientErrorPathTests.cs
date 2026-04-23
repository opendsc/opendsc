// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using AwesomeAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;
using Moq.Protected;

using OpenDsc.Lcm.Contracts;

using Xunit;

namespace OpenDsc.Lcm.Tests;

[Trait("Category", "Unit")]
public class PullServerClientErrorPathTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<PullServerClient>> _loggerMock;
    private readonly Mock<IOptionsMonitor<LcmConfig>> _configMonitorMock;
    private readonly Mock<ICertificateManager> _certificateManagerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly HttpClient _httpClient;
    private readonly LcmConfig _config;

    public PullServerClientErrorPathTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<PullServerClient>>();
        _configMonitorMock = new Mock<IOptionsMonitor<LcmConfig>>();
        _certificateManagerMock = new Mock<ICertificateManager>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

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
    public async Task RegisterAsync_WithNullServerUrl_ReturnsNull()
    {
        var configWithNullServerUrl = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = string.Empty,
                RegistrationKey = "test-key"
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithNullServerUrl);

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.RegisterAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithNullRegistrationKey_ReturnsNull()
    {
        var configWithNullKey = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = "http://localhost:8080",
                RegistrationKey = string.Empty
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithNullKey);

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.RegisterAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task RegisterAsync_WithHttpErrorStatus_ReturnsNull(HttpStatusCode statusCode)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("{\"error\":\"Server error\"}", Encoding.UTF8, "application/json")
            });

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.RegisterAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task HasConfigurationChangedAsync_WithNullPullServer_ReturnsFalse()
    {
        var configWithoutPullServer = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithoutPullServer);

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.HasConfigurationChangedAsync(TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasConfigurationChangedAsync_WithNullNodeId_ReturnsFalse()
    {
        var configWithoutNodeId = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = "http://localhost:8080",
                NodeId = null
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithoutNodeId);

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.HasConfigurationChangedAsync(TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task HasConfigurationChangedAsync_WithHttpError_ReturnsFalse(HttpStatusCode statusCode)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("{\"error\":\"Server error\"}", Encoding.UTF8, "application/json")
            });

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.HasConfigurationChangedAsync(TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetConfigurationAsync_WithNullPullServer_ReturnsNull()
    {
        var configWithoutPullServer = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithoutPullServer);

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.GetConfigurationAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetConfigurationAsync_WithHttpError_ReturnsNull(HttpStatusCode statusCode)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("Error", Encoding.UTF8, "text/plain")
            });

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.GetConfigurationAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConfigurationBundleAsync_WithNullPullServer_ReturnsNull()
    {
        var configWithoutPullServer = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithoutPullServer);

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.GetConfigurationBundleAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetConfigurationBundleAsync_WithHttpError_ReturnsNull(HttpStatusCode statusCode)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new ByteArrayContent(new byte[] { })
            });

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.GetConfigurationBundleAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SubmitReportAsync_WithNullPullServer_ReturnsFalse()
    {
        var configWithoutPullServer = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithoutPullServer);

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.SubmitReportAsync(OpenDsc.Schema.DscOperation.Test, new OpenDsc.Schema.DscResult(), TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitReportAsync_WithReportComplianceFalse_ReturnsTrue()
    {
        var configNoReporting = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = "http://localhost:8080",
                NodeId = Guid.NewGuid(),
                ReportCompliance = false
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configNoReporting);

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.SubmitReportAsync(OpenDsc.Schema.DscOperation.Test, new OpenDsc.Schema.DscResult(), TestContext.Current.CancellationToken);

        result.Should().BeTrue("when ReportCompliance is false, the method returns true without submitting");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SubmitReportAsync_WithHttpError_ReturnsFalse(HttpStatusCode statusCode)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("Error", Encoding.UTF8, "text/plain")
            });

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.SubmitReportAsync(OpenDsc.Schema.DscOperation.Test, new OpenDsc.Schema.DscResult(), TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPublicSettingsAsync_WithNullServerUrl_ReturnsNull()
    {
        var configWithoutServerUrl = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = string.Empty
            }
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithoutServerUrl);

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.GetPublicSettingsAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetPublicSettingsAsync_WithHttpError_ReturnsNull(HttpStatusCode statusCode)
    {
        var anonClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(PullServerClient.AnonymousClientName)).Returns(anonClient);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("{\"error\":\"Not found\"}", Encoding.UTF8, "application/json")
            });

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var result = await client.GetPublicSettingsAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateLcmStatusAsync_WithNullPullServer_ReturnsVoid()
    {
        var configWithoutPullServer = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithoutPullServer);

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var action = () => client.UpdateLcmStatusAsync(new LcmStatus(), TestContext.Current.CancellationToken);

        await action();  // Should not throw
    }

    [Fact]
    public async Task ReportLcmConfigAsync_WithNullPullServer_ReturnsVoid()
    {
        var configWithoutPullServer = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        _configMonitorMock.Setup(x => x.CurrentValue).Returns(configWithoutPullServer);

        var client = new PullServerClient(_httpClient, _httpClientFactoryMock.Object, _configMonitorMock.Object, _certificateManagerMock.Object, _loggerMock.Object);

        var action = () => client.ReportLcmConfigAsync(TestContext.Current.CancellationToken);

        await action();  // Should not throw
    }
}
