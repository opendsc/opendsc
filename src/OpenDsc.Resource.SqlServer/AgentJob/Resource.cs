// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using Microsoft.SqlServer.Management.Smo.Agent;

namespace OpenDsc.Resource.SqlServer.AgentJob;

[DscResource("OpenDsc.SqlServer/AgentJob", "0.1.0", Description = "Manage SQL Server Agent jobs", Tags = ["sql", "sqlserver", "agent", "job"])]
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
            var job = server.JobServer.Jobs.Cast<Job>()
                .FirstOrDefault(j => string.Equals(j.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (job == null)
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
                Name = job.Name,
                Description = string.IsNullOrEmpty(job.Description) ? null : job.Description,
                IsEnabled = job.IsEnabled,
                Category = job.Category,
                OwnerLoginName = job.OwnerLoginName,
                StartStepId = job.StartStepID,
                EmailLevel = job.EmailLevel,
                OperatorToEmail = string.IsNullOrEmpty(job.OperatorToEmail) ? null : job.OperatorToEmail,
                PageLevel = job.PageLevel,
                OperatorToPage = string.IsNullOrEmpty(job.OperatorToPage) ? null : job.OperatorToPage,
                NetSendLevel = job.NetSendLevel,
                OperatorToNetSend = string.IsNullOrEmpty(job.OperatorToNetSend) ? null : job.OperatorToNetSend,
                EventLogLevel = job.EventLogLevel,
                DeleteLevel = job.DeleteLevel,
                JobId = job.JobID,
                DateCreated = job.DateCreated,
                DateLastModified = job.DateLastModified,
                LastRunDate = job.LastRunDate == DateTime.MinValue ? null : job.LastRunDate,
                LastRunOutcome = job.LastRunOutcome,
                NextRunDate = job.NextRunDate == DateTime.MinValue ? null : job.NextRunDate,
                CurrentRunStatus = job.CurrentRunStatus,
                CurrentRunStep = job.CurrentRunStep,
                CurrentRunRetryAttempt = job.CurrentRunRetryAttempt,
                HasStep = job.HasStep,
                HasSchedule = job.HasSchedule,
                VersionNumber = job.VersionNumber
            };
        }
        finally
        {
            SqlConnectionHelper.SafeDisconnect(server);
        }
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var job = server.JobServer.Jobs.Cast<Job>()
                .FirstOrDefault(j => string.Equals(j.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (job == null)
            {
                CreateJob(server.JobServer, instance);
            }
            else
            {
                UpdateJob(job, instance);
            }

            return null;
        }
        finally
        {
            SqlConnectionHelper.SafeDisconnect(server);
        }
    }

    public void Delete(Schema instance)
    {
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var job = server.JobServer.Jobs.Cast<Job>()
                .FirstOrDefault(j => string.Equals(j.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            job?.Drop();
        }
        finally
        {
            SqlConnectionHelper.SafeDisconnect(server);
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
            var jobs = new List<Schema>();
            foreach (Job job in server.JobServer.Jobs)
            {
                jobs.Add(new Schema
                {
                    ServerInstance = serverInstance,
                    Name = job.Name,
                    Description = string.IsNullOrEmpty(job.Description) ? null : job.Description,
                    IsEnabled = job.IsEnabled,
                    Category = job.Category,
                    OwnerLoginName = job.OwnerLoginName,
                    StartStepId = job.StartStepID,
                    EmailLevel = job.EmailLevel,
                    OperatorToEmail = string.IsNullOrEmpty(job.OperatorToEmail) ? null : job.OperatorToEmail,
                    PageLevel = job.PageLevel,
                    OperatorToPage = string.IsNullOrEmpty(job.OperatorToPage) ? null : job.OperatorToPage,
                    NetSendLevel = job.NetSendLevel,
                    OperatorToNetSend = string.IsNullOrEmpty(job.OperatorToNetSend) ? null : job.OperatorToNetSend,
                    EventLogLevel = job.EventLogLevel,
                    DeleteLevel = job.DeleteLevel,
                    JobId = job.JobID,
                    DateCreated = job.DateCreated,
                    DateLastModified = job.DateLastModified,
                    LastRunDate = job.LastRunDate == DateTime.MinValue ? null : job.LastRunDate,
                    LastRunOutcome = job.LastRunOutcome,
                    NextRunDate = job.NextRunDate == DateTime.MinValue ? null : job.NextRunDate,
                    CurrentRunStatus = job.CurrentRunStatus,
                    CurrentRunStep = job.CurrentRunStep,
                    CurrentRunRetryAttempt = job.CurrentRunRetryAttempt,
                    HasStep = job.HasStep,
                    HasSchedule = job.HasSchedule,
                    VersionNumber = job.VersionNumber
                });
            }

            return jobs;
        }
        finally
        {
            SqlConnectionHelper.SafeDisconnect(server);
        }
    }

    private static void CreateJob(JobServer jobServer, Schema instance)
    {
        var job = new Job(jobServer, instance.Name);

        if (!string.IsNullOrEmpty(instance.Description))
        {
            job.Description = instance.Description;
        }

        if (instance.IsEnabled.HasValue)
        {
            job.IsEnabled = instance.IsEnabled.Value;
        }

        if (!string.IsNullOrEmpty(instance.Category))
        {
            job.Category = instance.Category;
        }

        if (!string.IsNullOrEmpty(instance.OwnerLoginName))
        {
            job.OwnerLoginName = instance.OwnerLoginName;
        }

        if (instance.StartStepId.HasValue)
        {
            job.StartStepID = instance.StartStepId.Value;
        }

        if (instance.EmailLevel.HasValue)
        {
            job.EmailLevel = instance.EmailLevel.Value;
        }

        if (!string.IsNullOrEmpty(instance.OperatorToEmail))
        {
            job.OperatorToEmail = instance.OperatorToEmail;
        }

        if (instance.PageLevel.HasValue)
        {
            job.PageLevel = instance.PageLevel.Value;
        }

        if (!string.IsNullOrEmpty(instance.OperatorToPage))
        {
            job.OperatorToPage = instance.OperatorToPage;
        }

        if (instance.NetSendLevel.HasValue)
        {
            job.NetSendLevel = instance.NetSendLevel.Value;
        }

        if (!string.IsNullOrEmpty(instance.OperatorToNetSend))
        {
            job.OperatorToNetSend = instance.OperatorToNetSend;
        }

        if (instance.EventLogLevel.HasValue)
        {
            job.EventLogLevel = instance.EventLogLevel.Value;
        }

        if (instance.DeleteLevel.HasValue)
        {
            job.DeleteLevel = instance.DeleteLevel.Value;
        }

        job.Create();
    }

    private static void UpdateJob(Job job, Schema instance)
    {
        bool altered = false;

        if (!string.IsNullOrEmpty(instance.Description) &&
            !string.Equals(job.Description, instance.Description, StringComparison.Ordinal))
        {
            job.Description = instance.Description;
            altered = true;
        }

        if (instance.IsEnabled.HasValue && job.IsEnabled != instance.IsEnabled.Value)
        {
            job.IsEnabled = instance.IsEnabled.Value;
            altered = true;
        }

        if (!string.IsNullOrEmpty(instance.Category) &&
            !string.Equals(job.Category, instance.Category, StringComparison.OrdinalIgnoreCase))
        {
            job.Category = instance.Category;
            altered = true;
        }

        if (!string.IsNullOrEmpty(instance.OwnerLoginName) &&
            !string.Equals(job.OwnerLoginName, instance.OwnerLoginName, StringComparison.OrdinalIgnoreCase))
        {
            job.OwnerLoginName = instance.OwnerLoginName;
            altered = true;
        }

        if (instance.StartStepId.HasValue && job.StartStepID != instance.StartStepId.Value)
        {
            job.StartStepID = instance.StartStepId.Value;
            altered = true;
        }

        if (instance.EmailLevel.HasValue && job.EmailLevel != instance.EmailLevel.Value)
        {
            job.EmailLevel = instance.EmailLevel.Value;
            altered = true;
        }

        if (!string.IsNullOrEmpty(instance.OperatorToEmail) &&
            !string.Equals(job.OperatorToEmail, instance.OperatorToEmail, StringComparison.OrdinalIgnoreCase))
        {
            job.OperatorToEmail = instance.OperatorToEmail;
            altered = true;
        }

        if (instance.PageLevel.HasValue && job.PageLevel != instance.PageLevel.Value)
        {
            job.PageLevel = instance.PageLevel.Value;
            altered = true;
        }

        if (!string.IsNullOrEmpty(instance.OperatorToPage) &&
            !string.Equals(job.OperatorToPage, instance.OperatorToPage, StringComparison.OrdinalIgnoreCase))
        {
            job.OperatorToPage = instance.OperatorToPage;
            altered = true;
        }

        if (instance.NetSendLevel.HasValue && job.NetSendLevel != instance.NetSendLevel.Value)
        {
            job.NetSendLevel = instance.NetSendLevel.Value;
            altered = true;
        }

        if (!string.IsNullOrEmpty(instance.OperatorToNetSend) &&
            !string.Equals(job.OperatorToNetSend, instance.OperatorToNetSend, StringComparison.OrdinalIgnoreCase))
        {
            job.OperatorToNetSend = instance.OperatorToNetSend;
            altered = true;
        }

        if (instance.EventLogLevel.HasValue && job.EventLogLevel != instance.EventLogLevel.Value)
        {
            job.EventLogLevel = instance.EventLogLevel.Value;
            altered = true;
        }

        if (instance.DeleteLevel.HasValue && job.DeleteLevel != instance.DeleteLevel.Value)
        {
            job.DeleteLevel = instance.DeleteLevel.Value;
            altered = true;
        }

        if (altered)
        {
            job.Alter();
        }
    }
}
