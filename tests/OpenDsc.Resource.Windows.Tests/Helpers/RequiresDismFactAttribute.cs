// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;

using Xunit;

namespace OpenDsc.Resource.Windows.Tests;

internal sealed class RequiresDismFactAttribute : FactAttribute
{
    [DllImport("DismApi.dll")]
    private static extern uint DismInitialize(uint dismInitializeFlags);

    [DllImport("DismApi.dll")]
    private static extern uint DismShutdown();

    private const uint DISMERR_SUCCESS = 0;
    private const uint DISM_ONLINE = 0;

    public RequiresDismFactAttribute()
    {
        try
        {
            uint result = DismInitialize(DISM_ONLINE);
            if (result != DISMERR_SUCCESS)
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
    }
}
