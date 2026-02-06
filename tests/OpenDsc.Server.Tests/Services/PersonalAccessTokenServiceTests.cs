// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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

        _dbContext = new ServerDbContext(options, NullLogger<ServerDbContext>.Instance);
        _tokenService = new PersonalAccessTokenService(_dbContext);

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
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        var token = await _tokenService.CreateTokenAsync(_testUserId, name, expiresAt);

        token.Should().NotBeNullOrEmpty();
        token.Should().StartWith("pat_");
        token.Length.Should().Be(44); // "pat_" + 40 characters

        var savedToken = await _dbContext.PersonalAccessTokens
            .FirstOrDefaultAsync(t => t.UserId == _testUserId && t.Name == name);

        savedToken.Should().NotBeNull();
        savedToken!.Name.Should().Be(name);
        savedToken.ExpiresAt.Should().Be(expiresAt);
        savedToken.LastUsedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateTokenAsync_WithoutExpiry_CreatesNonExpiringToken()
    {
        var name = "Non-Expiring Token";

        var token = await _tokenService.CreateTokenAsync(_testUserId, name, null);

        token.Should().NotBeNullOrEmpty();

        var savedToken = await _dbContext.PersonalAccessTokens
            .FirstOrDefaultAsync(t => t.UserId == _testUserId && t.Name == name);

        savedToken.Should().NotBeNull();
        savedToken!.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsUserId()
    {
        var token = await _tokenService.CreateTokenAsync(_testUserId, "Valid Token", null);

        var userId = await _tokenService.ValidateTokenAsync(token);

        userId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ReturnsNull()
    {
        var userId = await _tokenService.ValidateTokenAsync("pat_invalidtoken123456789012345678901234");

        userId.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ReturnsNull()
    {
        var token = await _tokenService.CreateTokenAsync(_testUserId, "Expired Token", DateTimeOffset.UtcNow.AddDays(-1));

        var userId = await _tokenService.ValidateTokenAsync(token);

        userId.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_UpdatesLastUsedAt()
    {
        var token = await _tokenService.CreateTokenAsync(_testUserId, "Usage Token", null);
        var beforeValidation = DateTimeOffset.UtcNow;

        await _tokenService.ValidateTokenAsync(token);

        var savedToken = await _dbContext.PersonalAccessTokens
            .FirstOrDefaultAsync(t => t.Name == "Usage Token");

        savedToken.Should().NotBeNull();
        savedToken!.LastUsedAt.Should().NotBeNull();
        savedToken.LastUsedAt.Should().BeOnOrAfter(beforeValidation);
    }

    [Fact]
    public async Task RevokeTokenAsync_RevokesToken()
    {
        var token = await _tokenService.CreateTokenAsync(_testUserId, "Revoke Token", null);
        var tokenId = (await _dbContext.PersonalAccessTokens.FirstAsync(t => t.Name == "Revoke Token")).Id;

        var result = await _tokenService.RevokeTokenAsync(tokenId, _testUserId);

        result.Should().BeTrue();

        var userId = await _tokenService.ValidateTokenAsync(token);
        userId.Should().BeNull();
    }

    [Fact]
    public async Task RevokeTokenAsync_WithWrongUser_ReturnsFalse()
    {
        var token = await _tokenService.CreateTokenAsync(_testUserId, "Other User Token", null);
        var tokenId = (await _dbContext.PersonalAccessTokens.FirstAsync(t => t.Name == "Other User Token")).Id;
        var otherUserId = Guid.NewGuid();

        var result = await _tokenService.RevokeTokenAsync(tokenId, otherUserId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserTokensAsync_ReturnsUserTokens()
    {
        await _tokenService.CreateTokenAsync(_testUserId, "Token 1", null);
        await _tokenService.CreateTokenAsync(_testUserId, "Token 2", DateTimeOffset.UtcNow.AddDays(7));

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
