// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using ShortcutResource = OpenDsc.Resource.Windows.Shortcut.Resource;
using ShortcutSchema = OpenDsc.Resource.Windows.Shortcut.Schema;
using ShortcutWindowStyle = OpenDsc.Resource.Windows.Shortcut.WindowStyle;

namespace OpenDsc.Resource.Windows.Tests.Shortcut;

[Trait("Category", "Integration")]
public sealed class ShortcutTests : WindowsTestBase
{
    private readonly ShortcutResource _resource = new(SourceGenerationContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(ShortcutResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/Shortcut");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentShortcut_ReturnsExistFalse()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.lnk");

        var result = _resource.Get(new ShortcutSchema { Path = shortcutPath });

        result.Exist.Should().BeFalse();
        result.Path.Should().Be(shortcutPath);
    }

    [Fact]
    public void Set_NewShortcut_CreatesLnkFile()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"shortcut_{Guid.NewGuid():N}.lnk");
        var targetPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "notepad.exe");

        try
        {
            _resource.Set(new ShortcutSchema
            {
                Path = shortcutPath,
                TargetPath = targetPath,
                Description = "Test shortcut"
            });

            var actual = _resource.Get(new ShortcutSchema { Path = shortcutPath });

            actual.Exist.Should().NotBe(false);
            actual.Path.Should().Be(shortcutPath);
            actual.TargetPath.Should().NotBeNullOrEmpty();
            actual.TargetPath!
                .Equals(targetPath, StringComparison.OrdinalIgnoreCase)
                .Should().BeTrue();
        }
        finally
        {
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
    }

    [Fact]
    public void Set_ExistingShortcut_UpdatesTarget()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"shortcut_{Guid.NewGuid():N}.lnk");
        var initialTarget = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "notepad.exe");
        var updatedTarget = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "cmd.exe");

        try
        {
            _resource.Set(new ShortcutSchema
            {
                Path = shortcutPath,
                TargetPath = initialTarget
            });

            _resource.Set(new ShortcutSchema
            {
                Path = shortcutPath,
                TargetPath = updatedTarget
            });

            var actual = _resource.Get(new ShortcutSchema { Path = shortcutPath });

            actual.Exist.Should().NotBe(false);
            actual.TargetPath.Should().NotBeNullOrEmpty();
            actual.TargetPath!
                .Equals(updatedTarget, StringComparison.OrdinalIgnoreCase)
                .Should().BeTrue();
        }
        finally
        {
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
    }

    [Fact]
    public void Delete_ExistingShortcut_RemovesFile()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"shortcut_{Guid.NewGuid():N}.lnk");
        var targetPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "notepad.exe");

        _resource.Set(new ShortcutSchema
        {
            Path = shortcutPath,
            TargetPath = targetPath
        });

        _resource.Delete(new ShortcutSchema { Path = shortcutPath });

        File.Exists(shortcutPath).Should().BeFalse();

        var result = _resource.Get(new ShortcutSchema { Path = shortcutPath });
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Delete_NonExistentShortcut_DoesNotThrow()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.lnk");

        var act = () => _resource.Delete(new ShortcutSchema { Path = shortcutPath });

        act.Should().NotThrow();
    }

    [Fact]
    public void Set_ShortcutWithArguments_StoresArguments()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"shortcut_args_{Guid.NewGuid():N}.lnk");
        var targetPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "notepad.exe");

        try
        {
            _resource.Set(new ShortcutSchema
            {
                Path = shortcutPath,
                TargetPath = targetPath,
                Arguments = "/arg1 /arg2"
            });

            var actual = _resource.Get(new ShortcutSchema { Path = shortcutPath });

            actual.Arguments.Should().Be("/arg1 /arg2");
        }
        finally
        {
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
    }

    [Fact]
    public void Set_ShortcutWithWorkingDirectory_StoresWorkingDirectory()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"shortcut_wd_{Guid.NewGuid():N}.lnk");
        var targetPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "notepad.exe");
        var workingDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows);

        try
        {
            _resource.Set(new ShortcutSchema
            {
                Path = shortcutPath,
                TargetPath = targetPath,
                WorkingDirectory = workingDir
            });

            var actual = _resource.Get(new ShortcutSchema { Path = shortcutPath });

            actual.WorkingDirectory.Should().NotBeNullOrEmpty();
            actual.WorkingDirectory!.Equals(workingDir, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
    }

    [Fact]
    public void Set_InvalidDirectory_ThrowsDirectoryNotFoundException()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"nonexistent_dir_{Guid.NewGuid():N}", "shortcut.lnk");
        var targetPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "notepad.exe");

        var act = () => _resource.Set(new ShortcutSchema
        {
            Path = shortcutPath,
            TargetPath = targetPath
        });

        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void Get_ExistingShortcut_ReturnsAllProperties()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"shortcut_allprops_{Guid.NewGuid():N}.lnk");
        var targetPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "notepad.exe");
        var workingDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows);

        try
        {
            _resource.Set(new ShortcutSchema
            {
                Path = shortcutPath,
                TargetPath = targetPath,
                Arguments = "/test",
                WorkingDirectory = workingDir,
                Description = "All properties test"
            });

            var actual = _resource.Get(new ShortcutSchema { Path = shortcutPath });

            actual.Path.Should().Be(shortcutPath);
            actual.TargetPath.Should().NotBeNullOrEmpty();
            actual.Arguments.Should().Be("/test");
            actual.WorkingDirectory.Should().NotBeNullOrEmpty();
            actual.Description.Should().Be("All properties test");
            actual.Exist.Should().BeNull();
        }
        finally
        {
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
    }

    [Fact]
    public void Set_WindowStyleMinimized_StoresWindowStyle()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"shortcut_winstyle_{Guid.NewGuid():N}.lnk");
        var targetPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "notepad.exe");

        try
        {
            _resource.Set(new ShortcutSchema
            {
                Path = shortcutPath,
                TargetPath = targetPath,
                WindowStyle = ShortcutWindowStyle.Minimized
            });

            var actual = _resource.Get(new ShortcutSchema { Path = shortcutPath });

            actual.WindowStyle.Should().Be(ShortcutWindowStyle.Minimized);
        }
        finally
        {
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
    }

    [Fact]
    public void Set_WindowStyleMaximized_StoresWindowStyle()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"shortcut_winstyle_max_{Guid.NewGuid():N}.lnk");
        var targetPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "notepad.exe");

        try
        {
            _resource.Set(new ShortcutSchema
            {
                Path = shortcutPath,
                TargetPath = targetPath,
                WindowStyle = ShortcutWindowStyle.Maximized
            });

            var actual = _resource.Get(new ShortcutSchema { Path = shortcutPath });

            actual.WindowStyle.Should().Be(ShortcutWindowStyle.Maximized);
        }
        finally
        {
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
    }

    [Fact]
    public void Set_ShortcutWithHotkey_StoresHotkey()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"shortcut_hotkey_{Guid.NewGuid():N}.lnk");
        var targetPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "notepad.exe");

        try
        {
            _resource.Set(new ShortcutSchema
            {
                Path = shortcutPath,
                TargetPath = targetPath,
                Hotkey = "Ctrl+Alt+N"
            });

            var actual = _resource.Get(new ShortcutSchema { Path = shortcutPath });

            actual.Hotkey.Should().NotBeNullOrEmpty();
        }
        finally
        {
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
    }

    [Fact]
    public void Set_ShortcutWithIconLocation_StoresIconLocation()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"shortcut_icon_{Guid.NewGuid():N}.lnk");
        var targetPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "notepad.exe");
        var iconLocation = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "shell32.dll,0");

        try
        {
            _resource.Set(new ShortcutSchema
            {
                Path = shortcutPath,
                TargetPath = targetPath,
                IconLocation = iconLocation
            });

            var actual = _resource.Get(new ShortcutSchema { Path = shortcutPath });

            actual.IconLocation.Should().NotBeNullOrEmpty();
        }
        finally
        {
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
    }

    [Fact]
    public void Set_ShortcutWithEmptyTarget_CreatesShortcut()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"shortcut_emptytarget_{Guid.NewGuid():N}.lnk");

        try
        {
            _resource.Set(new ShortcutSchema
            {
                Path = shortcutPath,
                TargetPath = string.Empty
            });

            var result = _resource.Get(new ShortcutSchema { Path = shortcutPath });

            result.Path.Should().Be(shortcutPath);
        }
        finally
        {
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
    }

    [Fact]
    public void Get_NonExistentShortcut_ReturnExistFalse()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"nonexistent_get_{Guid.NewGuid():N}.lnk");

        var result = _resource.Get(new ShortcutSchema { Path = shortcutPath });

        result.Exist.Should().BeFalse();
        result.Path.Should().Be(shortcutPath);
    }
}
