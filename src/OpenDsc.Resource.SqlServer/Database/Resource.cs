// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using Microsoft.SqlServer.Management.Smo;

using SmoCompatibilityLevel = Microsoft.SqlServer.Management.Smo.CompatibilityLevel;
using SmoContainmentType = Microsoft.SqlServer.Management.Smo.ContainmentType;
using SmoPageVerify = Microsoft.SqlServer.Management.Smo.PageVerify;
using SmoRecoveryModel = Microsoft.SqlServer.Management.Smo.RecoveryModel;
using SmoUserAccess = Microsoft.SqlServer.Management.Smo.DatabaseUserAccess;

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
      ITestable<Schema>,
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
                CompatibilityLevel = MapCompatibilityLevel(database.CompatibilityLevel),
                RecoveryModel = MapRecoveryModel(database.RecoveryModel),
                Owner = database.Owner,

                // Access and state options
                ReadOnly = database.ReadOnly,
                UserAccess = MapUserAccess(database.UserAccess),
                PageVerify = MapPageVerify(database.PageVerify),
                ContainmentType = MapContainmentType(database.ContainmentType),

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
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

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

    public TestResult<Schema> Test(Schema instance)
    {
        var actualState = Get(instance);
        bool inDesiredState = true;

        if (instance.Exist == false)
        {
            inDesiredState = actualState.Exist == false;
        }
        else if (actualState.Exist == false)
        {
            inDesiredState = false;
        }
        else
        {
            // Database options
            if (!string.IsNullOrEmpty(instance.Collation) &&
                !string.Equals(actualState.Collation, instance.Collation, StringComparison.OrdinalIgnoreCase))
            {
                inDesiredState = false;
            }

            if (instance.CompatibilityLevel.HasValue && actualState.CompatibilityLevel != instance.CompatibilityLevel)
            {
                inDesiredState = false;
            }

            if (instance.RecoveryModel.HasValue && actualState.RecoveryModel != instance.RecoveryModel)
            {
                inDesiredState = false;
            }

            if (!string.IsNullOrEmpty(instance.Owner) &&
                !string.Equals(actualState.Owner, instance.Owner, StringComparison.OrdinalIgnoreCase))
            {
                inDesiredState = false;
            }

            // Access and state options
            if (instance.ReadOnly.HasValue && actualState.ReadOnly != instance.ReadOnly)
            {
                inDesiredState = false;
            }

            if (instance.UserAccess.HasValue && actualState.UserAccess != instance.UserAccess)
            {
                inDesiredState = false;
            }

            if (instance.PageVerify.HasValue && actualState.PageVerify != instance.PageVerify)
            {
                inDesiredState = false;
            }

            if (instance.ContainmentType.HasValue && actualState.ContainmentType != instance.ContainmentType)
            {
                inDesiredState = false;
            }

            // ANSI options
            if (instance.AnsiNullDefault.HasValue && actualState.AnsiNullDefault != instance.AnsiNullDefault)
            {
                inDesiredState = false;
            }

            if (instance.AnsiNullsEnabled.HasValue && actualState.AnsiNullsEnabled != instance.AnsiNullsEnabled)
            {
                inDesiredState = false;
            }

            if (instance.AnsiPaddingEnabled.HasValue && actualState.AnsiPaddingEnabled != instance.AnsiPaddingEnabled)
            {
                inDesiredState = false;
            }

            if (instance.AnsiWarningsEnabled.HasValue && actualState.AnsiWarningsEnabled != instance.AnsiWarningsEnabled)
            {
                inDesiredState = false;
            }

            if (instance.ArithmeticAbortEnabled.HasValue && actualState.ArithmeticAbortEnabled != instance.ArithmeticAbortEnabled)
            {
                inDesiredState = false;
            }

            if (instance.ConcatenateNullYieldsNull.HasValue && actualState.ConcatenateNullYieldsNull != instance.ConcatenateNullYieldsNull)
            {
                inDesiredState = false;
            }

            if (instance.NumericRoundAbortEnabled.HasValue && actualState.NumericRoundAbortEnabled != instance.NumericRoundAbortEnabled)
            {
                inDesiredState = false;
            }

            if (instance.QuotedIdentifiersEnabled.HasValue && actualState.QuotedIdentifiersEnabled != instance.QuotedIdentifiersEnabled)
            {
                inDesiredState = false;
            }

            // Auto options
            if (instance.AutoClose.HasValue && actualState.AutoClose != instance.AutoClose)
            {
                inDesiredState = false;
            }

            if (instance.AutoShrink.HasValue && actualState.AutoShrink != instance.AutoShrink)
            {
                inDesiredState = false;
            }

            if (instance.AutoCreateStatisticsEnabled.HasValue && actualState.AutoCreateStatisticsEnabled != instance.AutoCreateStatisticsEnabled)
            {
                inDesiredState = false;
            }

            if (instance.AutoUpdateStatisticsEnabled.HasValue && actualState.AutoUpdateStatisticsEnabled != instance.AutoUpdateStatisticsEnabled)
            {
                inDesiredState = false;
            }

            if (instance.AutoUpdateStatisticsAsync.HasValue && actualState.AutoUpdateStatisticsAsync != instance.AutoUpdateStatisticsAsync)
            {
                inDesiredState = false;
            }

            // Cursor options
            if (instance.CloseCursorsOnCommitEnabled.HasValue && actualState.CloseCursorsOnCommitEnabled != instance.CloseCursorsOnCommitEnabled)
            {
                inDesiredState = false;
            }

            if (instance.LocalCursorsDefault.HasValue && actualState.LocalCursorsDefault != instance.LocalCursorsDefault)
            {
                inDesiredState = false;
            }

            // Trigger options
            if (instance.NestedTriggersEnabled.HasValue && actualState.NestedTriggersEnabled != instance.NestedTriggersEnabled)
            {
                inDesiredState = false;
            }

            if (instance.RecursiveTriggersEnabled.HasValue && actualState.RecursiveTriggersEnabled != instance.RecursiveTriggersEnabled)
            {
                inDesiredState = false;
            }

            // Advanced options
            if (instance.Trustworthy.HasValue && actualState.Trustworthy != instance.Trustworthy)
            {
                inDesiredState = false;
            }

            if (instance.DatabaseOwnershipChaining.HasValue && actualState.DatabaseOwnershipChaining != instance.DatabaseOwnershipChaining)
            {
                inDesiredState = false;
            }

            if (instance.DateCorrelationOptimization.HasValue && actualState.DateCorrelationOptimization != instance.DateCorrelationOptimization)
            {
                inDesiredState = false;
            }

            if (instance.BrokerEnabled.HasValue && actualState.BrokerEnabled != instance.BrokerEnabled)
            {
                inDesiredState = false;
            }

            if (instance.EncryptionEnabled.HasValue && actualState.EncryptionEnabled != instance.EncryptionEnabled)
            {
                inDesiredState = false;
            }

            if (instance.IsParameterizationForced.HasValue && actualState.IsParameterizationForced != instance.IsParameterizationForced)
            {
                inDesiredState = false;
            }

            if (instance.IsReadCommittedSnapshotOn.HasValue && actualState.IsReadCommittedSnapshotOn != instance.IsReadCommittedSnapshotOn)
            {
                inDesiredState = false;
            }

            if (instance.IsFullTextEnabled.HasValue && actualState.IsFullTextEnabled != instance.IsFullTextEnabled)
            {
                inDesiredState = false;
            }

            if (instance.TargetRecoveryTime.HasValue && actualState.TargetRecoveryTime != instance.TargetRecoveryTime)
            {
                inDesiredState = false;
            }

            if (instance.AcceleratedRecoveryEnabled.HasValue && actualState.AcceleratedRecoveryEnabled != instance.AcceleratedRecoveryEnabled)
            {
                inDesiredState = false;
            }
        }

        actualState.InDesiredState = inDesiredState;

        return new TestResult<Schema>(actualState);
    }

    public void Delete(Schema instance)
    {
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

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
        var username = Environment.GetEnvironmentVariable("SQLSERVER_USERNAME");
        var password = Environment.GetEnvironmentVariable("SQLSERVER_PASSWORD");
        return Export(serverInstance, username, password);
    }

    public IEnumerable<Schema> Export(string serverInstance, string? username = null, string? password = null)
    {
        var server = SqlConnectionHelper.CreateConnection(serverInstance, username, password);

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
                    CompatibilityLevel = MapCompatibilityLevel(database.CompatibilityLevel),
                    RecoveryModel = MapRecoveryModel(database.RecoveryModel),
                    Owner = database.Owner,
                    ReadOnly = database.ReadOnly,
                    UserAccess = MapUserAccess(database.UserAccess),
                    PageVerify = MapPageVerify(database.PageVerify),
                    ContainmentType = MapContainmentType(database.ContainmentType),
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
            database.ContainmentType = MapToSmoContainmentType(instance.ContainmentType.Value);
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
                primaryFile.Size = instance.PrimaryFileSize.Value * 1024; // Convert MB to KB
            }

            if (instance.PrimaryFileGrowth.HasValue)
            {
                primaryFile.Growth = instance.PrimaryFileGrowth.Value * 1024; // Convert MB to KB
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
                logFile.Size = instance.LogFileSize.Value * 1024; // Convert MB to KB
            }

            if (instance.LogFileGrowth.HasValue)
            {
                logFile.Growth = instance.LogFileGrowth.Value * 1024; // Convert MB to KB
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
            var smoRecoveryModel = MapToSmoRecoveryModel(instance.RecoveryModel.Value);
            if (database.RecoveryModel != smoRecoveryModel)
            {
                database.RecoveryModel = smoRecoveryModel;
                needsAlter = true;
            }
        }

        // Compatibility level
        if (instance.CompatibilityLevel.HasValue)
        {
            var smoCompatLevel = MapToSmoCompatibilityLevel(instance.CompatibilityLevel.Value);
            if (database.CompatibilityLevel != smoCompatLevel)
            {
                database.CompatibilityLevel = smoCompatLevel;
                needsAlter = true;
            }
        }

        // User access
        if (instance.UserAccess.HasValue)
        {
            var smoUserAccess = MapToSmoUserAccess(instance.UserAccess.Value);
            if (database.UserAccess != smoUserAccess)
            {
                database.UserAccess = smoUserAccess;
                needsAlter = true;
            }
        }

        // Page verify
        if (instance.PageVerify.HasValue)
        {
            var smoPageVerify = MapToSmoPageVerify(instance.PageVerify.Value);
            if (database.PageVerify != smoPageVerify)
            {
                database.PageVerify = smoPageVerify;
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

    private static CompatibilityLevel? MapCompatibilityLevel(SmoCompatibilityLevel level) => level switch
    {
        SmoCompatibilityLevel.Version100 => CompatibilityLevel.Version100,
        SmoCompatibilityLevel.Version110 => CompatibilityLevel.Version110,
        SmoCompatibilityLevel.Version120 => CompatibilityLevel.Version120,
        SmoCompatibilityLevel.Version130 => CompatibilityLevel.Version130,
        SmoCompatibilityLevel.Version140 => CompatibilityLevel.Version140,
        SmoCompatibilityLevel.Version150 => CompatibilityLevel.Version150,
        SmoCompatibilityLevel.Version160 => CompatibilityLevel.Version160,
        _ => null
    };

    private static SmoCompatibilityLevel MapToSmoCompatibilityLevel(CompatibilityLevel level) => level switch
    {
        CompatibilityLevel.Version100 => SmoCompatibilityLevel.Version100,
        CompatibilityLevel.Version110 => SmoCompatibilityLevel.Version110,
        CompatibilityLevel.Version120 => SmoCompatibilityLevel.Version120,
        CompatibilityLevel.Version130 => SmoCompatibilityLevel.Version130,
        CompatibilityLevel.Version140 => SmoCompatibilityLevel.Version140,
        CompatibilityLevel.Version150 => SmoCompatibilityLevel.Version150,
        CompatibilityLevel.Version160 => SmoCompatibilityLevel.Version160,
        _ => SmoCompatibilityLevel.Version160
    };

    private static RecoveryModel? MapRecoveryModel(SmoRecoveryModel model) => model switch
    {
        SmoRecoveryModel.Full => RecoveryModel.Full,
        SmoRecoveryModel.BulkLogged => RecoveryModel.BulkLogged,
        SmoRecoveryModel.Simple => RecoveryModel.Simple,
        _ => null
    };

    private static SmoRecoveryModel MapToSmoRecoveryModel(RecoveryModel model) => model switch
    {
        RecoveryModel.Full => SmoRecoveryModel.Full,
        RecoveryModel.BulkLogged => SmoRecoveryModel.BulkLogged,
        RecoveryModel.Simple => SmoRecoveryModel.Simple,
        _ => SmoRecoveryModel.Full
    };

    private static UserAccess? MapUserAccess(SmoUserAccess access) => access switch
    {
        SmoUserAccess.Multiple => UserAccess.Multiple,
        SmoUserAccess.Single => UserAccess.Single,
        SmoUserAccess.Restricted => UserAccess.Restricted,
        _ => null
    };

    private static SmoUserAccess MapToSmoUserAccess(UserAccess access) => access switch
    {
        UserAccess.Multiple => SmoUserAccess.Multiple,
        UserAccess.Single => SmoUserAccess.Single,
        UserAccess.Restricted => SmoUserAccess.Restricted,
        _ => SmoUserAccess.Multiple
    };

    private static PageVerify? MapPageVerify(SmoPageVerify verify) => verify switch
    {
        SmoPageVerify.None => PageVerify.None,
        SmoPageVerify.TornPageDetection => PageVerify.TornPageDetection,
        SmoPageVerify.Checksum => PageVerify.Checksum,
        _ => null
    };

    private static SmoPageVerify MapToSmoPageVerify(PageVerify verify) => verify switch
    {
        PageVerify.None => SmoPageVerify.None,
        PageVerify.TornPageDetection => SmoPageVerify.TornPageDetection,
        PageVerify.Checksum => SmoPageVerify.Checksum,
        _ => SmoPageVerify.Checksum
    };

    private static ContainmentType? MapContainmentType(SmoContainmentType type) => type switch
    {
        SmoContainmentType.None => ContainmentType.None,
        SmoContainmentType.Partial => ContainmentType.Partial,
        _ => null
    };

    private static SmoContainmentType MapToSmoContainmentType(ContainmentType type) => type switch
    {
        ContainmentType.None => SmoContainmentType.None,
        ContainmentType.Partial => SmoContainmentType.Partial,
        _ => SmoContainmentType.None
    };
}
