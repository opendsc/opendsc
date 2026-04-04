// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Text;
using System.Text.Json;

using OpenDsc.Mof;
using OpenDsc.Schema;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Converts DSC v1 compiled MOF text to a DSC v3 configuration document.
/// </summary>
[Cmdlet(VerbsData.ConvertFrom, "DscConfigurationMof")]
[OutputType(typeof(DscConfigDocument))]
[OutputType(typeof(string))]
public sealed class ConvertFromDscConfigurationMofCommand : PSCmdlet
{
    /// <summary>
    /// The MOF text to convert. Pipe the output of <c>Get-Content</c> on a compiled <c>.mof</c> file.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    [AllowEmptyString]
    public string InputObject { get; set; } = string.Empty;

    /// <summary>
    /// If set, output JSON text instead of a <see cref="DscConfigDocument"/> object.
    /// </summary>
    [Parameter()]
    public SwitchParameter AsJson { get; set; }

    private readonly StringBuilder _buffer = new();

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        if (string.IsNullOrWhiteSpace(InputObject))
        {
            return;
        }

        _buffer.AppendLine(InputObject);
    }

    /// <inheritdoc/>
    protected override void EndProcessing()
    {
        var document = MofConverter.ConvertText(_buffer.ToString());

        if (AsJson.IsPresent)
        {
            var json = JsonSerializer.Serialize(document, SourceGenerationContext.Default.DscConfigDocument);
            WriteObject(json);
        }
        else
        {
            WriteObject(document);
        }
    }
}
