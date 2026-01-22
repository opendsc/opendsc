// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using Microsoft.SqlServer.Management.Smo;

namespace OpenDsc.Resource.SqlServer.Database;

[DscResource("OpenDsc.SqlServer/Database", "0.1.0", Description = "Manage SQL Server databases", Tags = ["sql", "sqlserver", "database"])]
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
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.Authentication);

        try
        {
            var database = server.Databases.Cast<Microsoft.SqlServer.Management.Smo.Database>()
                .FirstOrDefault(d => string.Equals(d.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (database == null)
            {
                return new Schema
                {
                    ServerInstance = instance.ServerInstance,
                    Name = instance.Name,
                    Exist = false
                };
            }

            return new Schema
            {
                ServerInstance = instance.ServerInstance,
                Name = database.Name,

                // Database options
                Collation = database.Collation,
                CompatibilityLevel = database.CompatibilityLevel,
                RecoveryModel = database.RecoveryModel,
                Owner = database.Owner,

                // Access and state options
                ReadOnly = database.ReadOnly,
                UserAccess = database.UserAccess,
                PageVerify = database.PageVerify,
                ContainmentType = database.ContainmentType,

                // ANSI options
                AnsiNullDefault = database.AnsiNullDefault,
                AnsiNullsEnabled = database.AnsiNullsEnabled,
                AnsiPaddingEnabled = database.AnsiPaddingEnabled,
                AnsiWarningsEnabled = database.AnsiWarningsEnabled,
                ArithmeticAbortEnabled = database.ArithmeticAbortEnabled,
                ConcatenateNullYieldsNull = database.ConcatenateNullYieldsNull,
                NumericRoundAbortEnabled = database.NumericRoundAbortEnabled,
                QuotedIdentifiersEnabled = database.QuotedIdentifiersEnabled,

                // Auto options
                AutoClose = database.AutoClose,
                AutoShrink = database.AutoShrink,
                AutoCreateStatisticsEnabled = database.AutoCreateStatisticsEnabled,
                AutoUpdateStatisticsEnabled = database.AutoUpdateStatisticsEnabled,
                AutoUpdateStatisticsAsync = database.AutoUpdateStatisticsAsync,

                // Cursor options
                CloseCursorsOnCommitEnabled = database.CloseCursorsOnCommitEnabled,
                LocalCursorsDefault = database.LocalCursorsDefault,

                // Trigger options
                NestedTriggersEnabled = database.NestedTriggersEnabled,
                RecursiveTriggersEnabled = database.RecursiveTriggersEnabled,

                // Advanced options
                Trustworthy = database.Trustworthy,
                DatabaseOwnershipChaining = database.DatabaseOwnershipChaining,
                DateCorrelationOptimization = database.DateCorrelationOptimization,
                BrokerEnabled = database.BrokerEnabled,
                EncryptionEnabled = database.EncryptionEnabled,
                IsParameterizationForced = database.IsParameterizationForced,
                IsReadCommittedSnapshotOn = database.IsReadCommittedSnapshotOn,
                IsFullTextEnabled = database.IsFullTextEnabled,
                TargetRecoveryTime = database.TargetRecoveryTime,
                AcceleratedRecoveryEnabled = GetAcceleratedRecoveryEnabled(database),

                // Read-only properties
                Id = database.ID,
                CreateDate = database.CreateDate,
                Size = database.Size,
                SpaceAvailable = database.SpaceAvailable,
                DataSpaceUsage = database.DataSpaceUsage,
                IndexSpaceUsage = database.IndexSpaceUsage,
                ActiveConnections = database.ActiveConnections,
                LastBackupDate = database.LastBackupDate == DateTime.MinValue ? null : database.LastBackupDate,
                LastDifferentialBackupDate = database.LastDifferentialBackupDate == DateTime.MinValue ? null : database.LastDifferentialBackupDate,
                LastLogBackupDate = database.LastLogBackupDate == DateTime.MinValue ? null : database.LastLogBackupDate,
                Status = database.Status.ToString(),
                IsSystemObject = database.IsSystemObject,
                IsAccessible = database.IsAccessible,
                IsUpdateable = database.IsUpdateable,
                IsDatabaseSnapshot = database.IsDatabaseSnapshot,
                IsMirroringEnabled = database.IsMirroringEnabled,
                AvailabilityGroupName = string.IsNullOrEmpty(database.AvailabilityGroupName) ? null : database.AvailabilityGroupName,
                CaseSensitive = database.CaseSensitive,
                PrimaryFilePathActual = database.PrimaryFilePath,
                DefaultFileGroup = database.DefaultFileGroup
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
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.Authentication);

        try
        {
            var database = server.Databases.Cast<Microsoft.SqlServer.Management.Smo.Database>()
                .FirstOrDefault(d => string.Equals(d.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (database == null)
            {
                CreateDatabase(server, instance);
            }
            else
            {
                UpdateDatabase(database, instance);
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
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.Authentication);

        try
        {
            var database = server.Databases.Cast<Microsoft.SqlServer.Management.Smo.Database>()
                .FirstOrDefault(d => string.Equals(d.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            database?.Drop();
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
        var authTypeStr = Environment.GetEnvironmentVariable("SQLSERVER_AUTH_TYPE");
        var username = Environment.GetEnvironmentVariable("SQLSERVER_USERNAME");
        var password = Environment.GetEnvironmentVariable("SQLSERVER_PASSWORD");

        SqlAuthentication? authentication = null;
        if (!string.IsNullOrEmpty(authTypeStr) && Enum.TryParse<SqlAuthType>(authTypeStr, true, out var authType))
        {
            authentication = new SqlAuthentication
            {
                AuthType = authType,
                Username = username,
                Password = password
            };
        }
        else if (!string.IsNullOrEmpty(username))
        {
            authentication = new SqlAuthentication
            {
                AuthType = SqlAuthType.Sql,
                Username = username,
                Password = password
            };
        }

        return Export(serverInstance, authentication);
    }

    public IEnumerable<Schema> Export(string serverInstance, SqlAuthentication? authentication = null)
    {
        var server = SqlConnectionHelper.CreateConnection(serverInstance, authentication);

        try
        {
            var databases = new List<Schema>();

            foreach (Microsoft.SqlServer.Management.Smo.Database database in server.Databases)
            {
                if (database.IsSystemObject)
                {
                    continue;
                }

                databases.Add(new Schema
                {
                    ServerInstance = serverInstance,
                    Name = database.Name,
                    Collation = database.Collation,
                    CompatibilityLevel = database.CompatibilityLevel,
                    RecoveryModel = database.RecoveryModel,
                    Owner = database.Owner,
                    ReadOnly = database.ReadOnly,
                    UserAccess = database.UserAccess,
                    PageVerify = database.PageVerify,
                    ContainmentType = database.ContainmentType,
                    AutoClose = database.AutoClose,
                    AutoShrink = database.AutoShrink,
                    Trustworthy = database.Trustworthy
                });
            }

            return databases;
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    private static void CreateDatabase(Server server, Schema instance)
    {
        var database = new Microsoft.SqlServer.Management.Smo.Database(server, instance.Name);

        // Set collation if specified
        if (!string.IsNullOrEmpty(instance.Collation))
        {
            database.Collation = instance.Collation;
        }

        // Set containment type before creation if specified
        if (instance.ContainmentType.HasValue)
        {
            database.ContainmentType = instance.ContainmentType.Value;
        }

        // Configure file groups and files if paths are specified
        if (!string.IsNullOrEmpty(instance.PrimaryFilePath))
        {
            var primaryFileGroup = new FileGroup(database, "PRIMARY");
            database.FileGroups.Add(primaryFileGroup);

            var primaryFile = new DataFile(primaryFileGroup, instance.Name)
            {
                FileName = instance.PrimaryFilePath
            };

            if (instance.PrimaryFileSize.HasValue)
            {
                primaryFile.Size = instance.PrimaryFileSize.Value * 1024.0; // Convert MB to KB
            }

            if (instance.PrimaryFileGrowth.HasValue)
            {
                primaryFile.Growth = instance.PrimaryFileGrowth.Value * 1024.0; // Convert MB to KB
                primaryFile.GrowthType = FileGrowthType.KB;
            }

            primaryFileGroup.Files.Add(primaryFile);
        }

        if (!string.IsNullOrEmpty(instance.LogFilePath))
        {
            var logFile = new LogFile(database, instance.Name + "_Log")
            {
                FileName = instance.LogFilePath
            };

            if (instance.LogFileSize.HasValue)
            {
                logFile.Size = instance.LogFileSize.Value * 1024.0; // Convert MB to KB
            }

            if (instance.LogFileGrowth.HasValue)
            {
                logFile.Growth = instance.LogFileGrowth.Value * 1024.0; // Convert MB to KB
                logFile.GrowthType = FileGrowthType.KB;
            }

            database.LogFiles.Add(logFile);
        }

        database.Create();

        // Set owner after creation
        if (!string.IsNullOrEmpty(instance.Owner))
        {
            database.SetOwner(instance.Owner);
        }

        // Apply other settings after creation
        UpdateDatabase(database, instance);
    }

    private static void UpdateDatabase(Microsoft.SqlServer.Management.Smo.Database database, Schema instance)
    {
        bool needsAlter = false;

        // Recovery model
        if (instance.RecoveryModel.HasValue)
        {
            if (database.RecoveryModel != instance.RecoveryModel.Value)
            {
                database.RecoveryModel = instance.RecoveryModel.Value;
                needsAlter = true;
            }
        }

        // Compatibility level
        if (instance.CompatibilityLevel.HasValue)
        {
            if (database.CompatibilityLevel != instance.CompatibilityLevel.Value)
            {
                database.CompatibilityLevel = instance.CompatibilityLevel.Value;
                needsAlter = true;
            }
        }

        // User access
        if (instance.UserAccess.HasValue)
        {
            if (database.UserAccess != instance.UserAccess.Value)
            {
                database.UserAccess = instance.UserAccess.Value;
                needsAlter = true;
            }
        }

        // Page verify
        if (instance.PageVerify.HasValue)
        {
            if (database.PageVerify != instance.PageVerify.Value)
            {
                database.PageVerify = instance.PageVerify.Value;
                needsAlter = true;
            }
        }

        // Read-only state
        if (instance.ReadOnly.HasValue && database.ReadOnly != instance.ReadOnly.Value)
        {
            database.ReadOnly = instance.ReadOnly.Value;
            needsAlter = true;
        }

        // ANSI options
        if (instance.AnsiNullDefault.HasValue && database.AnsiNullDefault != instance.AnsiNullDefault.Value)
        {
            database.AnsiNullDefault = instance.AnsiNullDefault.Value;
            needsAlter = true;
        }

        if (instance.AnsiNullsEnabled.HasValue && database.AnsiNullsEnabled != instance.AnsiNullsEnabled.Value)
        {
            database.AnsiNullsEnabled = instance.AnsiNullsEnabled.Value;
            needsAlter = true;
        }

        if (instance.AnsiPaddingEnabled.HasValue && database.AnsiPaddingEnabled != instance.AnsiPaddingEnabled.Value)
        {
            database.AnsiPaddingEnabled = instance.AnsiPaddingEnabled.Value;
            needsAlter = true;
        }

        if (instance.AnsiWarningsEnabled.HasValue && database.AnsiWarningsEnabled != instance.AnsiWarningsEnabled.Value)
        {
            database.AnsiWarningsEnabled = instance.AnsiWarningsEnabled.Value;
            needsAlter = true;
        }

        if (instance.ArithmeticAbortEnabled.HasValue && database.ArithmeticAbortEnabled != instance.ArithmeticAbortEnabled.Value)
        {
            database.ArithmeticAbortEnabled = instance.ArithmeticAbortEnabled.Value;
            needsAlter = true;
        }

        if (instance.ConcatenateNullYieldsNull.HasValue && database.ConcatenateNullYieldsNull != instance.ConcatenateNullYieldsNull.Value)
        {
            database.ConcatenateNullYieldsNull = instance.ConcatenateNullYieldsNull.Value;
            needsAlter = true;
        }

        if (instance.NumericRoundAbortEnabled.HasValue && database.NumericRoundAbortEnabled != instance.NumericRoundAbortEnabled.Value)
        {
            database.NumericRoundAbortEnabled = instance.NumericRoundAbortEnabled.Value;
            needsAlter = true;
        }

        if (instance.QuotedIdentifiersEnabled.HasValue && database.QuotedIdentifiersEnabled != instance.QuotedIdentifiersEnabled.Value)
        {
            database.QuotedIdentifiersEnabled = instance.QuotedIdentifiersEnabled.Value;
            needsAlter = true;
        }

        // Auto options
        if (instance.AutoClose.HasValue && database.AutoClose != instance.AutoClose.Value)
        {
            database.AutoClose = instance.AutoClose.Value;
            needsAlter = true;
        }

        if (instance.AutoShrink.HasValue && database.AutoShrink != instance.AutoShrink.Value)
        {
            database.AutoShrink = instance.AutoShrink.Value;
            needsAlter = true;
        }

        if (instance.AutoCreateStatisticsEnabled.HasValue && database.AutoCreateStatisticsEnabled != instance.AutoCreateStatisticsEnabled.Value)
        {
            database.AutoCreateStatisticsEnabled = instance.AutoCreateStatisticsEnabled.Value;
            needsAlter = true;
        }

        if (instance.AutoUpdateStatisticsEnabled.HasValue && database.AutoUpdateStatisticsEnabled != instance.AutoUpdateStatisticsEnabled.Value)
        {
            database.AutoUpdateStatisticsEnabled = instance.AutoUpdateStatisticsEnabled.Value;
            needsAlter = true;
        }

        if (instance.AutoUpdateStatisticsAsync.HasValue && database.AutoUpdateStatisticsAsync != instance.AutoUpdateStatisticsAsync.Value)
        {
            database.AutoUpdateStatisticsAsync = instance.AutoUpdateStatisticsAsync.Value;
            needsAlter = true;
        }

        // Cursor options
        if (instance.CloseCursorsOnCommitEnabled.HasValue && database.CloseCursorsOnCommitEnabled != instance.CloseCursorsOnCommitEnabled.Value)
        {
            database.CloseCursorsOnCommitEnabled = instance.CloseCursorsOnCommitEnabled.Value;
            needsAlter = true;
        }

        if (instance.LocalCursorsDefault.HasValue && database.LocalCursorsDefault != instance.LocalCursorsDefault.Value)
        {
            database.LocalCursorsDefault = instance.LocalCursorsDefault.Value;
            needsAlter = true;
        }

        // Trigger options
        if (instance.NestedTriggersEnabled.HasValue && database.NestedTriggersEnabled != instance.NestedTriggersEnabled.Value)
        {
            database.NestedTriggersEnabled = instance.NestedTriggersEnabled.Value;
            needsAlter = true;
        }

        if (instance.RecursiveTriggersEnabled.HasValue && database.RecursiveTriggersEnabled != instance.RecursiveTriggersEnabled.Value)
        {
            database.RecursiveTriggersEnabled = instance.RecursiveTriggersEnabled.Value;
            needsAlter = true;
        }

        // Advanced options
        if (instance.Trustworthy.HasValue && database.Trustworthy != instance.Trustworthy.Value)
        {
            database.Trustworthy = instance.Trustworthy.Value;
            needsAlter = true;
        }

        if (instance.DatabaseOwnershipChaining.HasValue && database.DatabaseOwnershipChaining != instance.DatabaseOwnershipChaining.Value)
        {
            database.DatabaseOwnershipChaining = instance.DatabaseOwnershipChaining.Value;
            needsAlter = true;
        }

        if (instance.DateCorrelationOptimization.HasValue && database.DateCorrelationOptimization != instance.DateCorrelationOptimization.Value)
        {
            database.DateCorrelationOptimization = instance.DateCorrelationOptimization.Value;
            needsAlter = true;
        }

        if (instance.BrokerEnabled.HasValue && database.BrokerEnabled != instance.BrokerEnabled.Value)
        {
            database.BrokerEnabled = instance.BrokerEnabled.Value;
            needsAlter = true;
        }

        if (instance.IsParameterizationForced.HasValue && database.IsParameterizationForced != instance.IsParameterizationForced.Value)
        {
            database.IsParameterizationForced = instance.IsParameterizationForced.Value;
            needsAlter = true;
        }

        if (instance.TargetRecoveryTime.HasValue && database.TargetRecoveryTime != instance.TargetRecoveryTime.Value)
        {
            database.TargetRecoveryTime = instance.TargetRecoveryTime.Value;
            needsAlter = true;
        }

        if (needsAlter)
        {
            database.Alter();
        }

        // Handle special cases that require separate methods
        if (instance.EncryptionEnabled.HasValue && database.EncryptionEnabled != instance.EncryptionEnabled.Value)
        {
            database.EnableEncryption(instance.EncryptionEnabled.Value);
        }

        if (instance.IsReadCommittedSnapshotOn.HasValue && database.IsReadCommittedSnapshotOn != instance.IsReadCommittedSnapshotOn.Value)
        {
            database.SetSnapshotIsolation(instance.IsReadCommittedSnapshotOn.Value);
        }

        // Owner change needs to be done separately
        if (!string.IsNullOrEmpty(instance.Owner) &&
            !string.Equals(database.Owner, instance.Owner, StringComparison.OrdinalIgnoreCase))
        {
            database.SetOwner(instance.Owner);
        }
    }

    private static bool? GetAcceleratedRecoveryEnabled(Microsoft.SqlServer.Management.Smo.Database database)
    {
        try
        {
            return database.AcceleratedRecoveryEnabled;
        }
        catch
        {
            return null;
        }
    }
}
