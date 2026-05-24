// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using AwesomeAssertions;
using Xunit;
using OpenDsc.Client.Services;
using OpenDsc.Client.Tests.Helpers;

namespace OpenDsc.Client.Tests.Services;

public sealed class HealthHttpServiceTests
{
    private static HealthHttpService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new HealthHttpService(client);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CanConnectAsync_Returns_True_When_Server_Responds_Ok()
    {
        var handler = new FakeHttpMessageHandler()
            .Respond(new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(handler);

        var result = await service.CanConnectAsync(TestContext.Current.CancellationToken);

        result.Should().BeTrue();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("health/ready");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CanConnectAsync_Returns_False_When_Server_Returns_ServiceUnavailable()
    {
        var handler = new FakeHttpMessageHandler()
            .Respond(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var service = CreateService(handler);

        var result = await service.CanConnectAsync(TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CanConnectAsync_Returns_False_On_Network_Error()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Respond(new HttpResponseMessage(HttpStatusCode.OK));

        var faultyHandler = new FaultingHttpMessageHandler();
        var client = new HttpClient(faultyHandler) { BaseAddress = new Uri("https://localhost/") };
        var service = new HealthHttpService(client);

        var result = await service.CanConnectAsync(TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }
}

internal sealed class FaultingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection refused."));
}
