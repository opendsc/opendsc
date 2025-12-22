// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.OptionalFeature;

public sealed class RestartRequired
{
    [Nullable(false)]
    [ReadOnly]
    public string? System { get; set; }
}
