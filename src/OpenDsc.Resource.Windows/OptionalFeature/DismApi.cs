// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;

namespace OpenDsc.Resource.Windows.OptionalFeature;

internal static partial class DismApi
{
    private const string DismDll = "dismapi.dll";

    [LibraryImport(DismDll, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int DismInitialize(
        DismLogLevel logLevel,
        string? logFilePath,
        string? scratchDirectory);

    [LibraryImport(DismDll)]
    public static partial int DismShutdown();

    [LibraryImport(DismDll, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int DismOpenSession(
        string imagePath,
        string? windowsDirectory,
        string? systemDrive,
        out IntPtr session);

    [LibraryImport(DismDll)]
    public static partial int DismCloseSession(IntPtr session);

    [LibraryImport(DismDll, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int DismGetFeatures(
        IntPtr session,
        string? identifier,
        DismPackageIdentifier packageIdentifier,
        out IntPtr feature,
        out uint count);

    [LibraryImport(DismDll, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int DismGetFeatureInfo(
        IntPtr session,
        string featureName,
        string? identifier,
        DismPackageIdentifier packageIdentifier,
        out IntPtr featureInfo);

    [LibraryImport(DismDll, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int DismEnableFeature(
        IntPtr session,
        string featureName,
        string? identifier,
        DismPackageIdentifier packageIdentifier,
        [MarshalAs(UnmanagedType.Bool)] bool limitAccess,
        string[]? sourcePaths,
        uint sourcePathCount,
        [MarshalAs(UnmanagedType.Bool)] bool enableAll,
        IntPtr cancelEvent,
        IntPtr progress,
        IntPtr userData);

    [LibraryImport(DismDll, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int DismDisableFeature(
        IntPtr session,
        string featureName,
        string? packageName,
        [MarshalAs(UnmanagedType.Bool)] bool removePayload,
        IntPtr cancelEvent,
        IntPtr progress,
        IntPtr userData);

    [LibraryImport(DismDll)]
    public static partial int DismDelete(IntPtr dismStructure);

    [LibraryImport(DismDll)]
    public static partial int DismGetLastErrorMessage(out IntPtr errorMessage);

    public static void Initialize()
    {
        var hr = DismInitialize(DismLogLevel.LogErrors, null, null);
        if (hr != 0 && hr != unchecked((int)0x00000001))
        {
            throw new InvalidOperationException($"Failed to initialize DISM API: 0x{hr:X8}");
        }
    }

    public static void Shutdown()
    {
        _ = DismShutdown();
    }

    public static IntPtr OpenOnlineSession()
    {
        var hr = DismOpenSession("DISM_{53BFAE52-B167-4E2F-A258-0A37B57FF845}", null, null, out var session);
        if (hr != 0)
        {
            throw new InvalidOperationException($"Failed to open DISM session: 0x{hr:X8}");
        }
        return session;
    }

    public static void CloseSession(IntPtr session)
    {
        if (session != IntPtr.Zero)
        {
            _ = DismCloseSession(session);
        }
    }

    public static string? GetLastErrorMessage()
    {
        if (DismGetLastErrorMessage(out var errorMessagePtr) == 0 && errorMessagePtr != IntPtr.Zero)
        {
            try
            {
                return Marshal.PtrToStringUni(errorMessagePtr);
            }
            finally
            {
                _ = DismDelete(errorMessagePtr);
            }
        }
        return null;
    }
}

internal enum DismLogLevel
{
    LogErrors = 0,
    LogErrorsWarnings = 1,
    LogErrorsWarningsInfo = 2
}

internal enum DismPackageIdentifier
{
    None = 0,
    Name = 1,
    Path = 2
}

public enum DismPackageFeatureState
{
    NotPresent = 0,
    UninstallPending = 1,
    Staged = 2,
    Removed = 3,
    Installed = 4,
    InstallPending = 5,
    Superseded = 6,
    PartiallyInstalled = 7
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
internal struct DismFeature
{
    public string FeatureName;
    public DismPackageFeatureState State;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
internal struct DismFeatureInfo
{
    public string FeatureName;
    public DismPackageFeatureState State;
    public string DisplayName;
    public string Description;
    public DismRestartType RestartRequired;
    public IntPtr CustomProperty;
    public uint CustomPropertyCount;
}

public enum DismRestartType
{
    No = 0,
    Possible = 1,
    Required = 2
}
