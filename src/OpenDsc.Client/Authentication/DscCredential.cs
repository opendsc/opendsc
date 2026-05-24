// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Client.Authentication;

/// <summary>
/// Abstract base class for DSC server credentials.
/// Subclass to implement a specific authentication mechanism.
/// </summary>
public abstract class DscCredential
{
    /// <summary>
    /// Returns the bearer token to include in the <c>Authorization</c> header.
    /// </summary>
    public abstract ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
