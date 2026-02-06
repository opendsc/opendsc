// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Options;

namespace OpenDsc.Server.Authentication;

/// <summary>
/// Authentication handler for admin API key-based authentication.
/// </summary>
public sealed class ApiKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>
    /// Authentication scheme name for admin API keys.
    /// </summary>
    public const string AdminScheme = "AdminApiKey";

    private const string AuthorizationHeader = "Authorization";
    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        await Task.CompletedTask;
        return AuthenticateResult.NoResult();
    }

    /// <summary>
    /// Hashes a password using PBKDF2-HMAC-SHA256.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <param name="salt">The salt (output parameter).</param>
    /// <returns>The hashed password.</returns>
    public static string HashPasswordPbkdf2(string password, out string salt)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        salt = Convert.ToBase64String(saltBytes);

        var hashBytes = KeyDerivation.Pbkdf2(
            password: password,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 32);

        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Verifies a password against a PBKDF2-HMAC-SHA256 hash.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="saltBase64">The salt (Base64-encoded).</param>
    /// <param name="hashBase64">The expected hash (Base64-encoded).</param>
    /// <returns>True if the password matches, false otherwise.</returns>
    public static bool VerifyPasswordPbkdf2(string password, string saltBase64, string hashBase64)
    {
        var saltBytes = Convert.FromBase64String(saltBase64);
        var expectedHash = Convert.FromBase64String(hashBase64);

        var computedHash = KeyDerivation.Pbkdf2(
            password: password,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 32);

        return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
    }

    /// <summary>
    /// Generates a new random registration key (32 bytes, Base64-encoded).
    /// </summary>
    public static string GenerateRegistrationKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
