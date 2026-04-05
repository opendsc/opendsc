// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Text.Json;

using Json.Schema;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Converts a PowerShell DSC class-based resource to a JSON Schema string.
/// </summary>
[Cmdlet(VerbsData.ConvertFrom, "DscSchemaPSClass", DefaultParameterSetName = ParameterSetByInfo)]
[OutputType(typeof(string))]
public sealed class ConvertFromDscSchemaPSClassCommand : PSCmdlet
{
    private const string ParameterSetByInfo = "ByInfo";
    private const string ParameterSetByType = "ByType";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    /// <summary>
    /// The DSC resource info to convert. Pipe the output of <c>Get-DscResource</c>.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSetByInfo)]
    [ValidateNotNull]
    public PSObject InputObject { get; set; } = null!;

    /// <summary>
    /// The DSC resource class <see cref="Type"/> to convert directly.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = ParameterSetByType)]
    [ValidateNotNull]
    public Type ResourceType { get; set; } = null!;

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        if (ParameterSetName == ParameterSetByType)
        {
            ConvertByType();
        }
        else
        {
            ConvertByInfo();
        }
    }

    private void ConvertByType()
    {
        JsonSchema schema = PsClassSchemaConverter.Convert(ResourceType.Name, ResourceType);
        var json = JsonSerializer.Serialize(schema, JsonSerializerOptions);
        WriteObject(json);
    }

    private void ConvertByInfo()
    {
        var resourceName = InputObject.Properties["Name"]?.Value?.ToString();
        var resourceType = InputObject.Properties["ResourceType"]?.Value?.ToString();
        var implementationDetail = InputObject.Properties["ImplementationDetail"]?.Value?.ToString();

        if (resourceName is null || resourceType is null)
        {
            WriteError(new ErrorRecord(
                new InvalidOperationException(
                    "InputObject does not have Name and ResourceType properties. Pipe the output of Get-DscResource."),
                "InvalidInputObject",
                ErrorCategory.InvalidArgument,
                InputObject));
            return;
        }

        if (!string.Equals(implementationDetail, "ClassBased", StringComparison.OrdinalIgnoreCase))
        {
            WriteError(new ErrorRecord(
                new InvalidOperationException(
                    $"Resource '{resourceName}' is not a class-based DSC resource " +
                    $"(ImplementationDetail = {implementationDetail ?? "unknown"}). Only class-based resources are supported."),
                "NotPowerShellClassResource",
                ErrorCategory.InvalidArgument,
                InputObject));
            return;
        }

        Type? type;
        try
        {
            var result = InvokeCommand.InvokeScript($"[{resourceType}]");
            type = result.Count > 0 ? result[0].BaseObject as Type : null;
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(
                new InvalidOperationException(
                    $"Could not load type '{resourceType}': {ex.Message}. " +
                    "Ensure the resource module is imported in the current session.",
                    ex),
                "TypeResolutionFailed",
                ErrorCategory.InvalidOperation,
                InputObject));
            return;
        }

        if (type is null)
        {
            WriteError(new ErrorRecord(
                new InvalidOperationException(
                    $"Type '{resourceType}' could not be resolved. " +
                    "Ensure the resource module is imported with Import-Module before converting."),
                "TypeNotFound",
                ErrorCategory.ObjectNotFound,
                InputObject));
            return;
        }

        JsonSchema schema = PsClassSchemaConverter.Convert(resourceName, type);
        var json = JsonSerializer.Serialize(schema, JsonSerializerOptions);
        WriteObject(json);
    }
}
