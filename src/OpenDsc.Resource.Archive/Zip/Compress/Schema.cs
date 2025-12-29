// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO.Compression;
using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Archive.Zip.Compress;

[Title("Zip Compress Schema")]
[Description("Schema for creating ZIP archives via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The path to the ZIP archive file.")]
    public string ArchivePath { get; set; } = string.Empty;

    [Required]
    [Description("The path to the source directory or file to archive.")]
    public string SourcePath { get; set; } = string.Empty;

    [Description("The compression level to use when creating the archive.")]
    [Nullable(false)]
    [Default("Optimal")]
    public CompressionLevel? CompressionLevel { get; set; }

    [JsonPropertyName("_inDesiredState")]
    [Description("Indicates whether the archive contents match the source.")]
    [ReadOnly]
    public bool? InDesiredState { get; set; }
}
