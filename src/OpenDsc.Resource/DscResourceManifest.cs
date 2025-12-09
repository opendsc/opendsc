// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDsc.Resource;

/// <summary>
/// Represents a DSC v3 resource manifest containing metadata and command definitions.
/// </summary>
public class DscResourceManifest
{
    /// <summary>
    /// Gets or sets the URI of the manifest schema.
    /// </summary>
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = "https://aka.ms/dsc/schemas/v3/bundled/resource/manifest.json";

    /// <summary>
    /// Gets or sets the resource type name.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the semantic version of the resource.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the tags associated with the resource.
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// Gets or sets the mapping of exit codes to their descriptions.
    /// </summary>
    public Dictionary<string, string>? ExitCodes { get; set; }

    /// <summary>
    /// Gets or sets the embedded JSON schema for the resource.
    /// </summary>
    [JsonPropertyName("schema")]
    public ManifestSchema? EmbeddedSchema { get; set; }

    /// <summary>
    /// Gets or sets the Get operation definition.
    /// </summary>
    public ManifestMethod? Get { get; set; }

    /// <summary>
    /// Gets or sets the Set operation definition.
    /// </summary>
    public ManifestSetMethod? Set { get; set; }

    /// <summary>
    /// Gets or sets the Test operation definition.
    /// </summary>
    public ManifestTestMethod? Test { get; set; }

    /// <summary>
    /// Gets or sets the Delete operation definition.
    /// </summary>
    public ManifestMethod? Delete { get; set; }

    /// <summary>
    /// Gets or sets the Export operation definition.
    /// </summary>
    public ManifestExportMethod? Export { get; set; }
}

/// <summary>
/// Represents an embedded JSON schema in a resource manifest.
/// </summary>
public class ManifestSchema
{
    /// <summary>
    /// Gets or sets the embedded JSON schema element.
    /// </summary>
    public JsonElement Embedded { get; set; }
}

/// <summary>
/// Represents a method definition in a resource manifest.
/// </summary>
public class ManifestMethod
{
    /// <summary>
    /// Gets or sets the path to the executable that implements this method.
    /// </summary>
    public string Executable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command-line arguments for the method.
    /// </summary>
    public object[]? Args { get; set; }
}

/// <summary>
/// Represents a Set method definition with return type specification.
/// </summary>
public class ManifestSetMethod : ManifestMethod
{
    /// <summary>
    /// Gets or sets what information the Set operation returns ("state" or "stateAndDiff").
    /// </summary>
    public string? Return { get; set; }
}

/// <summary>
/// Represents a Test method definition with return type specification.
/// </summary>
public class ManifestTestMethod : ManifestMethod
{
    /// <summary>
    /// Gets or sets what information the Test operation returns ("state" or "stateAndDiff").
    /// </summary>
    public string? Return { get; set; }
}

/// <summary>
/// Represents an Export method definition in a resource manifest.
/// </summary>
public class ManifestExportMethod
{
    /// <summary>
    /// Gets or sets the path to the executable that implements this method.
    /// </summary>
    public string Executable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command-line arguments for the method.
    /// </summary>
    public string[]? Args { get; set; }
}

/// <summary>
/// Represents a JSON input argument specification in a manifest method.
/// </summary>
public class JsonInputArg
{
    /// <summary>
    /// Gets or sets the command-line argument name that accepts JSON input.
    /// </summary>
    [JsonPropertyName("jsonInputArg")]
    public string Arg { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this argument is mandatory.
    /// </summary>
    public bool? Mandatory { get; set; }
}
