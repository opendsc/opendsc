// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using OpenDsc.Contracts.DscFunctions;
using OpenDsc.Schema;

namespace OpenDsc.Server.Mcp;

/// <summary>
/// Client implementation for the DSC MCP server.
/// Uses the ModelContextProtocol SDK to communicate with the "dsc mcp" subprocess via stdio.
/// </summary>
public sealed class DscMcpClient : IMcpClient, IAsyncDisposable
{
    private readonly ILogger<DscMcpClient> _logger;
    private StdioClientTransport? _transport;
    private McpClient? _mcpClient;

    public bool IsConnected { get; private set; }

    public DscMcpClient(ILogger<DscMcpClient> logger)
    {
        _logger = logger;
        IsConnected = false;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected && _mcpClient is not null)
        {
            return;
        }

        try
        {
            // Create transport that spawns "dsc mcp" subprocess
            var options = new StdioClientTransportOptions
            {
                Command = "dsc",
                Arguments = ["mcp"],
            };

            _transport = new StdioClientTransport(options);

            // Create MCP client using the transport
            _mcpClient = await McpClient.CreateAsync(
                clientTransport: _transport,
                clientOptions: null,
                loggerFactory: null,
                cancellationToken: cancellationToken);

            IsConnected = true;
            _logger.LogInformation("Connected to DSC MCP server");
        }
        catch (Exception ex)
        {
            IsConnected = false;
            _logger.LogError(ex, "Failed to initialize DSC MCP client. Ensure 'dsc mcp' is installed and available in PATH.");
            throw new InvalidOperationException("Failed to initialize DSC MCP client. Ensure 'dsc mcp' is installed and available in PATH.", ex);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_mcpClient is not null)
        {
            await _mcpClient.DisposeAsync();
            _mcpClient = null;
        }

        // The transport is managed by McpClient and disposed by it
        _transport = null;

        IsConnected = false;
        _logger.LogInformation("Disconnected from DSC MCP server");
    }

    public async Task<List<DscResourceInfo>> ListResourcesAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _mcpClient is null)
        {
            throw new InvalidOperationException("MCP client is not connected. Call InitializeAsync first.");
        }

        try
        {
            // Call the list_dsc_resources tool via MCP
            var result = await _mcpClient.CallToolAsync(
                "list_dsc_resources",
                arguments: null,
                progress: null,
                options: null,
                cancellationToken: cancellationToken);

            var resources = new List<DscResourceInfo>();

            // Extract text content from the tool result
            var textContent = result.Content
                .OfType<TextContentBlock>()
                .FirstOrDefault();

            if (textContent is null)
            {
                _logger.LogWarning("No text content in list_dsc_resources response");
                return resources;
            }

            try
            {
                // Parse the JSON response containing the resources array
                var resourcesDoc = JsonNode.Parse(textContent.Text);
                if (resourcesDoc?["resources"] is JsonArray actualResourcesArray)
                {
                    _logger.LogInformation("Found {Count} resources in list_dsc_resources response", actualResourcesArray.Count);

                    foreach (var resourceItem in actualResourcesArray)
                    {
                        var resource = ParseResourceManifest(resourceItem);
                        if (resource is not null)
                        {
                            resources.Add(resource);
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse list_dsc_resources response as JSON");
            }

            _logger.LogInformation("Listed {Count} DSC resources from MCP server", resources.Count);
            return resources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list DSC resources from MCP server");
            throw new InvalidOperationException("Failed to list DSC resources from MCP server", ex);
        }
    }

    public async Task<DscResourceInfo?> GetResourceDetailsAsync(string typeName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        if (!IsConnected || _mcpClient is null)
        {
            throw new InvalidOperationException("MCP client is not connected. Call InitializeAsync first.");
        }

        try
        {
            // Call the show_dsc_resource tool with the resource type
            var result = await _mcpClient.CallToolAsync(
                "show_dsc_resource",
                arguments: new Dictionary<string, object?> { { "type", typeName } },
                progress: null,
                options: null,
                cancellationToken: cancellationToken);

            // Extract text content from the tool result
            var textContent = result.Content
                .OfType<TextContentBlock>()
                .FirstOrDefault();

            if (textContent is null)
            {
                _logger.LogWarning("No text content in show_dsc_resource response for '{ResourceType}'", typeName);
                return null;
            }

            try
            {
                // Parse the JSON response containing resource manifest
                var resourceDoc = JsonNode.Parse(textContent.Text);
                var manifest = ParseResourceManifest(resourceDoc);

                if (manifest is not null)
                {
                    _logger.LogInformation("Fetched manifest for resource '{ResourceType}'", typeName);
                }
                else
                {
                    _logger.LogWarning("Resource '{ResourceType}' not found or could not be parsed", typeName);
                }

                return manifest;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse show_dsc_resource response as JSON for resource '{ResourceType}'", typeName);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get details for resource '{ResourceType}'", typeName);
            return null;
        }
    }

    public async Task<List<DscFunctionInfo>> ListFunctionsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _mcpClient is null)
        {
            throw new InvalidOperationException("MCP client is not connected. Call InitializeAsync first.");
        }

        try
        {
            // Call the list_dsc_functions tool via MCP
            var result = await _mcpClient.CallToolAsync(
                "list_dsc_functions",
                arguments: null,
                progress: null,
                options: null,
                cancellationToken: cancellationToken);

            var functions = new List<DscFunctionInfo>();

            // Extract text content from the tool result
            var textContent = result.Content
                .OfType<TextContentBlock>()
                .FirstOrDefault();

            if (textContent is null)
            {
                _logger.LogWarning("No text content in list_dsc_functions response");
                return functions;
            }

            try
            {
                // Parse the JSON response containing the functions array
                var functionsDoc = JsonNode.Parse(textContent.Text);
                if (functionsDoc is JsonArray functionsArray)
                {
                    _logger.LogInformation("Found {Count} functions in list_dsc_functions response", functionsArray.Count);

                    foreach (var functionItem in functionsArray)
                    {
                        var function = ParseFunctionInfo(functionItem);
                        if (function is not null)
                        {
                            functions.Add(function);
                        }
                    }
                }
                else if (functionsDoc?["functions"] is JsonArray actualFunctionsArray)
                {
                    _logger.LogInformation("Found {Count} functions in list_dsc_functions response", actualFunctionsArray.Count);

                    foreach (var functionItem in actualFunctionsArray)
                    {
                        var function = ParseFunctionInfo(functionItem);
                        if (function is not null)
                        {
                            functions.Add(function);
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse list_dsc_functions response as JSON");
            }

            _logger.LogInformation("Listed {Count} DSC functions from MCP server", functions.Count);
            return functions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list DSC functions from MCP server");
            throw new InvalidOperationException("Failed to list DSC functions from MCP server", ex);
        }
    }

    public async Task<JsonNode?> InvokeFunctionAsync(string functionName, IReadOnlyList<object?> parameters, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _mcpClient is null)
        {
            throw new InvalidOperationException("MCP client is not connected. Call InitializeAsync first.");
        }

        try
        {
            var paramsArray = new JsonArray();
            foreach (var p in parameters)
            {
                paramsArray.Add(JsonValue.Create(p));
            }

            var result = await _mcpClient.CallToolAsync(
                "invoke_dsc_function",
                arguments: new Dictionary<string, object?> { ["function"] = functionName, ["parameters"] = paramsArray },
                progress: null,
                options: null,
                cancellationToken: cancellationToken);

            var textContent = result.Content.OfType<TextContentBlock>().FirstOrDefault();
            if (textContent is null)
            {
                _logger.LogWarning("No text content in invoke_dsc_function response");
                return null;
            }

            try
            {
                return JsonNode.Parse(textContent.Text)?["result"];
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse invoke_dsc_function response as JSON");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke DSC function '{FunctionName}'", functionName);
            throw new InvalidOperationException($"Failed to invoke DSC function '{functionName}'", ex);
        }
    }

    public async Task<JsonNode?> InvokeExpressionAsync(string expression, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _mcpClient is null)
        {
            throw new InvalidOperationException("MCP client is not connected. Call InitializeAsync first.");
        }

        try
        {
            var result = await _mcpClient.CallToolAsync(
                "invoke_dsc_expression",
                arguments: new Dictionary<string, object?> { ["expression"] = expression },
                progress: null,
                options: null,
                cancellationToken: cancellationToken);

            var textContent = result.Content.OfType<TextContentBlock>().FirstOrDefault();
            if (textContent is null)
            {
                _logger.LogWarning("No text content in invoke_dsc_expression response");
                return null;
            }

            try
            {
                return JsonNode.Parse(textContent.Text)?["result"];
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse invoke_dsc_expression response as JSON");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke DSC expression '{Expression}'", expression);
            throw new InvalidOperationException($"Failed to invoke DSC expression '{expression}'", ex);
        }
    }

    private static DscFunctionInfo? ParseFunctionInfo(JsonNode? element)
    {
        try
        {
            if (element is JsonObject obj &&
                obj.TryGetPropertyValue("name", out var nameElement) &&
                nameElement?.GetValue<string>() is { } name)
            {
                var description = obj.TryGetPropertyValue("description", out var descElement)
                    ? descElement?.GetValue<string>()
                    : null;

                var minArgs = obj.TryGetPropertyValue("minArgs", out var minElement)
                    ? GetNullableInt(minElement)
                    : null;

                var maxArgs = obj.TryGetPropertyValue("maxArgs", out var maxElement)
                    ? GetNullableInt(maxElement)
                    : null;

                var parameterTypes = new List<string>();
                if (obj.TryGetPropertyValue("parameterTypes", out var paramTypesElement) &&
                    paramTypesElement is JsonArray paramTypesArray)
                {
                    foreach (var paramType in paramTypesArray)
                    {
                        if (paramType?.GetValue<string>() is { } typeStr)
                        {
                            parameterTypes.Add(typeStr);
                        }
                    }
                }

                var returnType = obj.TryGetPropertyValue("returnType", out var returnElement)
                    ? returnElement?.GetValue<string>()
                    : null;

                var categories = new List<string>();
                if (obj.TryGetPropertyValue("category", out var categoryElement))
                {
                    if (categoryElement is JsonArray categoryArray)
                    {
                        foreach (var cat in categoryArray)
                        {
                            if (cat?.GetValue<string>() is { } catStr)
                            {
                                categories.Add(catStr);
                            }
                        }
                    }
                }

                return new DscFunctionInfo
                {
                    Name = name,
                    Description = description,
                    MinArgs = minArgs,
                    MaxArgs = maxArgs,
                    ParameterTypes = parameterTypes,
                    ReturnType = returnType,
                    Categories = categories
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse function info: {ex.Message}");
        }

        return null;
    }

    private static int? GetNullableInt(JsonNode? element)
    {
        if (element is null)
        {
            return null;
        }

        try
        {
            // Handle JsonValue with numeric type
            if (element is JsonValue jsonValue)
            {
                if (jsonValue.TryGetValue<int>(out var intValue))
                {
                    return intValue;
                }
                // Try parsing as long and then converting
                if (jsonValue.TryGetValue<long>(out var longValue))
                {
                    return (int)longValue;
                }
                // Try parsing as double and then converting
                if (jsonValue.TryGetValue<double>(out var doubleValue))
                {
                    return (int)doubleValue;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static DscResourceInfo? ParseResourceManifest(JsonNode? element)
    {
        try
        {
            if (element is JsonObject obj &&
                obj.TryGetPropertyValue("type", out var typeElement) &&
                typeElement?.GetValue<string>() is { } type)
            {
                // Parse kind enum
                DscResourceKind? kind = null;
                if (obj.TryGetPropertyValue("kind", out var kindElement) &&
                    kindElement?.GetValue<string>() is { } kindStr &&
                    Enum.TryParse<DscResourceKind>(kindStr, ignoreCase: true, out var parsedKind))
                {
                    kind = parsedKind;
                }

                var description = obj.TryGetPropertyValue("description", out var descElement)
                    ? descElement?.GetValue<string>()
                    : null;

                var version = obj.TryGetPropertyValue("version", out var versionElement)
                    ? versionElement?.GetValue<string>()
                    : null;

                // Extract schema
                string? schema = null;
                if (obj.TryGetPropertyValue("schema", out var schemaElement))
                {
                    // Schema can be a JSON object or a string
                    if (schemaElement is JsonObject schemaObj)
                    {
                        schema = schemaObj.ToJsonString();
                    }
                    else if (schemaElement?.GetValue<string>() is { } schemaStr)
                    {
                        schema = schemaStr;
                    }
                    else
                    {
                        schema = schemaElement?.ToJsonString();
                    }
                }

                // Extract and parse capabilities
                DscCapability[]? capabilities = null;
                if (obj.TryGetPropertyValue("capabilities", out var capabilitiesElement) &&
                    capabilitiesElement is JsonArray capsArray)
                {
                    var capsList = new List<DscCapability>();
                    foreach (var cap in capsArray)
                    {
                        if (cap?.GetValue<string>() is { } capStr &&
                            Enum.TryParse<DscCapability>(capStr, ignoreCase: true, out var parsedCap))
                        {
                            capsList.Add(parsedCap);
                        }
                    }
                    capabilities = capsList.Count > 0 ? capsList.ToArray() : null;
                }

                return new DscResourceInfo
                {
                    Type = type,
                    Kind = kind,
                    Description = description,
                    Version = version,
                    Schema = schema,
                    Capabilities = capabilities
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse resource manifest: {ex.Message}");
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}

