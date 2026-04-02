// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using AwesomeAssertions;

using OpenDsc.Server.Contracts;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class RegistrationKeyEndpointsTests : IDisposable
{
    private readonly ServerWebApplicationFactory _factory = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private HttpClient CreateAuthenticatedClient() => _factory.CreateAuthenticatedClient();

    public void Dispose() => _factory.Dispose();

    // --- List ---

    [Fact]
    public async Task GetRegistrationKeys_WithAdminAuth_ReturnsKeys()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/admin/registration-keys", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var keys = await response.Content.ReadFromJsonAsync<List<RegistrationKeyResponse>>(JsonOptions, TestContext.Current.CancellationToken);
        keys.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRegistrationKeys_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/registration-keys", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Create ---

    [Fact]
    public async Task CreateRegistrationKey_WithoutDescription_ReturnsKeyWithNullDescription()
    {
        var client = CreateAuthenticatedClient();
        var request = new CreateRegistrationKeyRequest
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegistrationKeyResponse>(JsonOptions, TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Key.Should().NotBeNullOrEmpty();
        result.Description.Should().BeNull();
    }

    [Fact]
    public async Task CreateRegistrationKey_WithDescription_ReturnsKeyWithDescription()
    {
        var client = CreateAuthenticatedClient();
        var request = new CreateRegistrationKeyRequest
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            Description = "IIS team servers"
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegistrationKeyResponse>(JsonOptions, TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Key.Should().NotBeNullOrEmpty();
        result.Description.Should().Be("IIS team servers");
    }

    [Fact]
    public async Task CreateRegistrationKey_WithDescription_DescriptionAppearsInList()
    {
        var client = CreateAuthenticatedClient();
        var request = new CreateRegistrationKeyRequest
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            Description = "Finance team nodes"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", request, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>(JsonOptions, TestContext.Current.CancellationToken);

        var listResponse = await client.GetAsync("/api/v1/admin/registration-keys", TestContext.Current.CancellationToken);
        var keys = await listResponse.Content.ReadFromJsonAsync<List<RegistrationKeyResponse>>(JsonOptions, TestContext.Current.CancellationToken);

        var listed = keys!.FirstOrDefault(k => k.Id == created!.Id);
        listed.Should().NotBeNull();
        listed!.Description.Should().Be("Finance team nodes");
        listed.Key.Should().BeNull();
    }

    [Fact]
    public async Task CreateRegistrationKey_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new CreateRegistrationKeyRequest { ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };

        var response = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Update description ---

    [Fact]
    public async Task UpdateRegistrationKey_SetsDescription()
    {
        var client = CreateAuthenticatedClient();

        var createRequest = new CreateRegistrationKeyRequest { ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) };
        var createResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", createRequest, TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>(JsonOptions, TestContext.Current.CancellationToken);

        var updateRequest = new UpdateRegistrationKeyRequest { Description = "Dev team registration key" };
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/admin/registration-keys/{created!.Id}", updateRequest, TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>(JsonOptions, TestContext.Current.CancellationToken);
        updated!.Description.Should().Be("Dev team registration key");
        updated.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task UpdateRegistrationKey_ClearsDescription_WhenNullProvided()
    {
        var client = CreateAuthenticatedClient();

        var createRequest = new CreateRegistrationKeyRequest
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            Description = "Initial description"
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", createRequest, TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>(JsonOptions, TestContext.Current.CancellationToken);

        var updateRequest = new UpdateRegistrationKeyRequest { Description = null };
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/admin/registration-keys/{created!.Id}", updateRequest, TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>(JsonOptions, TestContext.Current.CancellationToken);
        updated!.Description.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRegistrationKey_WithNonExistentId_ReturnsNotFound()
    {
        var client = CreateAuthenticatedClient();
        var updateRequest = new UpdateRegistrationKeyRequest { Description = "Something" };

        var response = await client.PutAsJsonAsync($"/api/v1/admin/registration-keys/{Guid.NewGuid()}", updateRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRegistrationKey_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var updateRequest = new UpdateRegistrationKeyRequest { Description = "Something" };

        var response = await client.PutAsJsonAsync($"/api/v1/admin/registration-keys/{Guid.NewGuid()}", updateRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Revoke ---

    [Fact]
    public async Task RevokeRegistrationKey_WithAdminAuth_RevokesKey()
    {
        var client = CreateAuthenticatedClient();

        var createRequest = new CreateRegistrationKeyRequest { ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) };
        var createResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", createRequest, TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>(JsonOptions, TestContext.Current.CancellationToken);

        var revokeResponse = await client.DeleteAsync($"/api/v1/admin/registration-keys/{created!.Id}", TestContext.Current.CancellationToken);

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RevokeRegistrationKey_WithNonExistentId_ReturnsNotFound()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/v1/admin/registration-keys/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RevokeRegistrationKey_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/admin/registration-keys/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
