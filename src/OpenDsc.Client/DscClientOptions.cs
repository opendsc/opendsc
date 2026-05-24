// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Client.Authentication;

namespace OpenDsc.Client;

/// <summary>
/// Configuration options for the OpenDSC HTTP client.
/// </summary>
public sealed class DscClientOptions
{
    /// <summary>
    /// The base address of the OpenDSC Pull Server (e.g. <c>https://my-server:8080/</c>).
    /// A trailing slash is required.
    /// </summary>
    public required Uri BaseAddress { get; set; }

    /// <summary>
    /// The credential used to authenticate requests. Use <see cref="ApiKeyCredential"/>
    /// to authenticate with a Personal Access Token.
    /// </summary>
    public required DscCredential Credential { get; set; }

    /// <summary>
    /// Optional timeout for individual HTTP requests. Defaults to 100 seconds when not set.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}
