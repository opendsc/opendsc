// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Linq;
using System.Reflection;
using System.Security.Principal;

using AwesomeAssertions;

using OpenDsc.Schema;

using Xunit;

using UserRightResource = OpenDsc.Resource.Windows.UserRight.Resource;
using UserRightSchema = OpenDsc.Resource.Windows.UserRight.Schema;

namespace OpenDsc.Resource.Windows.Tests.UserRight;

[Trait("Category", "Integration")]
public sealed class UserRightTests
{
    private readonly UserRightResource _resource = new(SourceGenerationContext.Default);

    private static string CreateUserName() => $"DscTestUser_{Guid.NewGuid():N}"[..16];

    private static void CreateLocalUser(string userName)
    {
        using var context = new System.DirectoryServices.AccountManagement.PrincipalContext(System.DirectoryServices.AccountManagement.ContextType.Machine);
        var user = System.DirectoryServices.AccountManagement.UserPrincipal.FindByIdentity(context, System.DirectoryServices.AccountManagement.IdentityType.SamAccountName, userName);
        if (user != null)
        {
            return;
        }

        using var newUser = new System.DirectoryServices.AccountManagement.UserPrincipal(context)
        {
            SamAccountName = userName,
            DisplayName = userName,
            Description = "Temporary user for DSC UserRight tests",
            Enabled = true
        };

        newUser.SetPassword("P@ssw0rd!123");
        newUser.Save();
    }

    private static void DeleteLocalUser(string userName)
    {
        using var context = new System.DirectoryServices.AccountManagement.PrincipalContext(System.DirectoryServices.AccountManagement.ContextType.Machine);
        using var user = System.DirectoryServices.AccountManagement.UserPrincipal.FindByIdentity(context, System.DirectoryServices.AccountManagement.IdentityType.SamAccountName, userName);
        if (user != null)
        {
            user.Delete();
        }
    }

    private static void RevokeUserRight(string principal, OpenDsc.Resource.Windows.UserRight.UserRight right)
    {
        var helperType = Type.GetType("OpenDsc.Resource.Windows.UserRight.LsaHelper, OpenDsc.Resource.Windows");
        if (helperType == null)
        {
            return;
        }

        var method = helperType.GetMethod("RevokeRight", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        if (method == null)
        {
            return;
        }

        method.Invoke(null, new object[] { principal, right });
    }

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
        var attr = typeof(UserRightResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/UserRight");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [RequiresAdminFact]
    public void Get_SeShutdownPrivilege_ReturnsRight()
    {
        var result = _resource.Get(new UserRightSchema
        {
            Principal = "Administrators",
            Rights = new[] { OpenDsc.Resource.Windows.UserRight.UserRight.SeShutdownPrivilege }
        });

        result.Principal.Should().NotBeNullOrEmpty();
        result.Rights.Should().NotBeNull();
        result.Principal.Should().NotMatch("^S-1-");
    }

    [RequiresAdminFact]
    public void Set_PurgeFalse_AddsIdentity()
    {
        var userName = CreateUserName();
        const OpenDsc.Resource.Windows.UserRight.UserRight right = OpenDsc.Resource.Windows.UserRight.UserRight.SeShutdownPrivilege;

        try
        {
            CreateLocalUser(userName);

            _resource.Set(new UserRightSchema
            {
                Principal = userName,
                Rights = new[] { right },
                Purge = false
            });

            var result = _resource.Get(new UserRightSchema
            {
                Principal = userName,
                Rights = new[] { right }
            });

            result.Rights.Should().Contain(right);
        }
        finally
        {
            RevokeUserRight(userName, right);
            DeleteLocalUser(userName);
        }
    }

    [RequiresAdminFact]
    public void Set_PurgeTrue_SetsExactIdentities()
    {
        var userName = CreateUserName();
        const OpenDsc.Resource.Windows.UserRight.UserRight right = OpenDsc.Resource.Windows.UserRight.UserRight.SeBatchLogonRight;

        IReadOnlyList<string> previousPrincipals = Array.Empty<string>();

        try
        {
            CreateLocalUser(userName);

            previousPrincipals = _resource.Export(null)
                .Where(x => x.Rights.Contains(right))
                .Select(x => x.Principal)
                .ToArray();

            _resource.Set(new UserRightSchema
            {
                Principal = userName,
                Rights = new[] { right },
                Purge = true
            });

            var result = _resource.Get(new UserRightSchema
            {
                Principal = userName,
                Rights = new[] { right }
            });

            result.Rights.Should().Contain(right);
        }
        finally
        {
            foreach (var principal in previousPrincipals ?? Array.Empty<string>())
            {
                if (string.Equals(principal, userName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _resource.Set(new UserRightSchema
                {
                    Principal = principal,
                    Rights = new[] { right },
                    Purge = false
                });
            }

            RevokeUserRight(userName, right);
            DeleteLocalUser(userName);
        }
    }

    [RequiresAdminFact]
    public void Export_NoFilter_ReturnsRights()
    {
        var results = _resource.Export(null).ToList();

        results.Should().NotBeEmpty();
        results.All(r => !string.IsNullOrWhiteSpace(r.Principal)).Should().BeTrue();
    }

    [RequiresAdminFact]
    public void Get_PrincipalWithNoMatchingRights_ReturnsEmptyRights()
    {
        var userName = CreateUserName();
        const OpenDsc.Resource.Windows.UserRight.UserRight right = OpenDsc.Resource.Windows.UserRight.UserRight.SeShutdownPrivilege;

        try
        {
            CreateLocalUser(userName);

            var result = _resource.Get(new UserRightSchema
            {
                Principal = userName,
                Rights = new[] { right }
            });

            result.Rights.Should().NotContain(right);
        }
        finally
        {
            DeleteLocalUser(userName);
        }
    }
}
