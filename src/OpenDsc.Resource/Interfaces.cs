// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource;

/// <summary>
/// Base interface for DSC resources that defines core serialization and schema operations.
/// </summary>
/// <typeparam name="T">The schema type that defines the resource's properties.</typeparam>
public interface IDscResource<T>
{
    /// <summary>
    /// Gets the JSON schema for the resource.
    /// </summary>
    /// <returns>A JSON string representing the resource schema.</returns>
    string GetSchema();

    /// <summary>
    /// Serializes a resource instance to a JSON string.
    /// </summary>
    /// <param name="instance">The resource instance to serialize.</param>
    /// <returns>A JSON string representation of the resource instance.</returns>
    string ToJson(T instance);

    /// <summary>
    /// Parses a JSON string into a resource instance.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The deserialized resource instance.</returns>
    T Parse(string json);
}

/// <summary>
/// Defines a resource that supports retrieving its current state.
/// </summary>
/// <typeparam name="T">The schema type that defines the resource's properties.</typeparam>
public interface IGettable<T> : IDscResource<T>
{
    /// <summary>
    /// Retrieves the current state of the resource.
    /// </summary>
    /// <param name="instance">The desired state instance containing identifying properties.</param>
    /// <returns>The actual current state of the resource.</returns>
    T Get(T instance);
}

/// <summary>
/// Defines a resource that supports applying configuration changes.
/// </summary>
/// <typeparam name="T">The schema type that defines the resource's properties.</typeparam>
public interface ISettable<T> : IDscResource<T>
{
    /// <summary>
    /// Applies the desired state to the resource.
    /// </summary>
    /// <param name="instance">The desired state to apply.</param>
    /// <returns>A <see cref="SetResult{T}"/> containing the actual state after changes and optionally the list of changed properties, or null if no changes were made.</returns>
    SetResult<T>? Set(T instance);
}

/// <summary>
/// Defines a resource that supports simulating configuration changes without applying them.
/// </summary>
/// <typeparam name="T">The schema type that defines the resource's properties.</typeparam>
public interface ISettableWhatIf<T> : ISettable<T>
{
    /// <summary>
    /// Simulates applying the desired state to the resource without making actual changes.
    /// </summary>
    /// <param name="instance">The desired state to simulate.</param>
    /// <returns>A <see cref="SetResult{T}"/> containing what the state would be and what properties would change.</returns>
    SetResult<T> SetWhatIf(T instance);
}

/// <summary>
/// Defines a resource that supports deletion or removal.
/// </summary>
/// <typeparam name="T">The schema type that defines the resource's properties.</typeparam>
public interface IDeletable<T> : IDscResource<T>
{
    /// <summary>
    /// Deletes or removes the resource.
    /// </summary>
    /// <param name="instance">The instance containing identifying properties of the resource to delete.</param>
    void Delete(T instance);
}

/// <summary>
/// Defines a resource that supports testing for configuration drift.
/// </summary>
/// <typeparam name="T">The schema type that defines the resource's properties.</typeparam>
public interface ITestable<T> : IDscResource<T>
{
    /// <summary>
    /// Tests whether the resource is in the desired state.
    /// </summary>
    /// <param name="instance">The desired state to test against.</param>
    /// <returns>A <see cref="TestResult{T}"/> containing the actual state and optionally the list of differing properties.</returns>
    TestResult<T> Test(T instance);
}

/// <summary>
/// Defines a resource that supports exporting its current configuration.
/// </summary>
/// <typeparam name="T">The schema type that defines the resource's properties.</typeparam>
public interface IExportable<T> : IDscResource<T>
{
    /// <summary>
    /// Exports all instances of the resource.
    /// </summary>
    /// <returns>An enumerable collection of resource instances representing the current configuration.</returns>
    IEnumerable<T> Export();
}
