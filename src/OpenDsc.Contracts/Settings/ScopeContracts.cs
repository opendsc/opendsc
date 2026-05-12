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
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int Precedence { get; init; }
    public required bool IsSystem { get; init; }
    public required bool IsEnabled { get; init; }
    public required ScopeValueMode ValueMode { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public int ParameterFileCount { get; init; }
}

/// <summary>
/// Request to create a scope type.
/// </summary>
public sealed class CreateScopeTypeRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public ScopeValueMode? ValueMode { get; init; }
}

/// <summary>
/// Request to update a scope type.
/// </summary>
public sealed class UpdateScopeTypeRequest
{
    public string? Description { get; init; }
}

/// <summary>
/// Request to reorder scope types.
/// </summary>
public sealed class ReorderScopeTypesRequest
{
    public required List<Guid> ScopeTypeIds { get; init; }
}

/// <summary>
/// Scope value response.
/// </summary>
public sealed class ScopeValueDetails
{
    public required Guid Id { get; init; }
    public required Guid ScopeTypeId { get; init; }
    public required string Value { get; init; }
    public string? Description { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public int NodeTagCount { get; init; }
    public int ParameterFileCount { get; init; }
}

/// <summary>
/// Request to create a scope value.
/// </summary>
public sealed class CreateScopeValueRequest
{
    public required string Value { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Request to update a scope value.
/// </summary>
public sealed class UpdateScopeValueRequest
{
    public string? Description { get; init; }
}

/// <summary>
/// Node information used for scope selection.
/// </summary>
public sealed class ScopeNodeInfo
{
    public required Guid Id { get; init; }
    public required string Fqdn { get; init; }
}

/// <summary>
/// Scope parameter information for selector lookup.
/// </summary>
public sealed class ScopeParameterInfo
{
    public required string ScopeValue { get; init; }
}

/// <summary>
/// Scope type with nested values.
/// </summary>
public sealed class ScopeTypeWithValuesDetails
{
    public required ScopeTypeDetails ScopeType { get; init; }
    public required IReadOnlyList<ScopeValueDetails> Values { get; init; }
}

/// <summary>
/// Aggregated scope summary for settings pages.
/// </summary>
public sealed class ScopeSummaryResponse
{
    public required IReadOnlyList<ScopeTypeDetails> ScopeTypes { get; init; }
    public required IReadOnlyList<ScopeValueDetails> ScopeValues { get; init; }
    public required int NodeCount { get; init; }
}
