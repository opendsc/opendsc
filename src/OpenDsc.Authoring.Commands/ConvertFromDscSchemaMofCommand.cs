// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Text;
using System.Text.Json;

using OpenDsc.Mof;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Converts a MOF schema class definition to a JSON Schema string.
/// </summary>
[Cmdlet(VerbsData.ConvertFrom, "DscSchemaMof")]
[OutputType(typeof(string))]
public sealed class ConvertFromDscSchemaMofCommand : PSCmdlet
{
    /// <summary>
    /// The MOF schema text to convert. Pipe the output of <c>Get-Content</c> on a <c>.schema.mof</c> file.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    [AllowEmptyString]
    public string InputObject { get; set; } = string.Empty;

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
        var schema = MofSchemaConverter.ConvertText(_buffer.ToString());
        var json = schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        WriteObject(json);
    }
}
