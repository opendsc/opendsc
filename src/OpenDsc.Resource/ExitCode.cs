// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource;

public sealed class ResourceExitCode
{
    public string Description { get; set; } = string.Empty;

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
