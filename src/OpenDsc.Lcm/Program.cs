// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net.Security;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenDsc.Lcm;

var builder = Host.CreateApplicationBuilder(args);

var configDir = ConfigPaths.GetLcmConfigDirectory();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile(Path.Combine(configDir, "appsettings.json"), optional: true, reloadOnChange: true)
    .AddEnvironmentVariables("LCM_")
    .AddCommandLine(args);

builder.Services.AddSingleton<IValidateOptions<LcmConfig>, LcmConfigValidator>();
builder.Services.AddOptionsWithValidateOnStart<LcmConfig>()
    .Bind(builder.Configuration.GetSection("LCM"));

builder.Services.AddSingleton<DscExecutor>();
builder.Services.AddSingleton<ICertificateManager, CertificateManager>();

var httpClientBuilder = builder.Services.AddHttpClient<PullServerClient>((sp, client) =>
{
    var lcmMonitor = sp.GetRequiredService<IOptionsMonitor<LcmConfig>>();
    var pullServer = lcmMonitor.CurrentValue.PullServer;
    if (pullServer is not null && !string.IsNullOrWhiteSpace(pullServer.ServerUrl))
    {
        client.BaseAddress = new Uri(pullServer.ServerUrl);
    }
});

httpClientBuilder.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var certificateManager = sp.GetRequiredService<ICertificateManager>();
    var handler = new SocketsHttpHandler();
    handler.SslOptions = new SslClientAuthenticationOptions
    {
        LocalCertificateSelectionCallback = (_, _, _, _, _) => certificateManager.GetClientCertificate()
    };
    return handler;
});

builder.Services.AddHttpClient(PullServerClient.AnonymousClientName, (sp, client) =>
{
    var lcmMonitor = sp.GetRequiredService<IOptionsMonitor<LcmConfig>>();
    var pullServer = lcmMonitor.CurrentValue.PullServer;
    if (pullServer is not null && !string.IsNullOrWhiteSpace(pullServer.ServerUrl))
    {
        client.BaseAddress = new Uri(pullServer.ServerUrl);
    }
});

builder.Services.AddHostedService<LcmWorker>();

#if !WINDOWS
if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.Services.AddSystemd();
    builder.Logging.AddSystemdConsole(options =>
    {
        options.IncludeScopes = false;
        options.TimestampFormat = "HH:mm:ss ";
    });
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
}
#endif

#if WINDOWS
builder.Services.AddWindowsService();
builder.Logging.AddEventLog();
#endif

var host = builder.Build();

await host.RunAsync();
