// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class CompositeConfigurationItem
{
    public Guid Id { get; set; }
    public required Guid CompositeConfigurationVersionId { get; set; }
    public required Guid ChildConfigurationId { get; set; }
    public Guid? ActiveVersionId { get; set; }
    public int Order { get; set; }

    public CompositeConfigurationVersion CompositeConfigurationVersion { get; set; } = null!;
    public Configuration ChildConfiguration { get; set; } = null!;
    public ConfigurationVersion? ActiveVersion { get; set; }
}
