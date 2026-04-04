// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Specialized;
using System.Management.Automation;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Creates a DSC resource manifests list for bundling multiple resources in a single file.
/// </summary>
[Cmdlet(VerbsCommon.New, "DscResourceManifest")]
[OutputType(typeof(DscResourceManifestList))]
public sealed class NewDscResourceManifestCommand : PSCmdlet
{
    /// <summary>
    /// One or more DscAdaptedResourceManifest objects to include in the manifests list.
    /// </summary>
    [Parameter(ValueFromPipeline = true)]
    public DscAdaptedResourceManifest[]? AdaptedResource { get; set; }

    /// <summary>
    /// One or more hashtables representing command-based DSC resource manifests.
    /// </summary>
    [Parameter()]
    public OrderedDictionary[]? Resource { get; set; }

    private DscResourceManifestList _manifestList = null!;

    /// <inheritdoc/>
    protected override void BeginProcessing()
    {
        _manifestList = new DscResourceManifestList();

        if (Resource is not null)
        {
            foreach (var res in Resource)
            {
                _manifestList.AddResource(res);
            }
        }
    }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        if (AdaptedResource is null)
        {
            return;
        }

        foreach (var adapted in AdaptedResource)
        {
            _manifestList.AddAdaptedResource(adapted);
        }
    }

    /// <inheritdoc/>
    protected override void EndProcessing()
    {
        WriteObject(_manifestList);
    }
}
