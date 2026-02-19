// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class PersonalAccessTokenServiceTests : IDisposable
{
    private readonly ServerDbContext _dbContext;
    private readonly IPersonalAccessTokenService _tokenService;
    private readonly Guid _testUserId = Guid.NewGuid();

    public PersonalAccessTokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ServerDbContext(options);
        var passwordHasher = new PasswordHasher();
        _tokenService = new PersonalAccessTokenService(_dbContext, passwordHasher);

        // Seed test user
        _dbContext.Users.Add(new User
        {
            Id = _testUserId,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            AccountType = AccountType.User,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateTokenAsync_CreatesValidToken()
    {
        var name = "Test Token";
        var scopes = new[] { "read", "write" };
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        var (token, _) = await _tokenService.CreateTokenAsync(_testUserId, name, scopes, expiresAt);

        token.Should().NotBeNullOrEmpty();
        token.Should().StartWith("pat_");
        token.Length.Should().Be(44); // "pat_" + 40 characters


        var savedToken = await _dbContext.PersonalAccessTokens
            .FirstOrDefaultAsync(t => t.UserId == _testUserId && t.Name == name);

        savedToken.Should().NotBeNull();
        savedToken!.Name.Should().Be(name);
        savedToken.ExpiresAt.Should().Be(expiresAt);
        savedToken.LastUsedAt.Should().BeNull();
        var savedScopes = JsonSerializer.Deserialize<string[]>(savedToken.Scopes);
        savedScopes.Should().BeEquivalentTo(scopes);
    }

    [Fact]
    public async Task CreateTokenAsync_WithoutExpiry_CreatesNonExpiringToken()
    {
        var name = "Non-Expiring Token";
        var scopes = new[] { "read" };

        var (token, metadata) = await _tokenService.CreateTokenAsync(_testUserId, name, scopes, null);

        token.Should().NotBeNullOrEmpty();

        var savedToken = await _dbContext.PersonalAccessTokens
            .FirstOrDefaultAsync(t => t.UserId == _testUserId && t.Name == name);

        savedToken.Should().NotBeNull();
        savedToken!.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsUserId()
    {
        var (token, metadata) = await _tokenService.CreateTokenAsync(_testUserId, "Valid Token", new[] { "read" }, null);

        var result = await _tokenService.ValidateTokenAsync(token);

        result.Should().NotBeNull();
        result!.Value.TokenId.Should().Be(metadata.Id);
        result!.Value.UserId.Should().Be(_testUserId);
        result!.Value.Scopes.Should().BeEquivalentTo(new[] { "read" });
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ReturnsNull()
    {
        var result = await _tokenService.ValidateTokenAsync("pat_invalidtoken123456789012345678901234");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ReturnsNull()
    {
        var (token, _) = await _tokenService.CreateTokenAsync(_testUserId, "Expired Token", new[] { "read" }, DateTimeOffset.UtcNow.AddDays(-1));

        var result = await _tokenService.ValidateTokenAsync(token);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeTokenAsync_RevokesToken()
    {
        var (token, _) = await _tokenService.CreateTokenAsync(_testUserId, "Revoke Token", new[] { "read" }, null);
        var tokenId = (await _dbContext.PersonalAccessTokens.FirstAsync(t => t.Name == "Revoke Token")).Id;

        await _tokenService.RevokeTokenAsync(tokenId);

        var revokedToken = await _dbContext.PersonalAccessTokens.FindAsync(tokenId);
        revokedToken.Should().NotBeNull();
        revokedToken!.IsRevoked.Should().BeTrue();
        revokedToken.RevokedAt.Should().NotBeNull();

        var result = await _tokenService.ValidateTokenAsync(token);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserTokensAsync_ReturnsUserTokens()
    {
        await _tokenService.CreateTokenAsync(_testUserId, "Token 1", Array.Empty<string>(), null);
        await _tokenService.CreateTokenAsync(_testUserId, "Token 2", Array.Empty<string>(), DateTimeOffset.UtcNow.AddDays(7));

        var tokens = await _tokenService.GetUserTokensAsync(_testUserId);

        tokens.Should().HaveCount(2);
        tokens.Should().Contain(t => t.Name == "Token 1");
        tokens.Should().Contain(t => t.Name == "Token 2");
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        GC.SuppressFinalize(this);
    }
}
