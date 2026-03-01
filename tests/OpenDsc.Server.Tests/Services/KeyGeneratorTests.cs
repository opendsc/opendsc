// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class KeyGeneratorTests
{
    [Fact]
    public void GenerateRegistrationKey_ProducesBase64String()
    {
        var key = KeyGenerator.GenerateRegistrationKey();

        key.Should().NotBeNullOrEmpty();
        var bytes = Convert.FromBase64String(key);
        bytes.Length.Should().Be(32);
    }

    [Fact]
    public void GenerateRegistrationKey_ProducesDifferentKeysOnEachCall()
    {
        var key1 = KeyGenerator.GenerateRegistrationKey();
        var key2 = KeyGenerator.GenerateRegistrationKey();

        key1.Should().NotBe(key2);
    }
}
