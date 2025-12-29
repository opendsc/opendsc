// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Archive.Zip.Expand;

[Title("Zip Expand Schema")]
[Description("Schema for extracting ZIP archives via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The path to the ZIP archive file.")]
    public string ArchivePath { get; set; } = string.Empty;

    [Required]
    [Description("The path to the destination directory where archive contents will be extracted.")]
    public string DestinationPath { get; set; } = string.Empty;

    [JsonPropertyName("_inDesiredState")]
    [Description("Indicates whether the destination contains all files from the archive with matching checksums.")]
    [ReadOnly]
    public bool? InDesiredState { get; set; }
}
