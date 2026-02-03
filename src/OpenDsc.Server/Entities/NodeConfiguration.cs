// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class NodeConfiguration
{
    public required Guid NodeId { get; set; }
    public required Guid ConfigurationId { get; set; }
    public Guid? ActiveVersionId { get; set; }
    public bool UseServerManagedParameters { get; set; } = true;
    public string? PrereleaseChannel { get; set; }
    public DateTimeOffset AssignedAt { get; set; }

    public Node Node { get; set; } = null!;
    public Configuration Configuration { get; set; } = null!;
    public ConfigurationVersion? ActiveVersion { get; set; }
}
