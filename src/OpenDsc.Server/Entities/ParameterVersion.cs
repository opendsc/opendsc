// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class ParameterVersion
{
    public Guid Id { get; set; }
    public required Guid ScopeId { get; set; }
    public required Guid ConfigurationId { get; set; }
    public required string Version { get; set; }
    public string? ContentType { get; set; }
    public required string Checksum { get; set; }
    public bool IsDraft { get; set; } = true;
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public Scope Scope { get; set; } = null!;
    public Configuration Configuration { get; set; } = null!;
}
