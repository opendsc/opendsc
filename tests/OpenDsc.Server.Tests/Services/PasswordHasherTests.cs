// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using FluentAssertions;

using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class PasswordHasherTests
{
    private readonly IPasswordHasher _hasher = new PasswordHasher();

    [Theory]
    [InlineData("password123")]
    [InlineData("P@ssw0rd!")]
    [InlineData("")]
    [InlineData("very-long-password-with-special-characters-123!@#$%^&*()")]
    public void HashPassword_CreatesValidHash(string password)
    {
        var hash = _hasher.HashPassword(password, out var salt);

        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(88); // Base64 encoded 64-byte hash
        salt.Should().HaveLength(24); // Base64 encoded 16-byte salt
    }

    [Fact]
    public void HashPassword_CreatesDifferentSalts()
    {
        var password = "test-password";

        var hash1 = _hasher.HashPassword(password, out var salt1);
        var hash2 = _hasher.HashPassword(password, out var salt2);

        salt1.Should().NotBe(salt2);
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var password = "correct-password";
        var hash = _hasher.HashPassword(password, out var salt);

        var result = _hasher.VerifyPassword(password, hash, salt);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        var password = "correct-password";
        var hash = _hasher.HashPassword(password, out var salt);

        var result = _hasher.VerifyPassword("wrong-password", hash, salt);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithWrongSalt_ReturnsFalse()
    {
        var password = "test-password";
        var hash = _hasher.HashPassword(password, out var _);
        var wrongHash = _hasher.HashPassword(password, out var wrongSalt);

        var result = _hasher.VerifyPassword(password, hash, wrongSalt);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_WorksCorrectly()
    {
        var password = "";
        var hash = _hasher.HashPassword(password, out var salt);

        var result = _hasher.VerifyPassword(password, hash, salt);

        result.Should().BeTrue();
    }
}
