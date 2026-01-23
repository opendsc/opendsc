// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Server;
using OpenDsc.Server.Authentication;
using OpenDsc.Server.Data;
using OpenDsc.Server.Endpoints;

using Scalar.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
});

builder.Services.AddSingleton(SourceGenerationContext.Default.Options);

builder.Services.AddOpenApi();

builder.Services.AddServerDatabase(builder.Configuration);
builder.Services.AddApiKeyAuthentication();

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("OpenDSC Server")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapNodeEndpoints();
app.MapConfigurationEndpoints();
app.MapReportEndpoints();
app.MapSettingsEndpoints();

app.Run();
