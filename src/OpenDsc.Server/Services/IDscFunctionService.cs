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
    /// Evaluates a DSC function expression with the provided arguments and
    /// optional mock values for parameters, variables, and environment variables.
    /// </summary>
    EvaluateDscFunctionResult Evaluate(EvaluateDscFunctionRequest request);
}
