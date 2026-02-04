// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.LinkedServer;

[Title("SQL Server Linked Server Schema")]
[Description("Schema for managing SQL Server linked servers via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The name of the SQL Server instance to connect to. Use '.' or " +
        "'(local)' for the default local instance, or 'servername\\instancename' " +
        "for named instances.")]
    [Pattern(@"^.+$")]
    public string ServerInstance { get; set; } = string.Empty;

    [Description("The username for SQL Server authentication when connecting to " +
        "the server. If not specified, Windows Authentication is used.")]
    [Nullable(false)]
    [WriteOnly]
    public string? ConnectUsername { get; set; }

    [Description("The password for SQL Server authentication when connecting to " +
        "the server. Required when ConnectUsername is specified.")]
    [Nullable(false)]
    [WriteOnly]
    public string? ConnectPassword { get; set; }

    [Description("The name of the linked server.")]
    [Pattern(@"^.+$")]
    public string Name { get; set; } = string.Empty;

    [Description("Indicates whether the linked server should exist.")]
    [JsonPropertyName("_exist")]
    [Default(true)]
    [Nullable(false)]
    public bool? Exist { get; set; }

    [Description("The product name of the OLE DB data source.")]
    [Nullable(false)]
    public string? ProductName { get; set; }

    [Description("The OLE DB provider name.")]
    [Nullable(false)]
    public string? ProviderName { get; set; }

    [Description("The OLE DB data source (e.g., server name or file path).")]
    [Nullable(false)]
    public string? DataSource { get; set; }

    [Description("The location of the database for the OLE DB provider.")]
    [Nullable(false)]
    public string? Location { get; set; }

    [Description("The default catalog (database) on the linked server.")]
    [Nullable(false)]
    public string? Catalog { get; set; }

    [Description("The OLE DB provider connection string.")]
    [Nullable(false)]
    public string? ProviderString { get; set; }

    [Description("Whether data access is enabled for the linked server.")]
    [Nullable(false)]
    public bool? DataAccess { get; set; }

    [Description("Whether RPC (remote procedure calls) from the linked server " +
        "are allowed.")]
    [Nullable(false)]
    public bool? Rpc { get; set; }

    [Description("Whether RPC out (remote procedure calls to the linked server) " +
        "is enabled.")]
    [Nullable(false)]
    public bool? RpcOut { get; set; }

    [Description("Whether to use the remote server's collation instead of the " +
        "local server's collation.")]
    [Nullable(false)]
    public bool? UseRemoteCollation { get; set; }

    [Description("The collation name used for character comparisons.")]
    [Nullable(false)]
    public string? CollationName { get; set; }

    [Description("Whether collation is compatible with the linked server.")]
    [Nullable(false)]
    public bool? CollationCompatible { get; set; }

    [Description("Whether to use lazy schema validation (validate remote table " +
        "schema only when needed).")]
    [Nullable(false)]
    public bool? LazySchemaValidation { get; set; }

    [Description("The connection timeout in seconds.")]
    [Nullable(false)]
    [Minimum(0)]
    public int? ConnectTimeout { get; set; }

    [Description("The query timeout in seconds.")]
    [Nullable(false)]
    [Minimum(0)]
    public int? QueryTimeout { get; set; }

    [Description("Whether promotion of distributed transactions for RPC is " +
        "enabled.")]
    [Nullable(false)]
    public bool? IsPromotionofDistributedTransactionsForRPCEnabled { get; set; }

    [ReadOnly]
    [Description("The unique identifier of the linked server.")]
    public int? Id { get; set; }

    [ReadOnly]
    [Description("The date the linked server was last modified.")]
    public DateTime? DateLastModified { get; set; }

    [ReadOnly]
    [Description("Whether the linked server is a distributor.")]
    public bool? Distributor { get; set; }

    [ReadOnly]
    [Description("Whether the linked server is a distribution publisher.")]
    public bool? DistPublisher { get; set; }

    [ReadOnly]
    [Description("Whether the linked server is a publisher.")]
    public bool? Publisher { get; set; }

    [ReadOnly]
    [Description("Whether the linked server is a subscriber.")]
    public bool? Subscriber { get; set; }
}
