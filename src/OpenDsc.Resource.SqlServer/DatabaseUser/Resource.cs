// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using SmoDatabase = Microsoft.SqlServer.Management.Smo.Database;
using SmoUser = Microsoft.SqlServer.Management.Smo.User;
using SmoUserType = Microsoft.SqlServer.Management.Smo.UserType;

namespace OpenDsc.Resource.SqlServer.DatabaseUser;

[DscResource("OpenDsc.SqlServer/DatabaseUser", "0.1.0", Description = "Manage SQL Server database users", Tags = ["sql", "sqlserver", "database", "user", "security"])]
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

            var user = database.Users.Cast<SmoUser>()
                .FirstOrDefault(u => string.Equals(u.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                return new Schema
                {
                    ServerInstance = instance.ServerInstance,
                    DatabaseName = instance.DatabaseName,
                    Name = instance.Name,
                    Exist = false
                };
            }

            return MapUserToSchema(instance.ServerInstance, instance.DatabaseName, user);
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

            var user = database.Users.Cast<SmoUser>()
                .FirstOrDefault(u => string.Equals(u.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                CreateUser(database, instance);
            }
            else
            {
                UpdateUser(user, instance);
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

            var user = database.Users.Cast<SmoUser>()
                .FirstOrDefault(u => string.Equals(u.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (user != null)
            {
                if (user.IsSystemObject)
                {
                    throw new InvalidOperationException($"Cannot delete system user '{instance.Name}'.");
                }

                user.Drop();
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
            var users = new List<Schema>();
            var databases = string.IsNullOrEmpty(databaseName)
                ? server.Databases.Cast<SmoDatabase>().Where(d => !d.IsSystemObject)
                : server.Databases.Cast<SmoDatabase>().Where(d => string.Equals(d.Name, databaseName, StringComparison.OrdinalIgnoreCase));

            foreach (var database in databases)
            {
                foreach (SmoUser user in database.Users)
                {
                    if (user.IsSystemObject)
                    {
                        continue;
                    }

                    users.Add(MapUserToSchema(serverInstance, database.Name, user));
                }
            }

            return users;
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    private static Schema MapUserToSchema(string serverInstance, string databaseName, SmoUser user)
    {
        var sidBytes = user.Sid;
        string? sidString = null;
        if (sidBytes != null && sidBytes.Length > 0)
        {
            sidString = "0x" + BitConverter.ToString(sidBytes).Replace("-", "");
        }

        return new Schema
        {
            ServerInstance = serverInstance,
            DatabaseName = databaseName,
            Name = user.Name,
            UserType = user.UserType,
            Login = !string.IsNullOrEmpty(user.Login) ? user.Login : null,
            DefaultSchema = !string.IsNullOrEmpty(user.DefaultSchema) ? user.DefaultSchema : null,
            DefaultLanguage = user.DefaultLanguage?.Name,
            AsymmetricKey = !string.IsNullOrEmpty(user.AsymmetricKey) ? user.AsymmetricKey : null,
            Certificate = !string.IsNullOrEmpty(user.Certificate) ? user.Certificate : null,
            CreateDate = user.CreateDate,
            DateLastModified = user.DateLastModified,
            HasDBAccess = user.HasDBAccess,
            IsSystemObject = user.IsSystemObject,
            Sid = sidString,
            AuthenticationType = user.AuthenticationType.ToString()
        };
    }

    private static void CreateUser(SmoDatabase database, Schema instance)
    {
        var user = new SmoUser(database, instance.Name);

        var userType = instance.UserType ?? SmoUserType.SqlUser;
        user.UserType = userType;

        if (instance.Login != null)
        {
            user.Login = instance.Login;
        }

        if (instance.DefaultSchema != null)
        {
            user.DefaultSchema = instance.DefaultSchema;
        }

        if (instance.AsymmetricKey != null)
        {
            user.AsymmetricKey = instance.AsymmetricKey;
        }

        if (instance.Certificate != null)
        {
            user.Certificate = instance.Certificate;
        }

        if (!string.IsNullOrEmpty(instance.Password))
        {
            user.Create(instance.Password);
        }
        else
        {
            user.Create();
        }
    }

    private static void UpdateUser(SmoUser user, Schema instance)
    {
        var changed = false;

        if (instance.Login != null && !string.Equals(user.Login, instance.Login, StringComparison.OrdinalIgnoreCase))
        {
            user.Login = instance.Login;
            changed = true;
        }

        if (instance.DefaultSchema != null && !string.Equals(user.DefaultSchema, instance.DefaultSchema, StringComparison.OrdinalIgnoreCase))
        {
            user.DefaultSchema = instance.DefaultSchema;
            changed = true;
        }

        if (changed)
        {
            user.Alter();
        }

        if (!string.IsNullOrEmpty(instance.Password))
        {
            user.ChangePassword(instance.Password);
        }
    }
}
