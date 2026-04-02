// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Resource;

using Xunit;

using MultiContext = TestResource.Multi.SourceGenerationContext;

namespace TestResource.Multi.Tests;

[Trait("Category", "Integration")]
public sealed class UserResourceTests
{
    private readonly UserResource _resource = new(MultiContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void GetSchema_ContainsNameAndFullNameProperties()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        var props = doc.RootElement.GetProperty("properties");
        props.TryGetProperty("name", out _).Should().BeTrue();
        props.TryGetProperty("fullName", out _).Should().BeTrue();
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectType()
    {
        var attr = typeof(UserResource).GetCustomAttribute<DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("TestResource.Multi/User");
    }

    [Fact]
    public void DscResourceAttribute_HasDescription()
    {
        var attr = typeof(UserResource).GetCustomAttribute<DscResourceAttribute>();

        attr!.Description.Should().Be("Manages user accounts");
    }

    [Fact]
    public void Get_ExistingUser_ReturnsUserWithFullName()
    {
        var result = _resource.Get(new UserSchema { Name = "TestUser" });

        result.Exist.Should().BeNull();
        result.Name.Should().Be("TestUser");
        result.FullName.Should().Be("Test User");
    }

    [Fact]
    public void Get_NonExistentUser_ReturnsExistFalse()
    {
        var result = _resource.Get(new UserSchema { Name = $"NonExistentUser_{Guid.NewGuid():N}" });

        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Test_ExistingUserWithMatchingFullName_ReturnsInDesiredState()
    {
        var result = _resource.Test(new UserSchema { Name = "TestUser", FullName = "Test User" });

        result.DifferingProperties.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Test_ExistingUserWithDifferentFullName_ReturnsDifferingProperty()
    {
        var result = _resource.Test(new UserSchema { Name = "TestUser", FullName = "Different Name" });

        result.DifferingProperties.Should().Contain(nameof(UserSchema.FullName));
    }

    [Fact]
    public void Test_NonExistentUser_ReturnsDifferingProperty()
    {
        var result = _resource.Test(new UserSchema { Name = $"NonExistentUser_{Guid.NewGuid():N}" });

        result.DifferingProperties.Should().Contain(nameof(UserSchema.Exist));
    }

    [Fact]
    public void Set_NewUser_CreatesUser()
    {
        var uniqueName = $"NewUser_{Guid.NewGuid():N}";
        try
        {
            var result = _resource.Set(new UserSchema { Name = uniqueName, FullName = "New User" });

            result!.ActualState.Exist.Should().BeNull();
            result.ActualState.FullName.Should().Be("New User");
        }
        finally
        {
            _resource.Set(new UserSchema { Name = uniqueName, Exist = false });
        }
    }

    [Fact]
    public void Set_ExistingUser_UpdatesFullName()
    {
        var uniqueName = $"UpdateUser_{Guid.NewGuid():N}";
        _resource.Set(new UserSchema { Name = uniqueName, FullName = "Original Name" });
        try
        {
            var result = _resource.Set(new UserSchema { Name = uniqueName, FullName = "Updated Name" });

            result!.ActualState.FullName.Should().Be("Updated Name");
        }
        finally
        {
            _resource.Set(new UserSchema { Name = uniqueName, Exist = false });
        }
    }

    [Fact]
    public void Set_WithExistFalse_RemovesUser()
    {
        var uniqueName = $"RemoveUser_{Guid.NewGuid():N}";
        _resource.Set(new UserSchema { Name = uniqueName, FullName = "To Remove" });

        _resource.Set(new UserSchema { Name = uniqueName, Exist = false });

        var result = _resource.Get(new UserSchema { Name = uniqueName });
        result.Exist.Should().BeFalse();
    }
}
