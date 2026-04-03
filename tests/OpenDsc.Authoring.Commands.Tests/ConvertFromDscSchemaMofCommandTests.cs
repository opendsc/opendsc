// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.Json.Nodes;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

[Trait("Category", "Unit")]
public class ConvertFromDscSchemaMofCommandTests
{
    private const string ServiceSchemaMof = """
        [ClassVersion("1.0.0"), FriendlyName("Service")]
        class MSFT_ServiceResource : OMI_BaseResource
        {
            [Key] String Name;
            [Write, ValueMap{"Running","Stopped"}, Values{"Running","Stopped"}] String State;
            [Write] Boolean Enabled;
        };
        """;

    private const string EmbeddedInstanceSchemaMof = """
        [ClassVersion("1.0.0")]
        class CredentialEntry
        {
            [Required] String Username;
            [Write] String Password;
        };

        [ClassVersion("1.0.0")]
        class MSFT_ServiceWithCred : OMI_BaseResource
        {
            [Key] String Name;
            [Required, EmbeddedInstance("CredentialEntry")] String Credential;
        };
        """;

    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Commands.Add(new SessionStateCmdletEntry("ConvertFrom-DscSchemaMof", typeof(ConvertFromDscSchemaMofCommand), null));
        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();
        return runspace;
    }

    [Fact]
    public void Invoke_ServiceSchemaMof_ReturnsJsonObject()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaMof");

        var results = ps.Invoke(ServiceSchemaMof.Split('\n'));

        results.Should().HaveCount(1);
        results[0].BaseObject.Should().BeOfType<JsonObject>();
    }

    [Fact]
    public void Invoke_ServiceSchemaMof_WithEmptyLines_DoesNotFail()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaMof");

        var input = ServiceSchemaMof.Split('\n').Concat(new[] { string.Empty, string.Empty }).ToArray();
        var schema = (JsonObject)ps.Invoke(input).Single().BaseObject;

        schema["title"]!.GetValue<string>().Should().Be("MSFT_ServiceResource");
        ps.Streams.Error.Should().BeEmpty();
    }

    [Fact]
    public void Invoke_ServiceSchemaMof_TitleIsClassName()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaMof");

        var schema = (JsonObject)ps.Invoke(ServiceSchemaMof.Split('\n')).Single().BaseObject;

        schema["title"]!.GetValue<string>().Should().Be("MSFT_ServiceResource");
    }

    [Fact]
    public void Invoke_ServiceSchemaMof_HasPropertiesNode()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaMof");

        var schema = (JsonObject)ps.Invoke(ServiceSchemaMof.Split('\n')).Single().BaseObject;

        schema["properties"].Should().NotBeNull();
        schema["properties"]!["Name"].Should().NotBeNull();
    }

    [Fact]
    public void Invoke_EmbeddedInstanceSchemaMof_HasDefs()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaMof");

        var schema = (JsonObject)ps.Invoke(EmbeddedInstanceSchemaMof.Split('\n')).Single().BaseObject;

        schema["$defs"].Should().NotBeNull();
        schema["$defs"]!["CredentialEntry"].Should().NotBeNull();
    }
}
