// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Controls how scope values are handled for a scope type.
/// </summary>
public enum ScopeValueMode
{
    /// <summary>
    /// Unrestricted mode allows any value for this scope type.
    /// </summary>
    Unrestricted = 0,

    /// <summary>
    /// Restricted mode only allows predefined scope values for this scope type.
    /// </summary>
    Restricted = 1
}
