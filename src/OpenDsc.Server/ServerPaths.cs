// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;

namespace OpenDsc.Server;

public static class ServerPaths
{
    public static string GetServerConfigDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(programData, "OpenDSC", "Server");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "/etc/opendsc/server";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/Library/Preferences/OpenDSC/Server";
        }

        throw new PlatformNotSupportedException("Unsupported platform");
    }

    public static string GetServerConfigPath()
    {
        return Path.Combine(GetServerConfigDirectory(), "appsettings.json");
    }
}
