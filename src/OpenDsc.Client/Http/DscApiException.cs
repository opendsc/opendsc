// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

namespace OpenDsc.Client.Http;

/// <summary>
/// Thrown when the DSC server returns a non-success HTTP response.
/// </summary>
public sealed class DscApiException : HttpRequestException
{
    /// <summary>The HTTP status code returned by the server.</summary>
    public new HttpStatusCode StatusCode { get; }

    /// <summary>The response body, if any.</summary>
    public string? ResponseBody { get; }

    internal DscApiException(HttpStatusCode statusCode, string? responseBody, string message)
        : base(message, inner: null, statusCode)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
