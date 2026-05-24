// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.Extensions.DependencyInjection;

using OpenDsc.Client.Http;
using OpenDsc.Client.Services;
using OpenDsc.Contracts.CompositeConfigurations;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Parameters;
using OpenDsc.Contracts.Reports;
using OpenDsc.Contracts.Settings;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client;

/// <summary>
/// Extension methods for registering OpenDSC client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string HttpClientName = "OpenDscClient";

    /// <summary>
    /// Adds the OpenDSC HTTP client and all service implementations to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An action to configure client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenDscClient(
        this IServiceCollection services,
        Action<DscClientOptions> configure)
    {
        var options = new DscClientOptions
        {
            BaseAddress = new Uri("https://localhost/"),
            Credential = null!
        };

        configure(options);

        services.AddTransient(_ => options.Credential);
        services.AddTransient<DscAuthenticationHandler>();

        var httpClientBuilder = services
            .AddHttpClient(HttpClientName, client =>
            {
                client.BaseAddress = options.BaseAddress;
                if (options.Timeout.HasValue)
                {
                    client.Timeout = options.Timeout.Value;
                }
            })
            .AddHttpMessageHandler<DscAuthenticationHandler>();

        services.AddTransient(sp =>
            new ConfigurationHttpService(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName)));
        services.AddTransient<IConfigurationPermissions>(sp =>
            sp.GetRequiredService<ConfigurationHttpService>());
        services.AddTransient<IConfigurationSettings>(sp =>
            sp.GetRequiredService<ConfigurationHttpService>());
        services.AddTransient<IConfigurationReader>(sp =>
            sp.GetRequiredService<ConfigurationHttpService>());
        services.AddTransient<IConfigurationManager>(sp =>
            sp.GetRequiredService<ConfigurationHttpService>());
        services.AddTransient<IConfigurationFileManager>(sp =>
            sp.GetRequiredService<ConfigurationHttpService>());

        services.AddTransient(sp =>
            new CompositeConfigurationHttpService(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName)));
        services.AddTransient<ICompositeConfigurationPermissions>(sp =>
            sp.GetRequiredService<CompositeConfigurationHttpService>());
        services.AddTransient<ICompositeConfigurationReader>(sp =>
            sp.GetRequiredService<CompositeConfigurationHttpService>());
        services.AddTransient<ICompositeConfigurationManager>(sp =>
            sp.GetRequiredService<CompositeConfigurationHttpService>());

        services.AddTransient(sp =>
            new NodeHttpService(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName)));
        services.AddTransient<INodeManager>(sp =>
            sp.GetRequiredService<NodeHttpService>());
        services.AddTransient<INodeReader>(sp =>
            sp.GetRequiredService<NodeHttpService>());
        services.AddTransient<INodeConfigurationManager>(sp =>
            sp.GetRequiredService<NodeHttpService>());
        services.AddTransient<INodeTagManager>(sp =>
            sp.GetRequiredService<NodeHttpService>());

        services.AddTransient(sp =>
            new HealthHttpService(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName)));
        services.AddTransient<IHealthService>(sp =>
            sp.GetRequiredService<HealthHttpService>());

        services.AddTransient(sp =>
            new SettingsHttpService(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName)));
        services.AddTransient<ISettingsReader>(sp =>
            sp.GetRequiredService<SettingsHttpService>());
        services.AddTransient<ISettingsManager>(sp =>
            sp.GetRequiredService<SettingsHttpService>());

        services.AddTransient(sp =>
            new ScopeHttpService(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName)));
        services.AddTransient<IScopeReader>(sp =>
            sp.GetRequiredService<ScopeHttpService>());
        services.AddTransient<IScopeManager>(sp =>
            sp.GetRequiredService<ScopeHttpService>());

        services.AddTransient(sp =>
            new ParameterHttpService(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName)));
        services.AddTransient<IParameterReader>(sp =>
            sp.GetRequiredService<ParameterHttpService>());
        services.AddTransient<IParameterManager>(sp =>
            sp.GetRequiredService<ParameterHttpService>());
        services.AddTransient<IParameterPermissions>(sp =>
            sp.GetRequiredService<ParameterHttpService>());

        services.AddTransient(sp =>
            new ReportHttpService(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName)));
        services.AddTransient<IReportService>(sp =>
            sp.GetRequiredService<ReportHttpService>());

        services.AddTransient(sp =>
            new UserHttpService(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName)));
        services.AddTransient<IUserReader>(sp =>
            sp.GetRequiredService<UserHttpService>());
        services.AddTransient<IUserManager>(sp =>
            sp.GetRequiredService<UserHttpService>());

        services.AddTransient(sp =>
            new GroupHttpService(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName)));
        services.AddTransient<IGroupReader>(sp =>
            sp.GetRequiredService<GroupHttpService>());
        services.AddTransient<IGroupManager>(sp =>
            sp.GetRequiredService<GroupHttpService>());

        services.AddTransient(sp =>
            new RoleHttpService(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName)));
        services.AddTransient<IRoleReader>(sp =>
            sp.GetRequiredService<RoleHttpService>());
        services.AddTransient<IRoleManager>(sp =>
            sp.GetRequiredService<RoleHttpService>());

        services.AddTransient(sp =>
            new RegistrationKeyHttpService(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName)));
        services.AddTransient<IRegistrationKeyService>(sp =>
            sp.GetRequiredService<RegistrationKeyHttpService>());
        services.AddTransient<IRegistrationKeyReader>(sp =>
            sp.GetRequiredService<RegistrationKeyHttpService>());
        services.AddTransient<IRegistrationKeyManager>(sp =>
            sp.GetRequiredService<RegistrationKeyHttpService>());

        return services;
    }
}
