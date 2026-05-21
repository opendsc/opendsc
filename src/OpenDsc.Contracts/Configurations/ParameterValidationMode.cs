// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Parameter validation behavior for configurations.
/// </summary>
public enum ParameterValidationMode
{
    /// <summary>
    /// No validation is performed on configuration parameters.
    /// </summary>
    None = 0,

    /// <summary>
    /// Parameter validation issues are logged as warnings but do not prevent execution.
    /// </summary>
    Warn = 1,

    /// <summary>
    /// Parameter validation issues cause configuration execution to fail.
    /// </summary>
    Strict = 2
}
