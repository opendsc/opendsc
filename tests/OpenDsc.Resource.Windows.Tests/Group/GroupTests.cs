// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Schema;

using Xunit;

using GroupResource = OpenDsc.Resource.Windows.Group.Resource;
using GroupSchema = OpenDsc.Resource.Windows.Group.Schema;

namespace OpenDsc.Resource.Windows.Tests.Group;

[Trait("Category", "Integration")]
public sealed class GroupTests
{
    private readonly GroupResource _resource = new(SourceGenerationContext.Default);

    private static string CreateGroupName() => $"DscTestGroup_{Guid.NewGuid():N}"[..16];
    private static string CreateUserName() => $"DscTestUser_{Guid.NewGuid():N}"[..16];

    private static string CurrentUserName
    {
        get
        {
            var name = WindowsIdentity.GetCurrent().Name;
            var parts = name.Split('\\');
            return parts.Length > 1 ? parts[^1] : name;
        }
    }

    private static void CreateLocalUser(string userName, string password)
    {
        using var context = new PrincipalContext(ContextType.Machine);
        var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, userName);
        if (user != null)
        {
            return;
        }

        using var newUser = new UserPrincipal(context)
        {
            SamAccountName = userName,
            DisplayName = userName,
            Description = "Temporary group membership test user"
        };

        newUser.SetPassword(password);
        newUser.Enabled = true;
        newUser.Save();
    }

    private static void DeleteLocalUser(string userName)
    {
        using var context = new PrincipalContext(ContextType.Machine);
        using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, userName);
        if (user != null)
        {
            user.Delete();
        }
    }

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.GetProperty("$schema").GetString()
            .Should().Be("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(GroupResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/Group");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentGroup_ReturnsExistFalse()
    {
        var groupName = "NonExistGroup_12345_UnitTest";

        var result = _resource.Get(new GroupSchema { GroupName = groupName });

        result.Exist.Should().BeFalse();
        result.GroupName.Should().Be(groupName);
    }

    [Fact]
    public void Get_BuiltInAdministratorsGroup_ReturnsGroup()
    {
        var result = _resource.Get(new GroupSchema { GroupName = "Administrators" });

        result.GroupName.Should().Be("Administrators");
        result.Exist.Should().NotBe(false);
        result.Members.Should().NotBeNull();
    }

    [RequiresAdminFact]
    public void Set_NewGroup_CreatesGroup()
    {
        var groupName = CreateGroupName();

        try
        {
            _resource.Set(new GroupSchema { GroupName = groupName, Description = "Test Group Description" });

            var result = _resource.Get(new GroupSchema { GroupName = groupName });

            result.GroupName.Should().Be(groupName);
            result.Description.Should().Be("Test Group Description");
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            _resource.Delete(new GroupSchema { GroupName = groupName });
        }
    }

    [RequiresAdminFact]
    public void Set_ExistingGroup_UpdatesDescription()
    {
        var groupName = CreateGroupName();

        try
        {
            _resource.Set(new GroupSchema { GroupName = groupName, Description = "Initial Desc" });
            _resource.Set(new GroupSchema { GroupName = groupName, Description = "Updated Desc" });

            var result = _resource.Get(new GroupSchema { GroupName = groupName });

            result.Description.Should().Be("Updated Desc");
        }
        finally
        {
            _resource.Delete(new GroupSchema { GroupName = groupName });
        }
    }

    [RequiresAdminFact]
    public void Set_WithMembers_PurgeFalse_AddsMembers()
    {
        var groupName = CreateGroupName();
        var extraUser = CreateUserName();

        try
        {
            CreateLocalUser(extraUser, "P@ssw0rd!123");

            _resource.Set(new GroupSchema
            {
                GroupName = groupName,
                Members = new[] { CurrentUserName }
            });

            _resource.Set(new GroupSchema
            {
                GroupName = groupName,
                Members = new[] { CurrentUserName, extraUser },
                Purge = false
            });

            var result = _resource.Get(new GroupSchema { GroupName = groupName });

            result.Members.Should().Contain(CurrentUserName);
            result.Members.Should().Contain(extraUser);
        }
        finally
        {
            _resource.Delete(new GroupSchema { GroupName = groupName });
            DeleteLocalUser(extraUser);
        }
    }

    [RequiresAdminFact]
    public void Set_WithMembers_PurgeTrue_SetsExactMembers()
    {
        var groupName = CreateGroupName();
        var extraUser = CreateUserName();

        try
        {
            CreateLocalUser(extraUser, "P@ssw0rd!123");

            _resource.Set(new GroupSchema
            {
                GroupName = groupName,
                Members = new[] { CurrentUserName, extraUser }
            });

            _resource.Set(new GroupSchema
            {
                GroupName = groupName,
                Members = new[] { CurrentUserName },
                Purge = true
            });

            var result = _resource.Get(new GroupSchema { GroupName = groupName });

            result.Members.Should().Contain(CurrentUserName);
            result.Members.Should().NotContain(extraUser);
        }
        finally
        {
            _resource.Delete(new GroupSchema { GroupName = groupName });
            DeleteLocalUser(extraUser);
        }
    }

    [RequiresAdminFact]
    public void Delete_ExistingGroup_RemovesGroup()
    {
        var groupName = CreateGroupName();

        _resource.Set(new GroupSchema { GroupName = groupName });

        _resource.Delete(new GroupSchema { GroupName = groupName });

        var result = _resource.Get(new GroupSchema { GroupName = groupName });

        result.Exist.Should().BeFalse();
    }

    [RequiresAdminFact]
    public void Export_NoFilter_ReturnsGroups()
    {
        var groupName = CreateGroupName();

        try
        {
            _resource.Set(new GroupSchema { GroupName = groupName });

            var results = _resource.Export(null).ToList();

            results.Should().NotBeEmpty();
            results.Select(r => r.GroupName).Should().Contain(groupName);
        }
        finally
        {
            _resource.Delete(new GroupSchema { GroupName = groupName });
        }
    }

    [Fact]
    public void Delete_NonExistentGroup_DoesNotThrow()
    {
        var groupName = "NonExistGroup_DSC_99";

        var act = () => _resource.Delete(new GroupSchema { GroupName = groupName });

        act.Should().NotThrow();
    }
}
