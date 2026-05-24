// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using OpenDsc.Client.Authentication;

using Xunit;

namespace OpenDsc.Client.Tests;

public sealed class ApiKeyCredentialTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTokenAsync_Returns_Provided_Token()
    {
        const string token = "pat_test_token";
        var credential = new ApiKeyCredential(token);

        var result = await credential.GetTokenAsync(TestContext.Current.CancellationToken);

        result.Should().Be(token);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DscClientOptions_Assigns_Configured_Values()
    {
        var baseAddress = new Uri("https://example.test/");
        var timeout = TimeSpan.FromSeconds(42);
        var credential = new ApiKeyCredential("pat_test_token");

        var options = new DscClientOptions
        {
            BaseAddress = baseAddress,
            Credential = credential,
            Timeout = timeout
        };

        options.BaseAddress.Should().Be(baseAddress);
        options.Credential.Should().BeSameAs(credential);
        options.Timeout.Should().Be(timeout);
    }
}
