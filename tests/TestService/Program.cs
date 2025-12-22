// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TestWorker>();
builder.Services.AddWindowsService();

var host = builder.Build();
await host.RunAsync();

public class TestWorker : BackgroundService
{
    private readonly ILogger<TestWorker> _logger;

    public TestWorker(ILogger<TestWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Test service started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Test service stopped at: {time}", DateTimeOffset.Now);
    }
}
