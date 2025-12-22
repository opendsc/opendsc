// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace OpenDsc.Resource.Windows.Service;

internal static partial class ServiceHelper
{
    private const int SC_MANAGER_CONNECT = 0x0001;
    private const int SC_MANAGER_CREATE_SERVICE = 0x0002;
    private const int SC_MANAGER_ALL_ACCESS = 0xF003F;
    private const int SERVICE_QUERY_CONFIG = 0x0001;
    private const int SERVICE_ALL_ACCESS = 0xF01FF;
    private const int SERVICE_CHANGE_CONFIG = 0x0002;
    private const int SERVICE_CONFIG_DESCRIPTION = 1;
    private const int SERVICE_NO_CHANGE = unchecked((int)0xffffffff);
    private const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;
    private const int SERVICE_ERROR_NORMAL = 0x00000001;

    [LibraryImport("advapi32.dll", EntryPoint = "OpenSCManagerW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr OpenSCManager(string? lpMachineName, string? lpDatabaseName, int dwDesiredAccess);

    [LibraryImport("advapi32.dll", EntryPoint = "OpenServiceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr OpenService(IntPtr hSCManager, string lpServiceName, int dwDesiredAccess);

    [LibraryImport("advapi32.dll", EntryPoint = "ChangeServiceConfigW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ChangeServiceConfig(
        IntPtr hService,
        int dwServiceType,
        int dwStartType,
        int dwErrorControl,
        string? lpBinaryPathName,
        string? lpLoadOrderGroup,
        IntPtr lpdwTagId,
        string? lpDependencies,
        string? lpServiceStartName,
        string? lpPassword,
        string? lpDisplayName);

    [LibraryImport("advapi32.dll", EntryPoint = "ChangeServiceConfig2W", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ChangeServiceConfig2(IntPtr hService, int dwInfoLevel, ref SERVICE_DESCRIPTION lpInfo);

    [LibraryImport("advapi32.dll", EntryPoint = "QueryServiceConfigW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool QueryServiceConfig(IntPtr hService, IntPtr lpServiceConfig, int cbBufSize, out int pcbBytesNeeded);

    [LibraryImport("advapi32.dll", EntryPoint = "QueryServiceConfig2W", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool QueryServiceConfig2(IntPtr hService, int dwInfoLevel, IntPtr lpBuffer, int cbBufSize, out int pcbBytesNeeded);

    [LibraryImport("advapi32.dll", EntryPoint = "CreateServiceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr CreateService(
        IntPtr hSCManager,
        string lpServiceName,
        string? lpDisplayName,
        int dwDesiredAccess,
        int dwServiceType,
        int dwStartType,
        int dwErrorControl,
        string lpBinaryPathName,
        string? lpLoadOrderGroup,
        IntPtr lpdwTagId,
        string? lpDependencies,
        string? lpServiceStartName,
        string? lpPassword);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteService(IntPtr hService);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseServiceHandle(IntPtr hSCObject);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SERVICE_DESCRIPTION
    {
        public IntPtr lpDescription;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct QUERY_SERVICE_CONFIG
    {
        public int dwServiceType;
        public int dwStartType;
        public int dwErrorControl;
        public IntPtr lpBinaryPathName;
        public IntPtr lpLoadOrderGroup;
        public int dwTagId;
        public IntPtr lpDependencies;
        public IntPtr lpServiceStartName;
        public IntPtr lpDisplayName;
    }

    public static string? GetServiceDescription(string serviceName)
    {
        IntPtr scm = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;
        IntPtr buffer = IntPtr.Zero;

        try
        {
            scm = OpenSCManager(null, null, SC_MANAGER_CONNECT);
            if (scm == IntPtr.Zero)
                return null;

            service = OpenService(scm, serviceName, SERVICE_QUERY_CONFIG);
            if (service == IntPtr.Zero)
                return null;

            // Query for buffer size
            QueryServiceConfig2(service, SERVICE_CONFIG_DESCRIPTION, IntPtr.Zero, 0, out int bytesNeeded);

            buffer = Marshal.AllocHGlobal(bytesNeeded);
            if (!QueryServiceConfig2(service, SERVICE_CONFIG_DESCRIPTION, buffer, bytesNeeded, out _))
                return null;

            var desc = Marshal.PtrToStructure<SERVICE_DESCRIPTION>(buffer);
            return desc.lpDescription != IntPtr.Zero ? Marshal.PtrToStringUni(desc.lpDescription) : null;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scm != IntPtr.Zero)
                CloseServiceHandle(scm);
        }
    }

    public static string? GetServicePath(string serviceName)
    {
        IntPtr scm = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;
        IntPtr buffer = IntPtr.Zero;

        try
        {
            scm = OpenSCManager(null, null, SC_MANAGER_CONNECT);
            if (scm == IntPtr.Zero)
                return null;

            service = OpenService(scm, serviceName, SERVICE_QUERY_CONFIG);
            if (service == IntPtr.Zero)
                return null;

            QueryServiceConfig(service, IntPtr.Zero, 0, out int bytesNeeded);

            buffer = Marshal.AllocHGlobal(bytesNeeded);
            if (!QueryServiceConfig(service, buffer, bytesNeeded, out _))
                return null;

            var config = Marshal.PtrToStructure<QUERY_SERVICE_CONFIG>(buffer);
            return config.lpBinaryPathName != IntPtr.Zero ? Marshal.PtrToStringUni(config.lpBinaryPathName) : null;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scm != IntPtr.Zero)
                CloseServiceHandle(scm);
        }
    }

    public static void SetServiceDisplayName(string serviceName, string displayName)
    {
        IntPtr scm = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;

        try
        {
            scm = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
            if (scm == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open Service Control Manager");

            service = OpenService(scm, serviceName, SERVICE_CHANGE_CONFIG);
            if (service == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to open service '{serviceName}'");

            if (!ChangeServiceConfig(service, SERVICE_NO_CHANGE, SERVICE_NO_CHANGE, SERVICE_NO_CHANGE,
                null, null, IntPtr.Zero, null, null, null, displayName))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to set display name for service '{serviceName}'");
            }
        }
        finally
        {
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scm != IntPtr.Zero)
                CloseServiceHandle(scm);
        }
    }

    public static void SetServiceDescription(string serviceName, string description)
    {
        IntPtr scm = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;
        IntPtr descPtr = IntPtr.Zero;

        try
        {
            scm = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
            if (scm == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open Service Control Manager");

            service = OpenService(scm, serviceName, SERVICE_CHANGE_CONFIG);
            if (service == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to open service '{serviceName}'");

            descPtr = Marshal.StringToHGlobalUni(description);
            var serviceDesc = new SERVICE_DESCRIPTION { lpDescription = descPtr };

            if (!ChangeServiceConfig2(service, SERVICE_CONFIG_DESCRIPTION, ref serviceDesc))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to set description for service '{serviceName}'");
            }
        }
        finally
        {
            if (descPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(descPtr);
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scm != IntPtr.Zero)
                CloseServiceHandle(scm);
        }
    }

    public static void SetServiceStartMode(string serviceName, ServiceStartMode startMode)
    {
        IntPtr scm = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;

        try
        {
            scm = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
            if (scm == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open Service Control Manager");

            service = OpenService(scm, serviceName, SERVICE_CHANGE_CONFIG);
            if (service == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to open service '{serviceName}'");

            int startValue = startMode switch
            {
                ServiceStartMode.Automatic => 2,
                ServiceStartMode.Manual => 3,
                ServiceStartMode.Disabled => 4,
                _ => throw new ArgumentException($"Unsupported start mode: {startMode}")
            };

            if (!ChangeServiceConfig(service, SERVICE_NO_CHANGE, startValue, SERVICE_NO_CHANGE,
                null, null, IntPtr.Zero, null, null, null, null))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to set start mode for service '{serviceName}'");
            }
        }
        finally
        {
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scm != IntPtr.Zero)
                CloseServiceHandle(scm);
        }
    }

    public static void SetServiceDependencies(string serviceName, string[]? dependencies)
    {
        IntPtr scm = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;

        try
        {
            scm = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
            if (scm == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open Service Control Manager");

            service = OpenService(scm, serviceName, SERVICE_CHANGE_CONFIG);
            if (service == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to open service '{serviceName}'");

            string? dependenciesString;
            if (dependencies == null || dependencies.Length == 0)
            {
                dependenciesString = "\0";
            }
            else
            {
                dependenciesString = string.Join("\0", dependencies) + "\0";
            }

            if (!ChangeServiceConfig(service, SERVICE_NO_CHANGE, SERVICE_NO_CHANGE, SERVICE_NO_CHANGE,
                null, null, IntPtr.Zero, dependenciesString, null, null, null))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to set dependencies for service '{serviceName}'");
            }
        }
        finally
        {
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scm != IntPtr.Zero)
                CloseServiceHandle(scm);
        }
    }

    public static void CreateWindowsService(string serviceName, string binaryPath, string? displayName = null, ServiceStartMode startMode = ServiceStartMode.Manual, string? dependencies = null)
    {
        IntPtr scm = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;

        try
        {
            scm = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE);
            if (scm == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open Service Control Manager");

            int startValue = startMode switch
            {
                ServiceStartMode.Automatic => 2,
                ServiceStartMode.Manual => 3,
                ServiceStartMode.Disabled => 4,
                _ => 3 // Default to Manual
            };

            service = CreateService(
                scm,
                serviceName,
                displayName ?? serviceName,
                SERVICE_ALL_ACCESS,
                SERVICE_WIN32_OWN_PROCESS,
                startValue,
                SERVICE_ERROR_NORMAL,
                binaryPath,
                null,
                IntPtr.Zero,
                dependencies,
                null,
                null);

            if (service == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to create service '{serviceName}'");
            }
        }
        finally
        {
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scm != IntPtr.Zero)
                CloseServiceHandle(scm);
        }
    }

    public static void DeleteWindowsService(string serviceName)
    {
        IntPtr scm = IntPtr.Zero;
        IntPtr service = IntPtr.Zero;

        try
        {
            scm = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
            if (scm == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open Service Control Manager");

            service = OpenService(scm, serviceName, SERVICE_ALL_ACCESS);
            if (service == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to open service '{serviceName}'");

            if (!DeleteService(service))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to delete service '{serviceName}'");
            }
        }
        finally
        {
            if (service != IntPtr.Zero)
                CloseServiceHandle(service);
            if (scm != IntPtr.Zero)
                CloseServiceHandle(scm);
        }
    }
}
