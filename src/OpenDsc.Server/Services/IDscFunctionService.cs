// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.DscFunctions;

namespace OpenDsc.Server.Services;

public interface IDscFunctionService
{
    /// <summary>
    /// Returns the catalog of available DSC configuration document functions.
    /// The list is built from the well-known function set and cached.
    /// </summary>
    IReadOnlyList<DscFunctionInfo> GetFunctions();

    /// <summary>
    /// Asynchronously returns the catalog of available DSC configuration document functions.
    /// Fetches the list from the DSC MCP server.
    /// </summary>
    Task<IReadOnlyList<DscFunctionInfo>> GetFunctionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a DSC function expression by delegating to the DSC MCP server.
    /// </summary>
    Task<EvaluateDscFunctionResult> EvaluateAsync(EvaluateDscFunctionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a pre-built DSC expression string by delegating to the DSC MCP server via <c>invoke_dsc_expression</c>.
    /// </summary>
    Task<EvaluateDscFunctionResult> EvaluateExpressionAsync(string expression, CancellationToken cancellationToken = default);
}
