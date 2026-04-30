// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public sealed class OidcUserProvisioningServiceTests : IDisposable
{
    private readonly ServerDbContext _db;
    private readonly OidcUserProvisioningService _sut;

    public OidcUserProvisioningServiceTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new ServerDbContext(options);
        _sut = new OidcUserProvisioningService(_db, NullLogger<OidcUserProvisioningService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ProvisionOrGetUserAsync_NewUser_CreatesUserAndExternalLogin()
    {
        var user = await _sut.ProvisionOrGetUserAsync("entra", "sub-001", "Alice", "alice@example.com", "alice", TestContext.Current.CancellationToken);

        user.Should().NotBeNull();
        user.Username.Should().Be("alice");
        user.Email.Should().Be("alice@example.com");
        user.IsActive.Should().BeTrue();
        user.RequirePasswordChange.Should().BeFalse();
        user.PasswordHash.Should().BeNull();

        var login = await _db.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken);
        login.Provider.Should().Be("entra");
        login.ProviderKey.Should().Be("sub-001");
        login.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task ProvisionOrGetUserAsync_ExistingUser_ReturnsExistingUserWithoutDuplicate()
    {
        var first = await _sut.ProvisionOrGetUserAsync("entra", "sub-002", null, "bob@example.com", "bob", TestContext.Current.CancellationToken);

        var second = await _sut.ProvisionOrGetUserAsync("entra", "sub-002", null, "bob@example.com", "bob", TestContext.Current.CancellationToken);

        second.Id.Should().Be(first.Id);
        (await _db.Users.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        (await _db.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task ProvisionOrGetUserAsync_UsernameCollision_AppendsSuffix()
    {
        // Seed a user with the same preferred username
        _db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Username = "carol",
            Email = "carol-existing@example.com",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var user = await _sut.ProvisionOrGetUserAsync("entra", "sub-003", null, "carol-new@example.com", "carol", TestContext.Current.CancellationToken);

        user.Username.Should().Be("carol1");
    }

    [Fact]
    public async Task ProvisionOrGetUserAsync_MultipleCollisions_IncrementsUntilUnique()
    {
        foreach (var suffix in new[] { "", "1", "2" })
        {
            _db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = $"dave{suffix}",
                Email = $"dave{suffix}@example.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var user = await _sut.ProvisionOrGetUserAsync("entra", "sub-004", null, "dave-new@example.com", "dave", TestContext.Current.CancellationToken);

        user.Username.Should().Be("dave3");
    }

    [Fact]
    public async Task ProvisionOrGetUserAsync_DerivesUsernameFromEmailPrefix_WhenNoPreferredUsername()
    {
        var user = await _sut.ProvisionOrGetUserAsync("entra", "sub-005", null, "eve.jones@example.com", null, TestContext.Current.CancellationToken);

        user.Username.Should().Be("eve.jones");
        user.Email.Should().Be("eve.jones@example.com");
    }

    [Fact]
    public async Task ProvisionOrGetUserAsync_FallsBackToUser_WhenNoEmailOrPreferredUsername()
    {
        var user = await _sut.ProvisionOrGetUserAsync("entra", "sub-006", null, null, null, TestContext.Current.CancellationToken);

        user.Username.Should().Be("user");
    }

    [Fact]
    public async Task ProvisionOrGetUserAsync_SanitizesInvalidCharsInPreferredUsername()
    {
        // Spaces, +, ! are stripped; letters, digits, -, _, . are kept
        var user = await _sut.ProvisionOrGetUserAsync("entra", "sub-007", null, null, "frank doe+admin!", TestContext.Current.CancellationToken);

        user.Username.Should().Be("frankdoeadmin");
    }

    [Fact]
    public async Task ProvisionOrGetUserAsync_GeneratesFallbackEmail_WhenNoEmailProvided()
    {
        var user = await _sut.ProvisionOrGetUserAsync("entra", "sub-008", null, null, "grace", TestContext.Current.CancellationToken);

        user.Email.Should().Be("grace@external");
    }

    [Fact]
    public async Task ProvisionOrGetUserAsync_SameProviderKeyDifferentProviders_CreatesSeparateUsers()
    {
        var user1 = await _sut.ProvisionOrGetUserAsync("entra", "sub-shared", null, "henry@entra.com", "henry", TestContext.Current.CancellationToken);
        var user2 = await _sut.ProvisionOrGetUserAsync("okta", "sub-shared", null, "henry@okta.com", "henry", TestContext.Current.CancellationToken);

        user1.Id.Should().NotBe(user2.Id);
        (await _db.Users.CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
        (await _db.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
    }
}
