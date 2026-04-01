// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Xunit;

namespace OpenDsc.Resource.Windows.Tests;

public abstract class WindowsTestBase
{
    protected WindowsTestBase()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Requires Windows");
    }
}
