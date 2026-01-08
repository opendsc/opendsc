// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
builder.Services.AddHostedService<LcmWorker>();

#if WINDOWS
builder.Services.AddWindowsService();
#endif

var host = builder.Build();

Directory.CreateDirectory(configDir);

if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Directory.CreateDirectory(ConfigPaths.GetLcmLogDirectory());
}

await host.RunAsync();
