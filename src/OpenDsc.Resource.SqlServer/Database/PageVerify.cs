// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.Database;

[Description("Specifies the database page verification option.")]
public enum PageVerify
{
    [Description("No page verification.")]
    None,

    [Description("Use torn page detection.")]
    TornPageDetection,

    [Description("Use checksum verification.")]
    Checksum
}
