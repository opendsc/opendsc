// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Mof.Tests;

public class MofConverterTests
{
    #region MOF test data

    // One File resource + OMI_ConfigurationDocument metadata instance.
    private const string SingleResourceMof = """
        instance of MSFT_FileDirectoryConfiguration as $MSFT_FileDirectoryConfiguration1ref
        {
            ResourceID = "[File]ExampleFile";
            DestinationPath = "C:\\Temp\\hosts";
            SourcePath = "C:\\Windows\\System32\\drivers\\etc\\hosts";
            Type = "File";
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
            Author="thomas";
            Name="TestConfig";
        };
        """;

    // Two resources of different types.
    private const string MultipleResourcesMof = """
        instance of MSFT_FileDirectoryConfiguration as $MSFT_FileDirectoryConfiguration1ref
        {
            ResourceID = "[File]HostsFile";
            DestinationPath = "C:\\Temp\\hosts";
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
            Author="thomas";
            Name="TestConfig";
        };
        """;

    // Second resource depends on the first.
    private const string WithDependenciesMof = """
        instance of MSFT_FileDirectoryConfiguration as $MSFT_FileDirectoryConfiguration1ref
        {
            ResourceID = "[File]HostsFile";
            DestinationPath = "C:\\Temp\\hosts";
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
            DependsOn = "[File]HostsFile";
            ModuleName = "PSDesiredStateConfiguration";
            ModuleVersion = "1.1";
            ConfigurationName = "TestConfig";
        };

        instance of OMI_ConfigurationDocument
        {
            Version="2.0.0";
            MinimumCompatibleVersion = "1.0.0";
            CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
            Author="thomas";
            Name="TestConfig";
        };
        """;

    // Resource with multiple DependsOn entries as an array.
    private const string MultiDependenciesMof = """
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
            ResourceID = "[Environment]EnvA";
            Name = "ENV_A";
            Value = "val";
            Ensure = "Present";
            ModuleName = "PSDesiredStateConfiguration";
            ModuleVersion = "1.1";
            ConfigurationName = "TestConfig";
        };

        instance of MSFT_WindowsProcess as $MSFT_WindowsProcess1ref
        {
            ResourceID = "[WindowsProcess]Notepad";
            Path = "C:\\Windows\\notepad.exe";
            Arguments = "";
            DependsOn = {"[File]FileA", "[Environment]EnvA"};
            ModuleName = "PSDesiredStateConfiguration";
            ModuleVersion = "1.1";
            ConfigurationName = "TestConfig";
        };

        instance of OMI_ConfigurationDocument
        {
            Version="2.0.0";
            MinimumCompatibleVersion = "1.0.0";
            CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
            Author="thomas";
            Name="TestConfig";
        };
        """;

    // Mirrors the real-world FileSystemAuditRuleEntry MOF: a resource whose property
    // holds an alias reference that itself contains a nested alias reference.
    private const string NestedAliasesMof = """
        instance of InnerHelper as $InnerHelper1ref
        {
            StringProp = "inner value";
            BoolProp = True;
        };

        instance of OuterHelper as $OuterHelper1ref
        {
            Nested = {
                $InnerHelper1ref
            };
            Name = "outer";
        };

        instance of MSFT_TestResource as $MSFT_TestResource1ref
        {
            ResourceID = "[TestResource]MyResource";
            Items = {
                $OuterHelper1ref
            };
            Count = 2;
            ModuleName = "TestModule";
            ModuleVersion = "1.0";
            ConfigurationName = "TestConfig";
        };

        instance of OMI_ConfigurationDocument
        {
            Version="2.0.0";
            MinimumCompatibleVersion = "1.0.0";
            CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
            Author="thomas";
            Name="TestConfig";
        };
        """;

    // Helper instances with no ResourceID must not appear as top-level resources.
    private const string HelperInstancesMof = """
        instance of HelperObject as $HelperObject1ref
        {
            Value = "helper data";
        };

        instance of MSFT_MainResource as $MSFT_MainResource1ref
        {
            ResourceID = "[MainResource]Primary";
            Data = $HelperObject1ref;
            ModuleName = "TestModule";
            ModuleVersion = "1.0";
            ConfigurationName = "TestConfig";
        };

        instance of OMI_ConfigurationDocument
        {
            Version="2.0.0";
            MinimumCompatibleVersion = "1.0.0";
            CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
            Author="thomas";
            Name="TestConfig";
        };
        """;

    // MOF with encrypted credentials (ContentType="PasswordEncrypted").
    private const string EncryptedCredentialsMof = """
        instance of MSFT_Credential as $MSFT_Credential1ref
        {
            Password = "-----BEGIN CMS-----\nMIIBpAYJKoZIhvcNAQcDoIIBlTCCAZE=\n-----END CMS-----";
            UserName = "domain\\test";
        };

        instance of MSFT_FileDirectoryConfiguration as $MSFT_FileDirectoryConfiguration1ref
        {
            ResourceID = "[File]exampleFile";
            SourcePath = "\\\\Server\\share\\file.ext";
            DestinationPath = "C:\\destinationPath";
            Credential = $MSFT_Credential1ref;
            ModuleName = "PSDesiredStateConfiguration";
            ModuleVersion = "1.0";
            ConfigurationName = "CredentialEncryptionExample";
        };

        instance of OMI_ConfigurationDocument
        {
            Version="2.0.0";
            MinimumCompatibleVersion = "1.0.0";
            CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
            Author="Thomas";
            ContentType="PasswordEncrypted";
            Name="CredentialEncryptionExample";
        };
        """;

    // Resource with string, integer, boolean, and array property values.
    private const string AllPropertyTypesMof = """
        instance of MSFT_TestResource as $MSFT_TestResource1ref
        {
            ResourceID = "[TestResource]AllTypes";
            StringProp = "hello world";
            IntProp = 42;
            BoolProp = TRUE;
            ArrayProp = {"item1", "item2", "item3"};
            ModuleName = "TestModule";
            ModuleVersion = "1.0";
            ConfigurationName = "TestConfig";
        };

        instance of OMI_ConfigurationDocument
        {
            Version="2.0.0";
            MinimumCompatibleVersion = "1.0.0";
            CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
            Author="thomas";
            Name="TestConfig";
        };
        """;

    #endregion

    [Fact]
    public void ConvertText_SingleResource_ReturnsSingleResource()
    {
        var doc = MofConverter.ConvertText(SingleResourceMof);

        doc.Resources.Should().HaveCount(1);
    }

    [Fact]
    public void ConvertText_SingleResource_TypeIsModuleNameSlashFriendlyName()
    {
        var doc = MofConverter.ConvertText(SingleResourceMof);

        doc.Resources[0].Type.Should().Be("PSDesiredStateConfiguration/File");
    }

    [Fact]
    public void ConvertText_SingleResource_NameIsInstanceName()
    {
        var doc = MofConverter.ConvertText(SingleResourceMof);

        doc.Resources[0].Name.Should().Be("ExampleFile");
    }

    [Fact]
    public void ConvertText_SingleResource_MetaPropertiesAreStripped()
    {
        var doc = MofConverter.ConvertText(SingleResourceMof);
        var props = doc.Resources[0].Properties;

        props.Should().NotContainKey("ResourceID");
        props.Should().NotContainKey("ModuleName");
        props.Should().NotContainKey("ModuleVersion");
        props.Should().NotContainKey("ConfigurationName");
        props.Should().NotContainKey("SourceInfo");
        props.Should().NotContainKey("DependsOn");
    }

    [Fact]
    public void ConvertText_SingleResource_ResourcePropertiesArePreserved()
    {
        var doc = MofConverter.ConvertText(SingleResourceMof);
        var props = doc.Resources[0].Properties;

        props.Should().ContainKey("DestinationPath");
        props.Should().ContainKey("SourcePath");
        props.Should().ContainKey("Ensure");
    }

    [Fact]
    public void ConvertText_SingleResource_HasNoDependsOn()
    {
        var doc = MofConverter.ConvertText(SingleResourceMof);

        doc.Resources[0].DependsOn.Should().BeNull();
    }

    [Fact]
    public void ConvertText_OmiConfigurationDocument_IsExcluded()
    {
        // The OMI_ConfigurationDocument metadata instance must never appear as a resource.
        var doc = MofConverter.ConvertText(SingleResourceMof);

        doc.Resources.Should().NotContain(r => r.Type.Contains("OMI_ConfigurationDocument"));
    }

    [Fact]
    public void ConvertText_MultipleResources_ReturnsAllResources()
    {
        var doc = MofConverter.ConvertText(MultipleResourcesMof);

        doc.Resources.Should().HaveCount(2);
    }

    [Fact]
    public void ConvertText_MultipleResources_TypesAreCorrect()
    {
        var doc = MofConverter.ConvertText(MultipleResourcesMof);

        doc.Resources[0].Type.Should().Be("PSDesiredStateConfiguration/File");
        doc.Resources[1].Type.Should().Be("PSDesiredStateConfiguration/Environment");
    }

    [Fact]
    public void ConvertText_MultipleResources_NamesAreCorrect()
    {
        var doc = MofConverter.ConvertText(MultipleResourcesMof);

        doc.Resources[0].Name.Should().Be("HostsFile");
        doc.Resources[1].Name.Should().Be("MyVar");
    }

    [Fact]
    public void ConvertText_SingleDependsOn_IsConvertedToResourceIdExpression()
    {
        var doc = MofConverter.ConvertText(WithDependenciesMof);

        var envResource = doc.Resources.Should().Contain(r => r.Name == "MyVar").Which;
        envResource.DependsOn.Should().HaveCount(1);
        envResource.DependsOn![0].Should().Be("[resourceId('PSDesiredStateConfiguration/File', 'HostsFile')]");
    }

    [Fact]
    public void ConvertText_MultiDependsOn_ConvertedFromArray()
    {
        var doc = MofConverter.ConvertText(MultiDependenciesMof);

        var process = doc.Resources.Should().Contain(r => r.Name == "Notepad").Which;
        process.DependsOn.Should().HaveCount(2);
        process.DependsOn![0].Should().Be("[resourceId('PSDesiredStateConfiguration/File', 'FileA')]");
        process.DependsOn![1].Should().Be("[resourceId('PSDesiredStateConfiguration/Environment', 'EnvA')]");
    }

    [Fact]
    public void ConvertText_DependsOnResource_HasNoDependsOn()
    {
        var doc = MofConverter.ConvertText(WithDependenciesMof);

        var fileResource = doc.Resources.Should().Contain(r => r.Name == "HostsFile").Which;
        fileResource.DependsOn.Should().BeNull();
    }

    [Fact]
    public void ConvertText_AllPropertyTypes_StringIsString()
    {
        var doc = MofConverter.ConvertText(AllPropertyTypesMof);
        var props = doc.Resources[0].Properties;

        props["StringProp"]!.GetValue<string>().Should().Be("hello world");
    }

    [Fact]
    public void ConvertText_AllPropertyTypes_IntegerIsLong()
    {
        var doc = MofConverter.ConvertText(AllPropertyTypesMof);
        var props = doc.Resources[0].Properties;

        props["IntProp"]!.GetValue<long>().Should().Be(42);
    }

    [Fact]
    public void ConvertText_AllPropertyTypes_BooleanIsBool()
    {
        var doc = MofConverter.ConvertText(AllPropertyTypesMof);
        var props = doc.Resources[0].Properties;

        props["BoolProp"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void ConvertText_AllPropertyTypes_ArrayIsJsonArray()
    {
        var doc = MofConverter.ConvertText(AllPropertyTypesMof);
        var props = doc.Resources[0].Properties;

        var array = props["ArrayProp"]!.AsArray();
        array.Should().HaveCount(3);
        array[0]!.GetValue<string>().Should().Be("item1");
        array[1]!.GetValue<string>().Should().Be("item2");
        array[2]!.GetValue<string>().Should().Be("item3");
    }

    [Fact]
    public void ConvertText_Document_HasCorrectSchema()
    {
        var doc = MofConverter.ConvertText(SingleResourceMof);

        doc.Schema.Should().Be("https://aka.ms/dsc/schemas/v3/bundled/config/document.json");
    }

    [Fact]
    public void ConvertText_AliasReference_IsInlinedAsObject()
    {
        var doc = MofConverter.ConvertText(HelperInstancesMof);

        var props = doc.Resources[0].Properties;
        var data = props["Data"]!.AsObject();
        data["Value"]!.GetValue<string>().Should().Be("helper data");
    }

    [Fact]
    public void ConvertText_HelperInstances_AreNotTopLevelResources()
    {
        var doc = MofConverter.ConvertText(HelperInstancesMof);

        doc.Resources.Should().HaveCount(1);
        doc.Resources[0].Name.Should().Be("Primary");
    }

    [Fact]
    public void ConvertText_NestedAliases_AreResolvedRecursively()
    {
        var doc = MofConverter.ConvertText(NestedAliasesMof);

        var items = doc.Resources[0].Properties["Items"]!.AsArray();
        var outer = items[0]!.AsObject();
        outer["Name"]!.GetValue<string>().Should().Be("outer");

        var nested = outer["Nested"]!.AsArray();
        var inner = nested[0]!.AsObject();
        inner["StringProp"]!.GetValue<string>().Should().Be("inner value");
        inner["BoolProp"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void ConvertText_NestedAliases_OnlyResourceWithResourceIdIsReturned()
    {
        var doc = MofConverter.ConvertText(NestedAliasesMof);

        doc.Resources.Should().HaveCount(1);
        doc.Resources[0].Type.Should().Be("TestModule/TestResource");
    }

    [Fact]
    public void ConvertText_EncryptedCredentials_ThrowsNotSupportedException()
    {
        var act = () => MofConverter.ConvertText(EncryptedCredentialsMof);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*PasswordEncrypted*");
    }

    // ── Real and Enum property value coverage ────────────────────────────────

    [Fact]
    public void ConvertText_RealProperties_AreConvertedToNumbers()
    {
        const string mofWithReal = """
            instance of MSFT_TestResource as $MSFT_TestResource1ref
            {
                ResourceID = "[TestResource]RealTest";
                Real32Prop = 3.14;
                Real64Prop = 2.71828;
                ModuleName = "TestModule";
                ModuleVersion = "1.0";
                ConfigurationName = "TestConfig";
            };

            instance of OMI_ConfigurationDocument
            {
                Version="2.0.0";
                MinimumCompatibleVersion = "1.0.0";
                CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
                Author="thomas";
                Name="TestConfig";
            };
            """;

        var doc = MofConverter.ConvertText(mofWithReal);
        var props = doc.Resources[0].Properties;

        props["Real32Prop"]!.GetValue<double>().Should().BeApproximately(3.14, 0.01);
        props["Real64Prop"]!.GetValue<double>().Should().BeApproximately(2.71828, 0.00001);
    }

    [Fact]
    public void ConvertText_EnumValue_IsConvertedToString()
    {
        const string mofWithEnum = """
            instance of MSFT_TestResource as $MSFT_TestResource1ref
            {
                ResourceID = "[TestResource]EnumTest";
                Status = "Running";
                ModuleName = "TestModule";
                ModuleVersion = "1.0";
                ConfigurationName = "TestConfig";
            };

            instance of OMI_ConfigurationDocument
            {
                Version="2.0.0";
                MinimumCompatibleVersion = "1.0.0";
                CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
                Author="thomas";
                Name="TestConfig";
            };
            """;

        var doc = MofConverter.ConvertText(mofWithEnum);
        var props = doc.Resources[0].Properties;

        props["Status"]!.GetValue<string>().Should().Be("Running");
    }

    [Fact]
    public void ConvertText_EnumArrayValue_IsConvertedToStringArray()
    {
        const string mofWithEnumArray = """
            instance of MSFT_TestResource as $MSFT_TestResource1ref
            {
                ResourceID = "[TestResource]EnumArrayTest";
                Colors = {"Red", "Green", "Blue"};
                ModuleName = "TestModule";
                ModuleVersion = "1.0";
                ConfigurationName = "TestConfig";
            };

            instance of OMI_ConfigurationDocument
            {
                Version="2.0.0";
                MinimumCompatibleVersion = "1.0.0";
                CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
                Author="thomas";
                Name="TestConfig";
            };
            """;

        var doc = MofConverter.ConvertText(mofWithEnumArray);
        var props = doc.Resources[0].Properties;

        var colors = props["Colors"]!.AsArray();
        colors.Should().HaveCount(3);
        colors[0]!.GetValue<string>().Should().Be("Red");
        colors[1]!.GetValue<string>().Should().Be("Green");
        colors[2]!.GetValue<string>().Should().Be("Blue");
    }

    // ── Edge cases for ResourceID and DependsOn parsing ─────────────────────

    [Fact]
    public void ConvertText_EmptyBracketsInResourceId_ResourceIsSkipped()
    {
        const string mofWithEmptyBrackets = """
            instance of MSFT_TestResource as $MSFT_TestResource1ref
            {
                ResourceID = "[]InstanceName";
                ModuleName = "TestModule";
                ModuleVersion = "1.0";
                ConfigurationName = "TestConfig";
            };

            instance of MSFT_FileDirectoryConfiguration as $File1ref
            {
                ResourceID = "[File]ValidFile";
                DestinationPath = "C:\\Temp\\file.txt";
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
                Author="thomas";
                Name="TestConfig";
            };
            """;

        // Empty brackets in ResourceID result in TryParseResourceId returning false,
        // so the resource is skipped and not included in the results
        var doc = MofConverter.ConvertText(mofWithEmptyBrackets);
        doc.Resources.Should().HaveCount(1);
        doc.Resources[0].Name.Should().Be("ValidFile");
    }

    [Fact]
    public void ConvertText_DependsOnWithEmptyInstanceName_IsHandledGracefully()
    {
        const string mofWithEmptyDepName = """
            instance of MSFT_FileDirectoryConfiguration as $File1ref
            {
                ResourceID = "[File]FileA";
                DestinationPath = "C:\\Temp\\a.txt";
                Ensure = "Present";
                ModuleName = "PSDesiredStateConfiguration";
                ModuleVersion = "1.1";
                ConfigurationName = "TestConfig";
            };

            instance of MSFT_Environment as $Env1ref
            {
                ResourceID = "[Environment]EnvB";
                Name = "ENV_B";
                Value = "val";
                Ensure = "Present";
                DependsOn = "[File]";
                ModuleName = "PSDesiredStateConfiguration";
                ModuleVersion = "1.1";
                ConfigurationName = "TestConfig";
            };

            instance of OMI_ConfigurationDocument
            {
                Version="2.0.0";
                MinimumCompatibleVersion = "1.0.0";
                CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
                Author="thomas";
                Name="TestConfig";
            };
            """;

        var doc = MofConverter.ConvertText(mofWithEmptyDepName);
        var envRes = doc.Resources.Should().Contain(r => r.Name == "EnvB").Which;

        // DependsOn with empty instance name should result in type "[resourceId('PSDesiredStateConfiguration/File', '')]"
        envRes.DependsOn.Should().HaveCount(1);
        envRes.DependsOn![0].Should().Be("[resourceId('PSDesiredStateConfiguration/File', '')]");
    }

    // ── Zero resources and missing ResourceID ────────────────────────────────

    [Fact]
    public void ConvertText_OnlyOmiConfigurationDocument_ReturnsEmptyResourcesList()
    {
        const string mofWithOnlyMetadata = """
            instance of OMI_ConfigurationDocument
            {
                Version="2.0.0";
                MinimumCompatibleVersion = "1.0.0";
                CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
                Author="thomas";
                Name="TestConfig";
            };
            """;

        var doc = MofConverter.ConvertText(mofWithOnlyMetadata);

        doc.Resources.Should().HaveCount(0);
    }

    [Fact]
    public void ConvertText_ResourceWithoutResourceID_IsSkipped()
    {
        const string mofWithMissingResourceId = """
            instance of MSFT_TestResource as $MSFT_TestResource1ref
            {
                SomeProp = "value";
                ModuleName = "TestModule";
                ModuleVersion = "1.0";
                ConfigurationName = "TestConfig";
            };

            instance of MSFT_FileDirectoryConfiguration as $File1ref
            {
                ResourceID = "[File]ValidFile";
                DestinationPath = "C:\\Temp\\file.txt";
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
                Author="thomas";
                Name="TestConfig";
            };
            """;

        var doc = MofConverter.ConvertText(mofWithMissingResourceId);

        // Only the resource with a valid ResourceID should be returned
        doc.Resources.Should().HaveCount(1);
        doc.Resources[0].Name.Should().Be("ValidFile");
    }

    [Fact]
    public void ConvertText_DependsOnWithNullValue_IsIgnored()
    {
        const string mofWithNullDepends = """
            instance of MSFT_Environment as $Env1ref
            {
                ResourceID = "[Environment]EnvA";
                Name = "ENV_A";
                Value = "val";
                Ensure = "Present";
                DependsOn = null;
                ModuleName = "PSDesiredStateConfiguration";
                ModuleVersion = "1.1";
                ConfigurationName = "TestConfig";
            };

            instance of OMI_ConfigurationDocument
            {
                Version="2.0.0";
                MinimumCompatibleVersion = "1.0.0";
                CompatibleVersionAdditionalProperties= {"Omi_BaseResource:ConfigurationName"};
                Author="thomas";
                Name="TestConfig";
            };
            """;

        var doc = MofConverter.ConvertText(mofWithNullDepends);
        var res = doc.Resources[0];

        res.DependsOn.Should().BeNull();
    }
}
