// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Text.Json;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Commands;

[Cmdlet(VerbsCommon.New, "DscResourceManifest")]
public class NewDscResourceManifest : PSCmdlet
{
    [Parameter(Mandatory = true)]
    public Type Schema { get; set; } = null!;

    [Parameter()]
    public string Path { get; set; } = string.Empty;

    protected override void EndProcessing()
    {
        var serialized = GetSchema(Schema);
        WriteObject(serialized);
    }

    private static string GetSchema(Type type)
    {
        var builder = new JsonSchemaBuilder();
        var schema = builder.FromType(type).Build();
        return JsonSerializer.Serialize(schema);
    }
}
