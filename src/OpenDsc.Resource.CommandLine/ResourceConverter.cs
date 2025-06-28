// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDsc.Resource.CommandLine;

public class ResourceConverter<T> : JsonConverter<IDscResource<T>>
{
    public override IDscResource<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Deserialization is not supported.");
    }

    public override void Write(Utf8JsonWriter writer, IDscResource<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("$schema", value.ManifestSchema);
        writer.WriteString("type", value.Type);
        writer.WriteString("description", value.Description);
        writer.WriteString("version", value.Version.ToString());
        writer.WritePropertyName("tags");
        writer.WriteStartArray();
        foreach (var tag in value.Tags)
        {
            writer.WriteStringValue(tag);
        }
        writer.WriteEndArray();

        writer.WritePropertyName("exitCodes");
        writer.WriteStartObject();

        foreach (var kvp in value.ExitCodes)
        {
            writer.WriteString(kvp.Key.ToString(), kvp.Value.Description);
        }

        writer.WriteEndObject();

        writer.WritePropertyName("schema");
        writer.WriteStartObject();
        writer.WritePropertyName("command");
        writer.WriteStartObject();
        writer.WriteString("executable", value.FileName);
        writer.WritePropertyName("args");
        writer.WriteStartArray();
        writer.WriteStringValue("schema");
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();

        if (value is IGettable<T>)
        {
            writer.WritePropertyName("get");
            writer.WriteStartObject();
            writer.WriteString("executable", value.FileName);
            writer.WritePropertyName("args");
            writer.WriteStartArray();
            writer.WriteStringValue("config");
            writer.WriteStringValue("get");
            writer.WriteStartObject();
            writer.WriteString("jsonInputArg", "--input");
            writer.WriteBoolean("mandatory", true);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        if (value is ISettable<T>)
        {
            writer.WritePropertyName("set");
            writer.WriteStartObject();
            writer.WriteString("executable", value.FileName);
            writer.WritePropertyName("args");
            writer.WriteStartArray();
            writer.WriteStringValue("config");
            writer.WriteStringValue("set");
            writer.WriteStartObject();
            writer.WriteString("jsonInputArg", "--input");
            writer.WriteBoolean("mandatory", true);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        if (value is ITestable<T>)
        {
            writer.WritePropertyName("test");
            writer.WriteStartObject();
            writer.WriteString("executable", value.FileName);
            writer.WritePropertyName("args");
            writer.WriteStartArray();
            writer.WriteStringValue("config");
            writer.WriteStringValue("test");
            writer.WriteStartObject();
            writer.WriteString("jsonInputArg", "--input");
            writer.WriteBoolean("mandatory", true);
            writer.WriteEndObject();
            writer.WriteEndArray();
            // TODO: Update dynamically
            writer.WriteString("return", "state");
            writer.WriteEndObject();
        }

        if (value is IDeletable<T>)
        {
            writer.WritePropertyName("delete");
            writer.WriteStartObject();
            writer.WriteString("executable", value.FileName);
            writer.WritePropertyName("args");
            writer.WriteStartArray();
            writer.WriteStringValue("config");
            writer.WriteStringValue("delete");
            writer.WriteStartObject();
            writer.WriteString("jsonInputArg", "--input");
            writer.WriteBoolean("mandatory", true);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        if (value is IExportable<T>)
        {
            writer.WritePropertyName("export");
            writer.WriteStartObject();
            writer.WriteString("executable", value.FileName);
            writer.WritePropertyName("args");
            writer.WriteStartArray();
            writer.WriteStringValue("config");
            writer.WriteStringValue("export");
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
