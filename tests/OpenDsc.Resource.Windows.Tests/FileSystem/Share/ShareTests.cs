// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using Json.Schema;
using Xunit;

using ShareResource = OpenDsc.Resource.Windows.FileSystem.Share.Resource;
using ShareSchema = OpenDsc.Resource.Windows.FileSystem.Share.Schema;
using ShareHelper = OpenDsc.Resource.Windows.FileSystem.Share.ShareHelper;

namespace OpenDsc.Resource.Windows.Tests.FileSystem.Share;

[Trait("Category", "Integration")]
public sealed class ShareTests
{
    [Fact]
    public void GetSchema_ReturnsValidJsonSchema()
    {
        // Arrange
        var resource = new ShareResource(SourceGenerationContext.Default);

        // Act
        var schema = resource.GetSchema();

        // Assert
        Assert.NotNull(schema);
        Assert.NotEmpty(schema);
        var bundle = JsonSerializer.Deserialize<JsonSchema>(schema);
        Assert.NotNull(bundle);
    }

    [Fact]
    public void Resource_HasCorrectDscAttributes()
    {
        // Arrange
        var resourceType = typeof(ShareResource);

        // Act
        var dscResourceAttr = resourceType.GetCustomAttributes(typeof(DscResourceAttribute), false).FirstOrDefault() as DscResourceAttribute;

        // Assert
        Assert.NotNull(dscResourceAttr);
        Assert.Equal("OpenDsc.Windows.FileSystem/Share", dscResourceAttr.Type);
        Assert.NotNull(dscResourceAttr.Version);
    }

    [RequiresAdminFact]
    public void Get_ExistingShare_ReturnsShare()
    {
        // Arrange
        var share = new ShareSchema { Name = "IPC$", Path = "" };

        // Act
        var result = new ShareResource(SourceGenerationContext.Default).Get(share);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("IPC$", result.Name);
        Assert.NotEqual(false, result.Exist);
    }

    [RequiresAdminFact]
    public void Get_NonExistentShare_ReturnsFalseExist()
    {
        // Arrange
        var share = new ShareSchema { Name = "NonExistentShare12345", Path = @"C:\temp" };

        // Act
        var result = new ShareResource(SourceGenerationContext.Default).Get(share);

        // Assert
        Assert.Equal(false, result.Exist);
    }

    [RequiresAdminFact]
    public void Set_CreateNewShare_SucceedsWhenPathExists()
    {
        // Arrange
        var shareName = "TestShare" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var share = new ShareSchema
        {
            Name = shareName,
            Path = @"C:\Windows\Temp",
            Description = "Test share",
            Exist = true
        };

        try
        {
            // Act
            new ShareResource(SourceGenerationContext.Default).Set(share);

            // Assert - share should be created
            var getResult = new ShareResource(SourceGenerationContext.Default).Get(share);
            Assert.NotEqual(false, getResult.Exist);
        }
        finally
        {
            // Cleanup
            ShareHelper.DeleteShare(shareName);
        }
    }

    [RequiresAdminFact]
    public void Set_DeleteShare_RemovesShare()
    {
        // Arrange - create a share first
        var shareName = "TestDel" + Guid.NewGuid().ToString("N").Substring(0, 8);

        try
        {
            ShareHelper.CreateShare(shareName, @"C:\Windows\Temp", "Temp share");

            var deleteShare = new ShareSchema { Name = shareName, Path = @"C:\Windows\Temp", Exist = false };

            // Act
            new ShareResource(SourceGenerationContext.Default).Set(deleteShare);

            // Assert
            var result = new ShareResource(SourceGenerationContext.Default).Get(deleteShare);
            Assert.False(result.Exist);
        }
        finally
        {
            // Cleanup if needed
            ShareHelper.DeleteShare(shareName);
        }
    }

    [RequiresAdminFact]
    public void Set_UpdateDescription_ChangesShareDescription()
    {
        // Arrange
        var shareName = "TestDesc" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var share = new ShareSchema
        {
            Name = shareName,
            Path = @"C:\Windows\Temp",
            Description = "Original",
            Exist = true
        };

        try
        {
            ShareHelper.CreateShare(shareName, @"C:\Windows\Temp", "Original");

            var updatedShare = new ShareSchema
            {
                Name = shareName,
                Path = @"C:\Windows\Temp",
                Description = "Updated",
                Exist = true
            };

            // Act
            new ShareResource(SourceGenerationContext.Default).Set(updatedShare);

            // Assert
            var result = new ShareResource(SourceGenerationContext.Default).Get(updatedShare);
            Assert.Equal("Updated", result.Description);
        }
        finally
        {
            // Cleanup
            ShareHelper.DeleteShare(shareName);
        }
    }

    [RequiresAdminFact]
    public void Delete_RemovesShare()
    {
        // Arrange
        var shareName = "TestDel2" + Guid.NewGuid().ToString("N").Substring(0, 8);
        ShareHelper.CreateShare(shareName, @"C:\Windows\Temp", "Test");

        var share = new ShareSchema { Name = shareName, Path = @"C:\Windows\Temp" };

        // Act
        new ShareResource(SourceGenerationContext.Default).Delete(share);

        // Assert
        var result = new ShareResource(SourceGenerationContext.Default).Get(share);
        Assert.False(result.Exist);
    }

    [RequiresAdminFact]
    public void Export_ReturnsAllShares()
    {
        // Act
        var result = new ShareResource(SourceGenerationContext.Default).Export(null).ToList();

        // Assert
        Assert.NotEmpty(result);
        // Should include system shares like ADMIN$, C$, IPC$
        Assert.Contains(result, s => s.Name == "IPC$" || s.Name == "ADMIN$");
    }

    // Phase 2: Permission Management Tests

    [RequiresAdminFact]
    public void Get_ShareWithPermissions_ReturnsPermissionsArray()
    {
        // Arrange
        var shareName = "TestPerm" + Guid.NewGuid().ToString("N").Substring(0, 8);
        ShareHelper.CreateShare(shareName, @"C:\Windows\Temp", "Test permissions");

        var share = new ShareSchema { Name = shareName, Path = @"C:\Windows\Temp" };

        try
        {
            // Act
            var result = new ShareResource(SourceGenerationContext.Default).Get(share);

            // Assert - should return permissions array (may be empty or have defaults)
            Assert.NotEqual(false, result.Exist);
            Assert.NotNull(result.Permissions);
        }
        finally
        {
            // Cleanup
            ShareHelper.DeleteShare(shareName);
        }
    }

    [RequiresAdminFact]
    public void Set_WithPermissions_GrantsAccessToAdministrators()
    {
        // Arrange
        var shareName = "TestPermSet" + Guid.NewGuid().ToString("N").Substring(0, 8);
        ShareHelper.CreateShare(shareName, @"C:\Windows\Temp", "Test");

        var permissions = new[]
        {
            new Windows.FileSystem.Share.SharePermission
            {
                Principal = "BUILTIN\\Administrators",
                Access = Windows.FileSystem.Share.AccessLevel.Full
            }
        };

        var share = new ShareSchema
        {
            Name = shareName,
            Path = @"C:\Windows\Temp",
            Permissions = permissions,
            Purge = true,
            Exist = true
        };

        try
        {
            // Act
            new ShareResource(SourceGenerationContext.Default).Set(share);

            // Assert
            var result = new ShareResource(SourceGenerationContext.Default).Get(share);
            Assert.NotNull(result.Permissions);
            Assert.Contains(result.Permissions, p => p.Principal.Contains("Administrators"));
        }
        finally
        {
            // Cleanup
            ShareHelper.DeleteShare(shareName);
        }
    }

    [RequiresAdminFact]
    public void Set_WithMultiplePermissions_AllowsPurge()
    {
        // Arrange
        var shareName = "TestMultiPerm" + Guid.NewGuid().ToString("N").Substring(0, 8);
        ShareHelper.CreateShare(shareName, @"C:\Windows\Temp", "Test");

        var permissions = new[]
        {
            new Windows.FileSystem.Share.SharePermission
            {
                Principal = "BUILTIN\\Administrators",
                Access = Windows.FileSystem.Share.AccessLevel.Full
            },
            new Windows.FileSystem.Share.SharePermission
            {
                Principal = "BUILTIN\\Users",
                Access = Windows.FileSystem.Share.AccessLevel.Read
            }
        };

        var share = new ShareSchema
        {
            Name = shareName,
            Path = @"C:\Windows\Temp",
            Permissions = permissions,
            Purge = true,
            Exist = true
        };

        try
        {
            // Act
            new ShareResource(SourceGenerationContext.Default).Set(share);

            // Assert
            var result = new ShareResource(SourceGenerationContext.Default).Get(share);
            Assert.NotNull(result.Permissions);
            Assert.True(result.Permissions.Length >= 2);
        }
        finally
        {
            // Cleanup
            ShareHelper.DeleteShare(shareName);
        }
    }
}
