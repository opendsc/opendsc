// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class ScopeType
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int Precedence { get; set; }
    public bool IsSystem { get; set; }
    public ScopeValueMode ValueMode { get; set; } = ScopeValueMode.Unrestricted;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<ScopeValue> ScopeValues { get; set; } = [];
    public ICollection<ParameterFile> ParameterFiles { get; set; } = [];
}
