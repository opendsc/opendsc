// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using NuGet.Versioning;

namespace OpenDsc.Resource;

public abstract class DscResourceBase<T> : IDscResource<T>
{
    [JsonPropertyName("$schema")]
    public string ManifestSchema => "https://aka.ms/dsc/schemas/v3/bundled/resource/manifest.json";

    public string Type
    {
        get
        {
            return _type;
        }

        set
        {
            var match = Regex.Match(value, @"^\w+(\.\w+){0,2}\/\w+$");

            if (!match.Success)
            {
                throw new ArgumentException("Value does not match format: <owner>[.<group>][.<area>]/<name>");
            }

            _type = value;
        }
    }

    public string Description { get; set; } = string.Empty;

    public SemanticVersion Version
    {
        get
        {
            if (_semanticVersion is null)
            {
                var version = Process.GetCurrentProcess().MainModule?.FileVersionInfo?.ProductVersion
                    ?? throw new InvalidOperationException();
                _semanticVersion = SemanticVersion.Parse(version);
            }

            return _semanticVersion;
        }

        set
        {
            _semanticVersion = value;
        }
    }

    public string FileName
    {
        get
        {
            if (_fileName is null)
            {
                _fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName)
                    ?? throw new InvalidOperationException();
            }

            return _fileName;
        }

        set
        {
            _fileName = value;
        }
    }

    protected JsonSchemaExporterOptions ExporterOptions
    {
        get
        {
            _exporterOptions ??= new JsonSchemaExporterOptions()
            {
                TreatNullObliviousAsNonNullable = true
            };

            return _exporterOptions;
        }

        set
        {
            _exporterOptions = value;
        }
    }

    private JsonSchemaExporterOptions? _exporterOptions;

    public IEnumerable<string> Tags { get; set; } = [];
    public IDictionary<int, ResourceExitCode> ExitCodes { get; set; } = new Dictionary<int, ResourceExitCode>();

    private SemanticVersion? _semanticVersion;
    private string _type = string.Empty;
    private string? _fileName;

    protected DscResourceBase(string type)
    {
        Type = type;
        ExitCodes.Add(0, new() { Description = "Success" });
        ExitCodes.Add(1, new() { Description = "Invalid parameter" });
        ExitCodes.Add(2, new() { Exception = typeof(Exception), Description = "Generic Error" });
        ExitCodes.Add(3, new() { Exception = typeof(JsonException), Description = "Invalid JSON" });
    }

    public abstract string GetSchema();

    public abstract T Parse(string json);

    public abstract string ToJson(T item);
}
