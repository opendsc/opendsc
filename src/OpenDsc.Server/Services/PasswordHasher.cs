// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;

namespace OpenDsc.Server.Services;

/// <summary>
/// Service for hashing and validating passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using PBKDF2-HMAC-SHA256.
    /// </summary>
    /// <param name="password">Plain text password.</param>
    /// <returns>Tuple of (hash, salt) in Base64 format.</returns>
    (string Hash, string Salt) HashPassword(string password);

    /// <summary>
    /// Validates a password against a hash and salt.
    /// </summary>
    /// <param name="password">Plain text password to validate.</param>
    /// <param name="hash">Stored hash in Base64 format.</param>
    /// <param name="salt">Stored salt in Base64 format.</param>
    /// <returns>True if password matches, false otherwise.</returns>
    bool ValidatePassword(string password, string hash, string salt);
}

/// <summary>
/// Password hashing implementation using PBKDF2-HMAC-SHA256.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public (string Hash, string Salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool ValidatePassword(string password, string hash, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var hashBytes = Convert.FromBase64String(hash);

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return CryptographicOperations.FixedTimeEquals(computedHash, hashBytes);
    }
}
