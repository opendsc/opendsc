// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Health;

/// <summary>
/// Live health check response indicating service availability.
/// </summary>
public sealed class HealthStatus
{
    /// <summary>
    /// Current health status string (e.g., "Healthy", "Unhealthy").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of the health check.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Readiness check response indicating whether the service is ready to accept requests.
/// </summary>
public sealed class ReadinessStatus
{
    /// <summary>
    /// Current readiness status string (e.g., "Ready", "NotReady").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Database connectivity status string.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of the readiness check.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
