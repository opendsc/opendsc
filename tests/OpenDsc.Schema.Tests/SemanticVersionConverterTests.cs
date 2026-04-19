// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using NuGet.Versioning;

using Xunit;

namespace OpenDsc.Schema.Tests;

[Trait("Category", "Unit")]
public class SemanticVersionConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new SemanticVersionConverter() },
        WriteIndented = true
    };

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.3.4")]
    [InlineData("0.0.1")]
    [InlineData("10.20.30")]
    public void SemanticVersionConverter_Read_WithValidVersion_ShouldDeserialize(string versionString)
    {
        var json = $"\"{versionString}\"";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        var converter = new SemanticVersionConverter();
        reader.Read();

        var result = converter.Read(ref reader, typeof(SemanticVersion), _options);

        result.Should().NotBeNull();
        result!.ToString().Should().Be(versionString);
    }

    [Theory]
    [InlineData("1.0.0-alpha")]
    [InlineData("2.3.4-beta.1")]
    [InlineData("1.0.0-rc.1+build.123")]
    [InlineData("1.0.0+metadata")]
    public void SemanticVersionConverter_Read_WithPreReleaseAndMetadata_ShouldDeserialize(string versionString)
    {
        var json = $"\"{versionString}\"";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        var converter = new SemanticVersionConverter();
        reader.Read();

        var result = converter.Read(ref reader, typeof(SemanticVersion), _options);

        result.Should().NotBeNull();
        result!.ToNormalizedString().Should().Be(SemanticVersion.Parse(versionString).ToNormalizedString());
    }

    [Fact]
    public void SemanticVersionConverter_Read_WithNullValue_ShouldReturnNull()
    {
        var json = "null";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        var converter = new SemanticVersionConverter();
        reader.Read();

        var result = converter.Read(ref reader, typeof(SemanticVersion), _options);

        result.Should().BeNull();
    }

    [Fact]
    public void SemanticVersionConverter_Read_WithEmptyString_ShouldThrowJsonException()
    {
        var json = "\"\"";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        var act = () =>
        {
            var reader = new Utf8JsonReader(bytes);
            var converter = new SemanticVersionConverter();
            reader.Read();
            converter.Read(ref reader, typeof(SemanticVersion), _options);
        };

        act.Should().Throw<JsonException>();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("1.0")]
    [InlineData("1.0.0.0")]
    [InlineData("a.b.c")]
    public void SemanticVersionConverter_Read_WithInvalidVersion_ShouldThrowJsonException(string versionString)
    {
        var json = $"\"{versionString}\"";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        var act = () =>
        {
            var reader = new Utf8JsonReader(bytes);
            var converter = new SemanticVersionConverter();
            reader.Read();
            converter.Read(ref reader, typeof(SemanticVersion), _options);
        };

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void SemanticVersionConverter_Write_WithValidVersion_ShouldSerialize()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(memoryStream);
        var converter = new SemanticVersionConverter();
        var version = SemanticVersion.Parse("1.2.3");

        writer.WriteStartObject();
        writer.WritePropertyName("version");
        converter.Write(writer, version, _options);
        writer.WriteEndObject();
        writer.Flush();

        var json = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        json.Should().Contain("1.2.3");
    }

    [Fact]
    public void SemanticVersionConverter_Write_WithPreReleaseVersion_ShouldSerializeWithPreRelease()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(memoryStream);
        var converter = new SemanticVersionConverter();
        var version = SemanticVersion.Parse("1.0.0-beta.1");

        writer.WriteStartObject();
        writer.WritePropertyName("version");
        converter.Write(writer, version, _options);
        writer.WriteEndObject();
        writer.Flush();

        var json = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        json.Should().Contain("1.0.0-beta.1");
    }

    [Fact]
    public void SemanticVersionConverter_Write_WithNull_ShouldWriteNull()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(memoryStream);
        var converter = new SemanticVersionConverter();

        writer.WriteStartObject();
        writer.WritePropertyName("version");
        converter.Write(writer, null, _options);
        writer.WriteEndObject();
        writer.Flush();

        var json = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        json.Should().Contain("null");
    }

    [Fact]
    public void SemanticVersionConverter_Write_WithLargeVersionNumbers_ShouldSerializeCorrectly()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(memoryStream);
        var converter = new SemanticVersionConverter();
        var version = SemanticVersion.Parse("999.888.777");

        writer.WriteStartObject();
        writer.WritePropertyName("version");
        converter.Write(writer, version, _options);
        writer.WriteEndObject();
        writer.Flush();

        var json = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        json.Should().Contain("999.888.777");
    }

    [Fact]
    public void SemanticVersionConverter_Read_WithZeroVersion_ShouldDeserialize()
    {
        var json = "\"0.0.0\"";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        var converter = new SemanticVersionConverter();
        reader.Read();

        var result = converter.Read(ref reader, typeof(SemanticVersion), _options);

        result.Should().NotBeNull();
        result!.Major.Should().Be(0);
        result.Minor.Should().Be(0);
        result.Patch.Should().Be(0);
    }
}
