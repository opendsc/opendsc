// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Text;

using AwesomeAssertions;

using Xunit;

using OpenDsc.Client.Services;
using OpenDsc.Client.Tests.Helpers;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Client.Tests.Services;

public sealed class ConfigurationHttpServiceTests
{
    private static ConfigurationHttpService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new ConfigurationHttpService(client);
    }

    // ── GetConfigurationsAsync ────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetConfigurationsAsync_Gets_Correct_Endpoint()
    {
        var expected = new List<ConfigurationSummary> { new() { Name = "web" } };
        var handler = new FakeHttpMessageHandler().RespondOk(expected);
        var service = CreateService(handler);

        var result = await service.GetConfigurationsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations");
    }

    // ── GetConfigurationAsync ────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetConfigurationAsync_Gets_Named_Endpoint()
    {
        var details = new ConfigurationDetails { Name = "web" };
        var handler = new FakeHttpMessageHandler().RespondOk(details);
        var service = CreateService(handler);

        var result = await service.GetConfigurationAsync("web", TestContext.Current.CancellationToken);

        result!.Name.Should().Be("web");
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetConfigurationAsync_Returns_Null_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var result = await service.GetConfigurationAsync("missing", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ── GetVersionsAsync ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetVersionsAsync_Gets_Versions_Endpoint()
    {
        var versions = new List<ConfigurationVersionDetails> { new() { Version = "1.0.0" } };
        var handler = new FakeHttpMessageHandler().RespondOk(versions);
        var service = CreateService(handler);

        var result = await service.GetVersionsAsync("web", TestContext.Current.CancellationToken);

        result!.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/versions");
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateAsync_Posts_Multipart_Form()
    {
        var created = new ConfigurationDetails { Name = "web" };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);

        var fileContent = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var request = new CreateConfigurationAdminRequest
        {
            Name = "web",
            EntryPoint = "main.dsc.yaml",
            Version = "1.0.0",
            UseServerManagedParameters = false,
            Files = [new FileUpload { FileName = "main.dsc.yaml", Content = fileContent }]
        };

        var result = await service.CreateAsync(request, TestContext.Current.CancellationToken);

        result.Name.Should().Be("web");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations");
        handler.LastRequest.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateAsync_Patches_Configuration()
    {
        var updated = new ConfigurationDetails { Name = "web" };
        var handler = new FakeHttpMessageHandler().RespondOk(updated);
        var service = CreateService(handler);

        var result = await service.UpdateAsync("web", new UpdateConfigurationAdminRequest { Description = "new" }, TestContext.Current.CancellationToken);

        result.Name.Should().Be("web");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Patch);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteAsync_Deletes_Configuration()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.DeleteAsync("web", TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web");
    }

    // ── Configuration inspection endpoints ──────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetConfigurationVersionListAsync_Gets_Version_List_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondOk(new List<string> { "1.0.0" });
        var service = CreateService(handler);

        var result = await service.GetConfigurationVersionListAsync("web", TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/versions/list");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IsConfigurationAssignedAsync_Gets_Assignment_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondOk(true);
        var service = CreateService(handler);

        var result = await service.IsConfigurationAssignedAsync("web", TestContext.Current.CancellationToken);

        result.Should().BeTrue();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/assignment");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IsVersionInUseAsync_Gets_Usage_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondOk(new VersionUsageInfo { IsInUse = true });
        var service = CreateService(handler);

        var result = await service.IsVersionInUseAsync("web", "1.0.0", TestContext.Current.CancellationToken);

        result.IsInUse.Should().BeTrue();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/versions/1.0.0/usage");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetParameterSchemaIdAsync_Gets_Schema_Id_Endpoint()
    {
        var schemaId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondOk(schemaId);
        var service = CreateService(handler);

        var result = await service.GetParameterSchemaIdAsync("web", TestContext.Current.CancellationToken);

        result.Should().Be(schemaId);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/parameter-schema-id");
    }

    // ── CreateVersionAsync ────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateVersionAsync_Posts_Multipart_Form()
    {
        var created = new ConfigurationVersionDetails { Version = "1.1.0" };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);

        var fileContent = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var request = new CreateConfigurationVersionRequest
        {
            Version = "1.1.0",
            Files = [new FileUpload { FileName = "main.dsc.yaml", Content = fileContent }]
        };

        var result = await service.CreateVersionAsync("web", request, TestContext.Current.CancellationToken);

        result.Version.Should().Be("1.1.0");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/versions");
        handler.LastRequest.Content.Should().BeOfType<MultipartFormDataContent>();
    }

    // ── PublishVersionAsync ───────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PublishVersionAsync_Puts_To_Publish_Endpoint()
    {
        var result = new PublishResult { Success = true };
        var handler = new FakeHttpMessageHandler().RespondOk(result);
        var service = CreateService(handler);

        var response = await service.PublishVersionAsync("web", "1.0.0", TestContext.Current.CancellationToken);

        response.Success.Should().BeTrue();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/versions/1.0.0/publish");
    }

    // ── DeleteVersionAsync ────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteVersionAsync_Deletes_Version()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.DeleteVersionAsync("web", "1.0.0", TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/versions/1.0.0");
    }

    // ── DownloadFileAsync ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DownloadFileAsync_Returns_Stream()
    {
        var bytes = Encoding.UTF8.GetBytes("file content");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes)
        };
        var handler = new FakeHttpMessageHandler().Respond(response);
        var service = CreateService(handler);

        var stream = await service.DownloadFileAsync("web", "1.0.0", "main.dsc.yaml", TestContext.Current.CancellationToken);

        stream.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/versions/1.0.0/files/main.dsc.yaml");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DownloadFileAsync_Returns_Null_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var stream = await service.DownloadFileAsync("web", "1.0.0", "missing.yaml", TestContext.Current.CancellationToken);

        stream.Should().BeNull();
    }

    // ── GetPermissionsAsync ───────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPermissionsAsync_Gets_Permissions_Endpoint()
    {
        var perms = new List<PermissionEntry> { new() { PrincipalType = PrincipalType.User } };
        var handler = new FakeHttpMessageHandler().RespondOk(perms);
        var service = CreateService(handler);

        var result = await service.GetPermissionsAsync("web", TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/permissions");
    }

    // ── GrantPermissionAsync ──────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GrantPermissionAsync_Puts_To_Permissions_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.GrantPermissionAsync("web", new GrantPermissionRequest(), TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/permissions");
    }

    // ── RevokePermissionAsync ─────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RevokePermissionAsync_Deletes_From_Permissions_Endpoint()
    {
        var principalId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.RevokePermissionAsync("web",
            new RevokePermissionRequest { PrincipalType = PrincipalType.User, PrincipalId = principalId },
            TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString()
            .Should().EndWith($"api/v1/configurations/web/permissions/User/{principalId}");
    }

    // ── GetSettingsAsync ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetSettingsAsync_Gets_Settings_Endpoint()
    {
        var settings = new ConfigurationSettingsSummary();
        var handler = new FakeHttpMessageHandler().RespondOk(settings);
        var service = CreateService(handler);

        var result = await service.GetSettingsAsync("web", TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/settings");
    }

    // ── UpdateSettingsAsync ───────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateSettingsAsync_Puts_To_Settings_Endpoint()
    {
        var updated = new ConfigurationSettingsSummary();
        var handler = new FakeHttpMessageHandler().RespondOk(updated);
        var service = CreateService(handler);

        var result = await service.UpdateSettingsAsync("web", new UpdateConfigurationSettingsRequest(), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/settings");
    }

    // ── DeleteSettingsAsync ───────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteSettingsAsync_Deletes_Settings_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.DeleteSettingsAsync("web", TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/settings");
    }

    // ── GetRetentionSettingsAsync ─────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetRetentionSettingsAsync_Gets_Retention_Endpoint()
    {
        var retention = new ConfigurationRetentionSummary();
        var handler = new FakeHttpMessageHandler().RespondOk(retention);
        var service = CreateService(handler);

        var result = await service.GetRetentionSettingsAsync("web", TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/settings/retention");
    }

    // ── SaveRetentionSettingsAsync ────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveRetentionSettingsAsync_Puts_To_Retention_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.SaveRetentionSettingsAsync("web", new SaveRetentionSettingsRequest(), TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/settings/retention");
    }

    // ── ResetRetentionSettingsAsync ───────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ResetRetentionSettingsAsync_Deletes_Retention_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.ResetRetentionSettingsAsync("web", TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/settings/retention");
    }

    // ── Error mapping ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteAsync_Throws_KeyNotFoundException_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var act = async () => await service.DeleteAsync("missing", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateVersionFromExistingAsync_Posts_To_FromExisting_Endpoint()
    {
        var created = new ConfigurationVersionDetails { Version = "2.0.0" };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);

        var result = await service.CreateVersionFromExistingAsync("web", new CreateVersionFromExistingRequest
        {
            SourceVersion = "1.0.0",
            NewVersion = "2.0.0"
        }, TestContext.Current.CancellationToken);

        result.Version.Should().Be("2.0.0");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/versions/from-existing");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AddFilesAsync_Posts_To_Files_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);
        using var stream = new MemoryStream([1, 2, 3]);

        await service.AddFilesAsync("web", "1.0.0", [new FileUpload { FileName = "extra.yaml", Content = stream, ContentType = "application/x-yaml" }], TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/versions/1.0.0/files");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteFileAsync_Deletes_From_Files_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.DeleteFileAsync("web", "1.0.0", "file.yaml", TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/versions/1.0.0/files/file.yaml");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SaveFileAsync_Puts_To_Files_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.SaveFileAsync("web", "1.0.0", "file.yaml", "content", TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/versions/1.0.0/files/file.yaml");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ChangeEntryPointAsync_Puts_To_EntryPoint_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.ChangeEntryPointAsync("web", "1.0.0", "new.dsc.yaml", TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/configurations/web/versions/1.0.0/entry-point");
    }
}
