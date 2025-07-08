// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Diagnostics;
using System.Text.RegularExpressions;

using NuGet.Versioning;

namespace OpenDsc.Resource;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DscResourceAttribute : Attribute
{
    public string Type { get; }
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

    public string ManifestSchema { get; set; } = "https://aka.ms/dsc/schemas/v3/bundled/resource/manifest.json";
    public string Description { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];

    private SemanticVersion? _semanticVersion;

    public DscResourceAttribute(string type)
    {
        var match = Regex.Match(type, @"^\w+(\.\w+){0,2}\/\w+$");

        if (!match.Success)
        {
            throw new ArgumentException("Type does not match format: <owner>[.<group>][.<area>]/<name>");
        }

        Type = type;
    }

    public DscResourceAttribute(string type, string version) : this(type)
    {
        if (!SemanticVersion.TryParse(version, out var parsedVersion))
        {
            throw new ArgumentException("Invalid version parameter");
        }

        Version = parsedVersion;
    }
}
