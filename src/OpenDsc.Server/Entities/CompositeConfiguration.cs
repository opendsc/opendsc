// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class CompositeConfiguration
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string EntryPoint { get; set; } = "main.dsc.yaml";
    public bool IsServerManaged { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<CompositeConfigurationVersion> Versions { get; set; } = [];
    public ICollection<NodeConfiguration> NodeConfigurations { get; set; } = [];
}
