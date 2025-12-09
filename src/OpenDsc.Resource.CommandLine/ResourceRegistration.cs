// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource.CommandLine;

internal sealed class ResourceRegistration
{
    internal string Type { get; }

    internal Type ResourceType { get; }

    internal Type SchemaType { get; }

    internal DscResourceAttribute Metadata { get; }

    internal Action<string>? GetAction { get; }

    internal Action<string>? SetAction { get; }

    internal Action<string>? TestAction { get; }

    internal Action<string>? DeleteAction { get; }

    internal Action? ExportAction { get; }

    internal Func<string>? SchemaFunc { get; }

    internal Func<Type, int> ExitCodeResolver { get; }

    internal ResourceRegistration(
        string type,
        Type resourceType,
        Type schemaType,
        DscResourceAttribute metadata,
        Action<string>? getAction,
        Action<string>? setAction,
        Action<string>? testAction,
        Action<string>? deleteAction,
        Action? exportAction,
        Func<string>? schemaFunc,
        Func<Type, int> exitCodeResolver)
    {
        Type = type;
        ResourceType = resourceType;
        SchemaType = schemaType;
        Metadata = metadata;
        GetAction = getAction;
        SetAction = setAction;
        TestAction = testAction;
        DeleteAction = deleteAction;
        ExportAction = exportAction;
        SchemaFunc = schemaFunc;
        ExitCodeResolver = exitCodeResolver;
    }
}
