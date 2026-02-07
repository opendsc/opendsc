// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Https;

using OpenDsc.Server;
using OpenDsc.Server.Authentication;
using OpenDsc.Server.Data;
using OpenDsc.Server.Endpoints;
using OpenDsc.Server.Services;

using Scalar.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args);

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

builder.Services.AddServerDatabase(builder.Configuration);
builder.Services.AddServerAuthentication(builder.Environment);

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

app.UseAuthentication();
app.UseAuthorization();

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
