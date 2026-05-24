// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Headers;

using AwesomeAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

using OpenDsc.Client.Authentication;
using OpenDsc.Client.Services;
using OpenDsc.Contracts.CompositeConfigurations;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Parameters;
using OpenDsc.Contracts.Reports;
using OpenDsc.Contracts.Settings;
using OpenDsc.Contracts.Users;

using Xunit;

namespace OpenDsc.Client.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddOpenDscClient_Registers_All_Client_Services()
    {
        var services = new ServiceCollection();

        services.AddOpenDscClient(options =>
        {
            options.BaseAddress = new Uri("https://server.test/");
            options.Credential = new ApiKeyCredential("pat_token");
        });

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ConfigurationHttpService>().Should().NotBeNull();
        provider.GetRequiredService<IConfigurationPermissions>().Should().BeOfType<ConfigurationHttpService>();
        provider.GetRequiredService<IConfigurationSettings>().Should().BeOfType<ConfigurationHttpService>();
        provider.GetRequiredService<IConfigurationReader>().Should().BeOfType<ConfigurationHttpService>();
        provider.GetRequiredService<IConfigurationManager>().Should().BeOfType<ConfigurationHttpService>();
        provider.GetRequiredService<IConfigurationFileManager>().Should().BeOfType<ConfigurationHttpService>();

        provider.GetRequiredService<CompositeConfigurationHttpService>().Should().NotBeNull();
        provider.GetRequiredService<ICompositeConfigurationPermissions>().Should().BeOfType<CompositeConfigurationHttpService>();
        provider.GetRequiredService<ICompositeConfigurationReader>().Should().BeOfType<CompositeConfigurationHttpService>();
        provider.GetRequiredService<ICompositeConfigurationManager>().Should().BeOfType<CompositeConfigurationHttpService>();

        provider.GetRequiredService<NodeHttpService>().Should().NotBeNull();
        provider.GetRequiredService<INodeManager>().Should().BeOfType<NodeHttpService>();
        provider.GetRequiredService<INodeReader>().Should().BeOfType<NodeHttpService>();
        provider.GetRequiredService<INodeConfigurationManager>().Should().BeOfType<NodeHttpService>();
        provider.GetRequiredService<INodeTagManager>().Should().BeOfType<NodeHttpService>();

        provider.GetRequiredService<HealthHttpService>().Should().NotBeNull();
        provider.GetRequiredService<IHealthService>().Should().BeOfType<HealthHttpService>();

        provider.GetRequiredService<SettingsHttpService>().Should().NotBeNull();
        provider.GetRequiredService<ISettingsReader>().Should().BeOfType<SettingsHttpService>();
        provider.GetRequiredService<ISettingsManager>().Should().BeOfType<SettingsHttpService>();

        provider.GetRequiredService<ScopeHttpService>().Should().NotBeNull();
        provider.GetRequiredService<IScopeReader>().Should().BeOfType<ScopeHttpService>();
        provider.GetRequiredService<IScopeManager>().Should().BeOfType<ScopeHttpService>();

        provider.GetRequiredService<ParameterHttpService>().Should().NotBeNull();
        provider.GetRequiredService<IParameterReader>().Should().BeOfType<ParameterHttpService>();
        provider.GetRequiredService<IParameterManager>().Should().BeOfType<ParameterHttpService>();
        provider.GetRequiredService<IParameterPermissions>().Should().BeOfType<ParameterHttpService>();

        provider.GetRequiredService<ReportHttpService>().Should().NotBeNull();
        provider.GetRequiredService<IReportService>().Should().BeOfType<ReportHttpService>();

        provider.GetRequiredService<UserHttpService>().Should().NotBeNull();
        provider.GetRequiredService<IUserReader>().Should().BeOfType<UserHttpService>();
        provider.GetRequiredService<IUserManager>().Should().BeOfType<UserHttpService>();

        provider.GetRequiredService<GroupHttpService>().Should().NotBeNull();
        provider.GetRequiredService<IGroupReader>().Should().BeOfType<GroupHttpService>();
        provider.GetRequiredService<IGroupManager>().Should().BeOfType<GroupHttpService>();

        provider.GetRequiredService<RoleHttpService>().Should().NotBeNull();
        provider.GetRequiredService<IRoleReader>().Should().BeOfType<RoleHttpService>();
        provider.GetRequiredService<IRoleManager>().Should().BeOfType<RoleHttpService>();

        provider.GetRequiredService<RegistrationKeyHttpService>().Should().NotBeNull();
        provider.GetRequiredService<IRegistrationKeyService>().Should().BeOfType<RegistrationKeyHttpService>();
        provider.GetRequiredService<IRegistrationKeyReader>().Should().BeOfType<RegistrationKeyHttpService>();
        provider.GetRequiredService<IRegistrationKeyManager>().Should().BeOfType<RegistrationKeyHttpService>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddOpenDscClient_Configures_BaseAddress_And_Timeout()
    {
        var services = new ServiceCollection();

        services.AddOpenDscClient(options =>
        {
            options.BaseAddress = new Uri("https://server.test/");
            options.Credential = new ApiKeyCredential("pat_token");
            options.Timeout = TimeSpan.FromSeconds(9);
        });

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("OpenDscClient");

        client.BaseAddress.Should().Be(new Uri("https://server.test/"));
        client.Timeout.Should().Be(TimeSpan.FromSeconds(9));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AddOpenDscClient_Adds_Bearer_Authorization_Header()
    {
        var recordingHandler = new RecordingMessageHandler();
        var services = new ServiceCollection();

        services.AddSingleton<IHttpMessageHandlerBuilderFilter>(new PrimaryHandlerFilter(recordingHandler));
        services.AddOpenDscClient(options =>
        {
            options.BaseAddress = new Uri("https://server.test/");
            options.Credential = new ApiKeyCredential("pat_token");
        });

        using var provider = services.BuildServiceProvider();
        var healthService = provider.GetRequiredService<IHealthService>();

        var result = await healthService.CanConnectAsync(TestContext.Current.CancellationToken);

        result.Should().BeTrue();
        recordingHandler.LastAuthorization.Should().BeEquivalentTo(new AuthenticationHeaderValue("Bearer", "pat_token"));
    }

    private sealed class RecordingMessageHandler : HttpMessageHandler
    {
        public AuthenticationHeaderValue? LastAuthorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastAuthorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class PrimaryHandlerFilter(HttpMessageHandler primaryHandler) : IHttpMessageHandlerBuilderFilter
    {
        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
            => builder =>
            {
                next(builder);
                builder.PrimaryHandler = primaryHandler;
            };
    }
}
