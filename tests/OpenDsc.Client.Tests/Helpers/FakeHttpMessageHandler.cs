// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Text;
using System.Text.Json;

namespace OpenDsc.Client.Tests.Helpers;

/// <summary>
/// A fake <see cref="HttpMessageHandler"/> that returns pre-configured responses in order.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();

    /// <summary>
    /// All requests received by this handler in order.
    /// </summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests;

    /// <summary>
    /// The most recent request received.
    /// </summary>
    public HttpRequestMessage? LastRequest => _requests.Count > 0 ? _requests[^1] : null;

    /// <summary>
    /// Enqueues a response to return for the next request.
    /// </summary>
    public FakeHttpMessageHandler Respond(HttpResponseMessage response)
    {
        _responses.Enqueue(response);
        return this;
    }

    /// <summary>
    /// Enqueues a JSON response with the specified status code and body.
    /// </summary>
    public FakeHttpMessageHandler RespondJson<T>(HttpStatusCode statusCode, T body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        _responses.Enqueue(new HttpResponseMessage(statusCode) { Content = content });
        return this;
    }

    /// <summary>
    /// Enqueues a 200 OK JSON response.
    /// </summary>
    public FakeHttpMessageHandler RespondOk<T>(T body) => RespondJson(HttpStatusCode.OK, body);

    /// <summary>
    /// Enqueues a 204 No Content response.
    /// </summary>
    public FakeHttpMessageHandler RespondNoContent()
    {
        _responses.Enqueue(new HttpResponseMessage(HttpStatusCode.NoContent));
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _requests.Add(request);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No more responses have been configured.");
        }

        return Task.FromResult(_responses.Dequeue());
    }
}
