// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Specialized;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class NewDscPropertyOverrideCommandTests
{
    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Commands.Add(new SessionStateCmdletEntry(
            "New-DscPropertyOverride",
            typeof(NewDscPropertyOverrideCommand), null));
        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();
        return runspace;
    }

    [Fact]
    public void Invoke_WithName_ReturnsOverrideWithName()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscPropertyOverride").AddParameter("Name", "TestProp");

        var result = ps.Invoke<DscPropertyOverride>().Single();

        result.Name.Should().Be("TestProp");
    }

    [Fact]
    public void Invoke_WithDescription_SetsDescription()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscPropertyOverride")
          .AddParameter("Name", "TestProp")
          .AddParameter("Description", "Test description.");

        var result = ps.Invoke<DscPropertyOverride>().Single();

        result.Description.Should().Be("Test description.");
    }

    [Fact]
    public void Invoke_WithTitle_SetsTitle()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscPropertyOverride")
          .AddParameter("Name", "TestProp")
          .AddParameter("Title", "Custom Title");

        var result = ps.Invoke<DscPropertyOverride>().Single();

        result.Title.Should().Be("Custom Title");
    }

    [Fact]
    public void Invoke_WithRemoveKeys_SetsRemoveKeys()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscPropertyOverride")
          .AddParameter("Name", "TestProp")
          .AddParameter("RemoveKeys", new[] { "type", "enum" });

        var result = ps.Invoke<DscPropertyOverride>().Single();

        result.RemoveKeys.Should().BeEquivalentTo(["type", "enum"]);
    }

    [Fact]
    public void Invoke_WithRequiredTrue_SetsRequired()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscPropertyOverride")
          .AddParameter("Name", "TestProp")
          .AddParameter("Required", true);

        var result = ps.Invoke<DscPropertyOverride>().Single();

        result.Required.Should().BeTrue();
    }

    [Fact]
    public void Invoke_WithJsonSchema_SetsJsonSchema()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;

        var jsonSchema = new OrderedDictionary
        {
            ["minimum"] = 0,
            ["maximum"] = 100,
        };

        ps.AddCommand("New-DscPropertyOverride")
          .AddParameter("Name", "TestProp")
          .AddParameter("JsonSchema", jsonSchema);

        var result = ps.Invoke<DscPropertyOverride>().Single();

        result.JsonSchema!["minimum"].Should().Be(0);
        result.JsonSchema["maximum"].Should().Be(100);
    }

    [Fact]
    public void Invoke_NameOnly_OtherFieldsAreNull()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscPropertyOverride").AddParameter("Name", "TestProp");

        var result = ps.Invoke<DscPropertyOverride>().Single();

        result.Description.Should().BeNull();
        result.Title.Should().BeNull();
        result.JsonSchema.Should().BeNull();
        result.RemoveKeys.Should().BeNull();
        result.Required.Should().BeNull();
    }
}
