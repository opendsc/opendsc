// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using OpenDsc.Client.Http;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Services;

/// <summary>
/// HTTP implementation of server health operations.
/// </summary>
public sealed class HealthHttpService(HttpClient client)
    : HttpServiceBase(client), IHealthService
{
    /// <inheritdoc />
    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await Client.GetAsync("health/ready", cancellationToken).ConfigureAwait(false);
            return response.StatusCode != HttpStatusCode.ServiceUnavailable;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}
