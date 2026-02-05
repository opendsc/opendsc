// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class ParameterFile
{
    public Guid Id { get; set; }
    public required Guid ParameterSchemaId { get; set; }
    public required Guid ScopeTypeId { get; set; }
    public string? ScopeValue { get; set; }
    public required string Version { get; set; }
    public string? ContentType { get; set; }
    public required string Checksum { get; set; }
    public bool IsDraft { get; set; } = true;
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public ParameterSchema ParameterSchema { get; set; } = null!;
    public ScopeType ScopeType { get; set; } = null!;
}
