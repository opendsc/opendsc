// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Buffers.Binary;
using System.IO.Hashing;

namespace OpenDsc.Resource.Archive.Zip;

internal static class ZipHelper
{
    internal static uint ComputeCrc32(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var crc32 = new Crc32();
        crc32.Append(stream);
        var hash = crc32.GetHashAndReset();
        return BinaryPrimitives.ReadUInt32LittleEndian(hash);
    }
}
