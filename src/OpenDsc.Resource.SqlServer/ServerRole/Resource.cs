// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using SmoServerRole = Microsoft.SqlServer.Management.Smo.ServerRole;

namespace OpenDsc.Resource.SqlServer.ServerRole;

[DscResource("OpenDsc.SqlServer/ServerRole", "0.1.0", Description = "Manage SQL Server server roles", Tags = ["sql", "sqlserver", "server", "role", "security"])]
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

    public Schema Get(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var role = server.Roles.Cast<SmoServerRole>()
                .FirstOrDefault(r => string.Equals(r.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (role == null)
            {
                return new Schema
                {
                    ServerInstance = instance.ServerInstance,
                    Name = instance.Name,
                    Exist = false
                };
            }

            var members = GetRoleMembers(role);

            return new Schema
            {
                ServerInstance = instance.ServerInstance,
                Name = role.Name,
                Owner = role.Owner,
                Members = members?.Length > 0 ? members : null,
                DateCreated = role.DateCreated,
                DateModified = role.DateModified,
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

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var role = server.Roles.Cast<SmoServerRole>()
                .FirstOrDefault(r => string.Equals(r.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (role == null)
            {
                CreateRole(server, instance);
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

    public void Delete(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var role = server.Roles.Cast<SmoServerRole>()
                .FirstOrDefault(r => string.Equals(r.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (role != null)
            {
                if (role.IsFixedRole)
                {
                    throw new InvalidOperationException($"Cannot delete fixed server role '{instance.Name}'.");
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

    public IEnumerable<Schema> Export(Schema? filter)
    {
        var serverInstance = Environment.GetEnvironmentVariable("SQLSERVER_INSTANCE") ?? ".";
        var username = Environment.GetEnvironmentVariable("SQLSERVER_USERNAME");
        var password = Environment.GetEnvironmentVariable("SQLSERVER_PASSWORD");
        return Export(serverInstance, username, password);
    }

    public IEnumerable<Schema> Export(string serverInstance, string? username = null, string? password = null)
    {
        var server = SqlConnectionHelper.CreateConnection(serverInstance, username, password);

        try
        {
            var roles = new List<Schema>();

            foreach (SmoServerRole role in server.Roles)
            {
                if (role.IsFixedRole)
                {
                    continue;
                }

                var members = GetRoleMembers(role);

                roles.Add(new Schema
                {
                    ServerInstance = serverInstance,
                    Name = role.Name,
                    Owner = role.Owner,
                    Members = members?.Length > 0 ? members : null,
                    DateCreated = role.DateCreated,
                    DateModified = role.DateModified,
                    IsFixedRole = role.IsFixedRole
                });
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

    private static void CreateRole(Microsoft.SqlServer.Management.Smo.Server server, Schema instance)
    {
        var role = new SmoServerRole(server, instance.Name);

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

    private static void UpdateRole(SmoServerRole role, Schema instance)
    {
        if (role.IsFixedRole)
        {
            if (!string.IsNullOrEmpty(instance.Owner))
            {
                throw new InvalidOperationException($"Cannot change owner of fixed server role '{role.Name}'.");
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

    private static void UpdateMembers(SmoServerRole role, Schema instance)
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

    private static string[]? GetRoleMembers(SmoServerRole role)
    {
        var members = role.EnumMemberNames();
        if (members == null || members.Count == 0)
        {
            return null;
        }

        var result = new string[members.Count];
        members.CopyTo(result, 0);
        return result;
    }
}
