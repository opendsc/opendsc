// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using AwesomeAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Moq;

using OpenDsc.Server.Authentication;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Authentication;

[Trait("Category", "Unit")]
public sealed class AuthenticationExtensionsTests
{
    // ─── TryGetJwtIssuer ─────────────────────────────────────────────────────

    private static string CreateTestJwt(string issuer = "https://issuer.example.com", bool includeSub = true)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-signing-key-32-bytes-long!!!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = includeSub
            ? new[] { new Claim("sub", "test-user-id") }
            : Array.Empty<Claim>();

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: "test-audience",
            claims: claims,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public void TryGetJwtIssuer_ValidJwt_ReturnsIssuer()
    {
        var token = CreateTestJwt("https://login.microsoftonline.com/tenant/v2.0");

        var issuer = AuthenticationExtensions.TryGetJwtIssuer(token);

        issuer.Should().Be("https://login.microsoftonline.com/tenant/v2.0");
    }

    [Fact]
    public void TryGetJwtIssuer_ValidJwtNoSub_ReturnsIssuer()
    {
        var token = CreateTestJwt("https://issuer.example.com", includeSub: false);

        var issuer = AuthenticationExtensions.TryGetJwtIssuer(token);

        issuer.Should().Be("https://issuer.example.com");
    }

    [Fact]
    public void TryGetJwtIssuer_NotAJwt_ReturnsNull()
    {
        var issuer = AuthenticationExtensions.TryGetJwtIssuer("pat_some_personal_access_token");

        issuer.Should().BeNull();
    }

    [Fact]
    public void TryGetJwtIssuer_EmptyString_ReturnsNull()
    {
        var issuer = AuthenticationExtensions.TryGetJwtIssuer(string.Empty);

        issuer.Should().BeNull();
    }

    [Fact]
    public void TryGetJwtIssuer_MalformedJwt_ReturnsNull()
    {
        var issuer = AuthenticationExtensions.TryGetJwtIssuer("not.a.valid.jwt.token.at.all");

        issuer.Should().BeNull();
    }

    [Fact]
    public void TryGetJwtIssuer_TwoSegmentString_ReturnsNull()
    {
        var issuer = AuthenticationExtensions.TryGetJwtIssuer("header.payload");

        issuer.Should().BeNull();
    }

    [Fact]
    public void TryGetJwtIssuer_ThreePartInvalidBase64_ReturnsNull()
    {
        // "!!!.!!!.!!!" has 2 dots (3 parts) so CanReadToken returns true,
        // but ReadJwtToken throws when it tries to base64url-decode the header.
        var issuer = AuthenticationExtensions.TryGetJwtIssuer("!!!.!!!.!!!");

        issuer.Should().BeNull();
    }

    // ─── HandleOidcTokenValidatedAsync ───────────────────────────────────────

    private static TokenValidatedContext CreateOidcContext(
        ClaimsPrincipal principal,
        IServiceProvider serviceProvider)
    {
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var scheme = new AuthenticationScheme("oidc_test", null, typeof(CookieAuthenticationHandler));
        return new TokenValidatedContext(
            httpContext, scheme, new OpenIdConnectOptions(), principal, new AuthenticationProperties());
    }

    [Fact]
    public async Task HandleOidcTokenValidated_NameIdentifierClaim_ProvisionsUserAndAddsIdClaim()
    {
        var mockService = new Mock<IOidcUserProvisioningService>();
        var expectedUserId = Guid.NewGuid();
        mockService
            .Setup(s => s.ProvisionOrGetUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = expectedUserId, Username = "alice" });

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "sub-001")], "test");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateOidcContext(principal, new ServiceCollection().BuildServiceProvider());

        await AuthenticationExtensions.HandleOidcTokenValidatedAsync(context, "entra", mockService.Object);

        mockService.Verify(s => s.ProvisionOrGetUserAsync(
            "entra", "sub-001", null, null, null, It.IsAny<CancellationToken>()), Times.Once);

        identity.Claims
            .Where(c => c.Type == ClaimTypes.NameIdentifier)
            .Select(c => c.Value)
            .Should().ContainSingle().Which.Should().Be(expectedUserId.ToString());
    }

    [Fact]
    public async Task HandleOidcTokenValidated_SubClaim_UsedAsProviderKey()
    {
        var mockService = new Mock<IOidcUserProvisioningService>();
        var expectedUserId = Guid.NewGuid();
        mockService
            .Setup(s => s.ProvisionOrGetUserAsync(
                "entra", "sub-from-sub-claim", It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = expectedUserId, Username = "bob" });

        var identity = new ClaimsIdentity([new Claim("sub", "sub-from-sub-claim")], "test");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateOidcContext(principal, new ServiceCollection().BuildServiceProvider());

        await AuthenticationExtensions.HandleOidcTokenValidatedAsync(context, "entra", mockService.Object);

        mockService.Verify(s => s.ProvisionOrGetUserAsync(
            "entra", "sub-from-sub-claim", null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleOidcTokenValidated_AllClaimsPresent_PassesCorrectValues()
    {
        var mockService = new Mock<IOidcUserProvisioningService>();
        var expectedUserId = Guid.NewGuid();
        mockService
            .Setup(s => s.ProvisionOrGetUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = expectedUserId, Username = "carol" });

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "sub-carol"),
                new Claim(ClaimTypes.Email, "carol@example.com"),
                new Claim(ClaimTypes.Name, "Carol Smith"),
                new Claim("preferred_username", "carol.smith"),
            ], "test");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateOidcContext(principal, new ServiceCollection().BuildServiceProvider());

        await AuthenticationExtensions.HandleOidcTokenValidatedAsync(context, "entra", mockService.Object);

        mockService.Verify(s => s.ProvisionOrGetUserAsync(
            "entra", "sub-carol", "Carol Smith", "carol@example.com", "carol.smith",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleOidcTokenValidated_NoSubjectClaim_ThrowsInvalidOperationException()
    {
        var mockService = new Mock<IOidcUserProvisioningService>();
        var identity = new ClaimsIdentity([new Claim("email", "dave@example.com")], "test");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateOidcContext(principal, new ServiceCollection().BuildServiceProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => AuthenticationExtensions.HandleOidcTokenValidatedAsync(context, "entra", mockService.Object));
    }

    [Fact]
    public async Task HandleOidcTokenValidated_EmailFallsBackToPreferredUsername()
    {
        var mockService = new Mock<IOidcUserProvisioningService>();
        var expectedUserId = Guid.NewGuid();
        mockService
            .Setup(s => s.ProvisionOrGetUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = expectedUserId, Username = "eve" });

        var identity = new ClaimsIdentity(
            [
                new Claim("sub", "sub-eve"),
                new Claim(ClaimTypes.Email, "eve@example.com"),
            ], "test");
        var principal = new ClaimsPrincipal(identity);
        var context = CreateOidcContext(principal, new ServiceCollection().BuildServiceProvider());

        await AuthenticationExtensions.HandleOidcTokenValidatedAsync(context, "entra", mockService.Object);

        // When no preferred_username claim, email is used as preferredUsername
        mockService.Verify(s => s.ProvisionOrGetUserAsync(
            "entra", "sub-eve", null, "eve@example.com", "eve@example.com",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
