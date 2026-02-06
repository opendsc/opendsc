// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class CompositeConfigurationVersion
{
    public Guid Id { get; set; }
    public required Guid CompositeConfigurationId { get; set; }
    public required string Version { get; set; }
    public bool IsDraft { get; set; } = true;
    public string? PrereleaseChannel { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public CompositeConfiguration CompositeConfiguration { get; set; } = null!;
    public ICollection<CompositeConfigurationItem> Items { get; set; } = [];
    public ICollection<NodeConfiguration> NodeConfigurations { get; set; } = [];
}
