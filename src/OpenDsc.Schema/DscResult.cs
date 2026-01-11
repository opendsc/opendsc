// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OpenDsc.Schema;

/// <summary>
/// Result from a DSC configuration operation.
/// </summary>
public sealed class DscResult
{
    /// <summary>
    /// Metadata from the DSC operation.
    /// </summary>
    public DscMetadata? Metadata { get; set; }

    /// <summary>
    /// List of resource results.
    /// </summary>
    public List<DscResourceResult>? Results { get; set; }

    /// <summary>
    /// List of messages from DSC.
    /// </summary>
    public List<DscMessage>? Messages { get; set; }

    /// <summary>
    /// Whether the operation had errors.
    /// </summary>
    [JsonRequired]
    public bool HadErrors { get; set; }
}

/// <summary>
/// Result for an individual DSC resource.
/// </summary>
public sealed partial class DscResourceResult
{
    /// <summary>
    /// The resource type.
    /// </summary>
    [JsonRequired]
    public string Type
    {
        get => _type;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Type cannot be null or empty.");
            }

            var match = GetTypeRegex().Match(value);
            if (!match.Success)
            {
                throw new ArgumentException("Type does not match format: <owner>[.<group>][.<area>]/<name>");
            }

            _type = value;
        }
    }

    private string _type = string.Empty;

    /// <summary>
    /// The resource name.
    /// </summary>
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The detailed result of the resource operation.
    /// Use JsonElement and deserialize to the appropriate operation result type
    /// (DscGetOperationResult, DscTestOperationResult, or DscSetOperationResult)
    /// based on the operation being performed.
    ///
    /// </summary>
    [JsonRequired]
    public JsonElement Result { get; set; }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"^\w+(\.\w+){0,2}\/\w+$")]
    private static partial Regex TypeRegex();
#else
    private static readonly Regex TypeRegex = new Regex(@"^\w+(\.\w+){0,2}\/\w+$");
#endif

    private static Regex GetTypeRegex() =>
#if NET7_0_OR_GREATER
        TypeRegex();
#else
        TypeRegex;
#endif
}

/// <summary>
/// Restart requirement information.
/// </summary>
public sealed class DscRestartRequirement
{
    /// <summary>
    /// System restart requirement. Contains the system/computer name.
    /// </summary>
    public string? System { get; set; }

    /// <summary>
    /// Service restart requirement. Contains the service name.
    /// </summary>
    public string? Service { get; set; }

    /// <summary>
    /// Process restart requirement. Contains process name and ID.
    /// </summary>
    public DscProcessRestartInfo? Process { get; set; }
}
