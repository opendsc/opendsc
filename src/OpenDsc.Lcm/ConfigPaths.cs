// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;

namespace OpenDsc.Lcm;

public static class ConfigPaths
{
    public static string GetLcmConfigDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(programData, "OpenDSC", "LCM");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "/etc/opendsc/lcm";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/Library/Preferences/OpenDSC/LCM";
        }

        throw new PlatformNotSupportedException("Unsupported platform");
    }

    public static string GetLcmLogDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "/var/log/opendsc";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/Library/Logs/OpenDSC";
        }
        else
        {
            return GetLcmConfigDirectory();
        }
    }
}
