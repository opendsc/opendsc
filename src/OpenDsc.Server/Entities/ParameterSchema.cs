// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class ParameterSchema
{
    public Guid Id { get; set; }
    public required Guid ConfigurationId { get; set; }
    public required string SchemaHash { get; set; }
    public required string SchemaDefinition { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Configuration Configuration { get; set; } = null!;
    public ICollection<ConfigurationVersion> ConfigurationVersions { get; set; } = [];
    public ICollection<ParameterFile> ParameterFiles { get; set; } = [];
}
