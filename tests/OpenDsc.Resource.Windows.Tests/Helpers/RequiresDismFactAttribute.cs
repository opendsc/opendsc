// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System;
using System.Runtime.InteropServices;

using Xunit;

namespace OpenDsc.Resource.Windows.Tests;

internal sealed class RequiresDismFactAttribute : FactAttribute
{
    [DllImport("dismapi.dll", CharSet = CharSet.Unicode, EntryPoint = "DismInitialize")]
    private static extern int DismInitialize(DismLogLevel logLevel, string? logFilePath, string? scratchDirectory);

    [DllImport("dismapi.dll", EntryPoint = "DismShutdown")]
    private static extern int DismShutdown();

    public RequiresDismFactAttribute()
    {
        try
        {
            int result = DismInitialize(DismLogLevel.LogErrors, null, null);
            if (result != 0 && result != 1)
            {
                Skip = "DISM API is not available on this system";
                return;
            }

            DismShutdown();
        }
        catch (DllNotFoundException)
        {
            Skip = "DISM API is not available on this system";
        }
        catch (EntryPointNotFoundException)
        {
            Skip = "DISM API is not available on this system";
        }
        catch (Exception)
        {
            Skip = "DISM API initialization failed on this system";
        }
    }
}

internal enum DismLogLevel
{
    LogErrors = 0,
    LogErrorsWarnings = 1,
    LogErrorsWarningsInfo = 2
}
