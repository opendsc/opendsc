// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource.CommandLine;

internal sealed class ResourceRegistry
{
    private readonly Dictionary<string, ResourceRegistration> _resources = new(StringComparer.OrdinalIgnoreCase);

    internal void Register<TResource, TSchema>(TResource resource)
        where TResource : DscResource<TSchema>
        where TSchema : class
    {
        if (resource == null)
        {
            throw new ArgumentNullException(nameof(resource));
        }

        var resourceType = typeof(TResource);

        var attribute = resourceType.GetCustomAttributes(typeof(DscResourceAttribute), false)
            .Cast<DscResourceAttribute>()
            .FirstOrDefault() ?? throw new ArgumentException($"Resource type {resourceType.Name} must have DscResourceAttribute.", nameof(TResource));

        if (_resources.ContainsKey(attribute.Type))
        {
            throw new ArgumentException($"Resource type '{attribute.Type}' is already registered.", nameof(TResource));
        }

        Action<string>? getAction = resource is IGettable<TSchema>
            ? (string input) => ResourceExecutor<TResource, TSchema>.ExecuteGet(resource, input)
            : null;

        Action<string>? setAction = resource is ISettable<TSchema>
            ? (string input) => ResourceExecutor<TResource, TSchema>.ExecuteSet(resource, input)
            : null;

        Action<string>? testAction = resource is ITestable<TSchema>
            ? (string input) => ResourceExecutor<TResource, TSchema>.ExecuteTest(resource, input)
            : null;

        Action<string>? deleteAction = resource is IDeletable<TSchema>
            ? (string input) => ResourceExecutor<TResource, TSchema>.ExecuteDelete(resource, input)
            : null;

        Action? exportAction = resource is IExportable<TSchema>
            ? () => ResourceExecutor<TResource, TSchema>.ExecuteExport(resource)
            : null;

        Func<string> schemaFunc = resource.GetSchema;

        int exitCodeResolver(Type exceptionType)
        {
            try
            {
                return ExitCodeResolver.GetExitCode(resource, exceptionType);
            }
            catch
            {
                return int.MaxValue;
            }
        }

        var registration = new ResourceRegistration(
            attribute.Type,
            typeof(TResource),
            typeof(TSchema),
            attribute,
            getAction,
            setAction,
            testAction,
            deleteAction,
            exportAction,
            schemaFunc,
            exitCodeResolver);

        _resources.Add(attribute.Type, registration);
    }

    internal ResourceRegistration? GetByType(string resourceType)
    {
        _resources.TryGetValue(resourceType, out var registration);
        return registration;
    }

    internal IReadOnlyList<ResourceRegistration> GetAll()
    {
        return [.. _resources.Values];
    }

    internal bool HasResources => _resources.Count > 0;

    internal int Count => _resources.Count;
}
