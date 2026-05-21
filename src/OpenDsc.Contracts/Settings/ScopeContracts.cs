// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.Configurations;

namespace OpenDsc.Contracts.Settings;

/// <summary>
/// Scope type response.
/// </summary>
public sealed class ScopeTypeDetails
{
    /// <summary>
    /// The unique identifier for the scope type.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the scope type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the scope type.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The precedence order used when resolving overlapping scope assignments (lower is higher priority).
    /// </summary>
    public int Precedence { get; set; }

    /// <summary>
    /// Whether this is a system-defined scope type that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Whether this scope type is active.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// How scope values are managed for this type.
    /// </summary>
    public ScopeValueMode ValueMode { get; set; }

    /// <summary>
    /// When the scope type was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the scope type was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// The number of parameter files associated with this scope type.
    /// </summary>
    public int ParameterFileCount { get; set; }
}

/// <summary>
/// Request to create a scope type.
/// </summary>
public sealed class CreateScopeTypeRequest
{
    /// <summary>
    /// The name of the new scope type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the scope type.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// How scope values are managed for this type.
    /// </summary>
    public ScopeValueMode? ValueMode { get; set; }
}

/// <summary>
/// Request to update a scope type.
/// </summary>
public sealed class UpdateScopeTypeRequest
{
    /// <summary>
    /// Updated description of the scope type.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request to reorder scope types.
/// </summary>
public sealed class ReorderScopeTypesRequest
{
    /// <summary>
    /// The ordered list of scope type IDs.
    /// </summary>
    public IReadOnlyList<Guid> ScopeTypeIds { get; set; } = [];
}

/// <summary>
/// Scope value response.
/// </summary>
public sealed class ScopeValueDetails
{
    /// <summary>
    /// The unique identifier for the scope value.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The scope type this value belongs to.
    /// </summary>
    public Guid ScopeTypeId { get; set; }

    /// <summary>
    /// The scope value string.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the scope value.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When the scope value was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the scope value was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// The number of node tags using this scope value.
    /// </summary>
    public int NodeTagCount { get; set; }

    /// <summary>
    /// The number of parameter files associated with this scope value.
    /// </summary>
    public int ParameterFileCount { get; set; }
}

/// <summary>
/// Request to create a scope value.
/// </summary>
public sealed class CreateScopeValueRequest
{
    /// <summary>
    /// The scope value string.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the scope value.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request to update a scope value.
/// </summary>
public sealed class UpdateScopeValueRequest
{
    /// <summary>
    /// Updated description of the scope value.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Node information used for scope selection.
/// </summary>
public sealed class ScopeNodeInfo
{
    /// <summary>
    /// The node's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The node's fully qualified domain name.
    /// </summary>
    public string Fqdn { get; set; } = string.Empty;
}

/// <summary>
/// Scope parameter information for selector lookup.
/// </summary>
public sealed class ScopeParameterInfo
{
    /// <summary>
    /// The resolved scope value string for parameter selection.
    /// </summary>
    public string ScopeValue { get; set; } = string.Empty;
}

/// <summary>
/// Scope type with nested values.
/// </summary>
public sealed class ScopeTypeWithValuesDetails
{
    /// <summary>
    /// The scope type details.
    /// </summary>
    public ScopeTypeDetails ScopeType { get; set; } = null!;

    /// <summary>
    /// The list of scope values belonging to this scope type.
    /// </summary>
    public IReadOnlyList<ScopeValueDetails> Values { get; set; } = [];
}

/// <summary>
/// Aggregated scope summary for settings pages.
/// </summary>
public sealed class ScopeSummaryResponse
{
    /// <summary>
    /// All configured scope types.
    /// </summary>
    public IReadOnlyList<ScopeTypeDetails> ScopeTypes { get; set; } = [];

    /// <summary>
    /// All configured scope values.
    /// </summary>
    public IReadOnlyList<ScopeValueDetails> ScopeValues { get; set; } = [];

    /// <summary>
    /// Total number of nodes with at least one scope tag.
    /// </summary>
    public int NodeCount { get; set; }
}
