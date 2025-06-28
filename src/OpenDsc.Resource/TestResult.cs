// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource;

public sealed class TestResult<T>
{
    public T ActualState { get; }

    public HashSet<string>? DifferingProperties { get; set; }

    public TestResult(T actualState)
    {
        ActualState = actualState;
    }
}
