// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using AwesomeAssertions;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using OpenDsc.Server.Authentication;

using Xunit;

namespace OpenDsc.Server.Tests.Authentication;

[Trait("Category", "Unit")]
public sealed class ForwardedClientCertificateExtensionsTests
{
    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=node01.example.com",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(1));
    }

    [Fact]
    public void ParseHeader_EnvoyForwardedClientCertFormat_ReturnsCertificate()
    {
        using var certificate = CreateCertificate();
        var encodedPem = Uri.EscapeDataString(certificate.ExportCertificatePem());
        var header = $"Hash=abc123;Cert=\"{encodedPem}\";Chain=\"{encodedPem}\"";

        using var result = ForwardedClientCertificateExtensions.ParseHeader(header);

        result.Should().NotBeNull();
        result!.Thumbprint.Should().Be(certificate.Thumbprint);
    }

    [Fact]
    public void ParseHeader_BarePem_ReturnsCertificate()
    {
        using var certificate = CreateCertificate();

        using var result = ForwardedClientCertificateExtensions.ParseHeader(certificate.ExportCertificatePem());

        result.Should().NotBeNull();
        result!.Thumbprint.Should().Be(certificate.Thumbprint);
    }

    [Fact]
    public void ParseHeader_Base64Der_ReturnsCertificate()
    {
        using var certificate = CreateCertificate();

        using var result = ForwardedClientCertificateExtensions.ParseHeader(Convert.ToBase64String(certificate.RawData));

        result.Should().NotBeNull();
        result!.Thumbprint.Should().Be(certificate.Thumbprint);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-certificate")]
    [InlineData("Hash=abc123;Cert=\"unterminated")]
    [InlineData("Hash=abc123;Cert=\"-----BEGIN%20CERTIFICATE-----%0Agarbage%0A-----END%20CERTIFICATE-----\"")]
    public void ParseHeader_InvalidValue_ReturnsNull(string header)
    {
        var result = ForwardedClientCertificateExtensions.ParseHeader(header);

        result.Should().BeNull();
    }

    [Fact]
    public void AddForwardedClientCertificate_WithHeaderConfigured_UsesConfiguredHeader()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ForwardedClientCertificateExtensions.HeaderConfigurationKey] = "X-Forwarded-Client-Cert"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddForwardedClientCertificate(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CertificateForwardingOptions>>().Value;
        options.CertificateHeader.Should().Be("X-Forwarded-Client-Cert");

        using var certificate = CreateCertificate();
        using var converted = options.HeaderConverter(Uri.EscapeDataString(certificate.ExportCertificatePem()));
        converted.Thumbprint.Should().Be(certificate.Thumbprint);
    }

    [Fact]
    public void AddForwardedClientCertificate_WithoutHeaderConfigured_KeepsDefaultOptions()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddOptions();

        services.AddForwardedClientCertificate(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CertificateForwardingOptions>>().Value;
        options.CertificateHeader.Should().Be("X-Client-Cert");
    }
}
