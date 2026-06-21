// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text;
using System.Text.Json;

using OpenDsc.Contracts.DscFunctions;
using OpenDsc.Server.Mcp;

namespace OpenDsc.Server.Services;

public sealed class DscFunctionService : IDscFunctionService
{
    private readonly IMcpClient _mcpClient;
    private readonly ILogger<DscFunctionService> _logger;
    private IReadOnlyList<DscFunctionInfo>? _cachedFunctions;

    public DscFunctionService(IMcpClient mcpClient, ILogger<DscFunctionService> logger)
    {
        _mcpClient = mcpClient;
        _logger = logger;
    }

    public IReadOnlyList<DscFunctionInfo> GetFunctions()
    {
        return _cachedFunctions ?? [];
    }

    public async Task<IReadOnlyList<DscFunctionInfo>> GetFunctionsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedFunctions is not null)
        {
            return _cachedFunctions;
        }

        if (!_mcpClient.IsConnected)
        {
            await _mcpClient.InitializeAsync(cancellationToken);
        }

        _cachedFunctions = await _mcpClient.ListFunctionsAsync(cancellationToken);
        _logger.LogInformation("Loaded {Count} DSC functions from MCP server", _cachedFunctions.Count);
        return _cachedFunctions;
    }

    public async Task<EvaluateDscFunctionResult> EvaluateAsync(EvaluateDscFunctionRequest request, CancellationToken cancellationToken = default)
    {
        var expression = BuildExpression(request.FunctionName, request.Args);

        try
        {
            if (!_mcpClient.IsConnected)
            {
                await _mcpClient.InitializeAsync(cancellationToken);
            }

            var result = await _mcpClient.InvokeExpressionAsync(expression, cancellationToken);

            return new EvaluateDscFunctionResult
            {
                FunctionExpression = expression,
                ResultJson = result?.ToJsonString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to evaluate DSC expression '{Expression}': {Error}", expression, ex.Message);
            return new EvaluateDscFunctionResult
            {
                FunctionExpression = expression,
                Error = ex.Message
            };
        }
    }

    public async Task<EvaluateDscFunctionResult> EvaluateExpressionAsync(string expression, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_mcpClient.IsConnected)
            {
                await _mcpClient.InitializeAsync(cancellationToken);
            }

            var result = await _mcpClient.InvokeExpressionAsync(expression, cancellationToken);

            return new EvaluateDscFunctionResult
            {
                FunctionExpression = expression,
                ResultJson = result?.ToJsonString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to evaluate DSC expression '{Expression}': {Error}", expression, ex.Message);
            return new EvaluateDscFunctionResult
            {
                FunctionExpression = expression,
                Error = ex.Message
            };
        }
    }

    private static string BuildExpression(string name, IReadOnlyList<object?> args)
    {
        var sb = new StringBuilder();
        sb.Append('[').Append(name).Append('(');
        for (int i = 0; i < args.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            AppendArg(sb, args[i]);
        }
        sb.Append(")]");
        return sb.ToString();
    }

    private static void AppendArg(StringBuilder sb, object? arg)
    {
        switch (arg)
        {
            case null:
                sb.Append("null");
                break;
            case bool b:
                sb.Append(b ? "true" : "false");
                break;
            case string s:
                AppendStringArg(sb, s);
                break;
            case JsonElement je:
                AppendJsonElementArg(sb, je);
                break;
            default:
                sb.Append(arg.ToString());
                break;
        }
    }

    private static void AppendStringArg(StringBuilder sb, string s)
    {
        var trimmed = s.Trim();
        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            sb.Append(trimmed);
        }
        else
        {
            sb.Append('\'').Append(s.Replace("'", "\\'")).Append('\'');
        }
    }

    private static void AppendJsonElementArg(StringBuilder sb, JsonElement je)
    {
        switch (je.ValueKind)
        {
            case JsonValueKind.String:
                AppendStringArg(sb, je.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Number:
                sb.Append(je.GetRawText());
                break;
            case JsonValueKind.True:
                sb.Append("true");
                break;
            case JsonValueKind.False:
                sb.Append("false");
                break;
            case JsonValueKind.Null:
                sb.Append("null");
                break;
            default:
                sb.Append(je.GetRawText());
                break;
        }
    }
}
