// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class ImportDscAdaptedResourceManifestCommandTests
{
    private static readonly string s_fixturesPath =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Commands.Add(new SessionStateCmdletEntry(
            "Import-DscAdaptedResourceManifest",
            typeof(ImportDscAdaptedResourceManifestCommand), null));
        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();
        return runspace;
    }

    [Fact]
    public void Invoke_SimpleResourceJson_ReturnsManifest()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource.dsc.adaptedResource.json");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Import-DscAdaptedResourceManifest").AddParameter("Path", path);

        var results = ps.Invoke<DscAdaptedResourceManifest>();

        results.Should().HaveCount(1);
    }

    [Fact]
    public void Invoke_SimpleResourceJson_TypeIsCorrect()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource.dsc.adaptedResource.json");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Import-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();

        manifest.Type.Should().Be("SimpleResource/SimpleResource");
    }

    [Fact]
    public void Invoke_SimpleResourceJson_CapabilitiesFromJson()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource.dsc.adaptedResource.json");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Import-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();

        manifest.Capabilities.Should().BeEquivalentTo(["get", "set", "test"]);
    }

    [Fact]
    public void Invoke_SimpleResourceJson_VersionFromJson()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource.dsc.adaptedResource.json");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Import-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();

        manifest.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void Invoke_MinimalResourceJson_DefaultsForMissingFields()
    {
        var path = Path.Combine(s_fixturesPath, "MinimalResource.dsc.adaptedResource.json");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Import-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();

        manifest.Kind.Should().Be("resource");
        manifest.Capabilities.Should().BeEmpty();
        manifest.Description.Should().BeEmpty();
        manifest.Author.Should().BeEmpty();
    }

    [Fact]
    public void Invoke_SimpleResourceJson_SchemaHasProperties()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource.dsc.adaptedResource.json");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Import-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();
        var embedded = manifest.ManifestSchema.Embedded;

        embedded.Contains("properties").Should().BeTrue();
    }

    [Fact]
    public void Invoke_SimpleResourceJson_ToJsonRoundTrips()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource.dsc.adaptedResource.json");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Import-DscAdaptedResourceManifest").AddParameter("Path", path);

        var manifest = ps.Invoke<DscAdaptedResourceManifest>().Single();
        var json = manifest.ToJson();

        var act = () => System.Text.Json.JsonDocument.Parse(json);
        act.Should().NotThrow();
    }
}
