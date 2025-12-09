// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Schema;

namespace OpenDsc.Resource;

/// <summary>
/// Provides default JSON schema exporter options configured for DSC resources.
/// </summary>
public static class DscJsonSchemaExporterOptions
{
    /// <summary>
    /// Gets the default JSON schema exporter options for DSC resources.
    /// Configured to treat null-oblivious types as non-nullable.
    /// </summary>
    public static JsonSchemaExporterOptions Default => new()
    {
        TreatNullObliviousAsNonNullable = true
    };
}
