// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using NuGet.Versioning;

namespace OpenDsc.Schema;

/// <summary>
/// JSON converter for SemanticVersion.
/// </summary>
public sealed class SemanticVersionConverter : JsonConverter<SemanticVersion?>
{
    /// <inheritdoc/>
    public override SemanticVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var value = reader.GetString();
        if (value is null)
        {
            return null;
        }

        try
        {
            return SemanticVersion.Parse(value);
        }
        catch (Exception ex)
        {
            throw new JsonException($"Invalid semantic version format: {value}", ex);
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, SemanticVersion? value, JsonSerializerOptions options)
    {
        if (value is not null)
        {
            writer.WriteStringValue(value.ToString());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
