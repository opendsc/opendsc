// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.ServiceProcess;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Management;

namespace OpenDsc.Resource.Windows.Service;

[DscResource("OpenDsc.Windows/Service", Description = "Manage Windows services.", Tags = ["windows", "service"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Description = "Invalid parameter")]
[ExitCode(2, Exception = typeof(Exception), Description = "Generic error")]
[ExitCode(3, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(4, Exception = typeof(Win32Exception), Description = "Failed to get services")]
public sealed class Resource(JsonSerializerContext context) : AotDscResource<Schema>(context), IGettable<Schema>, IDeletable<Schema>, IExportable<Schema>
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

    public void Delete(Schema instance)
    {
        using (var service = new ServiceController(instance.Name))
        {
            if (service.Status != ServiceControllerStatus.Stopped)
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped);
            }
        }

        using var serviceObject = new ManagementObject($"Win32_Service.Name='{instance.Name}'");
        var result = (uint)serviceObject.InvokeMethod("Delete", null, null)["ReturnValue"];
        if (result != 0)
        {
            Logger.WriteError($"Failed to delete service '{instance.Name}'. Return code: {result}");
            Environment.Exit(5);
        }

        Logger.WriteTrace($"Service '{instance.Name}' deleted successfully.");
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
