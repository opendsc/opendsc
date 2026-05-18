// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Health;

public sealed class HealthStatus
{
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}

public sealed class ReadinessStatus
{
    public string Status { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
