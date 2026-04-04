// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.Management.Automation;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Applies post-processing overrides to adapted resource manifest objects.
/// </summary>
[Cmdlet(VerbsData.Update, "DscAdaptedResourceManifest")]
[OutputType(typeof(DscAdaptedResourceManifest))]
public sealed class UpdateDscAdaptedResourceManifestCommand : PSCmdlet
{
    /// <summary>
    /// A DscAdaptedResourceManifest object to update.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public DscAdaptedResourceManifest InputObject { get; set; } = null!;

    /// <summary>
    /// One or more DscPropertyOverride objects specifying modifications.
    /// </summary>
    [Parameter()]
    public DscPropertyOverride[]? PropertyOverride { get; set; }

    /// <summary>
    /// Override the resource-level description.
    /// </summary>
    [Parameter()]
    public string? Description { get; set; }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        var schema = InputObject.ManifestSchema.Embedded;

        if (!string.IsNullOrEmpty(Description))
        {
            InputObject.Description = Description;
            schema["description"] = Description;
        }

        if (PropertyOverride is not null)
        {
            var properties = schema["properties"] as OrderedDictionary;
            if (properties is null)
            {
                WriteObject(InputObject);
                return;
            }

            var requiredList = new List<string>();
            if (schema.Contains("required") && schema["required"] is object[] currentRequired)
            {
                foreach (var item in currentRequired)
                {
                    if (item is string s)
                    {
                        requiredList.Add(s);
                    }
                }
            }
            else if (schema.Contains("required") && schema["required"] is string[] currentRequiredStrings)
            {
                requiredList.AddRange(currentRequiredStrings);
            }

            var requiredChanged = false;

            foreach (var pOverride in PropertyOverride)
            {
                if (!properties.Contains(pOverride.Name))
                {
                    WriteWarning($"Property '{pOverride.Name}' not found in schema for '{InputObject.Type}'. Skipping.");
                    continue;
                }

                var prop = properties[pOverride.Name] as OrderedDictionary;
                if (prop is null)
                {
                    continue;
                }

                if (pOverride.RemoveKeys is not null)
                {
                    foreach (var key in pOverride.RemoveKeys)
                    {
                        if (prop.Contains(key))
                        {
                            prop.Remove(key);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(pOverride.Description))
                {
                    prop["description"] = pOverride.Description;
                }

                if (!string.IsNullOrEmpty(pOverride.Title))
                {
                    prop["title"] = pOverride.Title;
                }

                if (pOverride.JsonSchema is not null && pOverride.JsonSchema.Count > 0)
                {
                    foreach (DictionaryEntry entry in pOverride.JsonSchema)
                    {
                        prop[entry.Key] = entry.Value;
                    }
                }

                if (pOverride.Required.HasValue)
                {
                    if (pOverride.Required.Value && !requiredList.Contains(pOverride.Name))
                    {
                        requiredList.Add(pOverride.Name);
                        requiredChanged = true;
                    }
                    else if (!pOverride.Required.Value && requiredList.Remove(pOverride.Name))
                    {
                        requiredChanged = true;
                    }
                }
            }

            if (requiredChanged)
            {
                schema["required"] = requiredList.ToArray();
            }
        }

        WriteObject(InputObject);
    }
}
