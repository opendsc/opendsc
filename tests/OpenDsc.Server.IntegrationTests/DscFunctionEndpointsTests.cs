// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Text.Json.Nodes;

using AwesomeAssertions;

using Microsoft.Extensions.DependencyInjection.Extensions;

using OpenDsc.Contracts.DscFunctions;
using OpenDsc.Schema;
using OpenDsc.Server.Mcp;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class DscFunctionEndpointsTests : IDisposable
{
    private readonly FakeMcpClientFactory _factory;

    public DscFunctionEndpointsTests()
    {
        _factory = new FakeMcpClientFactory();
    }

    public void Dispose()
    {
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetFunctions_WithAuth_ReturnsOk()
    {
        _factory.FakeMcpClient.Functions =
        [
            new() { Name = "concat", ParameterTypes = ["string"] },
            new() { Name = "base64", ParameterTypes = ["string"] }
        ];

        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            "/api/v1/dsc-functions",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<DscFunctionInfo>>(
            TestJsonOptions.Default,
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result.Should().Contain(f => f.Name == "concat");
        result.Should().Contain(f => f.Name == "base64");
    }

    [Fact]
    public async Task GetFunctions_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/dsc-functions",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Evaluate_WithValidRequest_ReturnsExpression()
    {
        _factory.FakeMcpClient.ExpressionResult = JsonNode.Parse("\"helloworld\"");

        using var client = await _factory.CreateAuthenticatedClientAsync();

        var request = new EvaluateDscFunctionRequest
        {
            FunctionName = "concat",
            Args = ["hello", "world"]
        };

        var response = await client.PostAsJsonAsync(
            "/api/v1/dsc-functions/evaluate",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EvaluateDscFunctionResult>(
            TestJsonOptions.Default,
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.FunctionExpression.Should().Be("[concat('hello', 'world')]");
        result.ResultJson.Should().Be("\"helloworld\"");
    }

    [Fact]
    public async Task Evaluate_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var request = new EvaluateDscFunctionRequest
        {
            FunctionName = "concat",
            Args = ["hello"]
        };

        var response = await client.PostAsJsonAsync(
            "/api/v1/dsc-functions/evaluate",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Evaluate_WhenMcpFails_ReturnsOkWithError()
    {
        _factory.FakeMcpClient.ShouldThrow = true;
        _factory.FakeMcpClient.ExceptionMessage = "Unknown function 'badFunc'";

        using var client = await _factory.CreateAuthenticatedClientAsync();

        var request = new EvaluateDscFunctionRequest
        {
            FunctionName = "badFunc",
            Args = []
        };

        var response = await client.PostAsJsonAsync(
            "/api/v1/dsc-functions/evaluate",
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EvaluateDscFunctionResult>(
            TestJsonOptions.Default,
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Error.Should().Contain("Unknown function 'badFunc'");
    }
}

/// <summary>
/// Configurable fake IMcpClient for integration testing.
/// </summary>
internal sealed class FakeMcpClientImpl : IMcpClient
{
    public bool IsConnected { get; set; } = true;
    public List<DscFunctionInfo> Functions { get; set; } = [];
    public JsonNode? ExpressionResult { get; set; }
    public JsonNode? FunctionResult { get; set; }
    public bool ShouldThrow { get; set; }
    public string ExceptionMessage { get; set; } = "MCP error";

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<List<DscResourceInfo>> ListResourcesAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<DscResourceInfo>());
    public Task<DscResourceInfo?> GetResourceDetailsAsync(string typeName, CancellationToken cancellationToken = default) => Task.FromResult<DscResourceInfo?>(null);

    public Task<List<DscFunctionInfo>> ListFunctionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Functions);

    public Task<JsonNode?> InvokeFunctionAsync(string functionName, IReadOnlyList<object?> parameters, CancellationToken cancellationToken = default)
    {
        if (ShouldThrow)
            throw new InvalidOperationException(ExceptionMessage);
        return Task.FromResult(FunctionResult);
    }

    public Task<JsonNode?> InvokeExpressionAsync(string expression, CancellationToken cancellationToken = default)
    {
        if (ShouldThrow)
            throw new InvalidOperationException(ExceptionMessage);
        return Task.FromResult(ExpressionResult);
    }
}

/// <summary>
/// WebApplicationFactory that replaces IMcpClient with a configurable fake.
/// </summary>
internal sealed class FakeMcpClientFactory : ServerWebApplicationFactory
{
    public FakeMcpClientImpl FakeMcpClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMcpClient>();
            services.AddSingleton<IMcpClient>(FakeMcpClient);
        });
    }
}
