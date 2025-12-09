// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;

namespace OpenDsc.Resource;

/// <summary>
/// Provides functionality to resolve exit codes for exceptions based on <see cref="ExitCodeAttribute"/> definitions.
/// </summary>
public static class ExitCodeResolver
{
    /// <summary>
    /// Gets the exit code for a specific exception type based on the resource's exit code attributes.
    /// </summary>
    /// <typeparam name="T">The schema type of the resource.</typeparam>
    /// <param name="resource">The DSC resource instance.</param>
    /// <param name="exceptionType">The type of exception to get the exit code for.</param>
    /// <returns>The exit code that corresponds to the exception type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no matching exit code attribute is found.</exception>
    public static int GetExitCode<T>(IDscResource<T> resource, Type exceptionType)
    {
        var attrs = resource.GetType().GetCustomAttributes<ExitCodeAttribute>()
                    ?? throw new InvalidOperationException($"No '{nameof(ExitCodeAttribute)}' attribute defined.");

        Type? bestMatch = null;
        int? bestCode = null;
        int bestDepth = int.MaxValue;

        foreach (var attr in attrs)
        {
            if (attr.Exception == null)
            {
                continue;
            }

            if (attr.Exception == exceptionType)
            {
                return attr.ExitCode;
            }

            // 2. Assignable match (skip System.Exception unless nothing else matches)
            if (attr.Exception.IsAssignableFrom(exceptionType))
            {
                int depth = GetInheritanceDistance(attr.Exception, exceptionType);

                if (depth >= 0 && depth < bestDepth)
                {
                    if (attr.Exception != typeof(Exception)) // prefer non-generic matches
                    {
                        bestMatch = attr.Exception;
                        bestCode = attr.ExitCode;
                        bestDepth = depth;
                    }
                    else if (bestMatch == null) // only accept Exception if no better match
                    {
                        bestMatch = attr.Exception;
                        bestCode = attr.ExitCode;
                        bestDepth = depth;
                    }
                }
            }
        }

        return bestCode ?? throw new InvalidOperationException();
    }

    private static int GetInheritanceDistance(Type baseType, Type derivedType)
    {
        int depth = 0;
        Type? current = derivedType;

        while (current != null)
        {
            if (current == baseType)
            {
                return depth;
            }

            current = current.BaseType;
            depth++;
        }

        return -1; // Not related
    }
}
