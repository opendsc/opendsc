// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using Microsoft.SqlServer.Management.Smo;

namespace OpenDsc.Resource.SqlServer.LinkedServer;

[DscResource("OpenDsc.SqlServer/LinkedServer", "0.1.0",
    Description = "Manage SQL Server linked servers",
    Tags = ["sql", "sqlserver", "linkedserver", "distributed"])]
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
        ValidateName(instance.Name);

        var server = SqlConnectionHelper.CreateConnection(
            instance.ServerInstance,
            instance.ConnectUsername,
            instance.ConnectPassword);

        try
        {
            var linkedServer = server.LinkedServers.Cast<Microsoft.SqlServer.Management.Smo.LinkedServer>()
                .FirstOrDefault(ls => string.Equals(
                    ls.Name,
                    instance.Name,
                    StringComparison.OrdinalIgnoreCase));

            if (linkedServer == null)
            {
                return new Schema
                {
                    ServerInstance = instance.ServerInstance,
                    Name = instance.Name,
                    Exist = false
                };
            }

            return MapLinkedServerToSchema(linkedServer, instance.ServerInstance);
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
        ValidateName(instance.Name);

        var server = SqlConnectionHelper.CreateConnection(
            instance.ServerInstance,
            instance.ConnectUsername,
            instance.ConnectPassword);

        try
        {
            var linkedServer = server.LinkedServers.Cast<Microsoft.SqlServer.Management.Smo.LinkedServer>()
                .FirstOrDefault(ls => string.Equals(
                    ls.Name,
                    instance.Name,
                    StringComparison.OrdinalIgnoreCase));

            if (linkedServer == null)
            {
                CreateLinkedServer(server, instance);
            }
            else
            {
                UpdateLinkedServer(linkedServer, instance);
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
        ValidateName(instance.Name);

        var server = SqlConnectionHelper.CreateConnection(
            instance.ServerInstance,
            instance.ConnectUsername,
            instance.ConnectPassword);

        try
        {
            var linkedServer = server.LinkedServers.Cast<Microsoft.SqlServer.Management.Smo.LinkedServer>()
                .FirstOrDefault(ls => string.Equals(
                    ls.Name,
                    instance.Name,
                    StringComparison.OrdinalIgnoreCase));

            linkedServer?.Drop();
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
        var serverInstance = filter?.ServerInstance ?? ".";
        var username = filter?.ConnectUsername;
        var password = filter?.ConnectPassword;

        var server = SqlConnectionHelper.CreateConnection(serverInstance, username, password);

        try
        {
            var linkedServers = new List<Schema>();
            foreach (Microsoft.SqlServer.Management.Smo.LinkedServer ls in server.LinkedServers)
            {
                linkedServers.Add(MapLinkedServerToSchema(ls, serverInstance));
            }

            return linkedServers;
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    private static void CreateLinkedServer(Server server, Schema instance)
    {
        var linkedServer = new Microsoft.SqlServer.Management.Smo.LinkedServer(
            server,
            instance.Name);

        if (!string.IsNullOrEmpty(instance.ProductName))
        {
            linkedServer.ProductName = instance.ProductName;
        }

        if (!string.IsNullOrEmpty(instance.ProviderName))
        {
            linkedServer.ProviderName = instance.ProviderName;
        }

        if (!string.IsNullOrEmpty(instance.DataSource))
        {
            linkedServer.DataSource = instance.DataSource;
        }

        if (!string.IsNullOrEmpty(instance.Location))
        {
            linkedServer.Location = instance.Location;
        }

        if (!string.IsNullOrEmpty(instance.Catalog))
        {
            linkedServer.Catalog = instance.Catalog;
        }

        if (!string.IsNullOrEmpty(instance.ProviderString))
        {
            linkedServer.ProviderString = instance.ProviderString;
        }

        linkedServer.Create();

        bool altered = false;

        if (instance.DataAccess.HasValue)
        {
            linkedServer.DataAccess = instance.DataAccess.Value;
            altered = true;
        }

        if (instance.Rpc.HasValue)
        {
            linkedServer.Rpc = instance.Rpc.Value;
            altered = true;
        }

        if (instance.RpcOut.HasValue)
        {
            linkedServer.RpcOut = instance.RpcOut.Value;
            altered = true;
        }

        if (instance.UseRemoteCollation.HasValue)
        {
            linkedServer.UseRemoteCollation = instance.UseRemoteCollation.Value;
            altered = true;
        }

        if (!string.IsNullOrEmpty(instance.CollationName))
        {
            linkedServer.CollationName = instance.CollationName;
            altered = true;
        }

        if (instance.CollationCompatible.HasValue)
        {
            linkedServer.CollationCompatible = instance.CollationCompatible.Value;
            altered = true;
        }

        if (instance.LazySchemaValidation.HasValue)
        {
            linkedServer.LazySchemaValidation = instance.LazySchemaValidation.Value;
            altered = true;
        }

        if (instance.ConnectTimeout.HasValue)
        {
            linkedServer.ConnectTimeout = instance.ConnectTimeout.Value;
            altered = true;
        }

        if (instance.QueryTimeout.HasValue)
        {
            linkedServer.QueryTimeout = instance.QueryTimeout.Value;
            altered = true;
        }

        if (instance.IsPromotionofDistributedTransactionsForRPCEnabled.HasValue)
        {
            linkedServer.IsPromotionofDistributedTransactionsForRPCEnabled =
                instance.IsPromotionofDistributedTransactionsForRPCEnabled.Value;
            altered = true;
        }

        if (altered)
        {
            linkedServer.Alter();
        }
    }

    private static void UpdateLinkedServer(
        Microsoft.SqlServer.Management.Smo.LinkedServer linkedServer,
        Schema instance)
    {
        bool altered = false;

        if (instance.DataAccess.HasValue &&
            linkedServer.DataAccess != instance.DataAccess.Value)
        {
            linkedServer.DataAccess = instance.DataAccess.Value;
            altered = true;
        }

        if (instance.Rpc.HasValue && linkedServer.Rpc != instance.Rpc.Value)
        {
            linkedServer.Rpc = instance.Rpc.Value;
            altered = true;
        }

        if (instance.RpcOut.HasValue && linkedServer.RpcOut != instance.RpcOut.Value)
        {
            linkedServer.RpcOut = instance.RpcOut.Value;
            altered = true;
        }

        if (instance.UseRemoteCollation.HasValue &&
            linkedServer.UseRemoteCollation != instance.UseRemoteCollation.Value)
        {
            linkedServer.UseRemoteCollation = instance.UseRemoteCollation.Value;
            altered = true;
        }

        if (!string.IsNullOrEmpty(instance.CollationName) &&
            !string.Equals(
                linkedServer.CollationName,
                instance.CollationName,
                StringComparison.OrdinalIgnoreCase))
        {
            linkedServer.CollationName = instance.CollationName;
            altered = true;
        }

        if (instance.CollationCompatible.HasValue &&
            linkedServer.CollationCompatible != instance.CollationCompatible.Value)
        {
            linkedServer.CollationCompatible = instance.CollationCompatible.Value;
            altered = true;
        }

        if (instance.LazySchemaValidation.HasValue &&
            linkedServer.LazySchemaValidation != instance.LazySchemaValidation.Value)
        {
            linkedServer.LazySchemaValidation = instance.LazySchemaValidation.Value;
            altered = true;
        }

        if (instance.ConnectTimeout.HasValue &&
            linkedServer.ConnectTimeout != instance.ConnectTimeout.Value)
        {
            linkedServer.ConnectTimeout = instance.ConnectTimeout.Value;
            altered = true;
        }

        if (instance.QueryTimeout.HasValue &&
            linkedServer.QueryTimeout != instance.QueryTimeout.Value)
        {
            linkedServer.QueryTimeout = instance.QueryTimeout.Value;
            altered = true;
        }

        if (instance.IsPromotionofDistributedTransactionsForRPCEnabled.HasValue &&
            linkedServer.IsPromotionofDistributedTransactionsForRPCEnabled !=
                instance.IsPromotionofDistributedTransactionsForRPCEnabled.Value)
        {
            linkedServer.IsPromotionofDistributedTransactionsForRPCEnabled =
                instance.IsPromotionofDistributedTransactionsForRPCEnabled.Value;
            altered = true;
        }

        if (altered)
        {
            linkedServer.Alter();
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Linked server name is required and cannot be empty.", nameof(name));
        }
    }

    private static Schema MapLinkedServerToSchema(
        Microsoft.SqlServer.Management.Smo.LinkedServer linkedServer,
        string serverInstance)
    {
        return new Schema
        {
            ServerInstance = serverInstance,
            Name = linkedServer.Name,
            ProductName = NullIfEmpty(linkedServer.ProductName),
            ProviderName = NullIfEmpty(linkedServer.ProviderName),
            DataSource = NullIfEmpty(linkedServer.DataSource),
            Location = NullIfEmpty(linkedServer.Location),
            Catalog = NullIfEmpty(linkedServer.Catalog),
            ProviderString = NullIfEmpty(linkedServer.ProviderString),
            DataAccess = linkedServer.DataAccess,
            Rpc = linkedServer.Rpc,
            RpcOut = linkedServer.RpcOut,
            UseRemoteCollation = linkedServer.UseRemoteCollation,
            CollationName = NullIfEmpty(linkedServer.CollationName),
            CollationCompatible = linkedServer.CollationCompatible,
            LazySchemaValidation = linkedServer.LazySchemaValidation,
            ConnectTimeout = linkedServer.ConnectTimeout,
            QueryTimeout = linkedServer.QueryTimeout,
            IsPromotionofDistributedTransactionsForRPCEnabled =
                linkedServer.IsPromotionofDistributedTransactionsForRPCEnabled,
            Id = linkedServer.ID,
            DateLastModified = linkedServer.DateLastModified,
            Distributor = linkedServer.Distributor,
            DistPublisher = linkedServer.DistPublisher,
            Publisher = linkedServer.Publisher,
            Subscriber = linkedServer.Subscriber
        };
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrEmpty(value) ? null : value;
}
