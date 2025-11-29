// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDsc.Resource;

public class DscResourceManifest
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = "https://aka.ms/dsc/schemas/v3/bundled/resource/manifest.json";

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Tags { get; set; }

    [JsonPropertyName("exitCodes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? ExitCodes { get; set; }

    [JsonPropertyName("schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ManifestSchema? EmbeddedSchema { get; set; }

    [JsonPropertyName("get")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ManifestMethod? Get { get; set; }

    [JsonPropertyName("set")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ManifestSetMethod? Set { get; set; }

    [JsonPropertyName("test")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ManifestTestMethod? Test { get; set; }

    [JsonPropertyName("delete")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ManifestMethod? Delete { get; set; }

    [JsonPropertyName("export")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ManifestExportMethod? Export { get; set; }
}

public class ManifestSchema
{
    [JsonPropertyName("embedded")]
    public JsonElement Embedded { get; set; }
}

public class ManifestMethod
{
    [JsonPropertyName("executable")]
    public string Executable { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object[]? Args { get; set; }
}

public class ManifestSetMethod : ManifestMethod
{
    [JsonPropertyName("return")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Return { get; set; }
}

public class ManifestTestMethod : ManifestMethod
{
    [JsonPropertyName("return")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Return { get; set; }
}

public class ManifestExportMethod
{
    [JsonPropertyName("executable")]
    public string Executable { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Args { get; set; }
}

public class JsonInputArg
{
    [JsonPropertyName("jsonInputArg")]
    public string Arg { get; set; } = string.Empty;

    [JsonPropertyName("mandatory")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Mandatory { get; set; }
}
