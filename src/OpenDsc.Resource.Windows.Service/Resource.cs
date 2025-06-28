// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.ServiceProcess;
using System.Text.Json.Serialization;

namespace OpenDsc.Resource.Windows.Service;

public sealed class Resource : AotDscResource<Schema>, IGettable<Schema>, IExportable<Schema>
{
    public Resource(JsonSerializerContext context) : base("OpenDsc.Windows/Service", context)
    {
        Description = "Manage Windows services.";
        Tags = ["Windows"];
        ExitCodes.Add(10, new() { Exception = typeof(Win32Exception), Description = "Failed to get services" });
    }

    public Schema Get(Schema instance)
    {
        foreach (var service in ServiceController.GetServices())
        {
            if (string.Equals(service.ServiceName, instance.Name, StringComparison.OrdinalIgnoreCase))
            {
                return new Schema()
                {
                    Name = service.ServiceName,
                    DisplayName = service.DisplayName,
                    Status = service.Status,
                    StartType = service.StartType
                };
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
