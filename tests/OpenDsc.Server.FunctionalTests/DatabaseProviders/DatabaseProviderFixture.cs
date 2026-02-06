// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;

namespace OpenDsc.Server.FunctionalTests.DatabaseProviders;

public abstract class DatabaseProviderFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected string? ConnectionString { get; set; }

    public abstract string ProviderName { get; }

    public abstract Task InitializeAsync();

    protected abstract Task CleanupAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await CleanupAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = ProviderName,
                ["Database:ConnectionString"] = ConnectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddOptions<HostOptions>()
                .Configure(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30));
        });

        builder.UseEnvironment("Testing");
    }
}
