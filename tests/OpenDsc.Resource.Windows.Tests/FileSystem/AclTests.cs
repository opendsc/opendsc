// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;

using AwesomeAssertions;
using AclResource = OpenDsc.Resource.Windows.FileSystem.Acl.Resource;
using AclAccessRule = OpenDsc.Resource.Windows.FileSystem.Acl.AccessRule;
using DscSchema = OpenDsc.Resource.Windows.FileSystem.Acl.Schema;
using DscFileSystemRights = OpenDsc.Resource.Windows.FileSystem.Acl.FileSystemRights;
using Xunit;

namespace OpenDsc.Resource.Windows.Tests.FileSystem;

[Trait("Category", "Integration")]
public sealed class AclTests
{
    private static AclResource CreateResource() => new(OpenDsc.Resource.Windows.SourceGenerationContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var resource = CreateResource();

        var schemaJson = resource.GetSchema();
        var doc = System.Text.Json.JsonDocument.Parse(schemaJson);

        doc.RootElement.TryGetProperty("$schema", out var schemaProperty).Should().BeTrue();
        schemaProperty.GetString().Should().Be("https://json-schema.org/draft/2020-12/schema");
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

    [WindowsOnlyFact]
    public void Get_NonExistentPath_ThrowsFileNotFoundException()
    {
        var resource = CreateResource();
        var path = Path.Combine(Path.GetTempPath(), "DscAclTest_NonExistent_" + Guid.NewGuid() + ".txt");

        Assert.Throws<FileNotFoundException>(() => resource.Get(new DscSchema { Path = path }));
    }

    [WindowsOnlyFact]
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
                        Rights = new[] { DscFileSystemRights.Read },
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
            actual.AccessRules!.Any(r => r.Identity.EndsWith("\\Users", StringComparison.OrdinalIgnoreCase) && r.AccessControlType == AccessControlType.Allow && r.Rights.Contains(DscFileSystemRights.Read)).Should().BeTrue();
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
                        Rights = new[] { DscFileSystemRights.Read },
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
                        Rights = new[] { DscFileSystemRights.FullControl },
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

            var explicitAdminRule = actual.AccessRules!.Any(r => r.Identity.EndsWith("\\Administrators", StringComparison.OrdinalIgnoreCase) && r.AccessControlType == AccessControlType.Allow && r.Rights.Contains(DscFileSystemRights.FullControl) && r.InheritanceFlags != null && r.InheritanceFlags.Length == 1 && r.InheritanceFlags[0] == InheritanceFlags.None);
            explicitAdminRule.Should().BeTrue();

            var explicitUsersRule = actual.AccessRules.Any(r => r.Identity.EndsWith("\\Users", StringComparison.OrdinalIgnoreCase) && r.InheritanceFlags != null && r.InheritanceFlags.Length == 1 && r.InheritanceFlags[0] == InheritanceFlags.None);
            explicitUsersRule.Should().BeFalse();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
