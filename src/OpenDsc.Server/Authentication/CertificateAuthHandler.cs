// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using OpenDsc.Server.Data;

namespace OpenDsc.Server.Authentication;

/// <summary>
/// Authentication handler for certificate-based authentication.
/// </summary>
public sealed class CertificateAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IServiceScopeFactory scopeFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly ILogger _logger = logger.CreateLogger<CertificateAuthHandler>();

    /// <summary>
    /// Authentication scheme name for node certificates.
    /// </summary>
    public const string NodeScheme = "NodeCertificate";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        _logger.LogDebug("CertificateAuthHandler.HandleAuthenticateAsync called");
        var clientCert = Context.Connection.ClientCertificate;

        // In testing environment, authenticate without certificate
        if (clientCert is null && Context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsEnvironment("Testing"))
        {
            // Extract node ID from URL for testing
            var path = Context.Request.Path;
            _logger.LogDebug("Testing auth: Path = {Path}", path);
            var nodeIdMatch = System.Text.RegularExpressions.Regex.Match(path, @"/api/v1/nodes/(?<nodeId>[a-f0-9\-]+)");
            _logger.LogDebug("Testing auth: Regex match success = {Success}", nodeIdMatch.Success);
            if (nodeIdMatch.Success)
            {
                _logger.LogDebug("Testing auth: Captured nodeId = {NodeId}", nodeIdMatch.Groups["nodeId"].Value);
                if (Guid.TryParse(nodeIdMatch.Groups["nodeId"].Value, out var testNodeId))
                {
                    _logger.LogDebug("Testing auth: Parsed GUID = {Guid}", testNodeId);
                    using var testScope = scopeFactory.CreateScope();
                    var testDbContext = testScope.ServiceProvider.GetRequiredService<ServerDbContext>();

                    var testNode = await testDbContext.Nodes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(n => n.Id == testNodeId);
                    _logger.LogDebug("Testing auth: Node found = {Found}", testNode is not null);
                    if (testNode is not null)
                    {
                        _logger.LogDebug("Testing auth: Creating test claims");
                        var testClaims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, testNode.Id.ToString()),
                            new Claim(ClaimTypes.Name, testNode.Fqdn),
                            new Claim("node_id", testNode.Id.ToString()),
                            new Claim("cert_thumbprint", "TEST-THUMBPRINT"),
                            new Claim(ClaimTypes.Role, "Node")
                        };

                        var testIdentity = new ClaimsIdentity(testClaims, Scheme.Name);
                        var testPrincipal = new ClaimsPrincipal(testIdentity);
                        var testTicket = new AuthenticationTicket(testPrincipal, Scheme.Name);

                        return AuthenticateResult.Success(testTicket);
                    }
                }
            }
            _logger.LogDebug("Testing auth: Falling back to certificate auth");

            return AuthenticateResult.NoResult();
        }

        if (clientCert is null)
        {
            return AuthenticateResult.NoResult();
        }

        var thumbprint = clientCert.Thumbprint;
        if (string.IsNullOrEmpty(thumbprint))
        {
            return AuthenticateResult.Fail("Certificate thumbprint is empty");
        }

        if (DateTime.UtcNow > clientCert.NotAfter)
        {
            return AuthenticateResult.Fail("Certificate has expired");
        }

        if (!ValidateCertificateChain(clientCert))
        {
            return AuthenticateResult.Fail("Certificate chain validation failed");
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        var node = await dbContext.Nodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.CertificateThumbprint == thumbprint);

        if (node is null)
        {
            return AuthenticateResult.Fail("Certificate not registered");
        }

        if (DateTime.UtcNow > node.CertificateNotAfter)
        {
            return AuthenticateResult.Fail("Registered certificate has expired");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, node.Id.ToString()),
            new Claim(ClaimTypes.Name, node.Fqdn),
            new Claim("node_id", node.Id.ToString()),
            new Claim("cert_thumbprint", thumbprint),
            new Claim(ClaimTypes.Role, "Node")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    /// <summary>
    /// Validates the certificate chain including CRL/OCSP checking for enterprise certificates.
    /// </summary>
    private static bool ValidateCertificateChain(X509Certificate2 certificate)
    {
        using var chain = new X509Chain
        {
            ChainPolicy =
            {
                RevocationMode = X509RevocationMode.Online,
                RevocationFlag = X509RevocationFlag.ExcludeRoot,
                VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority,
                UrlRetrievalTimeout = TimeSpan.FromSeconds(30)
            }
        };

        chain.Build(certificate);

        if (chain.ChainStatus.Length == 0)
        {
            return true;
        }

        foreach (var status in chain.ChainStatus)
        {
            if (status.Status == X509ChainStatusFlags.UntrustedRoot)
            {
                continue;
            }

            if (status.Status != X509ChainStatusFlags.NoError)
            {
                return false;
            }
        }

        return true;
    }
}
