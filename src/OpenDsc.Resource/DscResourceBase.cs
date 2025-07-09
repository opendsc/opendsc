// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Schema;

namespace OpenDsc.Resource;

public abstract class DscResourceBase<T> : IDscResource<T>
{
    public JsonSchemaExporterOptions ExporterOptions
    {
        get
        {
            _exporterOptions ??= new JsonSchemaExporterOptions()
            {
                TreatNullObliviousAsNonNullable = true
            };

            return _exporterOptions;
        }

        set
        {
            _exporterOptions = value;
        }
    }

    private JsonSchemaExporterOptions? _exporterOptions;

    public abstract string GetSchema();

    public abstract T Parse(string json);

    public abstract string ToJson(T item);
}
