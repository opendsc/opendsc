// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Diagnostics;
using System.Reflection;
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
        var dscAttr = value.GetType().GetCustomAttribute<DscResourceAttribute>()
                      ?? throw new InvalidOperationException($"Resource does not have '{nameof(DscResourceAttribute)}' attribute.");

        var fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName)
            ?? throw new InvalidOperationException($"Unable to get current process file name.");

        writer.WriteStartObject();
        writer.WriteString("$schema", dscAttr.ManifestSchema);
        writer.WriteString("type", dscAttr.Type);
        writer.WriteString("description", dscAttr.Description);
        writer.WriteString("version", dscAttr.Version.ToString());
        writer.WritePropertyName("tags");
        writer.WriteStartArray();
        foreach (var tag in dscAttr.Tags)
        {
            writer.WriteStringValue(tag);
        }
        writer.WriteEndArray();

        WriteExitCodes(writer, value);

        writer.WritePropertyName("schema");
        writer.WriteStartObject();
        writer.WritePropertyName("command");
        writer.WriteStartObject();
        writer.WriteString("executable", fileName);
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
            writer.WriteString("executable", fileName);
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
            writer.WriteString("executable", fileName);
            writer.WritePropertyName("args");
            writer.WriteStartArray();
            writer.WriteStringValue("config");
            writer.WriteStringValue("set");
            writer.WriteStartObject();
            writer.WriteString("jsonInputArg", "--input");
            writer.WriteBoolean("mandatory", true);
            writer.WriteEndObject();
            writer.WriteEndArray();

            var setReturn = GetSetReturn(value);
            if (setReturn != "none")
            {
                writer.WriteString("return", setReturn);
            }

            writer.WriteEndObject();
        }

        if (value is ITestable<T>)
        {
            writer.WritePropertyName("test");
            writer.WriteStartObject();
            writer.WriteString("executable", fileName);
            writer.WritePropertyName("args");
            writer.WriteStartArray();
            writer.WriteStringValue("config");
            writer.WriteStringValue("test");
            writer.WriteStartObject();
            writer.WriteString("jsonInputArg", "--input");
            writer.WriteBoolean("mandatory", true);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteString("return", GetTestReturn(value));
            writer.WriteEndObject();
        }

        if (value is IDeletable<T>)
        {
            writer.WritePropertyName("delete");
            writer.WriteStartObject();
            writer.WriteString("executable", fileName);
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
            writer.WriteString("executable", fileName);
            writer.WritePropertyName("args");
            writer.WriteStartArray();
            writer.WriteStringValue("config");
            writer.WriteStringValue("export");
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteExitCodes(Utf8JsonWriter writer, IDscResource<T> value)
    {
        var attrs = value.GetType().GetCustomAttributes<ExitCodeAttribute>();

        if (attrs is null)
        {
            return;
        }

        writer.WritePropertyName("exitCodes");
        writer.WriteStartObject();

        foreach (var attr in attrs)
        {
            writer.WriteString(attr.ExitCode.ToString(), attr.Description);
        }

        writer.WriteEndObject();
    }

    private static string GetSetReturn(IDscResource<T> value)
    {
        var attr = value.GetType().GetCustomAttribute<SetReturnAttribute>();

        if (attr is null)
        {
            return "none";
        }

        if (attr.SetReturn == SetReturn.None)
        {
            return "none";
        }
        else if (attr.SetReturn == SetReturn.State)
        {
            return "state";
        }
        else if (attr.SetReturn == SetReturn.StateAndDiff)
        {
            return "stateAndDiff";
        }

        throw new InvalidOperationException($"Invalid '{nameof(SetReturn)}' enum value.");
    }

    private static string GetTestReturn(IDscResource<T> value)
    {
        var attr = value.GetType().GetCustomAttribute<TestReturnAttribute>();

        if (attr is null)
        {
            return "state";
        }

        if (attr.TestReturn == TestReturn.State)
        {
            return "state";
        }
        else if (attr.TestReturn == TestReturn.StateAndDiff)
        {
            return "stateAndDiff";
        }

        throw new InvalidOperationException($"Invalid '{nameof(TestReturn)}' enum value.");
    }
}
