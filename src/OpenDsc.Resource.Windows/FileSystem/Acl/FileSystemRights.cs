// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource.Windows.FileSystem.Acl;

// This enum duplicates System.Security.AccessControl.FileSystemRights
// to allow for JSON Schema generation with unique items constraint.
// Issue logged: https://github.com/json-everything/json-everything/issues/950
[Flags]
public enum FileSystemRights
{
    ListDirectory = 1,
    CreateFiles = 2,
    CreateDirectories = 4,
    ReadExtendedAttributes = 8,
    WriteExtendedAttributes = 16,
    Traverse = 32,
    DeleteSubdirectoriesAndFiles = 64,
    ReadAttributes = 128,
    WriteAttributes = 256,
    Write = 278,
    Delete = 65536,
    ReadPermissions = 131072,
    Read = 131209,
    ReadAndExecute = 131241,
    Modify = 197055,
    ChangePermissions = 262144,
    TakeOwnership = 524288,
    Synchronize = 1048576,
    FullControl = 2032127
}
