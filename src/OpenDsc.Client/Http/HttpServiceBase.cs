// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;

namespace OpenDsc.Client.Http;

/// <summary>
/// Base class for HTTP service implementations.
/// </summary>
public abstract class HttpServiceBase(HttpClient client)
{
    /// <summary>
    /// Gets the underlying HTTP client used to call the OpenDSC API.
    /// </summary>
    protected HttpClient Client { get; } = client;

    /// <summary>
    /// Ensures the response is successful; otherwise throws a mapped exception.
    /// </summary>
    protected static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? body = null;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // ignore read failures; body is best-effort
        }

        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                throw new KeyNotFoundException(body ?? "The requested resource was not found.");

            case HttpStatusCode.BadRequest:
                throw new ArgumentException(body ?? "The request was invalid.");

            case HttpStatusCode.Conflict:
            case HttpStatusCode.UnprocessableEntity:
                throw new InvalidOperationException(body ?? "The operation is not valid in the current state.");

            default:
                throw new DscApiException(response.StatusCode, body,
                    $"DSC API returned {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
    }

    /// <summary>Sends a GET request and deserializes the response.</summary>
    protected async Task<T> GetAsync<T>(string url, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
    {
        var response = await Client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return (await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>Sends a GET request and returns null when the resource is not found.</summary>
    protected async Task<T?> GetOrNullAsync<T>(string url, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
    {
        var response = await Client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a POST request with a body and deserializes the response.</summary>
    protected async Task<TResponse> PostAsync<TBody, TResponse>(string url, TBody body, JsonTypeInfo<TBody> bodyInfo, JsonTypeInfo<TResponse> responseInfo, CancellationToken cancellationToken)
    {
        var response = await Client.PostAsJsonAsync(url, body, bodyInfo, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return (await response.Content.ReadFromJsonAsync(responseInfo, cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>Sends a POST request with a body and no expected response body.</summary>
    protected async Task PostAsync<TBody>(string url, TBody body, JsonTypeInfo<TBody> bodyInfo, CancellationToken cancellationToken)
    {
        var response = await Client.PostAsJsonAsync(url, body, bodyInfo, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a POST request with no body and no expected response body.</summary>
    protected async Task PostAsync(string url, CancellationToken cancellationToken)
    {
        var response = await Client.PostAsync(url, null, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a PUT request with a body and deserializes the response.</summary>
    protected async Task<TResponse> PutAsync<TBody, TResponse>(string url, TBody body, JsonTypeInfo<TBody> bodyInfo, JsonTypeInfo<TResponse> responseInfo, CancellationToken cancellationToken)
    {
        var response = await Client.PutAsJsonAsync(url, body, bodyInfo, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return (await response.Content.ReadFromJsonAsync(responseInfo, cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>Sends a PUT request with a body and no expected response body.</summary>
    protected async Task PutAsync<TBody>(string url, TBody body, JsonTypeInfo<TBody> bodyInfo, CancellationToken cancellationToken)
    {
        var response = await Client.PutAsJsonAsync(url, body, bodyInfo, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a PUT request with no body and no expected response body.</summary>
    protected async Task PutAsync(string url, CancellationToken cancellationToken)
    {
        var response = await Client.PutAsync(url, null, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a PUT request with no body and deserializes the response.</summary>
    protected async Task<TResponse> PutAsync<TResponse>(string url, JsonTypeInfo<TResponse> responseInfo, CancellationToken cancellationToken)
    {
        var response = await Client.PutAsync(url, null, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return (await response.Content.ReadFromJsonAsync(responseInfo, cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>Sends a PATCH request with a body and deserializes the response.</summary>
    protected async Task<TResponse> PatchAsync<TBody, TResponse>(string url, TBody body, JsonTypeInfo<TBody> bodyInfo, JsonTypeInfo<TResponse> responseInfo, CancellationToken cancellationToken)
    {
        var response = await Client.PatchAsJsonAsync(url, body, bodyInfo, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return (await response.Content.ReadFromJsonAsync(responseInfo, cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>Sends a PATCH request with no body and deserializes the response.</summary>
    protected async Task<TResponse> PatchAsync<TResponse>(string url, JsonTypeInfo<TResponse> responseInfo, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, url);
        var response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return (await response.Content.ReadFromJsonAsync(responseInfo, cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>Sends a DELETE request.</summary>
    protected async Task DeleteAsync(string url, CancellationToken cancellationToken)
    {
        var response = await Client.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Builds a URL with query string parameters, omitting null values.</summary>
    protected static string BuildUrlWithQuery(string baseUrl, params (string Name, string? Value)[] parameters)
    {
        var query = string.Join(
            "&",
            parameters
                .Where(parameter => parameter.Value is not null)
                .Select(parameter => $"{parameter.Name}={Uri.EscapeDataString(parameter.Value!)}"));

        return query.Length == 0
            ? baseUrl
            : $"{baseUrl}?{query}";
    }
}
