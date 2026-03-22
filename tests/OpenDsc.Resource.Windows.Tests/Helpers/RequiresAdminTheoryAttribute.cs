// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Principal;

using Xunit;

namespace OpenDsc.Resource.Windows.Tests;

internal sealed class RequiresAdminTheoryAttribute : TheoryAttribute
{
    public RequiresAdminTheoryAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Requires Windows";
            return;
        }

        var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            Skip = "Requires administrator privileges";
        }
    }
}
