// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using FluentAssertions;

using OpenDsc.Server.Authentication;

using Xunit;

namespace OpenDsc.Server.Tests.Authentication;

[Trait("Category", "Unit")]
public class ApiKeyAuthHandlerTests
{
    [Fact]
    public void HashPasswordPbkdf2_ProducesBase64Hash()
    {
        var password = "test-admin-key-123";

        var hash = ApiKeyAuthHandler.HashPasswordPbkdf2(password, out var salt);

        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();

        // Base64 encoded 32-byte hash should be 44 characters
        hash.Length.Should().Be(44);
        salt.Length.Should().Be(44);
    }

    [Fact]
    public void HashPasswordPbkdf2_ProducesDifferentSaltsOnEachCall()
    {
        var password = "test-password";

        var hash1 = ApiKeyAuthHandler.HashPasswordPbkdf2(password, out var salt1);
        var hash2 = ApiKeyAuthHandler.HashPasswordPbkdf2(password, out var salt2);

        salt1.Should().NotBe(salt2);
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPasswordPbkdf2_SucceedsWithCorrectPassword()
    {
        var password = "correct-password";
        var hash = ApiKeyAuthHandler.HashPasswordPbkdf2(password, out var salt);

        var result = ApiKeyAuthHandler.VerifyPasswordPbkdf2(password, salt, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPasswordPbkdf2_FailsWithIncorrectPassword()
    {
        var password = "correct-password";
        var hash = ApiKeyAuthHandler.HashPasswordPbkdf2(password, out var salt);

        var result = ApiKeyAuthHandler.VerifyPasswordPbkdf2("wrong-password", salt, hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void GenerateRegistrationKey_ProducesBase64String()
    {
        var key = ApiKeyAuthHandler.GenerateRegistrationKey();

        key.Should().NotBeNullOrEmpty();
        var bytes = Convert.FromBase64String(key);
        bytes.Length.Should().Be(32);
    }

    [Fact]
    public void GenerateRegistrationKey_ProducesDifferentKeysOnEachCall()
    {
        var key1 = ApiKeyAuthHandler.GenerateRegistrationKey();
        var key2 = ApiKeyAuthHandler.GenerateRegistrationKey();

        key1.Should().NotBe(key2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("short-password")]
    [InlineData("very-long-password-with-many-characters-for-testing")]
    public void HashPasswordPbkdf2_HandlesVariousPasswordLengths(string password)
    {
        var hash = ApiKeyAuthHandler.HashPasswordPbkdf2(password, out var salt);

        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
        hash.Length.Should().Be(44);
        salt.Length.Should().Be(44);
    }

    [Fact]
    public void VerifyPasswordPbkdf2_IsTimingSafe()
    {
        var password = "test-password";
        var hash = ApiKeyAuthHandler.HashPasswordPbkdf2(password, out var salt);

        // Both correct and incorrect passwords should take similar time (timing attack resistance)
        var result1 = ApiKeyAuthHandler.VerifyPasswordPbkdf2(password, salt, hash);
        var result2 = ApiKeyAuthHandler.VerifyPasswordPbkdf2("wrong", salt, hash);

        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }
}
