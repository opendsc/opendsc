// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Nodes;

using AwesomeAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using OpenDsc.Contracts.DscFunctions;
using OpenDsc.Server.Mcp;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class DscFunctionServiceTests
{
    private readonly Mock<IMcpClient> _mcpClientMock;
    private readonly DscFunctionService _service;

    public DscFunctionServiceTests()
    {
        _mcpClientMock = new Mock<IMcpClient>();
        _mcpClientMock.Setup(c => c.IsConnected).Returns(true);
        _service = new DscFunctionService(_mcpClientMock.Object, NullLogger<DscFunctionService>.Instance);
    }

    #region GetFunctions

    [Fact]
    public void GetFunctions_BeforeLoad_ReturnsEmpty()
    {
        _service.GetFunctions().Should().BeEmpty();
    }

    [Fact]
    public async Task GetFunctionsAsync_WhenNotCached_FetchesFromMcpClient()
    {
        var expected = new List<DscFunctionInfo>
        {
            new() { Name = "concat", ParameterTypes = [] },
            new() { Name = "base64", ParameterTypes = [] }
        };

        _mcpClientMock.Setup(c => c.ListFunctionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.GetFunctionsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
        _mcpClientMock.Verify(c => c.ListFunctionsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFunctionsAsync_WhenCached_DoesNotFetchAgain()
    {
        var functions = new List<DscFunctionInfo> { new() { Name = "concat", ParameterTypes = [] } };

        _mcpClientMock.Setup(c => c.ListFunctionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(functions);

        await _service.GetFunctionsAsync(TestContext.Current.CancellationToken);
        await _service.GetFunctionsAsync(TestContext.Current.CancellationToken);

        _mcpClientMock.Verify(c => c.ListFunctionsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region EvaluateAsync — expression building

    [Theory]
    [InlineData("concat", new object[] { "hello", "world" }, "[concat('hello', 'world')]")]
    [InlineData("base64", new object[] { "test" }, "[base64('test')]")]
    [InlineData("toUpper", new object[] { "abc" }, "[toUpper('abc')]")]
    public async Task EvaluateAsync_BuildsCorrectExpression(string functionName, object[] args, string expectedExpression)
    {
        _mcpClientMock.Setup(c => c.InvokeExpressionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonNode.Parse("\"result\""));

        var request = new EvaluateDscFunctionRequest
        {
            FunctionName = functionName,
            Args = args.Cast<object?>().ToList()
        };

        var result = await _service.EvaluateAsync(request, TestContext.Current.CancellationToken);

        result.FunctionExpression.Should().Be(expectedExpression);
    }

    [Fact]
    public async Task EvaluateAsync_WithNestedExpression_PassesExpressionUnquoted()
    {
        string capturedExpression = string.Empty;
        _mcpClientMock.Setup(c => c.InvokeExpressionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((expr, _) => capturedExpression = expr)
            .ReturnsAsync(JsonNode.Parse("\"result\""));

        var request = new EvaluateDscFunctionRequest
        {
            FunctionName = "base64",
            Args = ["[concat('a', 'b')]"]
        };

        await _service.EvaluateAsync(request, TestContext.Current.CancellationToken);

        capturedExpression.Should().Be("[base64([concat('a', 'b')])]");
    }

    [Fact]
    public async Task EvaluateAsync_WithNumericArg_RendersWithoutQuotes()
    {
        string capturedExpression = string.Empty;
        _mcpClientMock.Setup(c => c.InvokeExpressionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((expr, _) => capturedExpression = expr)
            .ReturnsAsync(JsonNode.Parse("42"));

        var request = new EvaluateDscFunctionRequest
        {
            FunctionName = "add",
            Args = [1L, 2L]
        };

        await _service.EvaluateAsync(request, TestContext.Current.CancellationToken);

        capturedExpression.Should().Be("[add(1, 2)]");
    }

    #endregion

    #region EvaluateAsync — result handling

    [Fact]
    public async Task EvaluateAsync_WhenMcpReturnsResult_ReturnsSuccess()
    {
        _mcpClientMock.Setup(c => c.InvokeExpressionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonNode.Parse("\"helloworld\""));

        var request = new EvaluateDscFunctionRequest
        {
            FunctionName = "concat",
            Args = ["hello", "world"]
        };

        var result = await _service.EvaluateAsync(request, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        result.ResultJson.Should().Be("\"helloworld\"");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_WhenMcpReturnsNull_ReturnsSuccessWithNullResult()
    {
        _mcpClientMock.Setup(c => c.InvokeExpressionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonNode?)null);

        var request = new EvaluateDscFunctionRequest
        {
            FunctionName = "null",
            Args = []
        };

        var result = await _service.EvaluateAsync(request, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        result.ResultJson.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateAsync_WhenMcpThrows_ReturnsError()
    {
        _mcpClientMock.Setup(c => c.InvokeExpressionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unknown function 'badFunc'"));

        var request = new EvaluateDscFunctionRequest
        {
            FunctionName = "badFunc",
            Args = []
        };

        var result = await _service.EvaluateAsync(request, TestContext.Current.CancellationToken);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Unknown function 'badFunc'");
    }

    #endregion

    #region EvaluateAsync — MCP initialization

    [Fact]
    public async Task EvaluateAsync_WhenNotConnected_InitializesMcpBeforeEvaluating()
    {
        _mcpClientMock.Setup(c => c.IsConnected).Returns(false);
        _mcpClientMock.Setup(c => c.InvokeExpressionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonNode.Parse("\"ok\""));

        var request = new EvaluateDscFunctionRequest
        {
            FunctionName = "concat",
            Args = ["a"]
        };

        await _service.EvaluateAsync(request, TestContext.Current.CancellationToken);

        _mcpClientMock.Verify(c => c.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
