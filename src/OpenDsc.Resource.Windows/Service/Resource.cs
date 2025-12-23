// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.ServiceProcess;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.Service;

[DscResource("OpenDsc.Windows/Service", "0.1.0", Description = "Manage Windows services", Tags = ["windows", "service"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(Win32Exception), Description = "Windows API error")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument or missing required parameter")]
[ExitCode(5, Exception = typeof(InvalidOperationException), Description = "Invalid operation or service state")]
[ExitCode(6, Exception = typeof(System.ServiceProcess.TimeoutException), Description = "Service operation timed out")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>, IExportable<Schema>
{
    private const int ServiceOperationTimeoutSeconds = 30;

    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        return JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema instance)
    {
        try
        {
            using var service = new ServiceController(instance.Name);
            service.Refresh();

            var dependencies = service.ServicesDependedOn.Select(s => s.ServiceName).ToArray();

            return new Schema()
            {
                Name = service.ServiceName,
                DisplayName = service.DisplayName,
                Description = ServiceHelper.GetServiceDescription(service.ServiceName),
                Path = ServiceHelper.GetServicePath(service.ServiceName),
                Dependencies = dependencies.Length > 0 ? dependencies : null,
                Status = service.Status,
                StartType = service.StartType
            };
        }
        catch (InvalidOperationException)
        {
            return new Schema()
            {
                Name = instance.Name,
                Exist = false
            };
        }
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        if (instance.Status is not null &&
            instance.Status != ServiceControllerStatus.Stopped &&
            instance.Status != ServiceControllerStatus.Running &&
            instance.Status != ServiceControllerStatus.Paused)
        {
            throw new ArgumentException($"Invalid service status '{instance.Status}'. Only Stopped, Running, and Paused are supported.");

        }

        if (instance.StartType is not null &&
            instance.StartType != ServiceStartMode.Automatic &&
            instance.StartType != ServiceStartMode.Manual &&
            instance.StartType != ServiceStartMode.Disabled)
        {
            throw new ArgumentException($"Invalid service start type '{instance.StartType}'. Only Automatic, Manual, and Disabled are supported.");
        }

        if (Get(instance).Exist == false)
        {
            CreateService(instance);
        }

        using var service = new ServiceController(instance.Name);
        service.Refresh();

        if (instance.DisplayName is not null && !string.Equals(service.DisplayName, instance.DisplayName, StringComparison.Ordinal))
        {
            ServiceHelper.SetServiceDisplayName(service.ServiceName, instance.DisplayName);
        }

        service.Refresh();

        if (instance.Description is not null)
        {
            var currentDesc = ServiceHelper.GetServiceDescription(service.ServiceName);
            if (!string.Equals(currentDesc, instance.Description, StringComparison.Ordinal))
            {
                ServiceHelper.SetServiceDescription(service.ServiceName, instance.Description);
            }
        }

        service.Refresh();

        if (instance.StartType is not null && service.StartType != instance.StartType.Value)
        {
            ServiceHelper.SetServiceStartMode(service.ServiceName, instance.StartType.Value);
        }

        service.Refresh();

        if (instance.Dependencies is not null)
        {
            var currentDependencies = service.ServicesDependedOn.Select(s => s.ServiceName).ToArray();
            var desiredDependencies = instance.Dependencies;

            bool dependenciesChanged = currentDependencies.Length != desiredDependencies.Length ||
                                      !currentDependencies.OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                                          .SequenceEqual(desiredDependencies.OrderBy(d => d, StringComparer.OrdinalIgnoreCase));

            if (dependenciesChanged)
            {
                ServiceHelper.SetServiceDependencies(service.ServiceName, desiredDependencies.Length > 0 ? desiredDependencies : null);
            }
        }

        service.Refresh();

        if (instance.Status is not null && service.Status != instance.Status.Value)
        {
            SetServiceStatus(service, instance.Status.Value);
        }

        return null;
    }

    public void Delete(Schema instance)
    {
        using var service = new ServiceController(instance.Name);
        service.Refresh();

        if (service.Status != ServiceControllerStatus.Stopped)
        {
            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(ServiceOperationTimeoutSeconds));
        }

        ServiceHelper.DeleteWindowsService(service.ServiceName);
    }

    public IEnumerable<Schema> Export()
    {
        foreach (var service in ServiceController.GetServices())
        {
            var dependencies = service.ServicesDependedOn.Select(s => s.ServiceName).ToArray();

            yield return new Schema
            {
                Name = service.ServiceName,
                DisplayName = service.DisplayName,
                Description = ServiceHelper.GetServiceDescription(service.ServiceName),
                Path = ServiceHelper.GetServicePath(service.ServiceName),
                Dependencies = dependencies.Length > 0 ? dependencies : null,
                Status = service.Status,
                StartType = service.StartType
            };
        }
    }

    private static void CreateService(Schema instance)
    {
        if (string.IsNullOrEmpty(instance.Path))
        {
            throw new ArgumentException("Path is required to create a new service");
        }

        if (instance.StartType is null)
        {
            throw new ArgumentException("StartType is required to create a new service");
        }

        var dependencies = instance.Dependencies != null && instance.Dependencies.Length > 0
            ? string.Join("\0", instance.Dependencies) + "\0"
            : null;

        ServiceHelper.CreateWindowsService(
            instance.Name,
            instance.Path,
            instance.DisplayName,
            instance.StartType.Value,
            dependencies);
    }

    private static void SetServiceStatus(ServiceController service, ServiceControllerStatus targetStatus)
    {
        if (targetStatus == ServiceControllerStatus.Running && service.Status != ServiceControllerStatus.Running)
        {
            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(ServiceOperationTimeoutSeconds));
        }
        else if (targetStatus == ServiceControllerStatus.Stopped && service.Status != ServiceControllerStatus.Stopped)
        {
            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(ServiceOperationTimeoutSeconds));
        }
        else if (targetStatus == ServiceControllerStatus.Paused && service.Status != ServiceControllerStatus.Paused)
        {
            service.Pause();
            service.WaitForStatus(ServiceControllerStatus.Paused, TimeSpan.FromSeconds(ServiceOperationTimeoutSeconds));
        }
    }
}
