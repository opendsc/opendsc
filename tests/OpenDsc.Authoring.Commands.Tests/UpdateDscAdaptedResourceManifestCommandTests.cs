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
public class UpdateDscAdaptedResourceManifestCommandTests
{
    private static readonly string s_fixturesPath =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Commands.Add(new SessionStateCmdletEntry(
            "New-DscAdaptedResourceManifest",
            typeof(NewDscAdaptedResourceManifestCommand), null));
        iss.Commands.Add(new SessionStateCmdletEntry(
            "Update-DscAdaptedResourceManifest",
            typeof(UpdateDscAdaptedResourceManifestCommand), null));
        iss.Commands.Add(new SessionStateCmdletEntry(
            "New-DscPropertyOverride",
            typeof(NewDscPropertyOverrideCommand), null));
        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();
        return runspace;
    }

    private DscAdaptedResourceManifest CreateSimpleManifest(Runspace runspace)
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);
        return ps.Invoke<DscAdaptedResourceManifest>().Single();
    }

    [Fact]
    public void Invoke_DescriptionOverride_UpdatesManifestDescription()
    {
        using var runspace = CreateRunspace();
        var manifest = CreateSimpleManifest(runspace);

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Update-DscAdaptedResourceManifest")
          .AddParameter("InputObject", manifest)
          .AddParameter("Description", "Custom description");

        var result = ps.Invoke<DscAdaptedResourceManifest>().Single();

        result.Description.Should().Be("Custom description");
    }

    [Fact]
    public void Invoke_PropertyDescriptionOverride_UpdatesPropertyDescription()
    {
        using var runspace = CreateRunspace();
        var manifest = CreateSimpleManifest(runspace);

        var overrides = new[]
        {
            new DscPropertyOverride { Name = "Name", Description = "Custom name description." },
        };

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Update-DscAdaptedResourceManifest")
          .AddParameter("InputObject", manifest)
          .AddParameter("PropertyOverride", overrides);

        var result = ps.Invoke<DscAdaptedResourceManifest>().Single();
        var properties = result.ManifestSchema.Embedded["properties"] as OrderedDictionary;
        var nameProp = properties!["Name"] as OrderedDictionary;

        nameProp!["description"].Should().Be("Custom name description.");
    }

    [Fact]
    public void Invoke_PropertyTitleOverride_UpdatesPropertyTitle()
    {
        using var runspace = CreateRunspace();
        var manifest = CreateSimpleManifest(runspace);

        var overrides = new[]
        {
            new DscPropertyOverride { Name = "Name", Title = "Resource Name" },
        };

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Update-DscAdaptedResourceManifest")
          .AddParameter("InputObject", manifest)
          .AddParameter("PropertyOverride", overrides);

        var result = ps.Invoke<DscAdaptedResourceManifest>().Single();
        var properties = result.ManifestSchema.Embedded["properties"] as OrderedDictionary;
        var nameProp = properties!["Name"] as OrderedDictionary;

        nameProp!["title"].Should().Be("Resource Name");
    }

    [Fact]
    public void Invoke_RemoveKeys_RemovesSpecifiedKeys()
    {
        using var runspace = CreateRunspace();
        var manifest = CreateSimpleManifest(runspace);

        var overrides = new[]
        {
            new DscPropertyOverride
            {
                Name = "Name",
                RemoveKeys = ["type"],
                JsonSchema = new OrderedDictionary { ["anyOf"] = new object[] { "string", "integer" } },
            },
        };

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Update-DscAdaptedResourceManifest")
          .AddParameter("InputObject", manifest)
          .AddParameter("PropertyOverride", overrides);

        var result = ps.Invoke<DscAdaptedResourceManifest>().Single();
        var properties = result.ManifestSchema.Embedded["properties"] as OrderedDictionary;
        var nameProp = properties!["Name"] as OrderedDictionary;

        nameProp!.Contains("type").Should().BeFalse();
        nameProp.Contains("anyOf").Should().BeTrue();
    }

    [Fact]
    public void Invoke_RequiredTrue_AddsPropertyToRequired()
    {
        using var runspace = CreateRunspace();
        var manifest = CreateSimpleManifest(runspace);

        var overrides = new[]
        {
            new DscPropertyOverride { Name = "Value", Required = true },
        };

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Update-DscAdaptedResourceManifest")
          .AddParameter("InputObject", manifest)
          .AddParameter("PropertyOverride", overrides);

        var result = ps.Invoke<DscAdaptedResourceManifest>().Single();
        var required = result.ManifestSchema.Embedded["required"] as string[];

        required.Should().Contain("Value");
    }

    [Fact]
    public void Invoke_RequiredFalse_RemovesPropertyFromRequired()
    {
        using var runspace = CreateRunspace();
        var manifest = CreateSimpleManifest(runspace);

        var overrides = new[]
        {
            new DscPropertyOverride { Name = "Name", Required = false },
        };

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Update-DscAdaptedResourceManifest")
          .AddParameter("InputObject", manifest)
          .AddParameter("PropertyOverride", overrides);

        var result = ps.Invoke<DscAdaptedResourceManifest>().Single();
        var required = result.ManifestSchema.Embedded["required"] as string[];

        required.Should().NotContain("Name");
    }

    [Fact]
    public void Invoke_UnknownProperty_WritesWarning()
    {
        using var runspace = CreateRunspace();
        var manifest = CreateSimpleManifest(runspace);

        var overrides = new[]
        {
            new DscPropertyOverride { Name = "NonExistent", Description = "Test" },
        };

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Update-DscAdaptedResourceManifest")
          .AddParameter("InputObject", manifest)
          .AddParameter("PropertyOverride", overrides);

        ps.Invoke<DscAdaptedResourceManifest>();

        ps.Streams.Warning.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Invoke_JsonSchemaKeyword_MergesIntoProperty()
    {
        using var runspace = CreateRunspace();
        var manifest = CreateSimpleManifest(runspace);

        var overrides = new[]
        {
            new DscPropertyOverride
            {
                Name = "Enabled",
                JsonSchema = new OrderedDictionary { ["default"] = true },
            },
        };

        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Update-DscAdaptedResourceManifest")
          .AddParameter("InputObject", manifest)
          .AddParameter("PropertyOverride", overrides);

        var result = ps.Invoke<DscAdaptedResourceManifest>().Single();
        var properties = result.ManifestSchema.Embedded["properties"] as OrderedDictionary;
        var enabledProp = properties!["Enabled"] as OrderedDictionary;

        enabledProp!["default"].Should().Be(true);
    }
}
