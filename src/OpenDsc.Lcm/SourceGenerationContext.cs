// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Lcm;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(LcmConfig))]
[JsonSerializable(typeof(DscResult))]
[JsonSerializable(typeof(DscMessage))]
[JsonSerializable(typeof(DscMessageFields))]
[JsonSerializable(typeof(DscResourceResult))]
[JsonSerializable(typeof(DscResourceOperationResult))]
[JsonSerializable(typeof(DscRestartRequirement))]
public partial class SourceGenerationContext : JsonSerializerContext { }
