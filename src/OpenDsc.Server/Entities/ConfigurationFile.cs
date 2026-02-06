// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class ConfigurationFile
{
    public Guid Id { get; set; }
    public required Guid VersionId { get; set; }
    public required string RelativePath { get; set; }
    public string? ContentType { get; set; }
    public required string Checksum { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ConfigurationVersion Version { get; set; } = null!;
}
