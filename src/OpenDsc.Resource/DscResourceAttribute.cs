// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.RegularExpressions;

using NuGet.Versioning;

namespace OpenDsc.Resource;

/// <summary>
/// Specifies metadata for a DSC resource including its type name, version, description, and capabilities.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DscResourceAttribute : Attribute
{
    /// <summary>
    /// Gets the resource type name in the format "Owner[.Group][.Area]/Name".
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets or sets the semantic version of the resource.
    /// If not explicitly set, the version is retrieved from the assembly's informational version attribute.
    /// </summary>
    public SemanticVersion Version
    {
        get
        {
            if (_semanticVersion is null)
            {
                var version = Assembly.GetEntryAssembly()
                    ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? throw new InvalidOperationException("Unable to retrieve assembly informational version attribute.");
                _semanticVersion = SemanticVersion.Parse(version);
            }

            return _semanticVersion;
        }

        set
        {
            _semanticVersion = value;
        }
    }

    /// <summary>
    /// Gets or sets the URI of the manifest schema.
    /// </summary>
    public string ManifestSchema { get; set; } = "https://aka.ms/dsc/schemas/v3/bundled/resource/manifest.json";

    /// <summary>
    /// Gets or sets the resource description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tags associated with the resource for categorization and discovery.
    /// </summary>
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets what the Set operation returns (None, State, or StateAndDiff).
    /// </summary>
    public SetReturn SetReturn { get; set; } = SetReturn.None;

    /// <summary>
    /// Gets or sets what the Test operation returns (State or StateAndDiff).
    /// </summary>
    public TestReturn TestReturn { get; set; } = TestReturn.State;

    private SemanticVersion? _semanticVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="DscResourceAttribute"/> class.
    /// </summary>
    /// <param name="type">The resource type name in the format "Owner[.Group][.Area]/Name".</param>
    /// <exception cref="ArgumentException">Thrown when the type format is invalid.</exception>
    public DscResourceAttribute(string type)
    {
        var match = Regex.Match(type, @"^\w+(\.\w+){0,2}\/\w+$");

        if (!match.Success)
        {
            throw new ArgumentException("Type does not match format: <owner>[.<group>][.<area>]/<name>");
        }

        Type = type;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DscResourceAttribute"/> class with a specific version.
    /// </summary>
    /// <param name="type">The resource type name in the format "Owner[.Group][.Area]/Name".</param>
    /// <param name="version">The semantic version string for the resource.</param>
    /// <exception cref="ArgumentException">Thrown when the type format or version is invalid.</exception>
    public DscResourceAttribute(string type, string version) : this(type)
    {
        if (!SemanticVersion.TryParse(version, out var parsedVersion))
        {
            throw new ArgumentException("Invalid version parameter");
        }

        Version = parsedVersion;
    }
}
