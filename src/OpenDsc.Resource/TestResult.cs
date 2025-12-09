// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource;

/// <summary>
/// Represents the result of a Test operation on a DSC resource.
/// </summary>
/// <typeparam name="T">The schema type that defines the resource's properties.</typeparam>
/// <param name="actualState">The actual current state of the resource.</param>
public sealed class TestResult<T>(T actualState)
{
    /// <summary>
    /// Gets the actual current state of the resource.
    /// </summary>
    public T ActualState { get; } = actualState;

    /// <summary>
    /// Gets or sets the set of property names that differ from the desired state.
    /// This is populated when <see cref="TestReturn.StateAndDiff"/> is specified.
    /// </summary>
    public HashSet<string>? DifferingProperties { get; set; }
}

/// <summary>
/// Specifies what information a Test operation should return.
/// This attribute is deprecated; use <see cref="DscResourceAttribute.TestReturn"/> instead.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TestReturnAttribute(TestReturn testReturn) : Attribute
{
    /// <summary>
    /// Gets what the Test operation returns.
    /// </summary>
    public TestReturn TestReturn { get; } = testReturn;
}

/// <summary>
/// Specifies what information a Test operation should return.
/// </summary>
public enum TestReturn
{
    /// <summary>
    /// Return only the actual state.
    /// </summary>
    State,

    /// <summary>
    /// Return the actual state and a set of differing property names.
    /// </summary>
    StateAndDiff
}
