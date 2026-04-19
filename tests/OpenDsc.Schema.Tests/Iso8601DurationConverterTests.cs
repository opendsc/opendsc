// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Schema.Tests;

public class Iso8601DurationConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new Iso8601DurationConverter() },
        WriteIndented = true
    };

    [Theory]
    [InlineData("PT1H")]
    [InlineData("PT30M")]
    [InlineData("PT45S")]
    [InlineData("PT1H30M")]
    [InlineData("PT1H30M45S")]
    public void Iso8601DurationConverter_Read_WithValidDuration_ShouldDeserialize(string duration)
    {
        var json = $"{{\"duration\": \"{duration}\"}}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        using var memoryStream = new MemoryStream(bytes);
        using var doc = JsonDocument.Parse(memoryStream);
        var element = doc.RootElement.GetProperty("duration");
        var reader = element.GetRawText();
        var utf8Reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(reader));
        var converter = new Iso8601DurationConverter();
        utf8Reader.Read();

        var result = converter.Read(ref utf8Reader, typeof(TimeSpan?), _options);

        result.Should().NotBeNull();
        result!.Value.Should().NotBe(TimeSpan.Zero);
    }

    [Fact]
    public void Iso8601DurationConverter_Read_WithNullValue_ShouldReturnNull()
    {
        var json = "null";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        var converter = new Iso8601DurationConverter();
        reader.Read();

        var result = converter.Read(ref reader, typeof(TimeSpan?), _options);

        result.Should().BeNull();
    }

    [Fact]
    public void Iso8601DurationConverter_Read_WithEmptyString_ShouldReturnNull()
    {
        var json = "\"\"";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        var converter = new Iso8601DurationConverter();
        reader.Read();

        var result = converter.Read(ref reader, typeof(TimeSpan?), _options);

        result.Should().BeNull();
    }

    [Fact]
    public void Iso8601DurationConverter_Read_WithInvalidDuration_ShouldThrowJsonException()
    {
        var json = "\"INVALID\"";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        var act = () =>
        {
            var reader = new Utf8JsonReader(bytes);
            var converter = new Iso8601DurationConverter();
            reader.Read();
            converter.Read(ref reader, typeof(TimeSpan?), _options);
        };

        act.Should().Throw<JsonException>()
            .WithInnerException<FormatException>();
    }

    [Fact]
    public void Iso8601DurationConverter_Write_WithValidTimeSpan_ShouldSerialize()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(memoryStream);
        var converter = new Iso8601DurationConverter();
        var timespan = new TimeSpan(1, 30, 45); // 1 hour, 30 minutes, 45 seconds

        writer.WriteStartObject();
        writer.WritePropertyName("duration");
        converter.Write(writer, timespan, _options);
        writer.WriteEndObject();
        writer.Flush();

        var json = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        json.Should().Contain("PT1H30M45S");
    }

    [Fact]
    public void Iso8601DurationConverter_Write_WithNull_ShouldWriteNull()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(memoryStream);
        var converter = new Iso8601DurationConverter();

        writer.WriteStartObject();
        writer.WritePropertyName("duration");
        converter.Write(writer, null, _options);
        writer.WriteEndObject();
        writer.Flush();

        var json = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        json.Should().Contain("null");
    }

    [Fact]
    public void Iso8601DurationConverter_Write_WithZeroTimeSpan_ShouldSerializeZero()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(memoryStream);
        var converter = new Iso8601DurationConverter();
        var timespan = TimeSpan.Zero;

        writer.WriteStartObject();
        writer.WritePropertyName("duration");
        converter.Write(writer, timespan, _options);
        writer.WriteEndObject();
        writer.Flush();

        var json = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        json.Should().Contain("PT0S");
    }
}
