// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.AccessControl;

using AwesomeAssertions;

using Xunit;

using AclAccessRule = OpenDsc.Resource.Windows.FileSystem.Acl.AccessRule;
using AclResource = OpenDsc.Resource.Windows.FileSystem.Acl.Resource;
using DscSchema = OpenDsc.Resource.Windows.FileSystem.Acl.Schema;

namespace OpenDsc.Resource.Windows.Tests.FileSystem;

[Trait("Category", "Integration")]
public sealed class AclTests : WindowsTestBase
{
    private static AclResource CreateResource() => new(OpenDsc.Resource.Windows.SourceGenerationContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var resource = CreateResource();

        var schemaJson = resource.GetSchema();
        var doc = System.Text.Json.JsonDocument.Parse(schemaJson);

        doc.RootElement.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Object);
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(AclResource).GetCustomAttributes(typeof(DscResourceAttribute), false).OfType<DscResourceAttribute>().SingleOrDefault();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows.FileSystem/AccessControlList");
        attr.Version.Should().NotBeNull();
        attr.Version!.ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Get_NonExistentPath_ThrowsFileNotFoundException()
    {
        var resource = CreateResource();
        var path = Path.Combine(Path.GetTempPath(), "DscAclTest_NonExistent_" + Guid.NewGuid() + ".txt");

        Assert.Throws<FileNotFoundException>(() => resource.Get(new DscSchema { Path = path }));
    }

    [Fact]
    public void Get_ExistingFile_ReturnsAcl()
    {
        var path = Path.Combine(Path.GetTempPath(), "DscAclTest_" + Guid.NewGuid() + ".txt");
        File.WriteAllText(path, "test");

        try
        {
            var resource = CreateResource();
            var result = resource.Get(new DscSchema { Path = path });

            result.Path.Should().Be(path);
            result.Owner.Should().NotBeNullOrWhiteSpace();
            result.AccessRules.Should().NotBeNullOrEmpty();
            result.AccessRules!.Any(r => !string.IsNullOrWhiteSpace(r.Identity)).Should().BeTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [RequiresAdminFact]
    public void Set_AddAccessRule_PurgeFalse_AddsRule()
    {
        var path = Path.Combine(Path.GetTempPath(), "DscAclTest_" + Guid.NewGuid() + ".txt");
        File.WriteAllText(path, "test");

        try
        {
            var resource = CreateResource();
            var input = new DscSchema
            {
                Path = path,
                AccessRules = new[]
                {
                    new AclAccessRule
                    {
                        Identity = "BUILTIN\\Users",
                        Rights = new[] { FileSystemRights.Read },
                        AccessControlType = AccessControlType.Allow,
                        InheritanceFlags = new[] { InheritanceFlags.None },
                        PropagationFlags = new[] { PropagationFlags.None }
                    }
                },
                Purge = false
            };

            resource.Set(input);

            var actual = resource.Get(new DscSchema { Path = path });
            actual.AccessRules.Should().NotBeNullOrEmpty();
            actual.AccessRules!.Any(r => r.Identity.EndsWith("\\Users", StringComparison.OrdinalIgnoreCase) && r.AccessControlType == AccessControlType.Allow && r.Rights.Contains(FileSystemRights.Read)).Should().BeTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [RequiresAdminFact]
    public void Set_PurgeTrue_SetsExactRules()
    {
        var path = Path.Combine(Path.GetTempPath(), "DscAclTest_" + Guid.NewGuid() + ".txt");
        File.WriteAllText(path, "test");

        try
        {
            var resource = CreateResource();
            var firstInput = new DscSchema
            {
                Path = path,
                AccessRules = new[]
                {
                    new AclAccessRule
                    {
                        Identity = "BUILTIN\\Users",
                        Rights = new[] { FileSystemRights.Read },
                        AccessControlType = AccessControlType.Allow,
                        InheritanceFlags = new[] { InheritanceFlags.None },
                        PropagationFlags = new[] { PropagationFlags.None }
                    }
                },
                Purge = false
            };
            resource.Set(firstInput);

            var secondInput = new DscSchema
            {
                Path = path,
                AccessRules = new[]
                {
                    new AclAccessRule
                    {
                        Identity = "BUILTIN\\Administrators",
                        Rights = new[] { FileSystemRights.FullControl },
                        AccessControlType = AccessControlType.Allow,
                        InheritanceFlags = new[] { InheritanceFlags.None },
                        PropagationFlags = new[] { PropagationFlags.None }
                    }
                },
                Purge = true
            };
            resource.Set(secondInput);

            var actual = resource.Get(new DscSchema { Path = path });
            actual.AccessRules.Should().NotBeNullOrEmpty();

            var explicitAdminRule = actual.AccessRules!.Any(r => r.Identity.EndsWith("\\Administrators", StringComparison.OrdinalIgnoreCase) && r.AccessControlType == AccessControlType.Allow && r.Rights.Contains(FileSystemRights.FullControl) && r.InheritanceFlags != null && r.InheritanceFlags.Length == 1 && r.InheritanceFlags[0] == InheritanceFlags.None);
            explicitAdminRule.Should().BeTrue();

            var explicitUsersRule = actual.AccessRules.Any(r => r.Identity.EndsWith("\\Users", StringComparison.OrdinalIgnoreCase) && r.InheritanceFlags != null && r.InheritanceFlags.Length == 1 && r.InheritanceFlags[0] == InheritanceFlags.None);
            explicitUsersRule.Should().BeFalse();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Export_NullFilter_ReturnsEmpty()
    {
        var resource = CreateResource();

        var results = resource.Export(null).ToList();

        results.Should().BeEmpty();
    }

    [Fact]
    public void Export_WithPath_ReturnsCurrentAclState()
    {
        var path = Path.Combine(Path.GetTempPath(), "DscAclTest_" + Guid.NewGuid() + ".txt");
        File.WriteAllText(path, "test");

        try
        {
            var resource = CreateResource();
            var results = resource.Export(new DscSchema { Path = path }).ToList();

            results.Should().ContainSingle();
            results[0].Path.Should().Be(path);
            results[0].Owner.Should().NotBeNullOrWhiteSpace();
            results[0].AccessRules.Should().NotBeNullOrEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
