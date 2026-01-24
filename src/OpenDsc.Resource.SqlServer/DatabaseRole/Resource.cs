// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using Json.Schema.Generation;
using SmoDatabase = Microsoft.SqlServer.Management.Smo.Database;
using SmoDatabaseRole = Microsoft.SqlServer.Management.Smo.DatabaseRole;

namespace OpenDsc.Resource.SqlServer.DatabaseRole;

[DscResource("OpenDsc.SqlServer/DatabaseRole", "0.1.0", Description = "Manage SQL Server database roles", Tags = ["sql", "sqlserver", "database", "role", "security"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(4, Exception = typeof(UnauthorizedAccessException), Description = "Unauthorized access")]
[ExitCode(5, Exception = typeof(InvalidOperationException), Description = "Invalid operation")]
public sealed class Resource(JsonSerializerContext context)
    : DscResource<Schema>(context),
      IGettable<Schema>,
      ISettable<Schema>,
      IDeletable<Schema>,
      IExportable<Schema>
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

    public Schema Get(Schema instance)
    {
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var database = server.Databases.Cast<SmoDatabase>()
                .FirstOrDefault(d => string.Equals(d.Name, instance.DatabaseName, StringComparison.OrdinalIgnoreCase));

            if (database == null)
            {
                return new Schema
                {
                    ServerInstance = instance.ServerInstance,
                    DatabaseName = instance.DatabaseName,
                    Name = instance.Name,
                    Exist = false
                };
            }

            var role = database.Roles.Cast<SmoDatabaseRole>()
                .FirstOrDefault(r => string.Equals(r.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (role == null)
            {
                return new Schema
                {
                    ServerInstance = instance.ServerInstance,
                    DatabaseName = instance.DatabaseName,
                    Name = instance.Name,
                    Exist = false
                };
            }

            var members = GetRoleMembers(role);

            return new Schema
            {
                ServerInstance = instance.ServerInstance,
                DatabaseName = instance.DatabaseName,
                Name = role.Name,
                Owner = role.Owner,
                Members = members?.Length > 0 ? members : null,
                CreateDate = role.CreateDate,
                DateLastModified = role.DateLastModified,
                IsFixedRole = role.IsFixedRole
            };
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var database = server.Databases.Cast<SmoDatabase>()
                .FirstOrDefault(d => string.Equals(d.Name, instance.DatabaseName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Database '{instance.DatabaseName}' not found on server '{instance.ServerInstance}'.");

            var role = database.Roles.Cast<SmoDatabaseRole>()
                .FirstOrDefault(r => string.Equals(r.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (role == null)
            {
                CreateRole(database, instance);
            }
            else
            {
                UpdateRole(role, instance);
            }

            return null;
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    public void Delete(Schema instance)
    {
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var database = server.Databases.Cast<SmoDatabase>()
                .FirstOrDefault(d => string.Equals(d.Name, instance.DatabaseName, StringComparison.OrdinalIgnoreCase));

            if (database == null)
            {
                return;
            }

            var role = database.Roles.Cast<SmoDatabaseRole>()
                .FirstOrDefault(r => string.Equals(r.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (role != null)
            {
                if (role.IsFixedRole)
                {
                    throw new InvalidOperationException($"Cannot delete fixed database role '{instance.Name}'.");
                }

                role.Drop();
            }
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    public IEnumerable<Schema> Export()
    {
        var serverInstance = Environment.GetEnvironmentVariable("SQLSERVER_INSTANCE") ?? ".";
        var username = Environment.GetEnvironmentVariable("SQLSERVER_USERNAME");
        var password = Environment.GetEnvironmentVariable("SQLSERVER_PASSWORD");
        var databaseName = Environment.GetEnvironmentVariable("SQLSERVER_DATABASE");
        return Export(serverInstance, databaseName, username, password);
    }

    public IEnumerable<Schema> Export(string serverInstance, string? databaseName = null, string? username = null, string? password = null)
    {
        var server = SqlConnectionHelper.CreateConnection(serverInstance, username, password);

        try
        {
            var roles = new List<Schema>();
            var databases = string.IsNullOrEmpty(databaseName)
                ? server.Databases.Cast<SmoDatabase>().Where(d => !d.IsSystemObject)
                : server.Databases.Cast<SmoDatabase>().Where(d => string.Equals(d.Name, databaseName, StringComparison.OrdinalIgnoreCase));

            foreach (var database in databases)
            {
                foreach (SmoDatabaseRole role in database.Roles)
                {
                    if (role.IsFixedRole)
                    {
                        continue;
                    }

                    var members = GetRoleMembers(role);

                    roles.Add(new Schema
                    {
                        ServerInstance = serverInstance,
                        DatabaseName = database.Name,
                        Name = role.Name,
                        Owner = role.Owner,
                        Members = members?.Length > 0 ? members : null,
                        CreateDate = role.CreateDate,
                        DateLastModified = role.DateLastModified,
                        IsFixedRole = role.IsFixedRole
                    });
                }
            }

            return roles;
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    private static void CreateRole(SmoDatabase database, Schema instance)
    {
        var role = new SmoDatabaseRole(database, instance.Name);

        if (!string.IsNullOrEmpty(instance.Owner))
        {
            role.Owner = instance.Owner;
        }

        role.Create();

        if (instance.Members != null)
        {
            foreach (var member in instance.Members)
            {
                role.AddMember(member);
            }
        }
    }

    private static void UpdateRole(SmoDatabaseRole role, Schema instance)
    {
        if (role.IsFixedRole)
        {
            if (!string.IsNullOrEmpty(instance.Owner))
            {
                throw new InvalidOperationException($"Cannot change owner of fixed database role '{role.Name}'.");
            }
        }
        else if (!string.IsNullOrEmpty(instance.Owner) &&
                 !string.Equals(role.Owner, instance.Owner, StringComparison.OrdinalIgnoreCase))
        {
            role.Owner = instance.Owner;
            role.Alter();
        }

        if (instance.Members != null)
        {
            UpdateMembers(role, instance);
        }
    }

    private static void UpdateMembers(SmoDatabaseRole role, Schema instance)
    {
        var desiredMembers = instance.Members!;
        var currentMembersArray = GetRoleMembers(role) ?? [];
        var currentMembers = new HashSet<string>(currentMembersArray, StringComparer.OrdinalIgnoreCase);
        var targetMembers = new HashSet<string>(desiredMembers, StringComparer.OrdinalIgnoreCase);

        if (instance.Purge == true)
        {
            var membersToRemove = currentMembers.Except(targetMembers);
            foreach (var member in membersToRemove)
            {
                role.DropMember(member);
            }
        }

        var membersToAdd = targetMembers.Except(currentMembers);
        foreach (var member in membersToAdd)
        {
            role.AddMember(member);
        }
    }

    private static string[]? GetRoleMembers(SmoDatabaseRole role)
    {
        var members = role.EnumMembers();
        if (members == null || members.Count == 0)
        {
            return null;
        }

        var result = new string[members.Count];
        members.CopyTo(result, 0);
        return result;
    }
}
