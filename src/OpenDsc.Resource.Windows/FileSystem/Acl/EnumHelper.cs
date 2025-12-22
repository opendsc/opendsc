// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using SysFileSystemRights = System.Security.AccessControl.FileSystemRights;

namespace OpenDsc.Resource.Windows.FileSystem.Acl;

internal static class EnumHelper
{
    public static T CombineFlags<T>(T[]? flags) where T : struct, Enum
    {
        if (flags == null || flags.Length == 0)
        {
            return (T)Enum.ToObject(typeof(T), 0);
        }

        int combined = 0;
        foreach (var flag in flags)
        {
            combined |= Convert.ToInt32(flag);
        }

        return (T)Enum.ToObject(typeof(T), combined);
    }

    public static T[] ExpandFlags<T>(T combinedFlags) where T : struct, Enum
    {
        var allFlags = Enum.GetValues<T>().Cast<T>();
        var result = new HashSet<T>();
        int combinedValue = Convert.ToInt32(combinedFlags);

        if (combinedValue == 0)
        {
            var zeroValue = (T)Enum.ToObject(typeof(T), 0);
            if (Enum.IsDefined(zeroValue))
            {
                return [zeroValue];
            }
            return [];
        }

        foreach (var flag in allFlags)
        {
            int flagValue = Convert.ToInt32(flag);
            if (flagValue != 0 && (combinedValue & flagValue) == flagValue)
            {
                result.Add(flag);
            }
        }

        return [.. result];
    }

    public static FileSystemRights[] ConvertFromSystem(SysFileSystemRights rights)
    {
        return ExpandFlags((FileSystemRights)(int)rights);
    }

    public static SysFileSystemRights ConvertToSystem(FileSystemRights rights)
    {
        return (SysFileSystemRights)(int)rights;
    }
}
