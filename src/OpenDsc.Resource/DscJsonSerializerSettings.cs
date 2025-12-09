// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace OpenDsc.Resource;

/// <summary>
/// Provides default JSON serializer settings configured for DSC resources.
/// Note: For Native AOT compatibility, prefer using <see cref="JsonSerializerContext"/> with source generation.
/// </summary>
#if NET6_0_OR_GREATER
    [RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public static class DscJsonSerializerSettings
{
    /// <summary>
    /// Gets the default JSON serializer options for DSC resources.
    /// Configured with camelCase naming, compact output, and enum string conversion.
    /// </summary>
    public static JsonSerializerOptions Default => new()
    {
        // DSC requires JSON lines for most output
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };
}
