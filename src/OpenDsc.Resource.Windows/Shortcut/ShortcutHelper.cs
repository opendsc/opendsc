// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;

namespace OpenDsc.Resource.Windows.Shortcut;

internal static partial class ShortcutHelper
{
    private const int STGM_READ = 0x00000000;
    private const int MAX_PATH = 260;

    public static void CreateShortcut(Schema schema)
    {
        IShellLinkW? link = null;
        IPersistFile? file = null;

        try
        {
            link = (IShellLinkW)new ShellLink();

            if (!string.IsNullOrWhiteSpace(schema.TargetPath))
            {
                link.SetPath(schema.TargetPath);
            }

            if (!string.IsNullOrWhiteSpace(schema.Arguments))
            {
                link.SetArguments(schema.Arguments);
            }

            if (!string.IsNullOrWhiteSpace(schema.WorkingDirectory))
            {
                link.SetWorkingDirectory(schema.WorkingDirectory);
            }

            if (!string.IsNullOrWhiteSpace(schema.Description))
            {
                link.SetDescription(schema.Description);
            }

            if (!string.IsNullOrWhiteSpace(schema.IconLocation))
            {
                ParseIconLocation(schema.IconLocation, out string iconPath, out int iconIndex);
                link.SetIconLocation(iconPath, iconIndex);
            }

            if (!string.IsNullOrWhiteSpace(schema.Hotkey))
            {
                ushort hotkey = ParseHotkey(schema.Hotkey);
                link.SetHotkey(hotkey);
            }

            if (schema.WindowStyle.HasValue)
            {
                link.SetShowCmd((int)schema.WindowStyle.Value);
            }

            file = (IPersistFile)link;
            file.Save(schema.Path, true);
        }
        finally
        {
            if (file != null)
            {
                Marshal.ReleaseComObject(file);
            }
            if (link != null)
            {
                Marshal.ReleaseComObject(link);
            }
        }
    }

    public static Schema ReadShortcut(string path)
    {
        IShellLinkW? link = null;
        IPersistFile? file = null;

        try
        {
            link = (IShellLinkW)new ShellLink();
            file = (IPersistFile)link;

            file.Load(path, STGM_READ);

            var result = new Schema { Path = path };

            char[] targetPath = new char[MAX_PATH];
            WIN32_FIND_DATAW findData = default;
            link.GetPath(targetPath, targetPath.Length, ref findData, 0);
            string target = new string(targetPath).TrimEnd('\0');
            result.TargetPath = string.IsNullOrWhiteSpace(target) ? null : target;

            char[] arguments = new char[MAX_PATH];
            link.GetArguments(arguments, arguments.Length);
            string args = new string(arguments).TrimEnd('\0');
            result.Arguments = string.IsNullOrWhiteSpace(args) ? null : args;

            char[] workingDir = new char[MAX_PATH];
            link.GetWorkingDirectory(workingDir, workingDir.Length);
            string wd = new string(workingDir).TrimEnd('\0');
            result.WorkingDirectory = string.IsNullOrWhiteSpace(wd) ? null : wd;

            char[] description = new char[MAX_PATH];
            link.GetDescription(description, description.Length);
            string desc = new string(description).TrimEnd('\0');
            result.Description = string.IsNullOrWhiteSpace(desc) ? null : desc;

            char[] iconPath = new char[MAX_PATH];
            link.GetIconLocation(iconPath, iconPath.Length, out int iconIndex);
            string icon = new string(iconPath).TrimEnd('\0');
            string iconLocation = string.IsNullOrWhiteSpace(icon) ? $",{iconIndex}" : $"{icon},{iconIndex}";
            result.IconLocation = iconLocation == Schema.DefaultIconLocation ? null : iconLocation;

            link.GetHotkey(out ushort hotkey);
            result.Hotkey = hotkey == 0 ? null : FormatHotkey(hotkey);

            link.GetShowCmd(out int showCmd);
            result.WindowStyle = (WindowStyle)showCmd == Enum.Parse<WindowStyle>(Schema.DefaultWindowStyle)
                ? null
                : (WindowStyle)showCmd;

            return result;
        }
        finally
        {
            if (file != null)
            {
                Marshal.ReleaseComObject(file);
            }
            if (link != null)
            {
                Marshal.ReleaseComObject(link);
            }
        }
    }

    private static void ParseIconLocation(string iconLocation, out string path, out int index)
    {
        int commaIndex = iconLocation.LastIndexOf(',');
        if (commaIndex >= 0)
        {
            path = iconLocation[..commaIndex];
            if (int.TryParse(iconLocation.AsSpan(commaIndex + 1), out int parsedIndex))
            {
                index = parsedIndex;
                return;
            }
        }

        path = iconLocation;
        index = 0;
    }

    private static ushort ParseHotkey(string hotkey)
    {
        ushort modifiers = 0;
        string[] parts = hotkey.Split('+');

        foreach (string part in parts[..^1])
        {
            modifiers |= part.ToUpperInvariant() switch
            {
                "SHIFT" => 0x0100,
                "CTRL" or "CONTROL" => 0x0200,
                "ALT" => 0x0400,
                "EXT" or "EXTENDED" => 0x0800,
                _ => 0
            };
        }

        string key = parts[^1].ToUpperInvariant();
        byte vkCode = key.Length == 1 ? (byte)key[0] : (byte)0;

        return (ushort)(vkCode | modifiers);
    }

    private static string FormatHotkey(ushort hotkey)
    {
        byte vkCode = (byte)(hotkey & 0xFF);
        ushort modifiers = (ushort)(hotkey & 0xFF00);

        List<string> parts = [];

        if ((modifiers & 0x0100) != 0) parts.Add("SHIFT");
        if ((modifiers & 0x0200) != 0) parts.Add("CTRL");
        if ((modifiers & 0x0400) != 0) parts.Add("ALT");
        if ((modifiers & 0x0800) != 0) parts.Add("EXT");

        if (vkCode != 0)
        {
            parts.Add(((char)vkCode).ToString());
        }

        return string.Join("+", parts);
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    [ClassInterface(ClassInterfaceType.None)]
    private class ShellLink { }

#pragma warning disable SYSLIB1096
    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] char[] pszFile, int cch, ref WIN32_FIND_DATAW pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] char[] pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] char[] pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] char[] pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out ushort pwHotkey);
        void SetHotkey(ushort wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] char[] pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
#pragma warning restore SYSLIB1096

#pragma warning disable SYSLIB1096
    [ComImport]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
#pragma warning restore SYSLIB1096

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WIN32_FIND_DATAW
    {
        public uint dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }
}
