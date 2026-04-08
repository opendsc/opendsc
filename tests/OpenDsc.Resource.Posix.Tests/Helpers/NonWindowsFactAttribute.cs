// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.CompilerServices;

using Xunit;

namespace OpenDsc.Resource.Posix.Tests.Helpers;

internal sealed class NonWindowsFactAttribute : FactAttribute
{
    public NonWindowsFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (OperatingSystem.IsWindows())
        {
            Skip = "Requires Linux or macOS";
        }
    }
}
