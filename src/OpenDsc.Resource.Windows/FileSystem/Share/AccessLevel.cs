// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource.Windows.FileSystem.Share;

/// <summary>
/// Defines the access level for an SMB share permission.
/// </summary>
public enum AccessLevel
{
    /// <summary>
    /// No access. Used to explicitly remove a principal from share permissions.
    /// </summary>
    None = 0,

    /// <summary>
    /// Read-only access (0x1201BF).
    /// </summary>
    Read = 1,

    /// <summary>
    /// Read and write access (0x1201BF | 0x00100000).
    /// </summary>
    Change = 2,

    /// <summary>
    /// Full administrative access (0x1F01FF).
    /// </summary>
    Full = 3
}
