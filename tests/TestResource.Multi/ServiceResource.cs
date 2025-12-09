// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using OpenDsc.Resource;

namespace TestResource.Multi;

public enum ServiceState
{
    Stopped,
    Running
}

public sealed class ServiceSchema
{
    public required string Name { get; set; }
    public ServiceState? State { get; set; }
    [JsonPropertyName("_exist")]
    public bool? Exist { get; set; }
}

[DscResource("TestResource.Multi/Service", "1.0.0", Description = "Manages service state", Tags = ["service", "state"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Description = "Invalid parameter")]
[ExitCode(2, Exception = typeof(Exception), Description = "Unhandled error")]
public sealed class ServiceResource(JsonSerializerContext context) : DscResource<ServiceSchema>(context),
    IGettable<ServiceSchema>, ITestable<ServiceSchema>
{
    private static readonly Dictionary<string, ServiceState> _services = new()
    {
        ["TestService1"] = ServiceState.Running,
        ["TestService2"] = ServiceState.Stopped
    };

    public ServiceSchema Get(ServiceSchema instance)
    {
        var exists = _services.ContainsKey(instance.Name);
        return new ServiceSchema
        {
            Name = instance.Name,
            State = exists ? _services[instance.Name] : null,
            Exist = exists == false ? false : null
        };
    }

    public TestResult<ServiceSchema> Test(ServiceSchema instance)
    {
        var current = Get(instance);
        var differingProperties = new List<string>();

        if (instance.Exist != current.Exist)
        {
            differingProperties.Add(nameof(ServiceSchema.Exist));
        }

        if (instance.Exist != false && instance.State != current.State)
        {
            differingProperties.Add(nameof(ServiceSchema.State));
        }

        return new TestResult<ServiceSchema>(current)
        {
            DifferingProperties = differingProperties.Count > 0 ? [.. differingProperties] : null
        };
    }
}
