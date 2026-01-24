// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenDsc.Lcm;

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
