// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public sealed class RegistrationKeyService(ServerDbContext db) : IRegistrationKeyService
{
    public async Task<IReadOnlyList<RegistrationKeyResponse>> GetKeysAsync(CancellationToken cancellationToken = default)
    {
        var keys = await db.RegistrationKeys
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return keys.OrderByDescending(k => k.CreatedAt).Select(ToResponse).ToList();
    }

    public async Task<RegistrationKeyResponse> CreateKeyAsync(
        CreateRegistrationKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        var expiresAt = request.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7);
        var keyValue = KeyGenerator.GenerateRegistrationKey();

        var registrationKey = new RegistrationKey
        {
            Id = Guid.NewGuid(),
            Key = keyValue,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            MaxUses = request.MaxUses,
            CurrentUses = 0,
            IsRevoked = false,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        db.RegistrationKeys.Add(registrationKey);
        await db.SaveChangesAsync(cancellationToken);

        return new RegistrationKeyResponse
        {
            Id = registrationKey.Id,
            Key = keyValue,
            ExpiresAt = registrationKey.ExpiresAt,
            CreatedAt = registrationKey.CreatedAt,
            MaxUses = registrationKey.MaxUses,
            CurrentUses = registrationKey.CurrentUses,
            IsRevoked = registrationKey.IsRevoked,
            Description = registrationKey.Description
        };
    }

    public async Task<RegistrationKeyResponse> UpdateKeyAsync(
        Guid id,
        UpdateRegistrationKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        var key = await db.RegistrationKeys.FindAsync([id], cancellationToken);
        if (key is null)
        {
            throw new KeyNotFoundException("Registration key not found.");
        }

        key.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return ToResponseWithoutSecret(key);
    }

    public async Task RevokeKeyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var key = await db.RegistrationKeys.FindAsync([id], cancellationToken);
        if (key is null)
        {
            throw new KeyNotFoundException("Registration key not found.");
        }

        key.IsRevoked = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<RegistrationKeyResponse> RotateKeyAsync(CancellationToken cancellationToken = default)
    {
        return CreateKeyAsync(
            new CreateRegistrationKeyRequest
            {
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
            },
            cancellationToken);
    }

    private static RegistrationKeyResponse ToResponse(RegistrationKey key)
    {
        return new RegistrationKeyResponse
        {
            Id = key.Id,
            Key = key.Key,
            KeyPreview = key.Key is null ? null : key.Key.Substring(0, Math.Min(8, key.Key.Length)),
            ExpiresAt = key.ExpiresAt,
            CreatedAt = key.CreatedAt,
            MaxUses = key.MaxUses,
            CurrentUses = key.CurrentUses,
            IsRevoked = key.IsRevoked,
            Description = key.Description
        };
    }

    private static RegistrationKeyResponse ToResponseWithoutSecret(RegistrationKey key)
    {
        return new RegistrationKeyResponse
        {
            Id = key.Id,
            Key = null,
            KeyPreview = key.Key.Substring(0, Math.Min(8, key.Key.Length)),
            ExpiresAt = key.ExpiresAt,
            CreatedAt = key.CreatedAt,
            MaxUses = key.MaxUses,
            CurrentUses = key.CurrentUses,
            IsRevoked = key.IsRevoked,
            Description = key.Description
        };
    }
}
