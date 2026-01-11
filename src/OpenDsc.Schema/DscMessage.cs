// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OpenDsc.Schema;

/// <summary>
/// A message from a DSC resource operation.
/// </summary>
public sealed partial class DscMessage
{
    /// <summary>
    /// The name of the resource instance.
    /// </summary>
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of the resource (e.g., "Microsoft.Windows/Registry").
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
    /// The message content.
    /// </summary>
    [JsonRequired]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The message level (error, warning, information).
    /// </summary>
    [JsonRequired]
    public DscMessageLevel Level { get; set; }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"^\w+(\.\w+){0,2}\/\w+$")]
    private static partial Regex TypeRegex();
#else
    private static readonly Regex TypeRegex = new(@"^\w+(\.\w+){0,2}\/\w+$");
#endif

    private static Regex GetTypeRegex() =>
#if NET7_0_OR_GREATER
        TypeRegex();
#else
        TypeRegex;
#endif
}
