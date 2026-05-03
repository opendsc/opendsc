// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Schema;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class ConvertFromDscConfigurationMofCommandTests
{
    private const string SingleResourceMof = """
        instance of MSFT_FileDirectoryConfiguration as $MSFT_FileDirectoryConfiguration1ref
        {
            ResourceID = "[File]TestFile";
            DestinationPath = "C:\\Temp\\test.txt";
            Ensure = "Present";
            ModuleName = "PSDesiredStateConfiguration";
            ModuleVersion = "1.1";
            ConfigurationName = "TestConfig";
        };

        instance of OMI_ConfigurationDocument
        {
            Version="2.0.0";
            MinimumCompatibleVersion = "1.0.0";
            CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
            Author="author";
            Name="TestConfig";
        };
        """;

    private const string MultipleResourcesMof = """
        instance of MSFT_FileDirectoryConfiguration as $MSFT_FileDirectoryConfiguration1ref
        {
            ResourceID = "[File]FileA";
            DestinationPath = "C:\\Temp\\a.txt";
            Ensure = "Present";
            ModuleName = "PSDesiredStateConfiguration";
            ModuleVersion = "1.1";
            ConfigurationName = "TestConfig";
        };

        instance of MSFT_Environment as $MSFT_Environment1ref
        {
            ResourceID = "[Environment]MyVar";
            Name = "MY_VAR";
            Value = "hello";
            Ensure = "Present";
            ModuleName = "PSDesiredStateConfiguration";
            ModuleVersion = "1.1";
            ConfigurationName = "TestConfig";
        };

        instance of OMI_ConfigurationDocument
        {
            Version="2.0.0";
            MinimumCompatibleVersion = "1.0.0";
            CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
            Author="author";
            Name="TestConfig";
        };
        """;

    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.Create();
        iss.Commands.Add(new SessionStateCmdletEntry("ConvertFrom-DscConfigurationMof", typeof(ConvertFromDscConfigurationMofCommand), null));
        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();
        return runspace;
    }

    [Fact]
    public void Invoke_SingleResourceMof_ReturnsDscConfigDocument()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscConfigurationMof");

        var results = ps.Invoke<DscConfigDocument>(SingleResourceMof.Split('\n'));

        results.Should().HaveCount(1);
        results[0].Should().BeOfType<DscConfigDocument>();
    }

    [Fact]
    public void Invoke_SingleResourceMof_WithEmptyLines_DoesNotFail()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscConfigurationMof");

        var input = SingleResourceMof.Split('\n').Concat(new[] { string.Empty, string.Empty }).ToArray();
        var doc = ps.Invoke<DscConfigDocument>(input).Single();

        doc.Resources.Should().HaveCount(1);
        ps.Streams.Error.Should().BeEmpty();
    }

    [Fact]
    public void Invoke_SingleResourceMof_DocumentHasOneResource()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscConfigurationMof");

        var doc = ps.Invoke<DscConfigDocument>(SingleResourceMof.Split('\n')).Single();

        doc.Resources.Should().HaveCount(1);
    }

    [Fact]
    public void Invoke_MultipleResourcesMof_DocumentHasAllResources()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscConfigurationMof");

        var doc = ps.Invoke<DscConfigDocument>(MultipleResourcesMof.Split('\n')).Single();

        doc.Resources.Should().HaveCount(2);
    }

    [Fact]
    public void Invoke_SingleResourceMof_ResourceTypeIsCorrect()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscConfigurationMof");

        var doc = ps.Invoke<DscConfigDocument>(SingleResourceMof.Split('\n')).Single();

        doc.Resources[0].Type.Should().Be("PSDesiredStateConfiguration/File");
    }

    [Fact]
    public void Invoke_SingleResourceMof_AsJson_ReturnsSerializedConfig()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscConfigurationMof").AddParameter("AsJson");

        var results = ps.Invoke<string>(SingleResourceMof.Split('\n'));

        results.Should().HaveCount(1);

        using var doc = JsonDocument.Parse(results[0]);
        doc.RootElement.GetProperty("resources").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void Invoke_SingleResourceMof_AsJson_WithEmptyLines_DoesNotFail()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscConfigurationMof").AddParameter("AsJson");

        var input = SingleResourceMof.Split('\n').Concat(new[] { string.Empty, string.Empty }).ToArray();
        var results = ps.Invoke<string>(input);

        results.Should().HaveCount(1);

        using var doc = JsonDocument.Parse(results[0]);
        doc.RootElement.GetProperty("resources").GetArrayLength().Should().Be(1);
        ps.Streams.Error.Should().BeEmpty();
    }

    [Fact]
    public void Invoke_SingleResourceMof_AsJson_ResourceTypeIsCorrect()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscConfigurationMof").AddParameter("AsJson");

        var results = ps.Invoke<string>(SingleResourceMof.Split('\n'));

        results.Should().HaveCount(1);

        using var doc = JsonDocument.Parse(results[0]);
        var resources = doc.RootElement.GetProperty("resources").EnumerateArray().ToArray();
        resources[0].GetProperty("type").GetString().Should().Be("PSDesiredStateConfiguration/File");
    }
}
