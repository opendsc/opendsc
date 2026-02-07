// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Https;

using MudBlazor.Services;

using OpenDsc.Server;
using OpenDsc.Server.Authentication;
using OpenDsc.Server.Components;
using OpenDsc.Server.Data;
using OpenDsc.Server.Endpoints;
using OpenDsc.Server.Services;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel((context, options) =>
{
    if (!context.HostingEnvironment.IsEnvironment("Testing"))
    {
        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            httpsOptions.AllowAnyClientCertificate();
        });
    }
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
});

builder.Services.AddSingleton(SourceGenerationContext.Default.Options);

builder.Services.AddOpenApi();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddServerDatabase(builder.Configuration);
builder.Services.AddServerAuthentication(builder.Environment);

// Add authorization policies for Blazor pages
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("nodes.read", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("nodes.write", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("nodes.delete", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("configurations.read", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("configurations.write", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("parameters.read", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("parameters.write", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("reports.read", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("users.manage", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("groups.manage", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("roles.manage", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("settings.read", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("settings.write", policy => policy.RequireAuthenticatedUser());

builder.Services.AddSingleton<IParameterMerger, ParameterMerger>();
builder.Services.AddScoped<IParameterMergeService, ParameterMergeService>();
builder.Services.AddScoped<IParameterSchemaService, ParameterSchemaService>();
builder.Services.AddScoped<IVersionRetentionService, VersionRetentionService>();

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

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAuthenticationEndpoints();
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
app.MapConfigurationSettingsEndpoints();
app.MapRegistrationKeyEndpoints();
app.MapRetentionEndpoints();

app.Run();
