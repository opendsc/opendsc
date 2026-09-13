// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpenDsc.Server.Authentication;

/// <summary>
/// Restores the node client certificate from a request header when the server runs behind a
/// TLS-terminating reverse proxy such as the Azure Container Apps ingress, Envoy, or nginx.
/// The proxy completes the mTLS handshake and forwards the certificate it received in a header.
/// The middleware places that certificate on <see cref="ConnectionInfo.ClientCertificate"/>,
/// so node registration and <see cref="CertificateAuthHandler"/> keep working unchanged.
/// </summary>
public static class ForwardedClientCertificateExtensions
{
    /// <summary>
    /// Configuration key holding the header name. Forwarding stays disabled when it is empty.
    /// </summary>
    public const string HeaderConfigurationKey = "Server:ClientCertificateHeader";

    private const string CertificateFieldPrefix = "Cert=\"";
    private const string PemHeader = "-----BEGIN CERTIFICATE-----";

    /// <summary>
    /// Registers certificate forwarding when <see cref="HeaderConfigurationKey"/> is configured.
    /// </summary>
    public static IServiceCollection AddForwardedClientCertificate(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var headerName = configuration[HeaderConfigurationKey];
        if (string.IsNullOrWhiteSpace(headerName))
        {
            return services;
        }

        services.AddCertificateForwarding(options =>
        {
            options.CertificateHeader = headerName;
            options.HeaderConverter = headerValue => ParseHeader(headerValue)!;
        });

        return services;
    }

    /// <summary>
    /// Adds the certificate forwarding middleware when <see cref="HeaderConfigurationKey"/> is
    /// configured. Call it before <c>UseAuthentication</c>.
    /// </summary>
    public static IApplicationBuilder UseForwardedClientCertificate(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration[HeaderConfigurationKey]))
        {
            app.UseCertificateForwarding();
        }

        return app;
    }

    /// <summary>
    /// Parses a forwarded client certificate header value. Supports the Envoy
    /// <c>X-Forwarded-Client-Cert</c> format (<c>Hash=...;Cert="&lt;URL-encoded PEM&gt;";Chain=...</c>),
    /// a bare PEM certificate (optionally URL-encoded), and a base64-encoded DER certificate.
    /// </summary>
    /// <param name="headerValue">The raw header value.</param>
    /// <returns>The certificate, or <see langword="null"/> when the value cannot be parsed.</returns>
    public static X509Certificate2? ParseHeader(string headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        try
        {
            var value = headerValue;
            var certIndex = value.IndexOf(CertificateFieldPrefix, StringComparison.OrdinalIgnoreCase);
            if (certIndex >= 0)
            {
                var start = certIndex + CertificateFieldPrefix.Length;
                var end = value.IndexOf('"', start);
                if (end < 0)
                {
                    return null;
                }

                value = value[start..end];
            }

            value = Uri.UnescapeDataString(value).Trim();

            if (value.Contains(PemHeader, StringComparison.Ordinal))
            {
                return X509Certificate2.CreateFromPem(value);
            }

            return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(value));
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or ArgumentException)
        {
            return null;
        }
    }
}
