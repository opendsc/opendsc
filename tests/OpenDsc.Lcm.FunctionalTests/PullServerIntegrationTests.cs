// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net.Http.Json;

using AwesomeAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenDsc.Lcm.Contracts;
using OpenDsc.Server.Contracts;

using Xunit;

namespace OpenDsc.Lcm.FunctionalTests;

[Collection("Server")]
[Trait("Category", "Functional")]
public sealed class PullServerIntegrationTests(ServerFixture serverFixture) : IAsyncLifetime
{
    private readonly ServerFixture _server = serverFixture;
    private string? _configId;
    private HttpClient? _httpClient;

    public async ValueTask InitializeAsync()
    {
        if (!_server.IsDockerAvailable)
        {
            return;
        }

        _httpClient = new HttpClient { BaseAddress = new Uri(_server.BaseUrl) };

        // Create a test configuration on the server
        var createRequest = new CreateConfigurationRequest
        {
            Name = "test-config",
            Content = await File.ReadAllTextAsync(Path.Combine("TestConfigurations", "simple.dsc.yaml"))
        };

        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _server.AdminApiKey);
        var response = await _httpClient.PostAsJsonAsync("/configurations", createRequest);
        response.EnsureSuccessStatusCode();
        var configDetails = await response.Content.ReadFromJsonAsync<ConfigurationDetails>();
        _configId = configDetails!.Id.ToString();
    }

    public ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact(Skip = "Requires Docker and current server API")]
    public async Task NodeRegistration_WithValidKey_Succeeds()
    {
        var config = CreateLcmConfig(ConfigurationMode.Monitor, _configId!);

        using var host = CreateTestHost(config);
        await host.StartAsync(TestContext.Current.CancellationToken);

        var pullServerClient = host.Services.GetRequiredService<PullServerClient>();
        var result = await pullServerClient.RegisterAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.NodeId.Should().NotBeEmpty();

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "Requires Docker and current server API")]
    public async Task NodeRegistration_WithInvalidKey_Fails()
    {
        var config = CreateLcmConfig(ConfigurationMode.Monitor, _configId!);
        config.PullServer!.RegistrationKey = "invalid-key";

        using var host = CreateTestHost(config);
        await host.StartAsync(TestContext.Current.CancellationToken);

        var pullServerClient = host.Services.GetRequiredService<PullServerClient>();
        var result = await pullServerClient.RegisterAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "Requires Docker and current server API")]
    public async Task ConfigurationDownload_AfterRegistration_Succeeds()
    {
        var config = CreateLcmConfig(ConfigurationMode.Monitor, _configId!);

        using var host = CreateTestHost(config);
        await host.StartAsync(TestContext.Current.CancellationToken);

        var pullServerClient = host.Services.GetRequiredService<PullServerClient>();
        var registrationResult = await pullServerClient.RegisterAsync(TestContext.Current.CancellationToken);
        registrationResult.Should().NotBeNull();

        config.PullServer!.NodeId = registrationResult!.NodeId;

        var configContent = await pullServerClient.GetConfigurationAsync(TestContext.Current.CancellationToken);
        configContent.Should().NotBeNullOrEmpty();
        configContent.Should().Contain("Microsoft.DSC");

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "Requires Docker and current server API")]
    public async Task ConfigurationChecksum_DetectsChanges()
    {
        var config = CreateLcmConfig(ConfigurationMode.Monitor, _configId!);

        using var host = CreateTestHost(config);
        await host.StartAsync(TestContext.Current.CancellationToken);

        var pullServerClient = host.Services.GetRequiredService<PullServerClient>();
        var registrationResult = await pullServerClient.RegisterAsync(TestContext.Current.CancellationToken);
        config.PullServer!.NodeId = registrationResult!.NodeId;

        var checksum1 = await pullServerClient.GetConfigurationChecksumAsync(TestContext.Current.CancellationToken);
        checksum1.Should().NotBeNull();
        checksum1!.Checksum.Should().NotBeNullOrEmpty();

        // Update configuration on server
        _httpClient!.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _server.AdminApiKey);
        var updateRequest = new UpdateConfigurationRequest
        {
            Content = "# Updated configuration\n" + await File.ReadAllTextAsync(Path.Combine("TestConfigurations", "simple.dsc.yaml"), TestContext.Current.CancellationToken)
        };
        await _httpClient.PutAsJsonAsync($"/configurations/{_configId}", updateRequest, TestContext.Current.CancellationToken);

        var checksum2 = await pullServerClient.GetConfigurationChecksumAsync(TestContext.Current.CancellationToken);
        checksum2.Should().NotBeNull();
        checksum2!.Checksum.Should().NotBe(checksum1.Checksum);

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "Requires Docker and current server API")]
    public async Task CertificateRotation_WithManagedCertificate_Succeeds()
    {
        var config = CreateLcmConfig(ConfigurationMode.Monitor, _configId!);

        using var host = CreateTestHost(config);
        await host.StartAsync(TestContext.Current.CancellationToken);

        var pullServerClient = host.Services.GetRequiredService<PullServerClient>();
        var certificateManager = host.Services.GetRequiredService<CertificateManager>();

        var registrationResult = await pullServerClient.RegisterAsync(TestContext.Current.CancellationToken);
        config.PullServer!.NodeId = registrationResult!.NodeId;

        // Force certificate rotation by creating a new certificate
        var newCert = certificateManager.RotateCertificate(config.PullServer);
        newCert.Should().NotBeNull();

        var rotateResult = await pullServerClient.RotateCertificateAsync(newCert!, TestContext.Current.CancellationToken);
        rotateResult.Should().BeTrue();

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "Requires Docker and current server API")]
    public async Task MonitorMode_WithPullServer_ExecutesPeriodicTest()
    {
        var config = CreateLcmConfig(ConfigurationMode.Monitor, _configId!);
        config.ConfigurationModeInterval = TimeSpan.FromSeconds(2);

        using var host = CreateTestHost(config);

        await host.StartAsync(TestContext.Current.CancellationToken);

        // Let it run for a few cycles
        await Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await host.StopAsync(TestContext.Current.CancellationToken);

        // Verify reports were submitted
        _httpClient!.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _server.AdminApiKey);
        var reportsResponse = await _httpClient.GetAsync("/reports", TestContext.Current.CancellationToken);
        reportsResponse.EnsureSuccessStatusCode();
        var reports = await reportsResponse.Content.ReadFromJsonAsync<List<ReportSummary>>(TestContext.Current.CancellationToken);

        reports.Should().NotBeNull();
        reports!.Count.Should().BeGreaterThan(0);
        reports.Should().Contain(r => r.Operation == OpenDsc.Schema.DscOperation.Test);
    }

    [Fact(Skip = "Requires Docker and current server API")]
    public async Task RemediateMode_WithDrift_AppliesCorrections()
    {
        // Create configuration with a directory resource
        var driftConfig = @"
# yaml-language-server: $schema=https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources:
  - name: test-directory
    type: OpenDsc.FileSystem/Directory
    properties:
      path: " + Path.Combine(Path.GetTempPath(), $"lcm-test-{Guid.NewGuid()}") + @"
";

        // Upload new configuration
        _httpClient!.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _server.AdminApiKey);
        var updateRequest = new UpdateConfigurationRequest { Content = driftConfig };
        await _httpClient.PutAsJsonAsync($"/configurations/{_configId}", updateRequest, TestContext.Current.CancellationToken);

        var config = CreateLcmConfig(ConfigurationMode.Remediate, _configId!);
        config.ConfigurationModeInterval = TimeSpan.FromSeconds(2);

        using var host = CreateTestHost(config);
        await host.StartAsync(TestContext.Current.CancellationToken);

        // Let it run and remediate
        await Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await host.StopAsync(TestContext.Current.CancellationToken);

        // Verify reports contain both Test and Set operations
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _server.AdminApiKey);
        var reportsResponse = await _httpClient.GetAsync("/reports", TestContext.Current.CancellationToken);
        var reports = await reportsResponse.Content.ReadFromJsonAsync<List<ReportSummary>>(TestContext.Current.CancellationToken);

        reports.Should().Contain(r => r.Operation == OpenDsc.Schema.DscOperation.Test);
        reports.Should().Contain(r => r.Operation == OpenDsc.Schema.DscOperation.Set);
    }

    private LcmConfig CreateLcmConfig(ConfigurationMode mode, string configId)
    {
        return new LcmConfig
        {
            ConfigurationMode = mode,
            ConfigurationModeInterval = TimeSpan.FromSeconds(5),
            ConfigurationSource = ConfigurationSource.Pull,
            ConfigurationPath = string.Empty,
            PullServer = new PullServerSettings
            {
                ServerUrl = _server.BaseUrl,
                RegistrationKey = _server.RegistrationKey,
                ReportCompliance = true
            }
        };
    }

    private async Task AssignConfigurationToNodeAsync(Guid nodeId, string configName)
    {
        _httpClient!.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _server.AdminApiKey);
        var assignRequest = new AssignConfigurationRequest { ConfigurationName = configName };
        await _httpClient.PostAsJsonAsync($"/nodes/{nodeId}/configuration", assignRequest);
    }

    private static IHost CreateTestHost(LcmConfig lcmConfig)
    {
        var configDict = new Dictionary<string, string?>
        {
            ["LCM:ConfigurationMode"] = lcmConfig.ConfigurationMode.ToString(),
            ["LCM:ConfigurationModeInterval"] = lcmConfig.ConfigurationModeInterval.ToString(),
            ["LCM:ConfigurationSource"] = lcmConfig.ConfigurationSource.ToString(),
            ["LCM:PullServer:ServerUrl"] = lcmConfig.PullServer?.ServerUrl,
            ["LCM:PullServer:RegistrationKey"] = lcmConfig.PullServer?.RegistrationKey,
            ["LCM:PullServer:ReportCompliance"] = lcmConfig.PullServer?.ReportCompliance.ToString(),
            ["LCM:PullServer:CertificateSource"] = "Managed"
        };

        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(configDict!);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<LcmConfig>(context.Configuration.GetSection("LCM"));
                services.AddSingleton<IValidateOptions<LcmConfig>, LcmConfigValidator>();
                services.AddSingleton<DscExecutor>();
                services.AddSingleton<CertificateManager>();
                services.AddSingleton<PullServerClient>();
                services.AddHostedService<LcmWorker>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Warning);
            })
            .Build();
    }
}
