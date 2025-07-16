// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource;

public sealed class SetResult<T>(T actualState)
{
    public T ActualState { get; } = actualState;

    public HashSet<string>? ChangedProperties { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class SetReturnAttribute(SetReturn setReturn) : Attribute
{
    public SetReturn SetReturn { get; } = setReturn;
}

public enum SetReturn
{
    None,
    State,
    StateAndDiff
}
