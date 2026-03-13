// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Server.Entities;

[JsonConverter(typeof(JsonStringEnumConverter<ParameterVersionStatus>))]
public enum ParameterVersionStatus
{
    Draft = 0,
    Published = 1
}
