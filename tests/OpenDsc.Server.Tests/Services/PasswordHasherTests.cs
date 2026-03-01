// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

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
        var (hash, salt) = _hasher.HashPassword(password);

        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(44); // Base64 encoded 32-byte hash
        salt.Should().HaveLength(24); // Base64 encoded 16-byte salt
    }

    [Fact]
    public void HashPassword_CreatesDifferentSalts()
    {
        var password = "test-password";

        var (hash1, salt1) = _hasher.HashPassword(password);
        var (hash2, salt2) = _hasher.HashPassword(password);

        salt1.Should().NotBe(salt2);
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ValidatePassword_WithCorrectPassword_ReturnsTrue()
    {
        var password = "correct-password";
        var (hash, salt) = _hasher.HashPassword(password);

        var result = _hasher.ValidatePassword(password, hash, salt);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidatePassword_WithIncorrectPassword_ReturnsFalse()
    {
        var password = "correct-password";
        var (hash, salt) = _hasher.HashPassword(password);

        var result = _hasher.ValidatePassword("wrong-password", hash, salt);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePassword_WithWrongSalt_ReturnsFalse()
    {
        var password = "test-password";
        var (hash, _) = _hasher.HashPassword(password);
        var (_, wrongSalt) = _hasher.HashPassword(password);

        var result = _hasher.ValidatePassword(password, hash, wrongSalt);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePassword_WithEmptyPassword_WorksCorrectly()
    {
        var password = "";
        var (hash, salt) = _hasher.HashPassword(password);

        var result = _hasher.ValidatePassword(password, hash, salt);

        result.Should().BeTrue();
    }
}
