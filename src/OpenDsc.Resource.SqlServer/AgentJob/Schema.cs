// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

using Microsoft.SqlServer.Management.Smo.Agent;

namespace OpenDsc.Resource.SqlServer.AgentJob;

[Title("SQL Server Agent Job Schema")]
[Description("Schema for managing SQL Server Agent jobs via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The name of the SQL Server instance to connect to. Use '.' or '(local)' for the default local instance, or 'servername\\instancename' for named instances.")]
    [Pattern(@"^.+$")]
    public string ServerInstance { get; set; } = string.Empty;

    [Description("The username for SQL Server authentication when connecting to the server. If not specified, Windows Authentication is used.")]
    [Nullable(false)]
    [WriteOnly]
    public string? ConnectUsername { get; set; }

    [Description("The password for SQL Server authentication when connecting to the server. Required when ConnectUsername is specified.")]
    [Nullable(false)]
    [WriteOnly]
    public string? ConnectPassword { get; set; }

    [Description("The name of the Agent job.")]
    [Pattern(@"^.+$")]
    public string Name { get; set; } = string.Empty;

    [Description("Indicates whether the job should exist.")]
    [JsonPropertyName("_exist")]
    [Default(true)]
    [Nullable(false)]
    public bool? Exist { get; set; }

    [Description("The description of the job.")]
    [Nullable(false)]
    public string? Description { get; set; }

    [Description("Whether the job is enabled.")]
    [Nullable(false)]
    public bool? IsEnabled { get; set; }

    [Description("The category of the job.")]
    [Nullable(false)]
    public string? Category { get; set; }

    [Description("The login name of the job owner.")]
    [Nullable(false)]
    public string? OwnerLoginName { get; set; }

    [Description("The job step ID at which execution starts.")]
    [Nullable(false)]
    [Minimum(1)]
    public int? StartStepId { get; set; }

    [Description("The action to take when the job completes to send an email notification.")]
    [Nullable(false)]
    public CompletionAction? EmailLevel { get; set; }

    [Description("The operator to email when EmailLevel condition is met.")]
    [Nullable(false)]
    public string? OperatorToEmail { get; set; }

    [Description("The action to take when the job completes to send a page notification.")]
    [Nullable(false)]
    public CompletionAction? PageLevel { get; set; }

    [Description("The operator to page when PageLevel condition is met.")]
    [Nullable(false)]
    public string? OperatorToPage { get; set; }

    [Description("The action to take when the job completes to send a net send notification.")]
    [Nullable(false)]
    public CompletionAction? NetSendLevel { get; set; }

    [Description("The operator for net send when NetSendLevel condition is met.")]
    [Nullable(false)]
    public string? OperatorToNetSend { get; set; }

    [Description("The action to take when the job completes to write to the Windows Application event log.")]
    [Nullable(false)]
    public CompletionAction? EventLogLevel { get; set; }

    [Description("The action to take when the job completes to delete the job.")]
    [Nullable(false)]
    public CompletionAction? DeleteLevel { get; set; }

    [ReadOnly]
    [Description("The unique identifier of the job.")]
    public Guid? JobId { get; set; }

    [ReadOnly]
    [Description("The date the job was created.")]
    public DateTime? DateCreated { get; set; }

    [ReadOnly]
    [Description("The date the job was last modified.")]
    public DateTime? DateLastModified { get; set; }

    [ReadOnly]
    [Description("The date the job was last run.")]
    public DateTime? LastRunDate { get; set; }

    [ReadOnly]
    [Description("The outcome of the last job run.")]
    public CompletionResult? LastRunOutcome { get; set; }

    [ReadOnly]
    [Description("The date of the next scheduled run.")]
    public DateTime? NextRunDate { get; set; }

    [ReadOnly]
    [Description("The current execution status of the job.")]
    public JobExecutionStatus? CurrentRunStatus { get; set; }

    [ReadOnly]
    [Description("The current step being executed.")]
    public string? CurrentRunStep { get; set; }

    [ReadOnly]
    [Description("The current retry attempt number.")]
    public int? CurrentRunRetryAttempt { get; set; }

    [ReadOnly]
    [Description("Whether the job has any steps defined.")]
    public bool? HasStep { get; set; }

    [ReadOnly]
    [Description("Whether the job has any schedules defined.")]
    public bool? HasSchedule { get; set; }

    [ReadOnly]
    [Description("The job version number.")]
    public int? VersionNumber { get; set; }
}
