// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class NewDscAdaptedResourceManifestCommandTests
{
    private static readonly string s_fixturesPath =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Commands.Add(new SessionStateCmdletEntry(
            "New-DscAdaptedResourceManifest",
            typeof(NewDscAdaptedResourceManifestCommand), null));
        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();
        return runspace;
    }

    [Fact]
    public void Invoke_SimpleResource_ReturnsSingleManifest()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var results = ps.Invoke<DscAdaptedResourceManifest>();

        results.Should().HaveCount(1);
    }

    [Fact]
    public void Invoke_SimpleResource_TypeIsCorrect()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();

        manifest.Type.Should().Be("SimpleResource/SimpleResource");
    }

    [Fact]
    public void Invoke_SimpleResource_VersionFromManifest()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();

        manifest.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void Invoke_SimpleResource_HasGetSetTestCapabilities()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();

        manifest.Capabilities.Should().BeEquivalentTo(["get", "set", "test"]);
    }

    [Fact]
    public void Invoke_SimpleResource_AuthorFromManifest()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();

        manifest.Author.Should().Be("Microsoft");
    }

    [Fact]
    public void Invoke_SimpleResource_KindIsResource()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();

        manifest.Kind.Should().Be("resource");
    }

    [Fact]
    public void Invoke_SimpleResource_RequiresAdapter()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();

        manifest.RequireAdapter.Should().Be("Microsoft.Adapter/PowerShell");
    }

    [Fact]
    public void Invoke_SimpleResource_PathIsPsd1()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();

        manifest.Path.Should().Be("SimpleResource.psd1");
    }

    [Fact]
    public void Invoke_SimpleResource_SchemaHasKeyPropertyRequired()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();
        var schema = manifest.ManifestSchema.Embedded;
        var required = schema["required"] as string[];

        required.Should().Contain("Name");
    }

    [Fact]
    public void Invoke_MultiResource_ReturnsTwoManifests()
    {
        var path = Path.Combine(s_fixturesPath, "MultiResource", "MultiResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var results = ps.Invoke<DscAdaptedResourceManifest>();

        results.Should().HaveCount(2);
    }

    [Fact]
    public void Invoke_MultiResource_ResourceAHasDeleteAndExportCapabilities()
    {
        var path = Path.Combine(s_fixturesPath, "MultiResource", "MultiResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var results = ps.Invoke<DscAdaptedResourceManifest>();
        var resourceA = results.First(r => r.Type == "MultiResource/ResourceA");

        resourceA.Capabilities.Should().Contain("delete");
        resourceA.Capabilities.Should().Contain("export");
    }

    [Fact]
    public void Invoke_MultiResource_ResourceBHasWhatIfCapability()
    {
        var path = Path.Combine(s_fixturesPath, "MultiResource", "MultiResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var results = ps.Invoke<DscAdaptedResourceManifest>();
        var resourceB = results.First(r => r.Type == "MultiResource/ResourceB");

        resourceB.Capabilities.Should().Contain("whatIf");
    }

    [Fact]
    public void Invoke_MultiResource_ResourceAInheritsBaseProperty()
    {
        var path = Path.Combine(s_fixturesPath, "MultiResource", "MultiResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var results = ps.Invoke<DscAdaptedResourceManifest>();
        var resourceA = results.First(r => r.Type == "MultiResource/ResourceA");
        var properties = resourceA.ManifestSchema.Embedded["properties"] as System.Collections.Specialized.OrderedDictionary;

        properties!.Contains("BaseProperty").Should().BeTrue();
    }

    [Fact]
    public void Invoke_MultiResource_ResourceAEnumPropertyHasEnumValues()
    {
        var path = Path.Combine(s_fixturesPath, "MultiResource", "MultiResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var results = ps.Invoke<DscAdaptedResourceManifest>();
        var resourceA = results.First(r => r.Type == "MultiResource/ResourceA");
        var properties = resourceA.ManifestSchema.Embedded["properties"] as System.Collections.Specialized.OrderedDictionary;
        var ensureProp = properties!["Ensure"] as System.Collections.Specialized.OrderedDictionary;

        var enumValues = ensureProp!["enum"] as string[];
        enumValues.Should().BeEquivalentTo(["Present", "Absent"]);
    }

    [Fact]
    public void Invoke_MultiResource_ResourceAArrayPropertyHasArrayType()
    {
        var path = Path.Combine(s_fixturesPath, "MultiResource", "MultiResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var results = ps.Invoke<DscAdaptedResourceManifest>();
        var resourceA = results.First(r => r.Type == "MultiResource/ResourceA");
        var properties = resourceA.ManifestSchema.Embedded["properties"] as System.Collections.Specialized.OrderedDictionary;
        var tagsProp = properties!["Tags"] as System.Collections.Specialized.OrderedDictionary;

        tagsProp!["type"].Should().Be("array");
    }

    [Fact]
    public void Invoke_MultiResource_ResourceBHashtablePropertyHasObjectType()
    {
        var path = Path.Combine(s_fixturesPath, "MultiResource", "MultiResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var results = ps.Invoke<DscAdaptedResourceManifest>();
        var resourceB = results.First(r => r.Type == "MultiResource/ResourceB");
        var properties = resourceB.ManifestSchema.Embedded["properties"] as System.Collections.Specialized.OrderedDictionary;
        var settingsProp = properties!["Settings"] as System.Collections.Specialized.OrderedDictionary;

        settingsProp!["type"].Should().Be("object");
    }

    [Fact]
    public void Invoke_StandaloneResource_ParsesPs1File()
    {
        var path = Path.Combine(s_fixturesPath, "StandaloneResource.ps1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var results = ps.Invoke<DscAdaptedResourceManifest>();

        results.Should().HaveCount(1);
        results[0].Type.Should().Be("StandaloneResource/StandaloneResource");
    }

    [Fact]
    public void Invoke_NoDscResource_WritesWarning()
    {
        var path = Path.Combine(s_fixturesPath, "NoDscResource.psm1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var results = ps.Invoke<DscAdaptedResourceManifest>();

        results.Should().BeEmpty();
        ps.Streams.Warning.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Invoke_HelpResource_UsesHelpDescription()
    {
        var path = Path.Combine(s_fixturesPath, "HelpResource", "HelpResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();
        var properties = manifest.ManifestSchema.Embedded["properties"] as System.Collections.Specialized.OrderedDictionary;
        var nameProp = properties!["Name"] as System.Collections.Specialized.OrderedDictionary;

        nameProp!["description"].Should().Be("The unique name identifying this resource instance.");
    }

    [Fact]
    public void Invoke_HelpResource_UsesSynopsisAsDescription()
    {
        var path = Path.Combine(s_fixturesPath, "HelpResource", "HelpResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();

        manifest.Description.Should().Be("Manages a help-documented resource.");
    }

    [Fact]
    public void Invoke_SimpleResource_SchemaExcludesNonDscProperties()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();
        var properties = manifest.ManifestSchema.Embedded["properties"] as System.Collections.Specialized.OrderedDictionary;

        properties!.Count.Should().Be(3);
        properties.Contains("Name").Should().BeTrue();
        properties.Contains("Value").Should().BeTrue();
        properties.Contains("Enabled").Should().BeTrue();
    }

    [Fact]
    public void Invoke_SimpleResource_ToJsonProducesValidJson()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();
        var json = manifest.ToJson();

        var act = () => System.Text.Json.JsonDocument.Parse(json);
        act.Should().NotThrow();
    }
}
