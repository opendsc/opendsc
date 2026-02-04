// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class ScopeValue
{
    public Guid Id { get; set; }
    public required Guid ScopeTypeId { get; set; }
    public required string Value { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ScopeType ScopeType { get; set; } = null!;
    public ICollection<NodeTag> NodeTags { get; set; } = [];
}
