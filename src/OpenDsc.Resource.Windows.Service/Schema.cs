// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ServiceProcess;
using System.Text.Json.Serialization;

namespace OpenDsc.Resource.Windows.Service;

public sealed class Schema
{
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<ServiceControllerStatus>))]
    public ServiceControllerStatus? Status { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<ServiceStartMode>))]
    public ServiceStartMode? StartType { get; set; }

    [JsonPropertyName("_exist")]
    public bool? Exist { get; set; }
}
