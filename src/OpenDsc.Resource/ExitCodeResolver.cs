// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;

namespace OpenDsc.Resource;

public static class ExitCodeResolver
{
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
