// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;
using Xunit.Sdk;

using LinkResource = OpenDsc.Resource.FileSystem.SymbolicLink.Resource;
using LinkSchema = OpenDsc.Resource.FileSystem.SymbolicLink.Schema;
using LinkType = OpenDsc.Resource.FileSystem.SymbolicLink.LinkType;

namespace OpenDsc.Resource.FileSystem.Tests.SymbolicLink;

[Trait("Category", "Integration")]
public sealed class SymbolicLinkTests
{
    private readonly LinkResource _resource = new(SourceGenerationContext.Default);

    private static bool IsAdministrator()
    {
        if (!OperatingSystem.IsWindows())
        {
            return true;
        }

        var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
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
        var attr = typeof(LinkResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.FileSystem/SymbolicLink");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentSymlink_ReturnsExistFalse()
    {
        var tempLink = Path.Combine(Path.GetTempPath(), $"nonexistent_symlink_{Guid.NewGuid():N}");

        var result = _resource.Get(new LinkSchema { Path = tempLink, Target = Path.Combine(Path.GetTempPath(), "doesnotmatter.tmp") });

        result.Exist.Should().BeFalse();
        result.Path.Should().Be(tempLink);
    }

    [Fact]
    public void Get_ExistingFileSymlink_ReturnsProperties()
    {
        if (!IsAdministrator())
        {
            return;
        }

        var target = Path.Combine(Path.GetTempPath(), $"symlink_target_file_{Guid.NewGuid():N}.txt");
        var link = Path.Combine(Path.GetTempPath(), $"symlink_file_{Guid.NewGuid():N}.lnk");

        System.IO.File.WriteAllText(target, "content");

        try
        {
            System.IO.File.CreateSymbolicLink(link, target);

            var result = _resource.Get(new LinkSchema { Path = link, Target = target });

            result.Path.Should().Be(link);
            result.Target.Should().NotBeNull();
            Path.GetFullPath(result.Target!).Should().Be(Path.GetFullPath(target));
            result.Type.Should().Be(LinkType.File);
            result.Exist.Should().BeNull();
        }
        finally
        {
            if (System.IO.File.Exists(link))
            {
                System.IO.File.Delete(link);
            }

            if (System.IO.File.Exists(target))
            {
                System.IO.File.Delete(target);
            }
        }
    }

    [Fact]
    public void Get_ExistingDirectorySymlink_ReturnsProperties()
    {
        if (!IsAdministrator())
        {
            return;
        }

        var target = Path.Combine(Path.GetTempPath(), $"symlink_target_dir_{Guid.NewGuid():N}");
        var link = Path.Combine(Path.GetTempPath(), $"symlink_dir_{Guid.NewGuid():N}");

        System.IO.Directory.CreateDirectory(target);

        try
        {
            System.IO.Directory.CreateSymbolicLink(link, target);

            var result = _resource.Get(new LinkSchema { Path = link, Target = target });

            result.Path.Should().Be(link);
            result.Target.Should().NotBeNull();
            Path.GetFullPath(result.Target!).Should().Be(Path.GetFullPath(target));
            result.Type.Should().Be(LinkType.Directory);
            result.Exist.Should().BeNull();
        }
        finally
        {
            if (System.IO.Directory.Exists(link))
            {
                System.IO.Directory.Delete(link);
            }

            if (System.IO.Directory.Exists(target))
            {
                System.IO.Directory.Delete(target, recursive: true);
            }
        }
    }

    [Fact]
    public void Set_NewFileSymlink_CreatesLink()
    {
        if (!IsAdministrator())
        {
            return;
        }

        var target = Path.Combine(Path.GetTempPath(), $"symlink_set_target_file_{Guid.NewGuid():N}.txt");
        var link = Path.Combine(Path.GetTempPath(), $"symlink_set_file_{Guid.NewGuid():N}.lnk");

        System.IO.File.WriteAllText(target, "content");

        try
        {
            _resource.Set(new LinkSchema { Path = link, Target = target });

            var result = _resource.Get(new LinkSchema { Path = link, Target = target });
            result.Exist.Should().BeNull();
            result.Target.Should().NotBeNull();
            Path.GetFullPath(result.Target!).Should().Be(Path.GetFullPath(target));
            result.Type.Should().Be(LinkType.File);
        }
        finally
        {
            if (System.IO.File.Exists(link))
            {
                System.IO.File.Delete(link);
            }

            if (System.IO.File.Exists(target))
            {
                System.IO.File.Delete(target);
            }
        }
    }

    [Fact]
    public void Set_NewDirectorySymlink_CreatesLink()
    {
        if (!IsAdministrator())
        {
            return;
        }

        var target = Path.Combine(Path.GetTempPath(), $"symlink_set_target_dir_{Guid.NewGuid():N}");
        var link = Path.Combine(Path.GetTempPath(), $"symlink_set_dir_{Guid.NewGuid():N}");

        System.IO.Directory.CreateDirectory(target);

        try
        {
            _resource.Set(new LinkSchema { Path = link, Target = target });

            var result = _resource.Get(new LinkSchema { Path = link, Target = target });
            result.Exist.Should().BeNull();
            result.Target.Should().NotBeNull();
            Path.GetFullPath(result.Target!).Should().Be(Path.GetFullPath(target));
            result.Type.Should().Be(LinkType.Directory);
        }
        finally
        {
            if (System.IO.Directory.Exists(link))
            {
                System.IO.Directory.Delete(link);
            }

            if (System.IO.Directory.Exists(target))
            {
                System.IO.Directory.Delete(target, recursive: true);
            }
        }
    }

    [Fact]
    public void Set_TargetDoesNotExist_ThrowsArgumentException()
    {
        if (!IsAdministrator())
        {
            return;
        }

        var target = Path.Combine(Path.GetTempPath(), $"symlink_missing_target_{Guid.NewGuid():N}");
        var link = Path.Combine(Path.GetTempPath(), $"symlink_missing_{Guid.NewGuid():N}");

        var act = () => _resource.Set(new LinkSchema { Path = link, Target = target });

        act.Should().Throw<ArgumentException>();

        System.IO.File.Exists(link).Should().BeFalse();
        System.IO.Directory.Exists(link).Should().BeFalse();
    }

    [Fact]
    public void Set_UpdateExistingSymlink_ChangesTarget()
    {
        if (!IsAdministrator())
        {
            return;
        }

        var target1 = Path.Combine(Path.GetTempPath(), $"symlink_update_target1_{Guid.NewGuid():N}.txt");
        var target2 = Path.Combine(Path.GetTempPath(), $"symlink_update_target2_{Guid.NewGuid():N}.txt");
        var link = Path.Combine(Path.GetTempPath(), $"symlink_update_{Guid.NewGuid():N}.lnk");

        System.IO.File.WriteAllText(target1, "first");
        System.IO.File.WriteAllText(target2, "second");

        try
        {
            _resource.Set(new LinkSchema { Path = link, Target = target1 });
            _resource.Set(new LinkSchema { Path = link, Target = target2 });

            var result = _resource.Get(new LinkSchema { Path = link, Target = target2 });
            Path.GetFullPath(result.Target!).Should().Be(Path.GetFullPath(target2));
            result.Type.Should().Be(LinkType.File);
        }
        finally
        {
            if (System.IO.File.Exists(link))
            {
                System.IO.File.Delete(link);
            }

            if (System.IO.File.Exists(target1))
            {
                System.IO.File.Delete(target1);
            }

            if (System.IO.File.Exists(target2))
            {
                System.IO.File.Delete(target2);
            }
        }
    }

    [Fact]
    public void Delete_ExistingSymlink_RemovesLink()
    {
        if (!IsAdministrator())
        {
            return;
        }

        var target = Path.Combine(Path.GetTempPath(), $"symlink_delete_target_{Guid.NewGuid():N}.txt");
        var link = Path.Combine(Path.GetTempPath(), $"symlink_delete_{Guid.NewGuid():N}.lnk");

        System.IO.File.WriteAllText(target, "content");
        _resource.Set(new LinkSchema { Path = link, Target = target });

        _resource.Delete(new LinkSchema { Path = link, Target = target, Exist = false });

        System.IO.File.Exists(link).Should().BeFalse();

        var result = _resource.Get(new LinkSchema { Path = link, Target = target });
        result.Exist.Should().BeFalse();

        if (System.IO.File.Exists(target))
        {
            System.IO.File.Delete(target);
        }
    }

    [Fact]
    public void Delete_NonExistentSymlink_DoesNotThrow()
    {
        if (!IsAdministrator())
        {
            return;
        }

        var link = Path.Combine(Path.GetTempPath(), $"symlink_delete_nonexistent_{Guid.NewGuid():N}");

        var act = () => _resource.Delete(new LinkSchema { Path = link, Target = Path.Combine(Path.GetTempPath(), "x"), Exist = false });

        act.Should().NotThrow();
    }
}
