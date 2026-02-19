// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Schema.Tests;

public class DscResourceResultTests
{
    [Fact]
    public void DscResourceResult_DefaultValues_ShouldBeCorrect()
    {
        var result = new DscResourceResult();

        result.Name.Should().Be(string.Empty);
        result.Type.Should().Be(string.Empty);
        result.Result.ValueKind.Should().Be(JsonValueKind.Undefined);
    }

    [Fact]
    public void DscResourceResult_WithProperties_ShouldStoreValues()
    {
        var jsonResult = JsonDocument.Parse("{\"test\": \"value\"}").RootElement;
        var result = new DscResourceResult
        {
            Name = "TestInstance",
            Type = "Test/Resource",
            Result = jsonResult
        };

        result.Name.Should().Be("TestInstance");
        result.Type.Should().Be("Test/Resource");
        result.Result.Should().NotBeNull();
        result.Result!.GetProperty("test").GetString().Should().Be("value");
    }
}
