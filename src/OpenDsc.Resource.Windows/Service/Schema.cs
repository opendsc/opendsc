// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ServiceProcess;
using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.Service;

[Title("Windows Service Schema")]
[Description("Schema for managing Windows services via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The name of the service. Case insensitive. Cannot contain forward slash (/) or backslash (\\) characters.")]
    [MinLength(1)]
    [MaxLength(256)]
    [Pattern(@"^[^/\\]+$")]
    public string Name { get; set; } = string.Empty;

    [Description("The display name of the service.")]
    [Nullable(false)]
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    [Description("The description of the service.")]
    [Nullable(false)]
    public string? Description { get; set; }

    [Description("The fully qualified path to the service binary file. If the path contains a space, it must be quoted. The path can also include arguments.")]
    [Nullable(false)]
    public string? Path { get; set; }

    [Description("The dependencies of the service.")]
    [Nullable(false)]
    public string[]? Dependencies { get; set; }

    [Description("The status of the service. Only Stopped, Running, and Paused are supported when setting.")]
    [JsonConverter(typeof(JsonStringEnumConverter<ServiceControllerStatus>))]
    [Nullable(false)]
    public ServiceControllerStatus? Status { get; set; }

    [Description("The start type of the service. Only Automatic, Manual, and Disabled are supported when setting.")]
    [JsonConverter(typeof(JsonStringEnumConverter<ServiceStartMode>))]
    [Nullable(false)]
    public ServiceStartMode? StartType { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the service exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
