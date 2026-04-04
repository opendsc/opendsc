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
public class NewDscResourceManifestCommandTests
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
            "New-DscResourceManifest",
            typeof(NewDscResourceManifestCommand), null));
        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();
        return runspace;
    }

    [Fact]
    public void Invoke_WithAdaptedResourcePipeline_ReturnsManifestList()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);
        ps.AddCommand("New-DscResourceManifest");

        var results = ps.Invoke<DscResourceManifestList>();

        results.Should().HaveCount(1);
        results[0].AdaptedResources.Should().HaveCount(1);
    }

    [Fact]
    public void Invoke_WithResource_ReturnsManifestListWithResource()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;

        var resource = new OrderedDictionary
        {
            ["$schema"] = "https://aka.ms/dsc/schemas/v3/bundled/resource/manifest.json",
            ["type"] = "Test/CommandResource",
            ["version"] = "1.0.0",
        };

        ps.AddCommand("New-DscResourceManifest")
          .AddParameter("Resource", new[] { resource });

        var results = ps.Invoke<DscResourceManifestList>();

        results.Should().HaveCount(1);
        results[0].Resources.Should().HaveCount(1);
    }

    [Fact]
    public void Invoke_WithMultipleAdapted_ReturnsAllInList()
    {
        var path = Path.Combine(s_fixturesPath, "MultiResource", "MultiResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);
        ps.AddCommand("New-DscResourceManifest");

        var results = ps.Invoke<DscResourceManifestList>();

        results.Should().HaveCount(1);
        results[0].AdaptedResources.Should().HaveCount(2);
    }

    [Fact]
    public void Invoke_EmptyPipeline_ReturnsEmptyManifestList()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscResourceManifest");

        var results = ps.Invoke<DscResourceManifestList>();

        results.Should().HaveCount(1);
        results[0].AdaptedResources.Should().BeEmpty();
        results[0].Resources.Should().BeEmpty();
    }

    [Fact]
    public void Invoke_WithAdapted_ToJsonProducesValidJson()
    {
        var path = Path.Combine(s_fixturesPath, "SimpleResource", "SimpleResource.psd1");
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("New-DscAdaptedResourceManifest").AddParameter("Path", path);
        ps.AddCommand("New-DscResourceManifest");

        var manifestList = ps.Invoke<DscResourceManifestList>().Single();
        var json = manifestList.ToJson();

        var act = () => System.Text.Json.JsonDocument.Parse(json);
        act.Should().NotThrow();
    }
}
