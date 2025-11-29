// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource.CommandLine;

internal interface ISerializer<TSchema>
{
    string Serialize(TSchema item);
    string SerializeManifest(DscResourceManifest manifest);
    string SerializeHashSet(HashSet<string> set);
    TSchema Deserialize(string json);
}
