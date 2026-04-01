// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Xunit;
using Xunit.Sdk;

namespace OpenDsc.Resource.Windows.Tests;

public abstract class WindowsTestBase
{
    protected WindowsTestBase()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new SkipException("Requires Windows");
        }
    }
}
