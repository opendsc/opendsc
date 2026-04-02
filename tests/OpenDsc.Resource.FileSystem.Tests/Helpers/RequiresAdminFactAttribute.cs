// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.CompilerServices;
using System.Security.Principal;

using Xunit;

namespace OpenDsc.Resource.FileSystem.Tests;

internal sealed class RequiresAdminFactAttribute : FactAttribute
{
    public RequiresAdminFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            Skip = "Requires administrator privileges";
        }
    }
}
