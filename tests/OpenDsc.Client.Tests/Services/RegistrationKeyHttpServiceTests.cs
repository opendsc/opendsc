// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using AwesomeAssertions;
using Xunit;
using OpenDsc.Client.Services;
using OpenDsc.Client.Tests.Helpers;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Tests.Services;

public sealed class RegistrationKeyHttpServiceTests
{
    private static RegistrationKeyHttpService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new RegistrationKeyHttpService(client);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetKeysAsync_Returns_Keys_From_Server()
    {
        var expected = new List<RegistrationKeyResponse>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        };

        var handler = new FakeHttpMessageHandler().RespondOk(expected);
        var service = CreateService(handler);

        var result = await service.GetKeysAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/admin/registration-keys");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateKeyAsync_Posts_To_Correct_Endpoint()
    {
        var created = new RegistrationKeyResponse { Id = Guid.NewGuid() };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);
        var request = new CreateRegistrationKeyRequest { Description = "test" };

        var result = await service.CreateKeyAsync(request, TestContext.Current.CancellationToken);

        result.Id.Should().Be(created.Id);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/admin/registration-keys");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RevokeKeyAsync_Deletes_To_Correct_Endpoint()
    {
        var id = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.RevokeKeyAsync(id, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/admin/registration-keys/{id}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RotateKeyAsync_Posts_To_Legacy_Endpoint()
    {
        var rotated = new RegistrationKeyResponse { Id = Guid.NewGuid() };
        var handler = new FakeHttpMessageHandler().RespondOk(rotated);
        var service = CreateService(handler);

        var result = await service.RotateKeyAsync(TestContext.Current.CancellationToken);

        result.Id.Should().Be(rotated.Id);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/settings/registration-keys");
    }
}
