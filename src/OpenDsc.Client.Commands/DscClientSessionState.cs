// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Client.Commands;

public sealed class DscClientSessionState
{
    public required Uri ServerUri { get; init; }

    public required DateTimeOffset ConnectedAt { get; init; }
}
