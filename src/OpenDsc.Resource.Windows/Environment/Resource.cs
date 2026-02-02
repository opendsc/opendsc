// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using OpenDsc.Schema;

namespace OpenDsc.Resource.Windows.Environment;

using Environment = System.Environment;

[DscResource("OpenDsc.Windows/Environment", "0.1.0", Description = "Manage Windows environment variables", Tags = ["windows", "environment", "variable"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(SecurityException), Description = "Access denied")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>, IExportable<Schema>
{
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

    public Schema Get(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var target = instance.Scope is DscScope.Machine ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;
        var value = Environment.GetEnvironmentVariable(instance.Name, target);

        return new Schema()
        {
            Name = instance.Name,
            Value = value,
            Exist = value is not null,
            Scope = instance.Scope is DscScope.Machine ? DscScope.Machine : null
        };
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (instance.Value is null)
        {
            throw new ArgumentException("Environment variable value cannot be empty.");
        }

        var target = instance.Scope is DscScope.Machine ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;
        Environment.SetEnvironmentVariable(instance.Name, instance.Value, target);

        return null;
    }

    public void Delete(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var target = instance.Scope is DscScope.Machine ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;
        Environment.SetEnvironmentVariable(instance.Name, null, target);
    }

    public IEnumerable<Schema> Export(Schema? filter)
    {
        var machineVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
        foreach (string key in machineVars.Keys)
        {
            yield return new Schema
            {
                Name = key,
                Value = machineVars[key]?.ToString(),
                Scope = DscScope.Machine
            };
        }

        var userVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
        foreach (string key in userVars.Keys)
        {
            yield return new Schema
            {
                Name = key,
                Value = userVars[key]?.ToString()
            };
        }
    }
}
