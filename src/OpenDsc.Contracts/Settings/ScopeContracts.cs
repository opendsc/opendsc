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
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Precedence { get; set; }
    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; }
    public ScopeValueMode ValueMode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int ParameterFileCount { get; set; }
}

/// <summary>
/// Request to create a scope type.
/// </summary>
public sealed class CreateScopeTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ScopeValueMode? ValueMode { get; set; }
}

/// <summary>
/// Request to update a scope type.
/// </summary>
public sealed class UpdateScopeTypeRequest
{
    public string? Description { get; set; }
}

/// <summary>
/// Request to reorder scope types.
/// </summary>
public sealed class ReorderScopeTypesRequest
{
    public List<Guid> ScopeTypeIds { get; set; } = [];
}

/// <summary>
/// Scope value response.
/// </summary>
public sealed class ScopeValueDetails
{
    public Guid Id { get; set; }
    public Guid ScopeTypeId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int NodeTagCount { get; set; }
    public int ParameterFileCount { get; set; }
}

/// <summary>
/// Request to create a scope value.
/// </summary>
public sealed class CreateScopeValueRequest
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Request to update a scope value.
/// </summary>
public sealed class UpdateScopeValueRequest
{
    public string? Description { get; set; }
}

/// <summary>
/// Node information used for scope selection.
/// </summary>
public sealed class ScopeNodeInfo
{
    public Guid Id { get; set; }
    public string Fqdn { get; set; } = string.Empty;
}

/// <summary>
/// Scope parameter information for selector lookup.
/// </summary>
public sealed class ScopeParameterInfo
{
    public string ScopeValue { get; set; } = string.Empty;
}

/// <summary>
/// Scope type with nested values.
/// </summary>
public sealed class ScopeTypeWithValuesDetails
{
    public ScopeTypeDetails ScopeType { get; set; } = null!;
    public IReadOnlyList<ScopeValueDetails> Values { get; set; } = [];
}

/// <summary>
/// Aggregated scope summary for settings pages.
/// </summary>
public sealed class ScopeSummaryResponse
{
    public IReadOnlyList<ScopeTypeDetails> ScopeTypes { get; set; } = [];
    public IReadOnlyList<ScopeValueDetails> ScopeValues { get; set; } = [];
    public int NodeCount { get; set; }
}
