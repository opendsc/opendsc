// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.PasswordPolicy;

[DscResource("OpenDsc.Windows/PasswordPolicy", "0.1.0", Description = "Manage Windows password policy settings", Tags = ["windows", "security", "password", "policy"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(Win32Exception), Description = "Win32 API error")]
[ExitCode(3, Exception = typeof(UnauthorizedAccessException), Description = "Access denied - requires administrator privileges")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>
{
    private const uint NERR_Success = 0;
    private const uint ERROR_ACCESS_DENIED = 5;
    private const uint ERROR_INVALID_PARAMETER = 87;
    private const uint TIMEQ_FOREVER = 0xFFFFFFFF;

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

    public Schema Get(Schema? instance)
    {
        IntPtr buffer = IntPtr.Zero;

        try
        {
            uint result = NetUserModalsGet(null, 0, out buffer);

            if (result == ERROR_ACCESS_DENIED)
            {
                throw new UnauthorizedAccessException("Access denied. Administrator privileges are required to read password policy.");
            }

            if (result != NERR_Success)
            {
                throw new Win32Exception((int)result, $"Failed to get password policy. Error code: {result}");
            }

            var info = Marshal.PtrToStructure<USER_MODALS_INFO_0>(buffer);

            return new Schema
            {
                MinimumPasswordLength = info.usrmod0_min_passwd_len,
                MaximumPasswordAgeDays = info.usrmod0_max_passwd_age == TIMEQ_FOREVER ? 0 : info.usrmod0_max_passwd_age / 86400,
                MinimumPasswordAgeDays = info.usrmod0_min_passwd_age / 86400,
                PasswordHistoryLength = info.usrmod0_password_hist_len
            };
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                NetApiBufferFree(buffer);
            }
        }
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        if (instance is null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var current = Get(instance);
        bool changed = false;

        IntPtr buffer = IntPtr.Zero;
        uint currentForceLogoff = TIMEQ_FOREVER;

        try
        {
            uint getResult = NetUserModalsGet(null, 0, out buffer);
            if (getResult == NERR_Success)
            {
                var currentInfo = Marshal.PtrToStructure<USER_MODALS_INFO_0>(buffer);
                currentForceLogoff = currentInfo.usrmod0_force_logoff;
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                NetApiBufferFree(buffer);
            }
        }

        var info = new USER_MODALS_INFO_0
        {
            usrmod0_min_passwd_len = instance.MinimumPasswordLength ?? current.MinimumPasswordLength!.Value,
            usrmod0_max_passwd_age = instance.MaximumPasswordAgeDays.HasValue
                ? (instance.MaximumPasswordAgeDays.Value == 0 ? TIMEQ_FOREVER : instance.MaximumPasswordAgeDays.Value * 86400)
                : (current.MaximumPasswordAgeDays == 0 ? TIMEQ_FOREVER : current.MaximumPasswordAgeDays!.Value * 86400),
            usrmod0_min_passwd_age = (instance.MinimumPasswordAgeDays ?? current.MinimumPasswordAgeDays!.Value) * 86400,
            usrmod0_force_logoff = currentForceLogoff,
            usrmod0_password_hist_len = instance.PasswordHistoryLength ?? current.PasswordHistoryLength!.Value
        };

        if (instance.MinimumPasswordLength.HasValue && instance.MinimumPasswordLength != current.MinimumPasswordLength)
        {
            changed = true;
        }

        if (instance.MaximumPasswordAgeDays.HasValue && instance.MaximumPasswordAgeDays != current.MaximumPasswordAgeDays)
        {
            changed = true;
        }

        if (instance.MinimumPasswordAgeDays.HasValue && instance.MinimumPasswordAgeDays != current.MinimumPasswordAgeDays)
        {
            changed = true;
        }

        if (instance.PasswordHistoryLength.HasValue && instance.PasswordHistoryLength != current.PasswordHistoryLength)
        {
            changed = true;
        }

        if (!changed)
        {
            return null;
        }

        uint result = NetUserModalsSet(null, 0, ref info, out _);

        if (result == ERROR_ACCESS_DENIED)
        {
            throw new UnauthorizedAccessException("Access denied. Administrator privileges are required to set password policy.");
        }

        if (result == ERROR_INVALID_PARAMETER)
        {
            throw new ArgumentException($"Invalid password policy parameter.");
        }

        if (result != NERR_Success)
        {
            throw new Win32Exception((int)result, $"Failed to set password policy. Error code: {result}");
        }

        return null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct USER_MODALS_INFO_0
    {
        public uint usrmod0_min_passwd_len;
        public uint usrmod0_max_passwd_age;
        public uint usrmod0_min_passwd_age;
        public uint usrmod0_force_logoff;
        public uint usrmod0_password_hist_len;
    }

    [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint NetUserModalsGet(
        [MarshalAs(UnmanagedType.LPWStr)] string? serverName,
        uint level,
        out IntPtr bufPtr);

    [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint NetUserModalsSet(
        [MarshalAs(UnmanagedType.LPWStr)] string? serverName,
        uint level,
        ref USER_MODALS_INFO_0 buf,
        out uint parmErr);

    [DllImport("netapi32.dll", SetLastError = true)]
    private static extern uint NetApiBufferFree(IntPtr buffer);
}
