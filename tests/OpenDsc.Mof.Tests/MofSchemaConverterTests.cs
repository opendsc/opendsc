// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Mof.Tests;

[Trait("Category", "Unit")]
public class MofSchemaConverterTests
{
    #region MOF test data

    // Simple resource with a variety of property types.
    private const string SimpleResourceMof = """
        [ClassVersion("1.0.0"), FriendlyName("Service")]
        class MSFT_ServiceResource : OMI_BaseResource
        {
            [Key] String Name;
            [Write, ValueMap{"Running", "Stopped"}, Values{"Running", "Stopped"}] String State;
            [Write] Boolean Enabled;
            [Write] UInt32 MaxRetry;
            [Write] SInt32 Priority;
            [Write] Real64 Timeout;
            [Write] DateTime LastRun;
            [Write, Description("The display name of the service.")] String DisplayName;
        };
        """;

    // Resource with [Required] and embedded-instance properties.
    private const string EmbeddedInstanceMof = """
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
            [Write, EmbeddedInstance("CredentialEntry")] String AdditionalCredentials[];
        };
        """;

    // Two-level nested $defs: Primary → ParentList → ChildEntry.
    private const string MultiClassMof = """
        [ClassVersion("0.9.0.0")]
        class ChildEntry
        {
            [Required] String Value;
        };

        [ClassVersion("0.9.0.0")]
        class ParentList
        {
            [Write, EmbeddedInstance("ChildEntry")] String Entries[];
        };

        [ClassVersion("0.9.0.0"), FriendlyName("MultiClass")]
        class MultiClassResource : OMI_BaseResource
        {
            [Key] String Path;
            [Required, EmbeddedInstance("ParentList")] String Config[];
        };
        """;

    // Primary class with a helper class that is never referenced.
    private const string UnreferencedClassMof = """
        [ClassVersion("1.0.0")]
        class UnusedHelper
        {
            [Write] String Prop;
        };

        [ClassVersion("1.0.0"), FriendlyName("SimpleResource")]
        class SimpleResource : OMI_BaseResource
        {
            [Key] String Name;
        };
        """;

    // No class inherits from OMI_BaseResource.
    private const string NoBaseResourceMof = """
        [ClassVersion("1.0.0")]
        class MSFT_SomeClass
        {
            [Key] String Name;
        };
        """;

    // Array property with ValueMap.
    private const string ArrayEnumMof = """
        [ClassVersion("1.0.0")]
        class ArrayEnumResource : OMI_BaseResource
        {
            [Key] String Name;
            [Write, ValueMap{"Red", "Green", "Blue"}, Values{"Red", "Green", "Blue"}] String Colors[];
        };
        """;

    #endregion

    // ── Root schema fields ────────────────────────────────────────────────────

    [Fact]
    public void ConvertText_HasSchemaUri()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        result["$schema"]!.GetValue<string>().Should().Be("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void ConvertText_HasTitleFromClassName()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        result["title"]!.GetValue<string>().Should().Be("MSFT_ServiceResource");
    }

    [Fact]
    public void ConvertText_TypeIsObject()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        result["type"]!.GetValue<string>().Should().Be("object");
    }

    // ── Required array ────────────────────────────────────────────────────────

    [Fact]
    public void ConvertText_KeyProperty_IsRequired()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        var required = result["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
        required.Should().Contain("Name");
    }

    [Fact]
    public void ConvertText_RequiredProperty_IsRequired()
    {
        var result = MofSchemaConverter.ConvertText(EmbeddedInstanceMof);
        var required = result["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
        required.Should().Contain("Credential");
    }

    [Fact]
    public void ConvertText_WriteProperty_IsNotRequired()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        var required = result["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
        required.Should().NotContain("State");
    }

    // ── Primitive type mapping ────────────────────────────────────────────────

    [Fact]
    public void ConvertText_StringProperty_IsStringType()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        var name = result["properties"]!["Name"]!.AsObject();
        name["type"]!.GetValue<string>().Should().Be("string");
    }

    [Fact]
    public void ConvertText_BooleanProperty_IsBooleanType()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        var enabled = result["properties"]!["Enabled"]!.AsObject();
        enabled["type"]!.GetValue<string>().Should().Be("boolean");
    }

    [Fact]
    public void ConvertText_Uint32Property_IsIntegerTypeWithMinimum()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        var maxRetry = result["properties"]!["MaxRetry"]!.AsObject();
        maxRetry["type"]!.GetValue<string>().Should().Be("integer");
        maxRetry["minimum"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public void ConvertText_Sint32Property_IsIntegerTypeWithoutMinimum()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        var priority = result["properties"]!["Priority"]!.AsObject();
        priority["type"]!.GetValue<string>().Should().Be("integer");
        priority.ContainsKey("minimum").Should().BeFalse();
    }

    [Fact]
    public void ConvertText_Real64Property_IsNumberType()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        var timeout = result["properties"]!["Timeout"]!.AsObject();
        timeout["type"]!.GetValue<string>().Should().Be("number");
    }

    [Fact]
    public void ConvertText_DatetimeProperty_IsStringWithDateTimeFormat()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        var lastRun = result["properties"]!["LastRun"]!.AsObject();
        lastRun["type"]!.GetValue<string>().Should().Be("string");
        lastRun["format"]!.GetValue<string>().Should().Be("date-time");
    }

    // ── Qualifiers ────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertText_ValueMapProperty_HasEnum()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        var state = result["properties"]!["State"]!.AsObject();
        var enumValues = state["enum"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
        enumValues.Should().BeEquivalentTo(new[] { "Running", "Stopped" });
    }

    [Fact]
    public void ConvertText_ArrayValueMapProperty_ItemsHaveEnum()
    {
        var result = MofSchemaConverter.ConvertText(ArrayEnumMof);
        var colors = result["properties"]!["Colors"]!.AsObject();
        colors["type"]!.GetValue<string>().Should().Be("array");
        var items = colors["items"]!.AsObject();
        items["type"]!.GetValue<string>().Should().Be("string");
        var enumValues = items["enum"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
        enumValues.Should().BeEquivalentTo(new[] { "Red", "Green", "Blue" });
    }

    [Fact]
    public void ConvertText_DescriptionQualifier_IsIncluded()
    {
        var result = MofSchemaConverter.ConvertText(SimpleResourceMof);
        var displayName = result["properties"]!["DisplayName"]!.AsObject();
        displayName["description"]!.GetValue<string>().Should().Be("The display name of the service.");
    }

    // ── Embedded instances and $ref ───────────────────────────────────────────

    [Fact]
    public void ConvertText_EmbeddedInstanceProperty_IsRef()
    {
        var result = MofSchemaConverter.ConvertText(EmbeddedInstanceMof);
        var credential = result["properties"]!["Credential"]!.AsObject();
        credential["$ref"]!.GetValue<string>().Should().Be("#/$defs/CredentialEntry");
    }

    [Fact]
    public void ConvertText_ArrayProperty_IsArrayType()
    {
        var result = MofSchemaConverter.ConvertText(EmbeddedInstanceMof);
        var creds = result["properties"]!["AdditionalCredentials"]!.AsObject();
        creds["type"]!.GetValue<string>().Should().Be("array");
    }

    [Fact]
    public void ConvertText_ArrayEmbeddedInstanceProperty_IsArrayOfRef()
    {
        var result = MofSchemaConverter.ConvertText(EmbeddedInstanceMof);
        var creds = result["properties"]!["AdditionalCredentials"]!.AsObject();
        var items = creds["items"]!.AsObject();
        items["$ref"]!.GetValue<string>().Should().Be("#/$defs/CredentialEntry");
    }

    // ── $defs ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertText_EmbeddedClasses_AreInDefs()
    {
        var result = MofSchemaConverter.ConvertText(EmbeddedInstanceMof);
        result["$defs"]!.AsObject().ContainsKey("CredentialEntry").Should().BeTrue();
    }

    [Fact]
    public void ConvertText_DefsClass_HasObjectType()
    {
        var result = MofSchemaConverter.ConvertText(EmbeddedInstanceMof);
        var credDef = result["$defs"]!["CredentialEntry"]!.AsObject();
        credDef["type"]!.GetValue<string>().Should().Be("object");
    }

    [Fact]
    public void ConvertText_DefsClass_HasProperties()
    {
        var result = MofSchemaConverter.ConvertText(EmbeddedInstanceMof);
        var credDef = result["$defs"]!["CredentialEntry"]!.AsObject();
        credDef["properties"]!.AsObject().ContainsKey("Username").Should().BeTrue();
    }

    [Fact]
    public void ConvertText_DefsClass_RequiredPropertiesAreRequired()
    {
        var result = MofSchemaConverter.ConvertText(EmbeddedInstanceMof);
        var credDef = result["$defs"]!["CredentialEntry"]!.AsObject();
        var required = credDef["required"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
        required.Should().Contain("Username");
        required.Should().NotContain("Password");
    }

    [Fact]
    public void ConvertText_TransitivelyReferencedClasses_AreInDefs()
    {
        var result = MofSchemaConverter.ConvertText(MultiClassMof);
        var defs = result["$defs"]!.AsObject();
        defs.ContainsKey("ParentList").Should().BeTrue();
        defs.ContainsKey("ChildEntry").Should().BeTrue();
    }

    [Fact]
    public void ConvertText_UnreferencedClasses_NotInDefs()
    {
        var result = MofSchemaConverter.ConvertText(UnreferencedClassMof);
        result["$defs"].Should().BeNull();
    }

    // ── Error handling ────────────────────────────────────────────────────────

    [Fact]
    public void ConvertText_NoOMIBaseResource_ThrowsInvalidOperationException()
    {
        Action act = () => MofSchemaConverter.ConvertText(NoBaseResourceMof);
        act.Should().Throw<InvalidOperationException>();
    }

    // ── File path entry point ─────────────────────────────────────────────────

    [Fact]
    public void Convert_FromFilePath_ReturnsPrimaryClassSchema()
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, SimpleResourceMof);
        try
        {
            var result = MofSchemaConverter.Convert(tempFile);
            result["title"]!.GetValue<string>().Should().Be("MSFT_ServiceResource");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
