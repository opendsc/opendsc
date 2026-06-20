// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.DscFunctions;
using OpenDsc.Schema;

namespace OpenDsc.Server.Mcp;

/// <summary>
/// Client for interacting with the DSC MCP (Model Context Protocol) server.
/// Provides methods to discover and fetch DSC resource information.
/// </summary>
public interface IMcpClient
{
    /// <summary>
    /// Gets a value indicating whether the MCP client is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Initializes and connects to the MCP server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the MCP server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available DSC resources.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the list of resources.</returns>
    Task<List<DscResourceInfo>> ListResourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific DSC resource, including its schema and capabilities.
    /// </summary>
    /// <param name="typeName">The fully-qualified resource type name (e.g., "OpenDsc.Windows/Service").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the resource manifest.</returns>
    Task<DscResourceInfo?> GetResourceDetailsAsync(string typeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available DSC configuration document functions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the list of functions.</returns>
    Task<List<DscFunctionInfo>> ListFunctionsAsync(CancellationToken cancellationToken = default);
}
