// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class NodeScopeAssignment
{
    public required Guid NodeId { get; set; }
    public required Guid ScopeId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }

    public Node Node { get; set; } = null!;
    public Scope Scope { get; set; } = null!;
}
