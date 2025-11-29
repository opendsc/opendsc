// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Schema;

namespace OpenDsc.Resource;

public static class DscJsonSchemaExporterOptions
{
    public static JsonSchemaExporterOptions Default => new JsonSchemaExporterOptions()
    {
        TreatNullObliviousAsNonNullable = true
    };
}
