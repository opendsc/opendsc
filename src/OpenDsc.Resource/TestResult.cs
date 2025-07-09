// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource;

public sealed class TestResult<T>(T actualState)
{
    public T ActualState { get; } = actualState;

    public HashSet<string>? DifferingProperties { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class TestReturnAttribute(TestReturn testReturn) : Attribute
{
    public TestReturn TestReturn { get; } = testReturn;
}

public enum TestReturn
{
    State,
    StateAndDiff
}
