// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;

using Microsoft.AspNetCore.Server.Kestrel.Https;

using MudBlazor.Services;

using OpenDsc.Server;
using OpenDsc.Server.Authentication;
using OpenDsc.Server.Components;
using OpenDsc.Server.Data;
using OpenDsc.Server.Endpoints;
using OpenDsc.Server.Middleware;
using OpenDsc.Server.Services;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var configDir = ServerPaths.GetServerConfigDirectory();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile(Path.Combine(configDir, "appsettings.json"), optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.Configure<ServerConfig>(builder.Configuration.GetSection("Server:Data"));

builder.WebHost.ConfigureKestrel((context, options) =>
{
    if (!context.HostingEnvironment.IsEnvironment("Testing"))
    {
        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            httpsOptions.AllowAnyClientCertificate();
        });
    }
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
});

builder.Services.AddSingleton(SourceGenerationContext.Default.Options);

builder.Services.AddHttpContextAccessor();

builder.Services.AddOpenApi();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddScoped<ThemeService>();

builder.Services.AddServerDatabase(builder.Configuration);
builder.Services.AddServerAuthentication(builder.Environment, builder.Configuration);

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IParameterMerger, ParameterMerger>();
builder.Services.AddScoped<IParameterMergeService, ParameterMergeService>();
builder.Services.AddScoped<IParameterSchemaService, ParameterSchemaService>();
builder.Services.AddScoped<IParameterSchemaBuilder, ParameterSchemaBuilder>();
builder.Services.AddScoped<IParameterValidator, ParameterValidator>();
builder.Services.AddScoped<IParameterCompatibilityService, ParameterCompatibilityService>();
builder.Services.AddScoped<IVersionRetentionService, VersionRetentionService>();
builder.Services.AddHostedService<RetentionBackgroundService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IParameterService, ParameterService>();
builder.Services.AddScoped<IJsonYamlConverter, JsonYamlConverter>();
builder.Services.AddSingleton<NodeEndpoints>();

#if !WINDOWS
if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.Host.UseSystemd();
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

var app = builder.Build();

await app.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("OpenDSC Server")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PasswordChangeEnforcementMiddleware>();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAuthenticationEndpoints();
app.MapOidcEndpoints(builder.Configuration);
app.MapUserEndpoints();
app.MapGroupEndpoints();
app.MapRoleEndpoints();
app.MapHealthEndpoints();
app.MapScopeTypeEndpoints();
app.MapScopeValueEndpoints();
app.MapNodeTagEndpoints();
app.MapParameterEndpoints();
app.MapNodeEndpoints();
app.MapConfigurationEndpoints();
app.MapCompositeConfigurationEndpoints();
app.MapReportEndpoints();
app.MapSettingsEndpoints();
app.MapValidationSettingsEndpoints();
app.MapRetentionSettingsEndpoints();
app.MapConfigurationSettingsEndpoints();
app.MapRegistrationKeyEndpoints();
app.MapRetentionEndpoints();

app.Run();
