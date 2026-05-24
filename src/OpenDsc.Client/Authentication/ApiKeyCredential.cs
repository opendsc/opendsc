// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Client.Authentication;

/// <summary>
/// Authenticates using a Personal Access Token (PAT).
/// The token is sent as <c>Authorization: Bearer pat_xxx</c>.
/// </summary>
public sealed class ApiKeyCredential(string token) : DscCredential
{
    /// <inheritdoc />
    public override ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(token);
}
