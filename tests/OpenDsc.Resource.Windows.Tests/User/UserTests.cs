// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;

using AwesomeAssertions;

using Xunit;

using UserResource = OpenDsc.Resource.Windows.User.Resource;
using UserSchema = OpenDsc.Resource.Windows.User.Schema;

namespace OpenDsc.Resource.Windows.Tests.User;

[Trait("Category", "Integration")]
public sealed class UserTests
{
    private readonly UserResource _resource = new(SourceGenerationContext.Default);

    private static string CreateUserName() => $"DscTestUser_{Guid.NewGuid():N}"[..16];

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = System.Text.Json.JsonDocument.Parse(schemaJson);

        doc.RootElement.GetProperty("$schema").GetString()
            .Should().Be("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(UserResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/User");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentUser_ReturnsExistFalse()
    {
        var userName = "NonExistUser_12345_UnitTest";

        var result = _resource.Get(new UserSchema { UserName = userName });

        result.Exist.Should().BeFalse();
        result.UserName.Should().Be(userName);
    }

    [RequiresAdminFact]
    public void Set_NewUser_CreatesUser()
    {
        var userName = CreateUserName();

        try
        {
            _resource.Set(new UserSchema
            {
                UserName = userName,
                Password = "P@ssw0rd!123",
                FullName = "Test User Full Name",
                Description = "Created by DSC test"
            });

            var result = _resource.Get(new UserSchema { UserName = userName });

            result.UserName.Should().Be(userName);
            result.FullName.Should().Be("Test User Full Name");
            result.Description.Should().Be("Created by DSC test");
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            _resource.Delete(new UserSchema { UserName = userName });
        }
    }

    [RequiresAdminFact]
    public void Set_ExistingUser_UpdatesDescription()
    {
        var userName = CreateUserName();

        try
        {
            _resource.Set(new UserSchema
            {
                UserName = userName,
                Password = "P@ssw0rd!123",
                FullName = "Initial Name",
                Description = "Initial Description"
            });

            _resource.Set(new UserSchema
            {
                UserName = userName,
                Description = "Updated Description"
            });

            var result = _resource.Get(new UserSchema { UserName = userName });

            result.Description.Should().Be("Updated Description");
        }
        finally
        {
            _resource.Delete(new UserSchema { UserName = userName });
        }
    }

    [RequiresAdminFact]
    public void Set_UserDisabled_DisablesAccount()
    {
        var userName = CreateUserName();

        try
        {
            _resource.Set(new UserSchema
            {
                UserName = userName,
                Password = "P@ssw0rd!123",
                FullName = "Temp User",
                Description = "Temporary disabled test"
            });

            _resource.Set(new UserSchema
            {
                UserName = userName,
                Disabled = true
            });

            var result = _resource.Get(new UserSchema { UserName = userName });

            result.Disabled.Should().BeTrue();
        }
        finally
        {
            _resource.Delete(new UserSchema { UserName = userName });
        }
    }

    [RequiresAdminFact]
    public void Delete_ExistingUser_RemovesUser()
    {
        var userName = CreateUserName();

        _resource.Set(new UserSchema
        {
            UserName = userName,
            Password = "P@ssw0rd!123"
        });

        _resource.Delete(new UserSchema { UserName = userName });

        var result = _resource.Get(new UserSchema { UserName = userName });

        result.Exist.Should().BeFalse();
    }

    [RequiresAdminFact]
    public void Export_NoFilter_ReturnsUsers()
    {
        var userName = CreateUserName();

        try
        {
            _resource.Set(new UserSchema
            {
                UserName = userName,
                Password = "P@ssw0rd!123"
            });

            var users = _resource.Export(null).ToList();

            users.Should().NotBeEmpty();
            users.Select(u => u.UserName).Should().Contain(userName);
        }
        finally
        {
            _resource.Delete(new UserSchema { UserName = userName });
        }
    }

    [Fact]
    public void Set_NewUser_WithoutPassword_ThrowsArgumentException()
    {
        var userName = CreateUserName();

        var act = () => _resource.Set(new UserSchema { UserName = userName });

        act.Should().Throw<ArgumentException>().WithMessage("*Password*");
    }

    [Fact]
    public void Delete_NonExistentUser_DoesNotThrow()
    {
        var act = () => _resource.Delete(new UserSchema { UserName = "NonExistUser_DSC_DoesNotExist" });

        act.Should().NotThrow();
    }

    [RequiresAdminFact]
    public void Get_ExistingUser_ReturnsAllProperties()
    {
        var userName = CreateUserName();

        try
        {
            _resource.Set(new UserSchema
            {
                UserName = userName,
                Password = "P@ssw0rd!123",
                FullName = "Full Name Test",
                Description = "Description Test",
                Disabled = false,
                PasswordNeverExpires = true,
                UserMayNotChangePassword = false
            });

            var result = _resource.Get(new UserSchema { UserName = userName });

            result.UserName.Should().Be(userName);
            result.FullName.Should().Be("Full Name Test");
            result.Description.Should().Be("Description Test");
            result.Disabled.Should().BeFalse();
            result.PasswordNeverExpires.Should().BeTrue();
            result.UserMayNotChangePassword.Should().BeFalse();
        }
        finally
        {
            _resource.Delete(new UserSchema { UserName = userName });
        }
    }

    [RequiresAdminFact]
    public void Set_ExistingUser_UpdatesFullName()
    {
        var userName = CreateUserName();

        try
        {
            _resource.Set(new UserSchema
            {
                UserName = userName,
                Password = "P@ssw0rd!123",
                FullName = "Original Name"
            });

            _resource.Set(new UserSchema
            {
                UserName = userName,
                FullName = "Updated Name"
            });

            var result = _resource.Get(new UserSchema { UserName = userName });

            result.FullName.Should().Be("Updated Name");
        }
        finally
        {
            _resource.Delete(new UserSchema { UserName = userName });
        }
    }

    [RequiresAdminFact]
    public void Set_ExistingUser_UpdatesPassword()
    {
        var userName = CreateUserName();

        try
        {
            _resource.Set(new UserSchema
            {
                UserName = userName,
                Password = "P@ssw0rd!123",
                FullName = "Password Update Test"
            });

            var act = () => _resource.Set(new UserSchema
            {
                UserName = userName,
                Password = "NewP@ssw0rd!456"
            });

            act.Should().NotThrow();
        }
        finally
        {
            _resource.Delete(new UserSchema { UserName = userName });
        }
    }

    [RequiresAdminFact]
    public void Set_ExistingUser_SetPasswordNeverExpires()
    {
        var userName = CreateUserName();

        try
        {
            _resource.Set(new UserSchema
            {
                UserName = userName,
                Password = "P@ssw0rd!123"
            });

            _resource.Set(new UserSchema
            {
                UserName = userName,
                PasswordNeverExpires = true
            });

            var result = _resource.Get(new UserSchema { UserName = userName });

            result.PasswordNeverExpires.Should().BeTrue();
        }
        finally
        {
            _resource.Delete(new UserSchema { UserName = userName });
        }
    }

    [RequiresAdminFact]
    public void Set_ExistingUser_SetUserMayNotChangePassword()
    {
        var userName = CreateUserName();

        try
        {
            _resource.Set(new UserSchema
            {
                UserName = userName,
                Password = "P@ssw0rd!123"
            });

            _resource.Set(new UserSchema
            {
                UserName = userName,
                UserMayNotChangePassword = true
            });

            var result = _resource.Get(new UserSchema { UserName = userName });

            result.UserMayNotChangePassword.Should().BeTrue();
        }
        finally
        {
            _resource.Delete(new UserSchema { UserName = userName });
        }
    }

    [RequiresAdminFact]
    public void Set_ExistingUser_ReenableAccount()
    {
        var userName = CreateUserName();

        try
        {
            _resource.Set(new UserSchema
            {
                UserName = userName,
                Password = "P@ssw0rd!123",
                Disabled = true
            });

            _resource.Set(new UserSchema
            {
                UserName = userName,
                Disabled = false
            });

            var result = _resource.Get(new UserSchema { UserName = userName });

            result.Disabled.Should().BeFalse();
        }
        finally
        {
            _resource.Delete(new UserSchema { UserName = userName });
        }
    }
}
