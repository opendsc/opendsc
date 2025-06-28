// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource;

public static class ExitCodeResolver
{
    public static int GetExitCode(IDictionary<int, ResourceExitCode> exitCodes, Type exceptionType)
    {
        ResourceExitCode? bestMatch = null;
        int? bestCode = null;
        int bestDepth = int.MaxValue;

        foreach (var kvp in exitCodes)
        {
            var mappedType = kvp.Value.Exception;

            if (mappedType == null)
                continue;

            if (mappedType == exceptionType)
            {
                return kvp.Key;
            }

            // 2. Assignable match (skip System.Exception unless nothing else matches)
            if (mappedType.IsAssignableFrom(exceptionType))
            {
                int depth = GetInheritanceDistance(mappedType, exceptionType);

                if (depth >= 0 && depth < bestDepth)
                {
                    if (mappedType != typeof(Exception)) // prefer non-generic matches
                    {
                        bestMatch = kvp.Value;
                        bestCode = kvp.Key;
                        bestDepth = depth;
                    }
                    else if (bestMatch == null) // only accept Exception if no better match
                    {
                        bestMatch = kvp.Value;
                        bestCode = kvp.Key;
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
