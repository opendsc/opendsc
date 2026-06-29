// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource.Windows.FileSystem.Share;

/// <summary>
/// Represents a single permission entry for an SMB share.
/// </summary>
public sealed class SharePermission
{
    /// <summary>
    /// Gets or sets the principal (user or group) name.
    /// Format: DOMAIN\name, BUILTIN\name, or .\name for local principals.
    /// </summary>
    public required string Principal { get; set; }

    /// <summary>
    /// Gets or sets the access level granted to this principal.
    /// </summary>
    public required AccessLevel Access { get; set; }
}
