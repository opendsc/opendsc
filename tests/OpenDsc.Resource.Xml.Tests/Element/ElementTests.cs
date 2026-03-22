// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using XmlElementResource = OpenDsc.Resource.Xml.Element.Resource;
using XmlElementSchema = OpenDsc.Resource.Xml.Element.Schema;

namespace OpenDsc.Resource.Xml.Tests.Element;

[Trait("Category", "Integration")]
public sealed class ElementTests
{
    private readonly XmlElementResource _resource = new(OpenDsc.Resource.Xml.SourceGenerationContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.GetProperty("$schema").GetString()
            .Should().Be("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(XmlElementResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Xml/Element");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentFile_ReturnsExistFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.xml");

        var result = _resource.Get(new XmlElementSchema
        {
            Path = tempFile,
            XPath = "/configuration/setting"
        });

        result.Exist.Should().BeFalse();
        result.Path.Should().Be(tempFile);
    }

    [Fact]
    public void Get_NonExistentElement_ReturnsExistFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"get_nonexistent_element_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><configuration><settings><value>123</value></settings></configuration>");

        try
        {
            var result = _resource.Get(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/nonexistent"
            });

            result.Exist.Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_ExistingElement_ReturnsValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"get_value_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><configuration><settings><level>Debug</level></settings></configuration>");

        try
        {
            var result = _resource.Get(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/settings/level"
            });

            result.Value.Should().Be("Debug");
            result.Exist.Should().BeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_ExistingElement_ReturnsAttributes()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"get_attributes_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><configuration><add key=\"Setting1\" value=\"Value1\" enabled=\"true\" /></configuration>");

        try
        {
            var result = _resource.Get(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/add"
            });

            result.Attributes.Should().NotBeNull();
            result.Attributes!["key"].Should().Be("Setting1");
            result.Attributes["value"].Should().Be("Value1");
            result.Attributes["enabled"].Should().Be("true");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"set_missing_{Guid.NewGuid():N}.xml");

        var act = () => _resource.Set(new XmlElementSchema
        {
            Path = tempFile,
            XPath = "/configuration/setting",
            Value = "test"
        });

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Set_CreateElementWithTextContent_Succeeds()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"set_create_value_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><configuration></configuration>");

        try
        {
            _resource.Set(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/logging/level",
                Value = "Debug"
            });

            var getResult = _resource.Get(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/logging/level"
            });

            getResult.Value.Should().Be("Debug");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_CreateElementWithAttributes_Succeeds()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"set_create_attributes_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><configuration></configuration>");

        try
        {
            _resource.Set(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/add",
                Attributes = new Dictionary<string, string>
                {
                    ["key"] = "DatabaseConnection",
                    ["value"] = "Server=localhost"
                }
            });

            var getResult = _resource.Get(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/add"
            });

            getResult.Attributes.Should().NotBeNull();
            getResult.Attributes!["key"].Should().Be("DatabaseConnection");
            getResult.Attributes["value"].Should().Be("Server=localhost");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_CreateNestedElementsRecursively_Succeeds()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"set_nested_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><root></root>");

        try
        {
            _resource.Set(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/root/level1/level2/level3/value",
                Value = "DeepValue"
            });

            var getResult = _resource.Get(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/root/level1/level2/level3/value"
            });

            getResult.Value.Should().Be("DeepValue");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_AddAttributesAdditiveMode_PreservesOtherAttributes()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"set_additive_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><configuration><setting version=\"1.0\" enabled=\"false\" /></configuration>");

        try
        {
            _resource.Set(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/setting",
                Attributes = new Dictionary<string, string>
                {
                    ["version"] = "2.0"
                }
            });

            var getResult = _resource.Get(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/setting"
            });

            getResult.Attributes!["version"].Should().Be("2.0");
            getResult.Attributes["enabled"].Should().Be("false");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_PurgeTrue_RemovesUnlistedAttributes()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"set_purge_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><configuration><setting version=\"1.0\" enabled=\"false\" deprecated=\"true\" /></configuration>");

        try
        {
            _resource.Set(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/setting",
                Attributes = new Dictionary<string, string>
                {
                    ["version"] = "2.0",
                    ["enabled"] = "true"
                },
                Purge = true
            });

            var getResult = _resource.Get(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/setting"
            });

            getResult.Attributes!["version"].Should().Be("2.0");
            getResult.Attributes["enabled"].Should().Be("true");
            getResult.Attributes.Should().NotContainKey("deprecated");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Delete_ExistingElement_RemovesElement()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"delete_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><configuration><setting>ToDelete</setting><keep>Keep</keep></configuration>");

        try
        {
            _resource.Delete(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/setting"
            });

            var result = _resource.Get(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/setting"
            });

            result.Exist.Should().BeFalse();

            var keepResult = _resource.Get(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/keep"
            });

            keepResult.Value.Should().Be("Keep");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Delete_NonExistentElement_DoesNotThrow()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"delete_nonexistent_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><configuration></configuration>");

        try
        {
            var act = () => _resource.Delete(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/nonexistent"
            });

            act.Should().NotThrow();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Delete_NonExistentFile_DoesNotThrow()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"delete_missing_{Guid.NewGuid():N}.xml");

        var act = () => _resource.Delete(new XmlElementSchema
        {
            Path = tempFile,
            XPath = "/configuration/setting"
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void Set_UpdateExistingTextContent_ReplacesValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"set_update_value_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><configuration><setting>OldValue</setting></configuration>");

        try
        {
            _resource.Set(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/setting",
                Value = "NewValue"
            });

            var result = _resource.Get(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/setting"
            });

            result.Value.Should().Be("NewValue");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_PreservesUtf8Encoding()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"encoding_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempFile, "<?xml version=\"1.0\" encoding=\"UTF-8\"?><configuration><setting>Original</setting></configuration>", Encoding.UTF8);

        try
        {
            _resource.Set(new XmlElementSchema
            {
                Path = tempFile,
                XPath = "/configuration/setting",
                Value = "Updated"
            });

            var content = File.ReadAllText(tempFile, Encoding.UTF8);
            content.ToLowerInvariant().Should().Contain("encoding=\"utf-8\"");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
