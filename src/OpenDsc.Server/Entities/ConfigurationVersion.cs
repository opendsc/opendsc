// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class ConfigurationVersion
{
    public Guid Id { get; set; }
    public required Guid ConfigurationId { get; set; }
    public required string Version { get; set; }
    public Guid? ParameterSchemaId { get; set; }
    public bool IsDraft { get; set; } = true;
    public string? PrereleaseChannel { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public Configuration Configuration { get; set; } = null!;
    public ParameterSchema? ParameterSchema { get; set; }
    public ICollection<ConfigurationFile> Files { get; set; } = [];
    public ICollection<NodeConfiguration> NodeConfigurations { get; set; } = [];
}
