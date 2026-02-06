// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;

namespace OpenDsc.Resource.Windows.AccountLockoutPolicy;

internal static class AccountLockoutPolicyHelper
{
    internal const uint NERR_Success = 0;
    internal const uint ERROR_ACCESS_DENIED = 5;
    internal const uint ERROR_INVALID_PARAMETER = 87;

    [StructLayout(LayoutKind.Sequential)]
    internal struct USER_MODALS_INFO_3
    {
        public uint usrmod3_lockout_duration;
        public uint usrmod3_lockout_observation_window;
        public uint usrmod3_lockout_threshold;
    }

    [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern uint NetUserModalsGet(
        [MarshalAs(UnmanagedType.LPWStr)] string? serverName,
        uint level,
        out IntPtr bufPtr);

    [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern uint NetUserModalsSet(
        [MarshalAs(UnmanagedType.LPWStr)] string? serverName,
        uint level,
        ref USER_MODALS_INFO_3 buf,
        out uint parmErr);

    [DllImport("netapi32.dll", SetLastError = true)]
    internal static extern uint NetApiBufferFree(IntPtr buffer);
}
