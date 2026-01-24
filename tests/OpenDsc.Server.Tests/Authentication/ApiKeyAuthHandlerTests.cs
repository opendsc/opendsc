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
    public void HashApiKey_ProducesSha256Hash()
    {
        var apiKey = "test-api-key-123";

        var hash = ApiKeyAuthHandler.HashApiKey(apiKey);

        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public void HashApiKey_ProducesConsistentResults()
    {
        var apiKey = "test-api-key-123";

        var hash1 = ApiKeyAuthHandler.HashApiKey(apiKey);
        var hash2 = ApiKeyAuthHandler.HashApiKey(apiKey);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashApiKey_ProducesDifferentHashesForDifferentKeys()
    {
        var apiKey1 = "test-api-key-123";
        var apiKey2 = "test-api-key-456";

        var hash1 = ApiKeyAuthHandler.HashApiKey(apiKey1);
        var hash2 = ApiKeyAuthHandler.HashApiKey(apiKey2);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GenerateApiKey_ProducesBase64String()
    {
        var apiKey = ApiKeyAuthHandler.GenerateApiKey();

        apiKey.Should().NotBeNullOrEmpty();
        var bytes = Convert.FromBase64String(apiKey);
        bytes.Length.Should().Be(32);
    }

    [Fact]
    public void GenerateApiKey_ProducesDifferentKeysOnEachCall()
    {
        var apiKey1 = ApiKeyAuthHandler.GenerateApiKey();
        var apiKey2 = ApiKeyAuthHandler.GenerateApiKey();

        apiKey1.Should().NotBe(apiKey2);
    }

    [Fact]
    public void GenerateApiKey_ProducesKeysThatCanBeHashed()
    {
        var apiKey = ApiKeyAuthHandler.GenerateApiKey();

        var hash = ApiKeyAuthHandler.HashApiKey(apiKey);

        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("short-key")]
    [InlineData("very-long-key-with-many-characters-for-testing")]
    public void HashApiKey_HandlesVariousKeyLengths(string apiKey)
    {
        var hash = ApiKeyAuthHandler.HashApiKey(apiKey);

        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public void HashApiKey_ProducesLowercaseHexString()
    {
        var apiKey = "TEST-API-KEY";

        var hash = ApiKeyAuthHandler.HashApiKey(apiKey);

        hash.Should().Be(hash.ToLowerInvariant());
    }
}
