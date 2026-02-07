// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;

namespace OpenDsc.Server.Services;

/// <summary>
/// Utility class for generating cryptographic keys.
/// </summary>
public static class KeyGenerator
{
    /// <summary>
    /// Generates a new random registration key (32 bytes, Base64-encoded).
    /// </summary>
    public static string GenerateRegistrationKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
