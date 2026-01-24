// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenDsc.Lcm;
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

    [Fact]
    public async Task RegisterAsync_WithValidRegistrationKey_ReturnsNodeIdAndApiKey()
    {
        using var scope = _factory.Services.CreateScope();
        var httpClient = _factory.CreateClient();
        var serverAddress = httpClient.BaseAddress!.ToString().TrimEnd('/');

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.yaml",
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = serverAddress,
                RegistrationKey = "test-lcm-registration-key"
            }
        };

        var optionsMonitor = new TestOptionsMonitor<LcmConfig>(lcmConfig);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PullServerClient>>();

        var client = new PullServerClient(httpClient, optionsMonitor, logger);

        var result = await client.RegisterAsync();

        result.Should().NotBeNull();
        result!.NodeId.Should().NotBeEmpty();
        result.ApiKey.Should().NotBeNullOrEmpty();
        result.KeyRotationInterval.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidRegistrationKey_ReturnsNull()
    {
        using var scope = _factory.Services.CreateScope();
        var httpClient = _factory.CreateClient();
        var serverAddress = httpClient.BaseAddress!.ToString().TrimEnd('/');

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.yaml",
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = serverAddress,
                RegistrationKey = "invalid-key"
            }
        };

        var optionsMonitor = new TestOptionsMonitor<LcmConfig>(lcmConfig);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PullServerClient>>();

        var client = new PullServerClient(httpClient, optionsMonitor, logger);

        var result = await client.RegisterAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithNullPullServer_ReturnsNull()
    {
        using var scope = _factory.Services.CreateScope();
        var httpClient = _factory.CreateClient();

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.yaml",
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationSource = ConfigurationSource.Local,
            PullServer = null
        };

        var optionsMonitor = new TestOptionsMonitor<LcmConfig>(lcmConfig);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PullServerClient>>();

        var client = new PullServerClient(httpClient, optionsMonitor, logger);

        var result = await client.RegisterAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyRegistrationKey_ReturnsNull()
    {
        using var scope = _factory.Services.CreateScope();
        var httpClient = _factory.CreateClient();
        var serverAddress = httpClient.BaseAddress!.ToString().TrimEnd('/');

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.yaml",
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = serverAddress,
                RegistrationKey = ""
            }
        };

        var optionsMonitor = new TestOptionsMonitor<LcmConfig>(lcmConfig);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PullServerClient>>();

        var client = new PullServerClient(httpClient, optionsMonitor, logger);

        var result = await client.RegisterAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task HasConfigurationChangedAsync_WithNoConfiguration_ReturnsFalse()
    {
        using var scope = _factory.Services.CreateScope();
        var httpClient = _factory.CreateClient();
        var serverAddress = httpClient.BaseAddress!.ToString().TrimEnd('/');

        var registrationResult = await RegisterTestNode(httpClient, serverAddress, scope);

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.yaml",
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = serverAddress,
                NodeId = registrationResult.NodeId,
                ApiKey = registrationResult.ApiKey
            }
        };

        var optionsMonitor = new TestOptionsMonitor<LcmConfig>(lcmConfig);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PullServerClient>>();

        var client = new PullServerClient(httpClient, optionsMonitor, logger);

        var result = await client.HasConfigurationChangedAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetConfigurationAsync_WithNoNodeId_ReturnsNull()
    {
        using var scope = _factory.Services.CreateScope();
        var httpClient = _factory.CreateClient();
        var serverAddress = httpClient.BaseAddress!.ToString().TrimEnd('/');

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.yaml",
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = serverAddress,
                NodeId = null,
                ApiKey = null
            }
        };

        var optionsMonitor = new TestOptionsMonitor<LcmConfig>(lcmConfig);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PullServerClient>>();

        var client = new PullServerClient(httpClient, optionsMonitor, logger);

        var result = await client.GetConfigurationAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConfigurationChecksumAsync_WithNoNodeId_ReturnsNull()
    {
        using var scope = _factory.Services.CreateScope();
        var httpClient = _factory.CreateClient();
        var serverAddress = httpClient.BaseAddress!.ToString().TrimEnd('/');

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.yaml",
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = serverAddress,
                NodeId = null,
                ApiKey = null
            }
        };

        var optionsMonitor = new TestOptionsMonitor<LcmConfig>(lcmConfig);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PullServerClient>>();

        var client = new PullServerClient(httpClient, optionsMonitor, logger);

        var result = await client.GetConfigurationChecksumAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task SubmitReportAsync_WithNoNodeId_ReturnsFalse()
    {
        using var scope = _factory.Services.CreateScope();
        var httpClient = _factory.CreateClient();
        var serverAddress = httpClient.BaseAddress!.ToString().TrimEnd('/');

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.yaml",
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = serverAddress,
                NodeId = null,
                ApiKey = null,
                ReportCompliance = true
            }
        };

        var optionsMonitor = new TestOptionsMonitor<LcmConfig>(lcmConfig);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PullServerClient>>();

        var client = new PullServerClient(httpClient, optionsMonitor, logger);

        var result = await client.SubmitReportAsync(DscOperation.Test, new DscResult());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitReportAsync_WithReportComplianceDisabled_ReturnsTrue()
    {
        using var scope = _factory.Services.CreateScope();
        var httpClient = _factory.CreateClient();
        var serverAddress = httpClient.BaseAddress!.ToString().TrimEnd('/');

        var registrationResult = await RegisterTestNode(httpClient, serverAddress, scope);

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.yaml",
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = serverAddress,
                NodeId = registrationResult.NodeId,
                ApiKey = registrationResult.ApiKey,
                ReportCompliance = false
            }
        };

        var optionsMonitor = new TestOptionsMonitor<LcmConfig>(lcmConfig);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PullServerClient>>();

        var client = new PullServerClient(httpClient, optionsMonitor, logger);

        var result = await client.SubmitReportAsync(DscOperation.Test, new DscResult());

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RotateApiKeyAsync_WithNoNodeId_ReturnsNull()
    {
        using var scope = _factory.Services.CreateScope();
        var httpClient = _factory.CreateClient();
        var serverAddress = httpClient.BaseAddress!.ToString().TrimEnd('/');

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.yaml",
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = serverAddress,
                NodeId = null,
                ApiKey = null
            }
        };

        var optionsMonitor = new TestOptionsMonitor<LcmConfig>(lcmConfig);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PullServerClient>>();

        var client = new PullServerClient(httpClient, optionsMonitor, logger);

        var result = await client.RotateApiKeyAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task RotateApiKeyAsync_WithValidNode_ReturnsNewApiKey()
    {
        using var scope = _factory.Services.CreateScope();
        var httpClient = _factory.CreateClient();
        var serverAddress = httpClient.BaseAddress!.ToString().TrimEnd('/');

        var registrationResult = await RegisterTestNode(httpClient, serverAddress, scope);

        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.yaml",
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = serverAddress,
                NodeId = registrationResult.NodeId,
                ApiKey = registrationResult.ApiKey
            }
        };

        var optionsMonitor = new TestOptionsMonitor<LcmConfig>(lcmConfig);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PullServerClient>>();

        var client = new PullServerClient(httpClient, optionsMonitor, logger);

        var result = await client.RotateApiKeyAsync();

        result.Should().NotBeNull();
        result!.ApiKey.Should().NotBeNullOrEmpty();
        result.ApiKey.Should().NotBe(registrationResult.ApiKey);
        result.KeyRotationInterval.Should().BeGreaterThan(TimeSpan.Zero);
    }

    private static async Task<RegisterNodeResult> RegisterTestNode(HttpClient httpClient, string serverAddress, IServiceScope scope)
    {
        var lcmConfig = new LcmConfig
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationPath = "test.yaml",
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ConfigurationSource = ConfigurationSource.Pull,
            PullServer = new PullServerSettings
            {
                ServerUrl = serverAddress,
                RegistrationKey = "test-lcm-registration-key"
            }
        };

        var optionsMonitor = new TestOptionsMonitor<LcmConfig>(lcmConfig);
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PullServerClient>>();
        var client = new PullServerClient(httpClient, optionsMonitor, logger);

        var result = await client.RegisterAsync();
        return result!;
    }
}

public class TestOptionsMonitor<T> : IOptionsMonitor<T>
{
    private readonly T _currentValue;

    public TestOptionsMonitor(T currentValue)
    {
        _currentValue = currentValue;
    }

    public T CurrentValue => _currentValue;

    public T Get(string? name) => _currentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
