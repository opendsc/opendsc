// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using OpenDsc.Schema;

namespace OpenDsc.Lcm;

/// <summary>
/// Request to submit a compliance report.
/// </summary>
public sealed class SubmitReportRequest
{
    /// <summary>
    /// The DSC operation that generated this report.
    /// </summary>
    [JsonRequired]
    public DscOperation Operation { get; set; }

    /// <summary>
    /// The full DSC result from the operation.
    /// </summary>
    [JsonRequired]
    public DscResult Result { get; set; } = null!;
}
