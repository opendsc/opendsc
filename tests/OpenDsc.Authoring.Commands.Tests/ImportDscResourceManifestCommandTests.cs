// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class ImportDscResourceManifestCommandTests
{
    private static readonly string s_fixturesPath =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Commands.Add(new SessionStateCmdletEntry(
            "Import-DscResourceManifest",
            typeof(ImportDscResourceManifestCommand), null));
        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();
        return runspace;
    }

    [Fact]
    public void Invoke_TestModule_ReturnsManifestList()
    {
        var path = Path.Combine(s_fixturesPath, "TestModule.dsc.manifests.json");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Import-DscResourceManifest").AddParameter("Path", path);

        var results = ps.Invoke<DscResourceManifestList>();

        results.Should().HaveCount(1);
    }

    [Fact]
    public void Invoke_TestModule_HasAdaptedResources()
    {
        var path = Path.Combine(s_fixturesPath, "TestModule.dsc.manifests.json");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Import-DscResourceManifest").AddParameter("Path", path);

        var manifestList = ps.Invoke<DscResourceManifestList>().Single();

        manifestList.AdaptedResources.Should().HaveCount(2);
    }

    [Fact]
    public void Invoke_TestModule_HasResources()
    {
        var path = Path.Combine(s_fixturesPath, "TestModule.dsc.manifests.json");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Import-DscResourceManifest").AddParameter("Path", path);

        var manifestList = ps.Invoke<DscResourceManifestList>().Single();

        manifestList.Resources.Should().HaveCount(1);
    }

    [Fact]
    public void Invoke_TestModule_HasExtensions()
    {
        var path = Path.Combine(s_fixturesPath, "TestModule.dsc.manifests.json");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Import-DscResourceManifest").AddParameter("Path", path);

        var manifestList = ps.Invoke<DscResourceManifestList>().Single();

        manifestList.Extensions.Should().HaveCount(1);
    }

    [Fact]
    public void Invoke_TestModule_ToJsonRoundTrips()
    {
        var path = Path.Combine(s_fixturesPath, "TestModule.dsc.manifests.json");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Import-DscResourceManifest").AddParameter("Path", path);

        var manifestList = ps.Invoke<DscResourceManifestList>().Single();
        var json = manifestList.ToJson();

        var act = () => System.Text.Json.JsonDocument.Parse(json);
        act.Should().NotThrow();
    }
}
