// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource;

/// <summary>
/// Represents the result of a Set operation on a DSC resource.
/// </summary>
/// <typeparam name="T">The schema type that defines the resource's properties.</typeparam>
/// <param name="actualState">The actual state of the resource after the Set operation.</param>
public sealed class SetResult<T>(T actualState)
{
    /// <summary>
    /// Gets the actual state of the resource after the Set operation.
    /// </summary>
    public T ActualState { get; } = actualState;

    /// <summary>
    /// Gets or sets the set of property names that were changed during the Set operation.
    /// This is populated when <see cref="SetReturn.StateAndDiff"/> is specified.
    /// </summary>
    public HashSet<string>? ChangedProperties { get; set; }
}

/// <summary>
/// Specifies what information a Set operation should return.
/// This attribute is deprecated; use <see cref="DscResourceAttribute.SetReturn"/> instead.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SetReturnAttribute(SetReturn setReturn) : Attribute
{
    /// <summary>
    /// Gets what the Set operation returns.
    /// </summary>
    public SetReturn SetReturn { get; } = setReturn;
}

/// <summary>
/// Specifies what information a Set operation should return.
/// </summary>
public enum SetReturn
{
    /// <summary>
    /// Do not return any information.
    /// </summary>
    None,

    /// <summary>
    /// Return only the actual state after changes.
    /// </summary>
    State,

    /// <summary>
    /// Return the actual state and a set of changed property names.
    /// </summary>
    StateAndDiff
}
