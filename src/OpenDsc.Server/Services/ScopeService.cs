// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Parameters;
using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public sealed partial class ScopeService(ServerDbContext db) : IScopeService
{
    public async Task<IReadOnlyList<ScopeTypeDetails>> GetScopeTypesAsync(CancellationToken cancellationToken = default)
    {
        var scopeTypes = await db.ScopeTypes
            .AsNoTracking()
            .OrderBy(st => st.Precedence)
            .ToListAsync(cancellationToken);

        var counts = await db.ParameterFiles
            .AsNoTracking()
            .GroupBy(pf => pf.ScopeTypeId)
            .Select(g => new { ScopeTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ScopeTypeId, x => x.Count, cancellationToken);

        return scopeTypes.Select(st => ToScopeTypeDetails(st, counts.GetValueOrDefault(st.Id, 0))).ToList();
    }

    public async Task<ScopeTypeDetails> GetScopeTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scopeType = await db.ScopeTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.Id == id, cancellationToken);

        if (scopeType is null)
        {
            throw new KeyNotFoundException("Scope type not found.");
        }

        var count = await db.ParameterFiles.CountAsync(pf => pf.ScopeTypeId == id, cancellationToken);
        return ToScopeTypeDetails(scopeType, count);
    }

    public async Task<ScopeTypeDetails> CreateScopeTypeAsync(
        CreateScopeTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Scope type name is required");
        }

        if (!ScopeNameRegex().IsMatch(request.Name))
        {
            throw new ArgumentException("Scope type name can only contain alphanumeric characters, hyphens, and underscores");
        }

        if (await db.ScopeTypes.AnyAsync(st => st.Name == request.Name, cancellationToken))
        {
            throw new InvalidOperationException($"Scope type '{request.Name}' already exists");
        }

        var allScopeTypes = await db.ScopeTypes.OrderBy(st => st.Precedence).ToListAsync(cancellationToken);
        var nodeScopeType = allScopeTypes.FirstOrDefault(st => st.Name == "Node");

        int newPrecedence;
        if (nodeScopeType != null)
        {
            newPrecedence = nodeScopeType.Precedence;
            foreach (var scopeType in allScopeTypes.Where(st => st.Precedence >= newPrecedence))
            {
                scopeType.Precedence++;
                scopeType.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
        else
        {
            newPrecedence = allScopeTypes.Count > 0 ? allScopeTypes.Max(st => st.Precedence) + 1 : 0;
        }

        var scope = new ScopeType
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Precedence = newPrecedence,
            IsSystem = false,
            ValueMode = request.ValueMode ?? ScopeValueMode.Unrestricted,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ScopeTypes.Add(scope);
        await db.SaveChangesAsync(cancellationToken);

        return ToScopeTypeDetails(scope, 0);
    }

    public async Task<ScopeTypeDetails> UpdateScopeTypeAsync(
        Guid id,
        UpdateScopeTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var scopeType = await db.ScopeTypes.FirstOrDefaultAsync(st => st.Id == id, cancellationToken);
        if (scopeType is null)
        {
            throw new KeyNotFoundException("Scope type not found.");
        }

        if (scopeType.IsSystem)
        {
            throw new InvalidOperationException("Cannot update system scope types");
        }

        scopeType.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        scopeType.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var count = await db.ParameterFiles.CountAsync(pf => pf.ScopeTypeId == id, cancellationToken);
        return ToScopeTypeDetails(scopeType, count);
    }

    public async Task<IReadOnlyList<ScopeTypeDetails>> ReorderScopeTypesAsync(
        ReorderScopeTypesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ScopeTypeIds is null || request.ScopeTypeIds.Count == 0)
        {
            throw new ArgumentException("Scope type IDs array is required");
        }

        var scopeTypes = await db.ScopeTypes
            .Where(st => request.ScopeTypeIds.Contains(st.Id))
            .ToListAsync(cancellationToken);

        if (scopeTypes.Count != request.ScopeTypeIds.Count)
        {
            throw new ArgumentException("Some scope type IDs were not found");
        }

        // Extract system scopes and reorder only custom scopes
        var defaultScopeType = scopeTypes.FirstOrDefault(st => st.Name == "Default");
        var nodeScopeType = scopeTypes.FirstOrDefault(st => st.Name == "Node");
        var customScopes = scopeTypes.Where(st => st.Name != "Default" && st.Name != "Node").ToList();

        // Rebuild ordered list: Default first, custom scopes in requested order, Node last
        var orderedIds = new List<Guid>();
        if (defaultScopeType != null)
        {
            orderedIds.Add(defaultScopeType.Id);
        }

        // Add custom scopes in the order they appear in the request
        foreach (var id in request.ScopeTypeIds)
        {
            if (customScopes.Any(st => st.Id == id))
            {
                orderedIds.Add(id);
            }
        }

        if (nodeScopeType != null)
        {
            orderedIds.Add(nodeScopeType.Id);
        }

        var now = DateTimeOffset.UtcNow;

        for (int i = 0; i < orderedIds.Count; i++)
        {
            var scopeType = scopeTypes.First(st => st.Id == orderedIds[i]);
            scopeType.Precedence = -(i + 1);
            scopeType.UpdatedAt = now;
        }
        await db.SaveChangesAsync(cancellationToken);

        for (int i = 0; i < orderedIds.Count; i++)
        {
            var scopeType = scopeTypes.First(st => st.Id == orderedIds[i]);
            int precedence;
            if (i == 0)
            {
                // Default scope gets lowest precedence (baseline, overridden by everything)
                precedence = 0;
            }
            else if (i == orderedIds.Count - 1)
            {
                // Node scope gets highest precedence (most specific, overrides everything)
                precedence = orderedIds.Count - 1;
            }
            else
            {
                // Custom scopes: reverse the indices so first custom scope has highest precedence
                precedence = orderedIds.Count - 1 - i;
            }
            scopeType.Precedence = precedence;
        }
        await db.SaveChangesAsync(cancellationToken);

        var counts = await db.ParameterFiles
            .AsNoTracking()
            .GroupBy(pf => pf.ScopeTypeId)
            .Select(g => new { ScopeTypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ScopeTypeId, x => x.Count, cancellationToken);

        // Return scopes in the enforced order (Default, custom in user order, Node)
        return orderedIds
            .Select(id => ToScopeTypeDetails(scopeTypes.First(st => st.Id == id), counts.GetValueOrDefault(id, 0)))
            .ToList();
    }

    public async Task DeleteScopeTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scopeType = await db.ScopeTypes
            .Include(st => st.ScopeValues)
            .ThenInclude(sv => sv.NodeTags)
            .Include(st => st.ParameterFiles)
            .FirstOrDefaultAsync(st => st.Id == id, cancellationToken);

        if (scopeType is null)
        {
            throw new KeyNotFoundException("Scope type not found.");
        }

        if (scopeType.IsSystem)
        {
            throw new InvalidOperationException($"Cannot delete system scope type '{scopeType.Name}'");
        }

        if (scopeType.ScopeValues.Any(v => v.NodeTags.Any()))
        {
            throw new InvalidOperationException(
                $"Cannot delete scope type '{scopeType.Name}' because one or more values are assigned to nodes");
        }

        if (scopeType.ParameterFiles.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete scope type '{scopeType.Name}' because it has {scopeType.ParameterFiles.Count} parameter files");
        }

        var allScopeTypes = await db.ScopeTypes.OrderBy(st => st.Precedence).ToListAsync(cancellationToken);
        var deletedPrecedence = scopeType.Precedence;

        db.ScopeTypes.Remove(scopeType);

        foreach (var current in allScopeTypes.Where(st => st.Precedence > deletedPrecedence))
        {
            current.Precedence--;
            current.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ScopeTypeDetails> EnableScopeTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scopeType = await db.ScopeTypes.FirstOrDefaultAsync(st => st.Id == id, cancellationToken);
        if (scopeType is null)
        {
            throw new KeyNotFoundException("Scope type not found.");
        }

        if (!scopeType.IsSystem)
        {
            throw new InvalidOperationException("Only system scope types support enable/disable");
        }

        scopeType.IsEnabled = true;
        scopeType.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var count = await db.ParameterFiles.CountAsync(pf => pf.ScopeTypeId == id, cancellationToken);
        return ToScopeTypeDetails(scopeType, count);
    }

    public async Task<ScopeTypeDetails> DisableScopeTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scopeType = await db.ScopeTypes.FirstOrDefaultAsync(st => st.Id == id, cancellationToken);
        if (scopeType is null)
        {
            throw new KeyNotFoundException("Scope type not found.");
        }

        if (!scopeType.IsSystem)
        {
            throw new InvalidOperationException("Only system scope types support enable/disable");
        }

        var publishedCount = await db.ParameterFiles
            .CountAsync(pf => pf.ScopeTypeId == id && pf.Status == ParameterVersionStatus.Published, cancellationToken);

        if (publishedCount > 0)
        {
            throw new InvalidOperationException(
                $"Cannot disable scope type '{scopeType.Name}': {publishedCount} published parameter file(s) exist. Archive or delete them first.");
        }

        scopeType.IsEnabled = false;
        scopeType.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var count = await db.ParameterFiles.CountAsync(pf => pf.ScopeTypeId == id, cancellationToken);
        return ToScopeTypeDetails(scopeType, count);
    }

    public async Task<IReadOnlyList<ScopeValueDetails>> GetScopeValuesAsync(
        Guid scopeTypeId,
        CancellationToken cancellationToken = default)
    {
        var scopeTypeExists = await db.ScopeTypes.AnyAsync(st => st.Id == scopeTypeId, cancellationToken);
        if (!scopeTypeExists)
        {
            throw new KeyNotFoundException("Scope type not found.");
        }

        var values = await db.ScopeValues
            .AsNoTracking()
            .Where(sv => sv.ScopeTypeId == scopeTypeId)
            .OrderBy(sv => sv.Value)
            .ToListAsync(cancellationToken);

        return await AddScopeValueUsageAsync(values, cancellationToken);
    }

    public async Task<ScopeValueDetails> GetScopeValueAsync(
        Guid scopeTypeId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var value = await db.ScopeValues
            .AsNoTracking()
            .FirstOrDefaultAsync(sv => sv.Id == id && sv.ScopeTypeId == scopeTypeId, cancellationToken);

        if (value is null)
        {
            throw new KeyNotFoundException("Scope value not found.");
        }

        var list = await AddScopeValueUsageAsync([value], cancellationToken);
        return list[0];
    }

    public async Task<ScopeValueDetails> CreateScopeValueAsync(
        Guid scopeTypeId,
        CreateScopeValueRequest request,
        CancellationToken cancellationToken = default)
    {
        var scopeType = await db.ScopeTypes.FindAsync([scopeTypeId], cancellationToken);
        if (scopeType is null)
        {
            throw new KeyNotFoundException("Scope type not found.");
        }

        if (scopeType.ValueMode != ScopeValueMode.Restricted)
        {
            throw new ArgumentException(
                $"Scope type '{scopeType.Name}' does not use restricted values. Only restricted scope types can have predefined values.");
        }

        if (string.IsNullOrWhiteSpace(request.Value))
        {
            throw new ArgumentException("Scope value is required");
        }

        if (!ScopeNameRegex().IsMatch(request.Value))
        {
            throw new ArgumentException("Scope value can only contain alphanumeric characters, hyphens, and underscores");
        }

        if (await db.ScopeValues.AnyAsync(
            sv => sv.ScopeTypeId == scopeTypeId && sv.Value == request.Value,
            cancellationToken))
        {
            throw new InvalidOperationException($"Scope value '{request.Value}' already exists for this scope type");
        }

        var scopeValue = new ScopeValue
        {
            Id = Guid.NewGuid(),
            ScopeTypeId = scopeTypeId,
            Value = request.Value,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ScopeValues.Add(scopeValue);
        await db.SaveChangesAsync(cancellationToken);

        return new ScopeValueDetails
        {
            Id = scopeValue.Id,
            ScopeTypeId = scopeValue.ScopeTypeId,
            Value = scopeValue.Value,
            Description = scopeValue.Description,
            CreatedAt = scopeValue.CreatedAt,
            UpdatedAt = scopeValue.UpdatedAt,
            NodeTagCount = 0,
            ParameterFileCount = 0
        };
    }

    public async Task<ScopeValueDetails> UpdateScopeValueAsync(
        Guid scopeTypeId,
        Guid id,
        UpdateScopeValueRequest request,
        CancellationToken cancellationToken = default)
    {
        var scopeValue = await db.ScopeValues
            .FirstOrDefaultAsync(sv => sv.Id == id && sv.ScopeTypeId == scopeTypeId, cancellationToken);

        if (scopeValue is null)
        {
            throw new KeyNotFoundException("Scope value not found.");
        }

        scopeValue.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        scopeValue.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var result = await AddScopeValueUsageAsync([scopeValue], cancellationToken);
        return result[0];
    }

    public async Task DeleteScopeValueAsync(Guid scopeTypeId, Guid id, CancellationToken cancellationToken = default)
    {
        var scopeValue = await db.ScopeValues
            .Include(sv => sv.NodeTags)
            .FirstOrDefaultAsync(sv => sv.Id == id && sv.ScopeTypeId == scopeTypeId, cancellationToken);

        if (scopeValue is null)
        {
            throw new KeyNotFoundException("Scope value not found.");
        }

        if (scopeValue.NodeTags.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete scope value '{scopeValue.Value}' because it is assigned to {scopeValue.NodeTags.Count} nodes");
        }

        var parameterCount = await db.ParameterFiles
            .CountAsync(pf => pf.ScopeTypeId == scopeTypeId && pf.ScopeValue == scopeValue.Value, cancellationToken);

        if (parameterCount > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete scope value '{scopeValue.Value}' because it has {parameterCount} parameter files");
        }

        db.ScopeValues.Remove(scopeValue);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> GetScopeTypeUsageCountAsync(Guid scopeTypeId, CancellationToken cancellationToken = default)
    {
        return db.ParameterFiles.CountAsync(pf => pf.ScopeTypeId == scopeTypeId, cancellationToken);
    }

    public async Task<int> GetScopeValueUsageCountAsync(Guid scopeValueId, CancellationToken cancellationToken = default)
    {
        var scopeValue = await db.ScopeValues
            .AsNoTracking()
            .FirstOrDefaultAsync(sv => sv.Id == scopeValueId, cancellationToken);

        if (scopeValue is null)
        {
            throw new KeyNotFoundException("Scope value not found.");
        }

        return await db.ParameterFiles
            .CountAsync(pf => pf.ScopeTypeId == scopeValue.ScopeTypeId && pf.ScopeValue == scopeValue.Value, cancellationToken);
    }

    public async Task<ScopeSummaryResponse> GetScopeSummaryAsync(CancellationToken cancellationToken = default)
    {
        var scopeTypes = await GetScopeTypesAsync(cancellationToken);
        var scopeValues = await db.ScopeValues
            .AsNoTracking()
            .OrderBy(sv => sv.Value)
            .ToListAsync(cancellationToken);

        var values = await AddScopeValueUsageAsync(scopeValues, cancellationToken);
        var nodeCount = await db.Nodes.CountAsync(cancellationToken);

        return new ScopeSummaryResponse
        {
            ScopeTypes = scopeTypes,
            ScopeValues = values,
            NodeCount = nodeCount
        };
    }

    public async Task<IReadOnlyList<ScopeTypeWithValuesDetails>> GetAllScopeTypesWithValuesAsync(
        CancellationToken cancellationToken = default)
    {
        var summary = await GetScopeSummaryAsync(cancellationToken);

        return summary.ScopeTypes
            .Select(scopeType => new ScopeTypeWithValuesDetails
            {
                ScopeType = scopeType,
                Values = summary.ScopeValues.Where(value => value.ScopeTypeId == scopeType.Id).ToList()
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ScopeNodeInfo>> GetScopeNodesAsync(
        Guid scopeTypeId,
        CancellationToken cancellationToken = default)
    {
        var scopeType = await db.ScopeTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.Id == scopeTypeId, cancellationToken);

        if (scopeType is null)
        {
            throw new KeyNotFoundException("Scope type not found.");
        }

        if (scopeType.Name == "Node")
        {
            return await db.Nodes
                .AsNoTracking()
                .OrderBy(node => node.Fqdn)
                .Select(node => new ScopeNodeInfo { Id = node.Id, Fqdn = node.Fqdn })
                .ToListAsync(cancellationToken);
        }

        return await db.NodeTags
            .AsNoTracking()
            .Where(tag => tag.ScopeValue.ScopeTypeId == scopeTypeId)
            .Select(tag => new ScopeNodeInfo
            {
                Id = tag.Node.Id,
                Fqdn = tag.Node.Fqdn
            })
            .Distinct()
            .OrderBy(node => node.Fqdn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScopeParameterInfo>> GetScopeParametersAsync(
        Guid schemaId,
        Guid scopeTypeId,
        string? scopeValue,
        CancellationToken cancellationToken = default)
    {
        var query = db.ParameterFiles
            .AsNoTracking()
            .Where(pf => pf.ParameterSchemaId == schemaId && pf.ScopeTypeId == scopeTypeId);

        if (!string.IsNullOrWhiteSpace(scopeValue))
        {
            query = query.Where(pf => pf.ScopeValue == scopeValue);
        }

        return await query
            .Where(pf => pf.ScopeValue != null)
            .Select(pf => pf.ScopeValue!)
            .Distinct()
            .OrderBy(value => value)
            .Select(value => new ScopeParameterInfo { ScopeValue = value })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUnrestrictedScopeValuesAsync(
        Guid scopeTypeId,
        CancellationToken cancellationToken = default)
    {
        return await db.ParameterFiles
            .AsNoTracking()
            .Where(pf => pf.ScopeTypeId == scopeTypeId && pf.ScopeValue != null)
            .Select(pf => pf.ScopeValue!)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ScopeValueDetails>> AddScopeValueUsageAsync(
        IReadOnlyList<ScopeValue> values,
        CancellationToken cancellationToken)
    {
        if (values.Count == 0)
        {
            return [];
        }

        var ids = values.Select(v => v.Id).ToList();
        var nodeTagCounts = await db.NodeTags
            .AsNoTracking()
            .Where(tag => ids.Contains(tag.ScopeValueId))
            .GroupBy(tag => tag.ScopeValueId)
            .Select(group => new { ScopeValueId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.ScopeValueId, x => x.Count, cancellationToken);

        var parameterCounts = await db.ParameterFiles
            .AsNoTracking()
            .Where(pf => ids.Contains(pf.ScopeType.ScopeValues
                .Where(sv => sv.Value == pf.ScopeValue)
                .Select(sv => sv.Id)
                .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var parameterCountLookup = values.ToDictionary(
            value => value.Id,
            value => parameterCounts.Count(pf => pf.ScopeTypeId == value.ScopeTypeId && pf.ScopeValue == value.Value));

        return values.Select(value => new ScopeValueDetails
        {
            Id = value.Id,
            ScopeTypeId = value.ScopeTypeId,
            Value = value.Value,
            Description = value.Description,
            CreatedAt = value.CreatedAt,
            UpdatedAt = value.UpdatedAt,
            NodeTagCount = nodeTagCounts.GetValueOrDefault(value.Id, 0),
            ParameterFileCount = parameterCountLookup.GetValueOrDefault(value.Id, 0)
        }).ToList();
    }

    private static ScopeTypeDetails ToScopeTypeDetails(ScopeType scopeType, int parameterFileCount)
    {
        return new ScopeTypeDetails
        {
            Id = scopeType.Id,
            Name = scopeType.Name,
            Description = scopeType.Description,
            Precedence = scopeType.Precedence,
            IsSystem = scopeType.IsSystem,
            IsEnabled = scopeType.IsEnabled,
            ValueMode = scopeType.ValueMode,
            CreatedAt = scopeType.CreatedAt,
            UpdatedAt = scopeType.UpdatedAt,
            ParameterFileCount = parameterFileCount
        };
    }

    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    private static partial Regex ScopeNameRegex();
}
