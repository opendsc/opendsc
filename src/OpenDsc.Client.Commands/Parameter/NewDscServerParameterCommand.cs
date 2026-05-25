// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Parameters;

namespace OpenDsc.Client.Commands.Parameter;

[Cmdlet("New", "DscServerParameter")]
[OutputType(typeof(ParameterVersionDetails))]
public sealed class NewDscServerParameterCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ScopeTypeId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Guid ConfigurationId { get; set; }

    [Parameter(Mandatory = false, Position = 2)]
    public string? ScopeValue { get; set; }

    [Parameter(Mandatory = true, Position = 3)]
    [ValidateNotNullOrEmpty]
    public string Version { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 4)]
    public string? Content { get; set; }

    [Parameter(Mandatory = false, Position = 5)]
    public string? ContentType { get; set; }

    [Parameter(Mandatory = false, Position = 6)]
    public bool? IsPassthrough { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ParameterHttpService>();
        var request = new CreateParameterRequest
        {
            ScopeValue = ScopeValue,
            Version = Version,
            Content = Content,
            ContentType = ContentType,
            IsPassthrough = IsPassthrough,
        };

        var result = service.CreateAsync(ScopeTypeId, ConfigurationId, request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
