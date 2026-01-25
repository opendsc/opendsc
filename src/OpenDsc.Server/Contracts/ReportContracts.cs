// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using OpenDsc.Schema;

namespace OpenDsc.Server.Contracts;

/// <summary>
/// Request to submit a compliance report.
/// </summary>
public sealed class SubmitReportRequest
{
    /// <summary>
    /// The DSC operation that was performed.
    /// </summary>
    [JsonRequired]
    public DscOperation Operation { get; set; }

    /// <summary>
    /// The full DSC result from the operation.
    /// </summary>
    [JsonRequired]
    public DscResult Result { get; set; } = null!;
}

/// <summary>
/// Summary information about a report.
/// </summary>
public sealed class ReportSummary
{
    /// <summary>
    /// The report's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The node that submitted the report.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// The FQDN of the node.
    /// </summary>
    public string NodeFqdn { get; set; } = string.Empty;

    /// <summary>
    /// When the report was submitted.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// The DSC operation that was performed.
    /// </summary>
    public DscOperation Operation { get; set; }

    /// <summary>
    /// Whether all resources were in desired state.
    /// </summary>
    public bool InDesiredState { get; set; }

    /// <summary>
    /// Whether the operation had errors.
    /// </summary>
    public bool HadErrors { get; set; }
}

/// <summary>
/// Full report details including result data.
/// </summary>
public sealed class ReportDetails
{
    /// <summary>
    /// The report's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The node that submitted the report.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// The FQDN of the node.
    /// </summary>
    public string NodeFqdn { get; set; } = string.Empty;

    /// <summary>
    /// When the report was submitted.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// The DSC operation that was performed.
    /// </summary>
    public DscOperation Operation { get; set; }

    /// <summary>
    /// Whether all resources were in desired state.
    /// </summary>
    public bool InDesiredState { get; set; }

    /// <summary>
    /// Whether the operation had errors.
    /// </summary>
    public bool HadErrors { get; set; }

    /// <summary>
    /// The full DSC result.
    /// </summary>
    public DscResult? Result { get; set; }
}
