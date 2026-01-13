// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;

namespace OpenDsc.Resource.Posix.FileSystem.Permission;

internal static partial class PosixInterop
{
    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int chown(string pathname, uint owner, uint group);

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int lstat(string pathname, ref StatStruct buf);

    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr getpwnam(string name);

    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr getgrnam(string name);

    [LibraryImport("libc")]
    internal static partial IntPtr getpwuid(uint uid);

    [LibraryImport("libc")]
    internal static partial IntPtr getgrgid(uint gid);

    [StructLayout(LayoutKind.Sequential)]
    internal struct StatStruct
    {
        public ulong st_dev;
        public ulong st_ino;
        public ulong st_nlink;
        public uint st_mode;
        public uint st_uid;
        public uint st_gid;
        public uint __pad0;
        public ulong st_rdev;
        public long st_size;
        public long st_blksize;
        public long st_blocks;
        public long st_atime;
        public long st_atime_nsec;
        public long st_mtime;
        public long st_mtime_nsec;
        public long st_ctime;
        public long st_ctime_nsec;
        private long __unused1;
        private long __unused2;
        private long __unused3;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PasswdStruct
    {
        public IntPtr pw_name;
        public IntPtr pw_passwd;
        public uint pw_uid;
        public uint pw_gid;
        public IntPtr pw_gecos;
        public IntPtr pw_dir;
        public IntPtr pw_shell;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GroupStruct
    {
        public IntPtr gr_name;
        public IntPtr gr_passwd;
        public uint gr_gid;
        public IntPtr gr_mem;
    }
}
