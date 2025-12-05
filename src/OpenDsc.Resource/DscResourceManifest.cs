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

    public string Type { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string[]? Tags { get; set; }

    public Dictionary<string, string>? ExitCodes { get; set; }

    [JsonPropertyName("schema")]
    public ManifestSchema? EmbeddedSchema { get; set; }

    public ManifestMethod? Get { get; set; }

    public ManifestSetMethod? Set { get; set; }

    public ManifestTestMethod? Test { get; set; }

    public ManifestMethod? Delete { get; set; }

    public ManifestExportMethod? Export { get; set; }
}

public class ManifestSchema
{
    public JsonElement Embedded { get; set; }
}

public class ManifestMethod
{
    public string Executable { get; set; } = string.Empty;

    public object[]? Args { get; set; }
}

public class ManifestSetMethod : ManifestMethod
{
    public string? Return { get; set; }
}

public class ManifestTestMethod : ManifestMethod
{
    public string? Return { get; set; }
}

public class ManifestExportMethod
{
    public string Executable { get; set; } = string.Empty;

    public string[]? Args { get; set; }
}

public class JsonInputArg
{
    [JsonPropertyName("jsonInputArg")]
    public string Arg { get; set; } = string.Empty;

    public bool? Mandatory { get; set; }
}
