// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Lcm.Contracts;

/// <summary>
/// The source of configuration documents for the LCM.
/// </summary>
public enum ConfigurationSource
{
    /// <summary>
    /// Use a local configuration file.
    /// </summary>
    Local,

    /// <summary>
    /// Pull configuration from a remote server.
    /// </summary>
    Pull
}
