// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.AuditPolicy;

[DscResource("OpenDsc.Windows/AuditPolicy", "0.1.0", Description = "Manage Windows audit policy for system security event auditing", Tags = ["windows", "audit", "security"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(SecurityException), Description = "Access denied - requires SeSecurityPrivilege or AUDIT_SET_SYSTEM_POLICY access")]
[ExitCode(3, Exception = typeof(ArgumentException), Description = "Invalid parameter")]
[ExitCode(4, Exception = typeof(Win32Exception), Description = "Win32 API error")]
public sealed class Resource(JsonSerializerContext context)
    : DscResource<Schema>(context),
      IGettable<Schema>,
      ISettable<Schema>,
      IDeletable<Schema>
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
        var guid = Guid.Parse(instance.SubcategoryGuid);
        var policyInfo = QueryAuditPolicy(guid);

        return new Schema
        {
            SubcategoryGuid = instance.SubcategoryGuid,
            Setting = ConvertToAuditSetting(policyInfo.AuditingInformation)
        };
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        var guid = Guid.Parse(instance.SubcategoryGuid);
        var currentPolicy = QueryAuditPolicy(guid);

        var desiredFlags = ConvertToAuditFlags(instance.Setting ?? AuditSetting.None);

        if (currentPolicy.AuditingInformation != desiredFlags)
        {
            SetAuditPolicy(guid, currentPolicy.AuditCategoryGuid, desiredFlags);
        }

        return null;
    }

    public void Delete(Schema instance)
    {
        var guid = Guid.Parse(instance.SubcategoryGuid);
        var currentPolicy = QueryAuditPolicy(guid);
        SetAuditPolicy(guid, currentPolicy.AuditCategoryGuid, POLICY_AUDIT_EVENT_NONE);
    }

    private static AUDIT_POLICY_INFORMATION QueryAuditPolicy(Guid subcategoryGuid)
    {
        IntPtr policyPtr = IntPtr.Zero;

        try
        {
            var guidArray = new[] { subcategoryGuid };

            var result = AuditQuerySystemPolicy(guidArray, 1, out policyPtr);

            var error = Marshal.GetLastWin32Error();

            if (!result || policyPtr == IntPtr.Zero)
            {
                if (error == ERROR_ACCESS_DENIED || error == ERROR_PRIVILEGE_NOT_HELD)
                {
                    throw new SecurityException("Access denied. Requires SeSecurityPrivilege or administrative privileges to query audit policy.");
                }
                if (error != 0)
                {
                    throw new Win32Exception(error, "Failed to query audit policy.");
                }
                throw new InvalidOperationException("AuditQuerySystemPolicy failed with unknown error.");
            }

            var buffer = new byte[36];
            Marshal.Copy(policyPtr, buffer, 0, 36);

            var policy = new AUDIT_POLICY_INFORMATION
            {
                AuditSubCategoryGuid = new Guid(buffer.AsSpan(0, 16)),
                AuditingInformation = BitConverter.ToUInt32(buffer, 16),
                AuditCategoryGuid = new Guid(buffer.AsSpan(20, 16))
            };

            return policy;
        }
        finally
        {
            if (policyPtr != IntPtr.Zero)
            {
                AuditFree(policyPtr);
            }
        }
    }

    private static void SetAuditPolicy(Guid subcategoryGuid, Guid categoryGuid, uint flags)
    {
        EnablePrivilege(SE_SECURITY_NAME);

        var policy = new AUDIT_POLICY_INFORMATION
        {
            AuditSubCategoryGuid = subcategoryGuid,
            AuditingInformation = flags,
            AuditCategoryGuid = categoryGuid
        };

        IntPtr policyPtr = IntPtr.Zero;
        try
        {
            int structSize = Marshal.SizeOf<AUDIT_POLICY_INFORMATION>();
            policyPtr = Marshal.AllocHGlobal(structSize);
            Marshal.StructureToPtr(policy, policyPtr, false);

            var result = AuditSetSystemPolicyPtr(policyPtr, 1);
            var error = Marshal.GetLastWin32Error();

            if (!result || error != 0)
            {
                if (error == ERROR_ACCESS_DENIED || error == ERROR_PRIVILEGE_NOT_HELD)
                {
                    throw new SecurityException("Access denied. Requires administrative privileges to set audit policy.");
                }
                if (error == ERROR_INVALID_PARAMETER)
                {
                    throw new ArgumentException("Invalid audit policy parameter.");
                }
                if (error != 0)
                {
                    throw new Win32Exception(error, "Failed to set audit policy.");
                }
            }
        }
        finally
        {
            if (policyPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(policyPtr);
            }
        }
    }

    private static void EnablePrivilege(string privilegeName)
    {
        IntPtr tokenHandle = IntPtr.Zero;

        try
        {
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open process token.");
            }

            if (!LookupPrivilegeValue(null, privilegeName, out var luid))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to lookup privilege: {privilegeName}");
            }

            var tokenPrivileges = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED
            };

            if (!AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to adjust token privileges: {privilegeName}");
            }

            var adjustError = Marshal.GetLastWin32Error();
            if (adjustError == ERROR_PRIVILEGE_NOT_HELD)
            {
                throw new SecurityException($"Process does not have {privilegeName}. Run as administrator.");
            }
        }
        finally
        {
            if (tokenHandle != IntPtr.Zero)
            {
                CloseHandle(tokenHandle);
            }
        }
    }

    private static AuditSetting ConvertToAuditSetting(uint flags)
    {
        var hasSuccess = (flags & POLICY_AUDIT_EVENT_SUCCESS) != 0;
        var hasFailure = (flags & POLICY_AUDIT_EVENT_FAILURE) != 0;

        if (hasSuccess && hasFailure)
        {
            return AuditSetting.SuccessAndFailure;
        }
        if (hasSuccess)
        {
            return AuditSetting.Success;
        }
        if (hasFailure)
        {
            return AuditSetting.Failure;
        }

        return AuditSetting.None;
    }

    private static uint ConvertToAuditFlags(AuditSetting setting)
    {
        return setting switch
        {
            AuditSetting.None => POLICY_AUDIT_EVENT_NONE,
            AuditSetting.Success => POLICY_AUDIT_EVENT_SUCCESS,
            AuditSetting.Failure => POLICY_AUDIT_EVENT_FAILURE,
            AuditSetting.SuccessAndFailure => POLICY_AUDIT_EVENT_SUCCESS | POLICY_AUDIT_EVENT_FAILURE,
            _ => POLICY_AUDIT_EVENT_NONE
        };
    }

    private const uint POLICY_AUDIT_EVENT_SUCCESS = 0x00000001;
    private const uint POLICY_AUDIT_EVENT_FAILURE = 0x00000002;
    private const uint POLICY_AUDIT_EVENT_NONE = 0x00000004;

    private const int ERROR_ACCESS_DENIED = 5;
    private const int ERROR_PRIVILEGE_NOT_HELD = 1314;
    private const int ERROR_INVALID_PARAMETER = 87;

    private const int SE_PRIVILEGE_ENABLED = 0x00000002;
    private const string SE_SECURITY_NAME = "SeSecurityPrivilege";
    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint TOKEN_QUERY = 0x0008;

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AUDIT_POLICY_INFORMATION
    {
        public Guid AuditSubCategoryGuid;
        public uint AuditingInformation;
        public Guid AuditCategoryGuid;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AuditQuerySystemPolicy(
        [In] Guid[] pSubCategoryGuids,
        uint dwPolicyCount,
        out IntPtr ppAuditPolicy);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustTokenPrivileges(
        IntPtr TokenHandle,
        [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState,
        uint BufferLength,
        IntPtr PreviousState,
        IntPtr ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AuditSetSystemPolicy(
        [In] AUDIT_POLICY_INFORMATION[] pAuditPolicy,
        uint dwPolicyCount);

    [DllImport("advapi32.dll", EntryPoint = "AuditSetSystemPolicy", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AuditSetSystemPolicyPtr(
        IntPtr pAuditPolicy,
        uint dwPolicyCount);

    [DllImport("advapi32.dll")]
    private static extern void AuditFree(IntPtr buffer);
}
