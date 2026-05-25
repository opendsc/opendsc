// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands;

[Cmdlet("Get", "DscServerCompositeConfigurationAvailableMajorVersions")]
[OutputType(typeof(IReadOnlyList<int>))]
public sealed class GetDscServerCompositeConfigurationAvailableMajorVersionsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ConfigurationId { get; set; } = default;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        var result = service.GetAvailableMajorVersionsAsync(ConfigurationId, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
