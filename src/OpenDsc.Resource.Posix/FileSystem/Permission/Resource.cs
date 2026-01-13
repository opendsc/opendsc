// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Posix.FileSystem.Permission;

[DscResource("OpenDsc.Posix.FileSystem/Permission", "0.1.0", Description = "Manage POSIX file and directory permissions (mode, owner, group) on Linux and macOS", Tags = ["posix", "filesystem", "permissions", "chmod", "chown"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(SecurityException), Description = "Access denied")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(5, Exception = typeof(UnauthorizedAccessException), Description = "Unauthorized access")]
[ExitCode(6, Exception = typeof(FileNotFoundException), Description = "File or directory not found")]
[ExitCode(7, Exception = typeof(DirectoryNotFoundException), Description = "Directory not found")]
[ExitCode(8, Exception = typeof(PlatformNotSupportedException), Description = "Platform not supported")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>
{
    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        return JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema instance)
    {
        if (string.IsNullOrEmpty(instance.Path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(instance));
        }

        var fullPath = Path.GetFullPath(instance.Path);

        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            return new Schema
            {
                Path = instance.Path,
                Exist = false
            };
        }

        var mode = File.GetUnixFileMode(fullPath);
        var modeString = $"0{Convert.ToString((int)mode & 0xFFF, 8).PadLeft(3, '0')}";

        var stat = new PosixInterop.StatStruct();
        var result = PosixInterop.lstat(fullPath, ref stat);

        if (result != 0)
        {
            var error = Marshal.GetLastPInvokeError();
            throw new IOException($"Failed to get owner/group for '{fullPath}'. Error code: {error}");
        }

        var owner = GetUserName(stat.st_uid) ?? stat.st_uid.ToString();
        var group = GetGroupName(stat.st_gid) ?? stat.st_gid.ToString();

        return new Schema
        {
            Path = instance.Path,
            Mode = modeString,
            Owner = owner,
            Group = group
        };
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        var fullPath = Path.GetFullPath(instance.Path);

        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            throw new FileNotFoundException($"File or directory not found: {fullPath}");
        }

        bool changed = false;

        if (instance.Mode is not null)
        {
            var trimmed = instance.Mode.TrimStart('0');
            var mode = string.IsNullOrWhiteSpace(trimmed) ? 0 : (UnixFileMode)Convert.ToUInt32(trimmed, 8);
            File.SetUnixFileMode(fullPath, mode);
            changed = true;
        }

        if (instance.Owner is not null || instance.Group is not null)
        {
            var stat = new PosixInterop.StatStruct();
            var statResult = PosixInterop.lstat(fullPath, ref stat);

            if (statResult != 0)
            {
                var error = Marshal.GetLastPInvokeError();
                throw new IOException($"Failed to get current owner/group for '{fullPath}'. Error code: {error}");
            }

            var uid = instance.Owner is not null ? ResolveUserId(instance.Owner) : stat.st_uid;
            var gid = instance.Group is not null ? ResolveGroupId(instance.Group) : stat.st_gid;

            var result = PosixInterop.chown(fullPath, uid, gid);

            if (result != 0)
            {
                var error = Marshal.GetLastPInvokeError();
                throw new UnauthorizedAccessException($"Failed to change owner/group for '{fullPath}'. Error code: {error}");
            }

            changed = true;
        }

        return changed ? null : null;
    }

    private static uint ResolveUserId(string owner)
    {
        if (uint.TryParse(owner, out var uid))
        {
            return uid;
        }

        var pwPtr = PosixInterop.getpwnam(owner);
        if (pwPtr == IntPtr.Zero)
        {
            throw new ArgumentException($"User '{owner}' not found.");
        }

        var pw = Marshal.PtrToStructure<PosixInterop.PasswdStruct>(pwPtr);
        return pw.pw_uid;
    }

    private static uint ResolveGroupId(string group)
    {
        if (uint.TryParse(group, out var gid))
        {
            return gid;
        }

        var grPtr = PosixInterop.getgrnam(group);
        if (grPtr == IntPtr.Zero)
        {
            throw new ArgumentException($"Group '{group}' not found.");
        }

        var gr = Marshal.PtrToStructure<PosixInterop.GroupStruct>(grPtr);
        return gr.gr_gid;
    }

    private static string? GetUserName(uint uid)
    {
        var pwPtr = PosixInterop.getpwuid(uid);
        if (pwPtr == IntPtr.Zero)
        {
            return null;
        }

        var pw = Marshal.PtrToStructure<PosixInterop.PasswdStruct>(pwPtr);
        if (pw.pw_name == IntPtr.Zero)
        {
            return null;
        }

        return Marshal.PtrToStringUTF8(pw.pw_name);
    }

    private static string? GetGroupName(uint gid)
    {
        var grPtr = PosixInterop.getgrgid(gid);
        if (grPtr == IntPtr.Zero)
        {
            return null;
        }

        var gr = Marshal.PtrToStructure<PosixInterop.GroupStruct>(grPtr);
        if (gr.gr_name == IntPtr.Zero)
        {
            return null;
        }

        return Marshal.PtrToStringUTF8(gr.gr_name);
    }
}
