// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;

namespace OpenDsc.Resource.Windows.FileSystem.Share;

internal sealed class ShareInfo
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public string? Description { get; init; }
}

internal static partial class ShareHelper
{
    private const string NetApi32Dll = "netapi32.dll";
    private const uint NERR_Success = 0;
    private const uint NERR_NetNameNotFound = 2310;
    private const uint ERROR_ACCESS_DENIED = 5;
    private const uint SHARE_INFO_LEVEL_2 = 2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SHARE_INFO_2
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? shi2_netname;

        public uint shi2_type;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? shi2_remark;

        public uint shi2_permissions;
        public uint shi2_max_uses;
        public uint shi2_current_uses;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? shi2_path;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? shi2_passwd;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SHARE_INFO_502
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? shi502_netname;

        public uint shi502_type;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? shi502_remark;

        public uint shi502_permissions;
        public uint shi502_max_uses;
        public uint shi502_current_uses;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? shi502_path;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? shi502_passwd;

        public uint shi502_reserved;

        public IntPtr shi502_security_descriptor;
    }

    [DllImport(NetApi32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint NetShareAdd(
        [MarshalAs(UnmanagedType.LPWStr)] string? servername,
        uint level,
        ref SHARE_INFO_2 buf,
        out uint parmErr);

    [DllImport(NetApi32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint NetShareDel(
        [MarshalAs(UnmanagedType.LPWStr)] string? servername,
        [MarshalAs(UnmanagedType.LPWStr)] string netname,
        uint reserved);

    [DllImport(NetApi32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint NetShareGetInfo(
        [MarshalAs(UnmanagedType.LPWStr)] string? servername,
        [MarshalAs(UnmanagedType.LPWStr)] string netname,
        uint level,
        out IntPtr bufPtr);

    [DllImport(NetApi32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint NetShareSetInfo(
        [MarshalAs(UnmanagedType.LPWStr)] string? servername,
        [MarshalAs(UnmanagedType.LPWStr)] string netname,
        uint level,
        ref SHARE_INFO_2 buf,
        out uint parmErr);

    [DllImport(NetApi32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint NetShareSetInfo(
        [MarshalAs(UnmanagedType.LPWStr)] string? servername,
        [MarshalAs(UnmanagedType.LPWStr)] string netname,
        uint level,
        ref SHARE_INFO_502 buf,
        out uint parmErr);

    [DllImport(NetApi32Dll, SetLastError = true)]
    private static extern uint NetApiBufferFree(IntPtr buffer);

    [DllImport(NetApi32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint NetShareEnum(
        [MarshalAs(UnmanagedType.LPWStr)] string? servername,
        uint level,
        out IntPtr bufPtr,
        uint prefmaxlen,
        out uint entriesread,
        out uint totalentries,
        ref uint resume_handle);

    internal static ShareInfo? GetShare(string shareName)
    {
        var result = NetShareGetInfo(null, shareName, SHARE_INFO_LEVEL_2, out var bufPtr);

        if (result == NERR_NetNameNotFound)
        {
            return null;
        }

        if (result != NERR_Success)
        {
            ThrowNetApiException("NetShareGetInfo", result);
        }

        try
        {
            var shareInfo = Marshal.PtrToStructure<SHARE_INFO_2>(bufPtr);
            return new ShareInfo
            {
                Name = shareInfo.shi2_netname ?? string.Empty,
                Path = shareInfo.shi2_path ?? string.Empty,
                Description = shareInfo.shi2_remark
            };
        }
        finally
        {
            NetApiBufferFree(bufPtr);
        }
    }

    internal static void CreateShare(string shareName, string sharePath, string? description)
    {
        if (string.IsNullOrWhiteSpace(shareName))
        {
            throw new ArgumentException("Share name cannot be empty.", nameof(shareName));
        }

        if (string.IsNullOrWhiteSpace(sharePath))
        {
            throw new ArgumentException("Share path cannot be empty.", nameof(sharePath));
        }

        var shareInfo = new SHARE_INFO_2
        {
            shi2_netname = shareName,
            shi2_type = 0,
            shi2_remark = description ?? string.Empty,
            shi2_permissions = 0,
            shi2_max_uses = 0xFFFFFFFF,
            shi2_path = sharePath,
            shi2_passwd = string.Empty
        };

        var result = NetShareAdd(null, SHARE_INFO_LEVEL_2, ref shareInfo, out _);

        if (result != NERR_Success)
        {
            ThrowNetApiException("NetShareAdd", result);
        }
    }

    internal static void UpdateShareDescription(string shareName, string? description)
    {
        var share = GetShare(shareName);
        if (share is null)
        {
            throw new InvalidOperationException($"Share '{shareName}' does not exist.");
        }

        var shareInfo = new SHARE_INFO_2
        {
            shi2_netname = shareName,
            shi2_type = 0,
            shi2_remark = description ?? string.Empty,
            shi2_permissions = 0,
            shi2_max_uses = 0xFFFFFFFF,
            shi2_path = share.Path,
            shi2_passwd = string.Empty
        };

        var result = NetShareSetInfo(null, shareName, SHARE_INFO_LEVEL_2, ref shareInfo, out _);

        if (result != NERR_Success)
        {
            ThrowNetApiException("NetShareSetInfo", result);
        }
    }

    internal static void DeleteShare(string shareName)
    {
        var result = NetShareDel(null, shareName, 0);

        if (result == NERR_NetNameNotFound)
        {
            return;
        }

        if (result != NERR_Success)
        {
            ThrowNetApiException("NetShareDel", result);
        }
    }

    internal static IEnumerable<ShareInfo> EnumerateShares()
    {
        const uint prefmaxlen = uint.MaxValue;
        uint resumeHandle = 0;

        var result = NetShareEnum(null, SHARE_INFO_LEVEL_2, out var bufPtr, prefmaxlen, out var entriesRead, out _, ref resumeHandle);

        if (result != NERR_Success && result != 234)  // ERROR_MORE_DATA
        {
            ThrowNetApiException("NetShareEnum", result);
        }

        try
        {
            var shareSize = Marshal.SizeOf<SHARE_INFO_2>();
            for (uint i = 0; i < entriesRead; i++)
            {
                var sharePtr = IntPtr.Add(bufPtr, (int)(i * shareSize));
                var shareInfo = Marshal.PtrToStructure<SHARE_INFO_2>(sharePtr);

                yield return new ShareInfo
                {
                    Name = shareInfo.shi2_netname ?? string.Empty,
                    Path = shareInfo.shi2_path ?? string.Empty,
                    Description = shareInfo.shi2_remark
                };
            }
        }
        finally
        {
            NetApiBufferFree(bufPtr);
        }
    }

    // P/Invoke declarations for security/ACL operations
    private const string AdvApi32Dll = "advapi32.dll";
    private const uint SE_FILE_OBJECT = 1;

    [DllImport(AdvApi32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupAccountSid(
        [MarshalAs(UnmanagedType.LPWStr)] string? systemName,
        IntPtr sid,
        IntPtr name,
        ref uint cchName,
        IntPtr referencedDomainName,
        ref uint cchReferencedDomainName,
        out SID_NAME_USE peUse);

    [DllImport(AdvApi32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupAccountName(
        [MarshalAs(UnmanagedType.LPWStr)] string? systemName,
        [MarshalAs(UnmanagedType.LPWStr)] string accountName,
        IntPtr sid,
        ref uint cbSid,
        IntPtr referencedDomainName,
        ref uint cchReferencedDomainName,
        out SID_NAME_USE peUse);

    [DllImport(AdvApi32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ConvertSidToStringSid(
        IntPtr sid,
        out IntPtr stringSid);

    [DllImport(AdvApi32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ConvertStringSidToSid(
        [MarshalAs(UnmanagedType.LPWStr)] string stringSid,
        out IntPtr sid);

    [DllImport(AdvApi32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
        [MarshalAs(UnmanagedType.LPWStr)] string stringSecurityDescriptor,
        uint stringSDRevision,
        out IntPtr securityDescriptor,
        out uint securityDescriptorSize);

    [DllImport(AdvApi32Dll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ConvertSecurityDescriptorToStringSecurityDescriptor(
        IntPtr securityDescriptor,
        uint requestedStringSDRevision,
        uint securityInformation,
        out IntPtr stringSecurityDescriptor,
        out uint stringSecurityDescriptorLen);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalAlloc(uint flags, UIntPtr size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr hMem);

    private enum SID_NAME_USE
    {
        SidTypeUser = 1,
        SidTypeGroup = 2,
        SidTypeDomain = 3,
        SidTypeAlias = 4,
        SidTypeWellKnownGroup = 5,
        SidTypeDeletedAccount = 6,
        SidTypeInvalid = 7,
        SidTypeUnknown = 8,
        SidTypeComputer = 9
    }

    internal static List<SharePermission> GetSharePermissions(string shareName)
    {
        const uint SHARE_INFO_LEVEL_502 = 502;
        var result = NetShareGetInfo(null, shareName, SHARE_INFO_LEVEL_502, out var bufPtr);

        if (result == NERR_NetNameNotFound)
        {
            throw new InvalidOperationException($"Share '{shareName}' does not exist.");
        }

        if (result != NERR_Success)
        {
            ThrowNetApiException("NetShareGetInfo", result);
        }

        try
        {
            var shareInfo = Marshal.PtrToStructure<SHARE_INFO_502>(bufPtr);
            var permissions = new List<SharePermission>();

            if (shareInfo.shi502_security_descriptor != IntPtr.Zero)
            {
                // Convert security descriptor to SDDL string for easier parsing
                if (ConvertSecurityDescriptorToStringSecurityDescriptor(
                    shareInfo.shi502_security_descriptor,
                    1, // SDDL_REVISION_1
                    0x04 | 0x08, // DACL_SECURITY_INFORMATION | OWNER_SECURITY_INFORMATION
                    out var sddlPtr,
                    out _))
                {
                    try
                    {
                        var sddlString = Marshal.PtrToStringUni(sddlPtr) ?? string.Empty;
                        permissions.AddRange(ParseSddlForPermissions(sddlString));
                    }
                    finally
                    {
                        LocalFree(sddlPtr);
                    }
                }
            }

            return permissions;
        }
        finally
        {
            NetApiBufferFree(bufPtr);
        }
    }

    internal static void SetSharePermissions(string shareName, IEnumerable<SharePermission> permissions, bool purge)
    {
        const uint SHARE_INFO_LEVEL_502 = 502;

        // Get current share info to preserve other settings
        var result = NetShareGetInfo(null, shareName, SHARE_INFO_LEVEL_502, out var bufPtr);

        if (result == NERR_NetNameNotFound)
        {
            throw new InvalidOperationException($"Share '{shareName}' does not exist.");
        }

        if (result != NERR_Success)
        {
            ThrowNetApiException("NetShareGetInfo", result);
        }

        try
        {
            var shareInfo = Marshal.PtrToStructure<SHARE_INFO_502>(bufPtr);

            // Build SDDL from permissions
            var sddlString = BuildSddlFromPermissions(permissions);

            // Convert SDDL to security descriptor
            if (!ConvertStringSecurityDescriptorToSecurityDescriptor(
                sddlString,
                1, // SDDL_REVISION_1
                out var newSecurityDescriptor,
                out _))
            {
                throw new InvalidOperationException("Failed to create security descriptor from permissions.");
            }

            try
            {
                // Update the share info with the new security descriptor
                shareInfo.shi502_security_descriptor = newSecurityDescriptor;

                // Set the updated share info
                result = NetShareSetInfo(null, shareName, SHARE_INFO_LEVEL_502, ref shareInfo, out _);

                if (result != NERR_Success)
                {
                    ThrowNetApiException("NetShareSetInfo", result);
                }
            }
            finally
            {
                LocalFree(newSecurityDescriptor);
            }
        }
        finally
        {
            NetApiBufferFree(bufPtr);
        }
    }

    private static string BuildSddlFromPermissions(IEnumerable<SharePermission> permissions)
    {
        var aceStrings = new List<string>();

        foreach (var permission in permissions.Where(p => p.Access != AccessLevel.None))
        {
            var sid = LookupAccountNameToSid(permission.Principal);
            var accessMask = GetAccessMaskForLevel(permission.Access);
            var aceString = $"(A;;{accessMask:X};;;{sid})";
            aceStrings.Add(aceString);
        }

        // Build SDDL string: D: prefix for DACL, followed by ACEs
        var daclString = "D:" + string.Concat(aceStrings);

        // Return full security descriptor (owner + group + DACL)
        return "O:BAG:BAD:AI" + daclString; // BA = BUILTIN\Administrators, AI = CONTAINER_INHERIT
    }

    private static string LookupAccountNameToSid(string accountName)
    {
        // Allocate buffer for SID (max 68 bytes)
        uint sidLen = 68;
        var sidBuffer = LocalAlloc(0, new UIntPtr(sidLen));

        // Allocate buffer for domain name
        uint domainNameLen = 256;
        var domainNameBuffer = LocalAlloc(0, new UIntPtr(domainNameLen * 2)); // Unicode chars

        if (!LookupAccountName(null, accountName, sidBuffer, ref sidLen, domainNameBuffer, ref domainNameLen, out _))
        {
            LocalFree(sidBuffer);
            LocalFree(domainNameBuffer);
            throw new InvalidOperationException($"Failed to lookup account '{accountName}'.");
        }

        try
        {
            if (!ConvertSidToStringSid(sidBuffer, out var stringSidPtr))
            {
                throw new InvalidOperationException($"Failed to convert SID to string for '{accountName}'.");
            }

            try
            {
                return Marshal.PtrToStringUni(stringSidPtr) ?? throw new InvalidOperationException("SID string is null.");
            }
            finally
            {
                LocalFree(stringSidPtr);
            }
        }
        finally
        {
            LocalFree(sidBuffer);
            LocalFree(domainNameBuffer);
        }
    }

    private static string GetAccessMaskForLevel(AccessLevel level) =>
        level switch
        {
            AccessLevel.Full => "1F01FF",      // FILE_ALL_ACCESS
            AccessLevel.Change => "1201BF",    // READ | WRITE | DELETE
            AccessLevel.Read => "1200A9",      // READ | EXECUTE
            _ => "0"
        };

    private static List<SharePermission> ParseSddlForPermissions(string sddlString)
    {
        var permissions = new List<SharePermission>();

        // Parse SDDL format: D:(A;;mask;;;sid)(A;;mask;;;sid)...
        // Extract the DACL portion (after "D:")
        var daclStart = sddlString.IndexOf("D:", StringComparison.Ordinal);
        if (daclStart < 0)
        {
            return permissions;
        }

        var daclString = sddlString[daclStart..];
        var acePattern = @"\(A;;([0-9A-Fa-f]+);;;([^)]+)\)";
        var regex = new System.Text.RegularExpressions.Regex(acePattern);

        foreach (System.Text.RegularExpressions.Match match in regex.Matches(daclString))
        {
            if (match.Groups.Count >= 3)
            {
                var maskStr = match.Groups[1].Value;
                var sidStr = match.Groups[2].Value;

                if (uint.TryParse(maskStr, System.Globalization.NumberStyles.HexNumber, null, out var mask))
                {
                    var accessLevel = GetAccessLevelForMask(mask);
                    var accountName = SidToAccountName(sidStr);

                    permissions.Add(new SharePermission
                    {
                        Principal = accountName,
                        Access = accessLevel
                    });
                }
            }
        }

        return permissions;
    }

    private static AccessLevel GetAccessLevelForMask(uint mask) =>
        mask switch
        {
            0x1F01FF => AccessLevel.Full,     // FILE_ALL_ACCESS
            0x1201BF => AccessLevel.Change,   // READ | WRITE | DELETE
            0x1200A9 => AccessLevel.Read,     // READ | EXECUTE
            _ => AccessLevel.None
        };

    private static string SidToAccountName(string sidString)
    {
        if (!ConvertStringSidToSid(sidString, out var sidPtr))
        {
            return sidString; // Return SID string if conversion fails
        }

        try
        {
            // Allocate buffers for account name and domain
            uint nameLen = 256;
            uint domainLen = 256;
            var nameBuffer = LocalAlloc(0, new UIntPtr(nameLen * 2));
            var domainBuffer = LocalAlloc(0, new UIntPtr(domainLen * 2));

            try
            {
                if (!LookupAccountSid(null, sidPtr, nameBuffer, ref nameLen, domainBuffer, ref domainLen, out _))
                {
                    return sidString; // Fallback to SID if lookup fails
                }

                var accountName = Marshal.PtrToStringUni(nameBuffer) ?? string.Empty;
                var domainName = Marshal.PtrToStringUni(domainBuffer) ?? string.Empty;

                return string.IsNullOrEmpty(domainName) ? accountName : $"{domainName}\\{accountName}";
            }
            finally
            {
                LocalFree(nameBuffer);
                LocalFree(domainBuffer);
            }
        }
        finally
        {
            LocalFree(sidPtr);
        }
    }

    private static void ThrowNetApiException(string functionName, uint errorCode)
    {
        var errorMessage = errorCode switch
        {
            ERROR_ACCESS_DENIED => "Access denied. Administrator privileges required.",
            _ => $"Error code {errorCode}"
        };

        throw new InvalidOperationException($"NetAPI32 call to {functionName} failed: {errorMessage} ({errorCode})");
    }
}

