// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace OpenDsc.Schema;

/// <summary>
/// JSON converter for TimeSpan that handles ISO 8601 duration format.
/// </summary>
public sealed class Iso8601DurationConverter : JsonConverter<TimeSpan?>
{
    /// <inheritdoc/>
    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            return XmlConvert.ToTimeSpan(value);
        }
        catch (FormatException ex)
        {
            throw new JsonException($"Invalid ISO 8601 duration format: {value}", ex);
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(XmlConvert.ToString(value.Value));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
