// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Schema;

using Xunit;

using ShortcutResource = OpenDsc.Resource.Windows.Shortcut.Resource;
using ShortcutSchema = OpenDsc.Resource.Windows.Shortcut.Schema;

namespace OpenDsc.Resource.Windows.Tests.Shortcut;

[Trait("Category", "Integration")]
public sealed class ShortcutTests
{
    private readonly ShortcutResource _resource = new(SourceGenerationContext.Default);

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
        var attr = typeof(ShortcutResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/Shortcut");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [WindowsOnlyFact]
    public void Get_NonExistentShortcut_ReturnsExistFalse()
    {
        var shortcutPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.lnk");

        var result = _resource.Get(new ShortcutSchema { Path = shortcutPath });

        result.Exist.Should().BeFalse();
        result.Path.Should().Be(shortcutPath);
    }

    [WindowsOnlyFact]
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

    [WindowsOnlyFact]
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

    [WindowsOnlyFact]
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
}
