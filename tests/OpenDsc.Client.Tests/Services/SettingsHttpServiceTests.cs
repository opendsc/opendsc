// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;
using Xunit;

using OpenDsc.Client.Services;
using OpenDsc.Client.Tests.Helpers;
using OpenDsc.Contracts.Settings;
using OpenDsc.Contracts.Retention;
using OpenDsc.Contracts.Lcm;

namespace OpenDsc.Client.Tests.Services;

public sealed class SettingsHttpServiceTests
{
    private static SettingsHttpService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new SettingsHttpService(client);
    }

    // GetServerSettingsAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetServerSettingsAsync_Gets_Settings_Endpoint()
    {
        var settings = new ServerSettingsSummary { StalenessMultiplier = 2.0 };
        var handler = new FakeHttpMessageHandler().RespondOk(settings);
        var service = CreateService(handler);
        var result = await service.GetServerSettingsAsync(TestContext.Current.CancellationToken);
        result.StalenessMultiplier.Should().Be(2.0);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/settings");
    }

    // GetServerLcmDefaultsAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetServerLcmDefaultsAsync_Gets_Lcm_Defaults_Endpoint()
    {
        var defaults = new ServerLcmDefaultsSummary();
        var handler = new FakeHttpMessageHandler().RespondOk(defaults);
        var service = CreateService(handler);
        var result = await service.GetServerLcmDefaultsAsync(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/settings/lcm-defaults");
    }

    // GetPublicSettingsAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPublicSettingsAsync_Gets_Public_Settings_Endpoint()
    {
        var pub = new PublicSettingsResponse();
        var handler = new FakeHttpMessageHandler().RespondOk(pub);
        var service = CreateService(handler);
        var result = await service.GetPublicSettingsAsync(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/settings/public");
    }

    // GetValidationSettingsAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetValidationSettingsAsync_Gets_Validation_Endpoint()
    {
        var validation = new ValidationSettingsSummary();
        var handler = new FakeHttpMessageHandler().RespondOk(validation);
        var service = CreateService(handler);
        var result = await service.GetValidationSettingsAsync(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/settings/validation");
    }

    // GetRetentionSettingsAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetRetentionSettingsAsync_Gets_Retention_Endpoint()
    {
        var retention = new RetentionSettingsSummary();
        var handler = new FakeHttpMessageHandler().RespondOk(retention);
        var service = CreateService(handler);
        var result = await service.GetRetentionSettingsAsync(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/settings/retention");
    }

    // GetRetentionHistoryAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetRetentionHistoryAsync_Gets_Retention_Runs_Endpoint()
    {
        var runs = new List<RetentionRunSummary> { new() { Id = Guid.NewGuid() } };
        var handler = new FakeHttpMessageHandler().RespondOk(runs);
        var service = CreateService(handler);
        var result = await service.GetRetentionHistoryAsync(TestContext.Current.CancellationToken);
        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/retention/runs");
    }

    // UpdateServerSettingsAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateServerSettingsAsync_Puts_To_Settings_Endpoint()
    {
        var updated = new ServerSettingsSummary { StalenessMultiplier = 3.0 };
        var handler = new FakeHttpMessageHandler().RespondOk(updated);
        var service = CreateService(handler);
        var result = await service.UpdateServerSettingsAsync(new UpdateServerSettingsRequest(), TestContext.Current.CancellationToken);
        result.StalenessMultiplier.Should().Be(3.0);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/settings");
    }

    // UpdateServerLcmDefaultsAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateServerLcmDefaultsAsync_Puts_To_Lcm_Defaults_Endpoint()
    {
        var updated = new ServerLcmDefaultsSummary();
        var handler = new FakeHttpMessageHandler().RespondOk(updated);
        var service = CreateService(handler);
        var result = await service.UpdateServerLcmDefaultsAsync(new UpdateServerLcmDefaultsRequest(), TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/settings/lcm-defaults");
    }

    // UpdateValidationSettingsAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateValidationSettingsAsync_Puts_To_Validation_Endpoint()
    {
        var updated = new ValidationSettingsSummary();
        var handler = new FakeHttpMessageHandler().RespondOk(updated);
        var service = CreateService(handler);
        var result = await service.UpdateValidationSettingsAsync(new UpdateValidationSettingsRequest(), TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/settings/validation");
    }

    // UpdateRetentionSettingsAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateRetentionSettingsAsync_Puts_To_Retention_Endpoint()
    {
        var updated = new RetentionSettingsSummary();
        var handler = new FakeHttpMessageHandler().RespondOk(updated);
        var service = CreateService(handler);
        var result = await service.UpdateRetentionSettingsAsync(new UpdateRetentionSettingsRequest(), TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/settings/retention");
    }
}
