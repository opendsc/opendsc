// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace OpenDsc.Resource.Windows.UserRight;

internal static partial class LsaHelper
{
    private const int POLICY_VIEW_LOCAL_INFORMATION = 0x00000001;
    private const int POLICY_LOOKUP_NAMES = 0x00000800;
    private const int POLICY_CREATE_ACCOUNT = 0x00000010;
    private const int STATUS_NO_MORE_ENTRIES = unchecked((int)0x8000001A);
    private const int STATUS_NO_SUCH_PRIVILEGE = unchecked((int)0xC0000060);

    [StructLayout(LayoutKind.Sequential)]
    private struct LSA_UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LSA_OBJECT_ATTRIBUTES
    {
        public int Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public int Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LSA_ENUMERATION_INFORMATION
    {
        public IntPtr Sid;
    }

    [LibraryImport("advapi32.dll", SetLastError = true)]
    private static partial int LsaOpenPolicy(
        ref LSA_UNICODE_STRING SystemName,
        ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
        int DesiredAccess,
        out IntPtr PolicyHandle);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    private static partial int LsaAddAccountRights(
        IntPtr PolicyHandle,
        IntPtr AccountSid,
        LSA_UNICODE_STRING[] UserRights,
        int CountOfRights);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    private static partial int LsaRemoveAccountRights(
        IntPtr PolicyHandle,
        IntPtr AccountSid,
        [MarshalAs(UnmanagedType.Bool)] bool AllRights,
        LSA_UNICODE_STRING[] UserRights,
        int CountOfRights);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    private static partial int LsaEnumerateAccountsWithUserRight(
        IntPtr PolicyHandle,
        ref LSA_UNICODE_STRING UserRight,
        out IntPtr Buffer,
        out int CountReturned);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    private static partial int LsaEnumerateAccountRights(
        IntPtr PolicyHandle,
        IntPtr AccountSid,
        out IntPtr UserRights,
        out int CountOfRights);

    [LibraryImport("advapi32.dll")]
    private static partial int LsaClose(IntPtr ObjectHandle);

    [LibraryImport("advapi32.dll")]
    private static partial int LsaFreeMemory(IntPtr Buffer);

    [LibraryImport("advapi32.dll")]
    private static partial int LsaNtStatusToWinError(int Status);

    private static LSA_UNICODE_STRING CreateLsaString(string str)
    {
        var lsaStr = new LSA_UNICODE_STRING
        {
            Length = (ushort)(str.Length * 2),
            MaximumLength = (ushort)((str.Length + 1) * 2),
            Buffer = Marshal.StringToHGlobalUni(str)
        };
        return lsaStr;
    }

    private static void FreeLsaString(LSA_UNICODE_STRING lsaStr)
    {
        if (lsaStr.Buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(lsaStr.Buffer);
        }
    }

    private static IntPtr OpenPolicy(int access)
    {
        var systemName = new LSA_UNICODE_STRING();
        var objectAttributes = new LSA_OBJECT_ATTRIBUTES
        {
            Length = Marshal.SizeOf<LSA_OBJECT_ATTRIBUTES>()
        };

        var status = LsaOpenPolicy(ref systemName, ref objectAttributes, access, out IntPtr policyHandle);
        if (status != 0)
        {
            throw new Win32Exception(LsaNtStatusToWinError(status), "Failed to open LSA policy.");
        }

        return policyHandle;
    }

    public static string[] GetPrincipalsWithRight(UserRight right)
    {
        IntPtr policyHandle = IntPtr.Zero;
        IntPtr buffer = IntPtr.Zero;
        var rightString = new LSA_UNICODE_STRING();

        try
        {
            policyHandle = OpenPolicy(POLICY_VIEW_LOCAL_INFORMATION | POLICY_LOOKUP_NAMES);
            rightString = CreateLsaString(right.ToString());

            var status = LsaEnumerateAccountsWithUserRight(policyHandle, ref rightString, out buffer, out int count);

            if (status == STATUS_NO_MORE_ENTRIES)
            {
                return Array.Empty<string>();
            }

            if (status != 0)
            {
                var winError = LsaNtStatusToWinError(status);
                throw new Win32Exception(winError, $"Failed to enumerate accounts with right '{right}'. LSA Status: 0x{status:X8}, Win32 Error: {winError}");
            }

            var principals = new List<string>();
            var structSize = Marshal.SizeOf<LSA_ENUMERATION_INFORMATION>();

            for (int i = 0; i < count; i++)
            {
                var itemPtr = IntPtr.Add(buffer, i * structSize);
                var enumInfo = Marshal.PtrToStructure<LSA_ENUMERATION_INFORMATION>(itemPtr);
                var sid = new SecurityIdentifier(enumInfo.Sid);
                principals.Add(TranslateSidToName(sid));
            }

            return principals.ToArray();
        }
        finally
        {
            FreeLsaString(rightString);
            if (buffer != IntPtr.Zero)
            {
                LsaFreeMemory(buffer);
            }
            if (policyHandle != IntPtr.Zero)
            {
                LsaClose(policyHandle);
            }
        }
    }

    public static void GrantRight(string principal, UserRight right)
    {
        IntPtr policyHandle = IntPtr.Zero;
        IntPtr sidPtr = IntPtr.Zero;
        var rightString = new LSA_UNICODE_STRING();

        try
        {
            policyHandle = OpenPolicy(POLICY_LOOKUP_NAMES | POLICY_CREATE_ACCOUNT);
            var sid = ResolvePrincipalToSid(principal);

            var sidBytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBytes, 0);
            sidPtr = Marshal.AllocHGlobal(sidBytes.Length);
            Marshal.Copy(sidBytes, 0, sidPtr, sidBytes.Length);

            rightString = CreateLsaString(right.ToString());
            var rights = new[] { rightString };

            var status = LsaAddAccountRights(policyHandle, sidPtr, rights, 1);
            if (status != 0)
            {
                throw new Win32Exception(LsaNtStatusToWinError(status), $"Failed to grant right '{right}' to principal '{principal}'.");
            }
        }
        finally
        {
            FreeLsaString(rightString);
            if (sidPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(sidPtr);
            }
            if (policyHandle != IntPtr.Zero)
            {
                LsaClose(policyHandle);
            }
        }
    }

    public static void RevokeRight(string principal, UserRight right)
    {
        IntPtr policyHandle = IntPtr.Zero;
        IntPtr sidPtr = IntPtr.Zero;
        var rightString = new LSA_UNICODE_STRING();

        try
        {
            policyHandle = OpenPolicy(POLICY_LOOKUP_NAMES | POLICY_CREATE_ACCOUNT);
            var sid = ResolvePrincipalToSid(principal);

            var sidBytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBytes, 0);
            sidPtr = Marshal.AllocHGlobal(sidBytes.Length);
            Marshal.Copy(sidBytes, 0, sidPtr, sidBytes.Length);

            rightString = CreateLsaString(right.ToString());
            var rights = new[] { rightString };

            var status = LsaRemoveAccountRights(policyHandle, sidPtr, false, rights, 1);
            if (status != 0)
            {
                throw new Win32Exception(LsaNtStatusToWinError(status), $"Failed to revoke right '{right}' from principal '{principal}'.");
            }
        }
        finally
        {
            FreeLsaString(rightString);
            if (sidPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(sidPtr);
            }
            if (policyHandle != IntPtr.Zero)
            {
                LsaClose(policyHandle);
            }
        }
    }

    public static string TranslateSidToName(SecurityIdentifier sid)
    {
        try
        {
            var account = (NTAccount)sid.Translate(typeof(NTAccount));
            return account.Value;
        }
        catch
        {
            return sid.Value;
        }
    }

    public static SecurityIdentifier ResolvePrincipalToSid(string principal)
    {
        if (principal.StartsWith("S-1-", StringComparison.OrdinalIgnoreCase))
        {
            return new SecurityIdentifier(principal);
        }

        try
        {
            var account = new NTAccount(principal);
            return (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Unable to resolve principal '{principal}' to a SID.", ex);
        }
    }
}
