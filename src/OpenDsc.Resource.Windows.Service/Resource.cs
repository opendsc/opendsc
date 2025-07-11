// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.ServiceProcess;
using System.Text.Json.Serialization;
using System.Management;

namespace OpenDsc.Resource.Windows.Service;

public sealed class Resource : AotDscResource<Schema>, IGettable<Schema>, IExportable<Schema>, IDeletable<Schema>
{
    public Resource(JsonSerializerContext context) : base("OpenDsc.Windows/Service", context)
    {
        Description = "Manage Windows services.";
        Tags = ["Windows"];
        ExitCodes.Add(5, new() { Exception = typeof(Win32Exception), Description = "Failed to delete service." });
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
