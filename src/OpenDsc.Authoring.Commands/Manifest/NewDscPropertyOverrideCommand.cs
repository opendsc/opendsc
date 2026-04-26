// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Specialized;
using System.Management.Automation;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Creates a DscPropertyOverride object for use with Update-DscAdaptedResourceManifest.
/// </summary>
[Cmdlet(VerbsCommon.New, "DscPropertyOverride")]
[OutputType(typeof(DscPropertyOverride))]
public sealed class NewDscPropertyOverrideCommand : PSCmdlet
{
    /// <summary>
    /// The name of the property in the embedded JSON schema to override.
    /// </summary>
    [Parameter(Mandatory = true)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Override the property description text.
    /// </summary>
    [Parameter()]
    public string? Description { get; set; }

    /// <summary>
    /// Override the property title text.
    /// </summary>
    [Parameter()]
    public string? Title { get; set; }

    /// <summary>
    /// A hashtable of JSON schema keywords to merge into the property definition.
    /// </summary>
    [Parameter()]
    public OrderedDictionary? JsonSchema { get; set; }

    /// <summary>
    /// An array of JSON schema key names to remove from the property before merging.
    /// </summary>
    [Parameter()]
    public string[]? RemoveKeys { get; set; }

    /// <summary>
    /// Set to true to add the property to the required list, false to remove it.
    /// </summary>
    [Parameter()]
    public bool? Required { get; set; }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        var propertyOverride = new DscPropertyOverride { Name = Name };

        if (MyInvocation.BoundParameters.ContainsKey(nameof(Description)))
        {
            propertyOverride.Description = Description;
        }

        if (MyInvocation.BoundParameters.ContainsKey(nameof(Title)))
        {
            propertyOverride.Title = Title;
        }

        if (MyInvocation.BoundParameters.ContainsKey(nameof(JsonSchema)))
        {
            propertyOverride.JsonSchema = JsonSchema;
        }

        if (MyInvocation.BoundParameters.ContainsKey(nameof(RemoveKeys)))
        {
            propertyOverride.RemoveKeys = RemoveKeys;
        }

        if (MyInvocation.BoundParameters.ContainsKey(nameof(Required)))
        {
            propertyOverride.Required = Required;
        }

        WriteObject(propertyOverride);
    }
}
