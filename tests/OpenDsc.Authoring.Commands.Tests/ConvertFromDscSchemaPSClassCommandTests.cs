// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Authoring.Commands.Tests;

// Test DSC class types defined with actual DSC attributes.

[DscResource()]
internal sealed class SimpleTestResource
{
    [DscProperty(Key = true)]
    public string Name { get; set; } = string.Empty;

    [DscProperty(Mandatory = true)]
    [ValidateSet("Present", "Absent")]
    public string Ensure { get; set; } = string.Empty;

    [DscProperty()]
    public bool Enabled { get; set; }
}

[DscResource()]
internal sealed class RangedTestResource
{
    [DscProperty(Key = true)]
    public string Id { get; set; } = string.Empty;

    [DscProperty()]
    [ValidateRange(1, 100)]
    public int Count { get; set; }

    [DscProperty()]
    [ValidatePattern(@"^\w+$")]
    public string Tag { get; set; } = string.Empty;
}

[DscResource()]
internal sealed class ArrayTestResource
{
    [DscProperty(Key = true)]
    public string Name { get; set; } = string.Empty;

    [DscProperty()]
    public string[] Members { get; set; } = [];
}

internal sealed class NestedClass
{
    [DscProperty(Mandatory = true)]
    public string Username { get; set; } = string.Empty;

    [DscProperty()]
    public string Password { get; set; } = string.Empty;
}

[DscResource()]
internal sealed class NestedTestResource
{
    [DscProperty(Key = true)]
    public string Name { get; set; } = string.Empty;

    [DscProperty()]
    public NestedClass Credential { get; set; } = new();
}

internal enum TestState { Running, Stopped, Paused }

[DscResource()]
internal sealed class EnumTestResource
{
    [DscProperty(Key = true)]
    public string Name { get; set; } = string.Empty;

    [DscProperty()]
    public TestState State { get; set; }
}

[DscResource()]
internal sealed class NotConfigurableTestResource
{
    [DscProperty(Key = true)]
    public string Name { get; set; } = string.Empty;

    [DscProperty(NotConfigurable = true)]
    public string Status { get; set; } = string.Empty;

    [DscProperty()]
    public string Description { get; set; } = string.Empty;
}

[Trait("Category", "Unit")]
public class ConvertFromDscSchemaPSClassCommandTests
{
    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Commands.Add(new SessionStateCmdletEntry(
            "ConvertFrom-DscSchemaPSClass",
            typeof(ConvertFromDscSchemaPSClassCommand),
            null));
        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();
        return runspace;
    }

    [Fact]
    public void Invoke_ByType_ReturnsJsonString()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(SimpleTestResource));

        var results = ps.Invoke();

        results.Should().HaveCount(1);
        results[0].BaseObject.Should().BeOfType<string>();
        ps.Streams.Error.Should().BeEmpty();
    }

    [Fact]
    public void Invoke_ByType_TitleDefaultsToTypeName()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(SimpleTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("title").GetString().Should().Be("SimpleTestResource");
    }

    [Fact]
    public void Invoke_ByType_KeyPropertyIsRequired()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(SimpleTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        var required = doc.RootElement.GetProperty("required")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToArray();

        required.Should().Contain("Name");
    }

    [Fact]
    public void Invoke_ByType_MandatoryPropertyIsRequired()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(SimpleTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        var required = doc.RootElement.GetProperty("required")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToArray();

        required.Should().Contain("Ensure");
    }

    [Fact]
    public void Invoke_ByType_OptionalPropertyNotRequired()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(SimpleTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        var required = doc.RootElement.GetProperty("required")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToArray();

        required.Should().NotContain("Enabled");
    }

    [Fact]
    public void Invoke_ByType_ValidateSetBecomesEnum()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(SimpleTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        var enumValues = doc.RootElement
            .GetProperty("properties")
            .GetProperty("Ensure")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToArray();

        enumValues.Should().BeEquivalentTo("Present", "Absent");
    }

    [Fact]
    public void Invoke_ByType_BoolPropertyHasBooleanType()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(SimpleTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        doc.RootElement
            .GetProperty("properties")
            .GetProperty("Enabled")
            .GetProperty("type")
            .GetString()
            .Should().Be("boolean");
    }

    [Fact]
    public void Invoke_ByType_ValidateRangeAddsMinimumAndMaximum()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(RangedTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        var countProp = doc.RootElement.GetProperty("properties").GetProperty("Count");
        countProp.GetProperty("minimum").GetDouble().Should().Be(1);
        countProp.GetProperty("maximum").GetDouble().Should().Be(100);
    }

    [Fact]
    public void Invoke_ByType_ValidatePatternAddsPattern()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(RangedTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        doc.RootElement
            .GetProperty("properties")
            .GetProperty("Tag")
            .GetProperty("pattern")
            .GetString()
            .Should().Be(@"^\w+$");
    }

    [Fact]
    public void Invoke_ByType_StringArrayPropertyHasArrayType()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(ArrayTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        var membersProp = doc.RootElement.GetProperty("properties").GetProperty("Members");
        membersProp.GetProperty("type").GetString().Should().Be("array");
        membersProp.GetProperty("items").GetProperty("type").GetString().Should().Be("string");
    }

    [Fact]
    public void Invoke_ByType_EnumPropertyUsesStringTypeWithEnumValues()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(EnumTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        var stateProp = doc.RootElement.GetProperty("properties").GetProperty("State");
        stateProp.GetProperty("type").GetString().Should().Be("string");

        var enumValues = stateProp.GetProperty("enum")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToArray();

        enumValues.Should().BeEquivalentTo("Running", "Stopped", "Paused");
    }

    [Fact]
    public void Invoke_ByType_NestedDscClassProducesRef()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(NestedTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        var credProp = doc.RootElement.GetProperty("properties").GetProperty("Credential");
        credProp.GetProperty("$ref").GetString().Should().Be("#/$defs/NestedClass");
    }

    [Fact]
    public void Invoke_ByType_NestedDscClassPresentInDefs()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(NestedTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("$defs").GetProperty("NestedClass").Should().NotBeNull();
    }

    [Fact]
    public void Invoke_ByType_NestedDscClassMandatoryPropertyIsRequired()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(NestedTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        var required = doc.RootElement
            .GetProperty("$defs")
            .GetProperty("NestedClass")
            .GetProperty("required")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToArray();

        required.Should().Contain("Username");
    }

    [Fact]
    public void Invoke_ByType_NotConfigurablePropertyHasReadOnly()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(NotConfigurableTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        var statusProp = doc.RootElement.GetProperty("properties").GetProperty("Status");
        statusProp.GetProperty("readOnly").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void Invoke_ByType_ConfigurablePropertyDoesNotHaveReadOnly()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("ConvertFrom-DscSchemaPSClass")
          .AddParameter("ResourceType", typeof(NotConfigurableTestResource));

        var json = (string)ps.Invoke().Single().BaseObject;

        using var doc = JsonDocument.Parse(json);
        var descProp = doc.RootElement.GetProperty("properties").GetProperty("Description");

        // readOnly should not be present or be false
        var hasReadOnly = descProp.TryGetProperty("readOnly", out var readOnlyElem);
        if (hasReadOnly)
        {
            readOnlyElem.GetBoolean().Should().BeFalse();
        }
    }

    [Fact]
    public void Invoke_ByInfo_NotPowerShellResource_WritesError()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;

        var fakeInfo = new PSObject();
        fakeInfo.Properties.Add(new PSNoteProperty("Name", "BinaryResource"));
        fakeInfo.Properties.Add(new PSNoteProperty("ResourceType", "BinaryResource"));
        fakeInfo.Properties.Add(new PSNoteProperty("ImplementationDetail", "Binary"));
        fakeInfo.Properties.Add(new PSNoteProperty("ImplementedAs", "Binary"));

        ps.AddCommand("ConvertFrom-DscSchemaPSClass");
        ps.Invoke(new[] { fakeInfo });

        ps.Streams.Error.Should().HaveCount(1);
        ps.Streams.Error[0].FullyQualifiedErrorId.Should().Contain("NotPowerShellClassResource");
    }

    [Fact]
    public void Invoke_ByInfo_MissingProperties_WritesError()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;

        var invalidObj = new PSObject();
        invalidObj.Properties.Add(new PSNoteProperty("SomeOtherProperty", "value"));

        ps.AddCommand("ConvertFrom-DscSchemaPSClass");
        ps.Invoke(new[] { invalidObj });

        ps.Streams.Error.Should().HaveCount(1);
        ps.Streams.Error[0].FullyQualifiedErrorId.Should().Contain("InvalidInputObject");
    }

    [Fact]
    public void Invoke_ByInfo_ValidPowerShellResource_ReturnsJsonString()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;

        // Define a class inline so the type is available in the runspace's PowerShell session.
        ps.AddScript(@"
            class ByInfoTestResource {
                [DscProperty(Key=$true)]
                [string] $Name
                [DscProperty()]
                [bool] $Enabled
            }
        ");
        ps.Invoke();
        ps.Commands.Clear();

        var fakeInfo = new PSObject();
        fakeInfo.Properties.Add(new PSNoteProperty("Name", "ByInfoTest"));
        fakeInfo.Properties.Add(new PSNoteProperty("ResourceType", "ByInfoTestResource"));
        fakeInfo.Properties.Add(new PSNoteProperty("ImplementationDetail", "ClassBased"));
        fakeInfo.Properties.Add(new PSNoteProperty("ImplementedAs", "PowerShell"));

        ps.AddCommand("ConvertFrom-DscSchemaPSClass");
        var results = ps.Invoke(new[] { fakeInfo });

        ps.Streams.Error.Should().BeEmpty();
        results.Should().HaveCount(1);
        results[0].BaseObject.Should().BeOfType<string>();
    }
}
