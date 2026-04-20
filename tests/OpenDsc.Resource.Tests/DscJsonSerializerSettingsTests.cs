// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Xunit;

namespace OpenDsc.Resource.Tests;

public class DscJsonSerializerSettingsTests
{
    [Fact]
    public void Default_ReturnsValidJsonSerializerOptions()
    {
        var options = DscJsonSerializerSettings.Default;

        options.Should().NotBeNull();
    }

    [Fact]
    public void Default_HasWriteIndentedFalse()
    {
        var options = DscJsonSerializerSettings.Default;

        options.WriteIndented.Should().BeFalse();
    }

    [Fact]
    public void Default_HasCamelCaseNamingPolicy()
    {
        var options = DscJsonSerializerSettings.Default;

        options.PropertyNamingPolicy.Should().NotBeNull();
        options.PropertyNamingPolicy!.ConvertName("TestProperty").Should().Be("testProperty");
    }

    [Fact]
    public void Default_HasUnmappedMemberHandlingDisallow()
    {
        var options = DscJsonSerializerSettings.Default;

        options.UnmappedMemberHandling.Should().Be(JsonUnmappedMemberHandling.Disallow);
    }

    [Fact]
    public void Default_HasDefaultIgnoreConditionWhenWritingNull()
    {
        var options = DscJsonSerializerSettings.Default;

        options.DefaultIgnoreCondition.Should().Be(JsonIgnoreCondition.WhenWritingNull);
    }

    [Fact]
    public void Default_HasStringEnumConverter()
    {
        var options = DscJsonSerializerSettings.Default;

        options.Converters.Should().NotBeEmpty();
        options.Converters.Should().ContainItemsAssignableTo<JsonStringEnumConverter>();
    }

    [Fact]
    public void DefaultSettings_SerializesCamelCase()
    {
        var options = DscJsonSerializerSettings.Default;
        var obj = new TestSchema { Name = "test", Value = 42, Enabled = true };

        var json = JsonSerializer.Serialize(obj, typeof(TestSchema), options);

        json.Should().Contain("\"name\"");
        json.Should().Contain("\"value\"");
        json.Should().Contain("\"enabled\"");
    }

    [Fact]
    public void DefaultSettings_DoesNotWriteNullProperties()
    {
        var options = DscJsonSerializerSettings.Default;
        var obj = new TestSchema { Name = null, Value = 42 };

        var json = JsonSerializer.Serialize(obj, typeof(TestSchema), options);

        json.Should().NotContain("null");
    }

    [Fact]
    public void DefaultSettings_DoesNotIndentJson()
    {
        var options = DscJsonSerializerSettings.Default;
        var obj = new TestSchema { Name = "test", Value = 42, Enabled = true };

        var json = JsonSerializer.Serialize(obj, typeof(TestSchema), options);

        json.Should().NotContain("\n");
        json.Should().NotContain("\r");
    }
}
