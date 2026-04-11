// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

namespace OpenDsc.Authoring.Commands;

internal static class ManifestJsonSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };
}
