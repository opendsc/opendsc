// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource;

/// <summary>
/// Maps an exception type or condition to a specific exit code for command-line error handling.
/// Multiple instances can be applied to a resource class to define different exit codes for different exceptions.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ExitCodeAttribute(int exitCode) : Attribute
{
    /// <summary>
    /// Gets the exit code value to return when this condition is met.
    /// </summary>
    public int ExitCode { get; } = exitCode;

    /// <summary>
    /// Gets or sets a description of what this exit code represents.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception type that maps to this exit code.
    /// The type must inherit from <see cref="System.Exception"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the value does not inherit from Exception.</exception>
    public Type? Exception
    {
        get
        {
            return _exception;
        }

        set
        {
            if (!typeof(Exception).IsAssignableFrom(value))
            {
                throw new ArgumentException("Type does not inherit from Exception.");
            }

            _exception = value;
        }
    }

    private Type? _exception;
}
