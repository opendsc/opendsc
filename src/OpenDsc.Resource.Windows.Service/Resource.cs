// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.ServiceProcess;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDsc.Resource.Windows.Service;

[DscResource("OpenDsc.Windows/Service", Description = "Manage Windows services.", Tags = ["windows", "service"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Description = "Invalid parameter")]
[ExitCode(2, Exception = typeof(Exception), Description = "Generic error")]
[ExitCode(3, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(4, Exception = typeof(Win32Exception), Description = "Failed to get services")]
public sealed class Resource(JsonSerializerContext context) : AotDscResource<Schema>(context), IGettable<Schema>, IExportable<Schema>
{
    public Schema Get(Schema instance)
    {
        foreach (var service in Export())
        {
            if (string.Equals(service.Name, instance.Name, StringComparison.OrdinalIgnoreCase))
            {
                return service;
            }
        }

        return new Schema()
        {
            Name = instance.Name,
            Exist = false
        };
    }

    public IEnumerable<Schema> Export()
    {
        foreach (var service in ServiceController.GetServices())
        {
            yield return new Schema
            {
                Name = service.ServiceName,
                DisplayName = service.DisplayName,
                Status = service.Status,
                StartType = service.StartType
            };
        }
    }
}