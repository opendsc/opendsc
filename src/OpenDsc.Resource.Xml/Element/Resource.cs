// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Xml.Element;

[DscResource("OpenDsc.Xml/Element", "0.1.0", Description = "Manage XML element content and attributes", Tags = ["xml", "element", "attribute", "xpath"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(FileNotFoundException), Description = "XML file not found")]
[ExitCode(4, Exception = typeof(XmlException), Description = "Invalid XML")]
[ExitCode(5, Exception = typeof(XPathException), Description = "Invalid XPath expression")]
[ExitCode(6, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(7, Exception = typeof(IOException), Description = "IO error")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>
{
    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        return JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (!File.Exists(instance.Path))
        {
            return new Schema()
            {
                Path = instance.Path,
                XPath = instance.XPath,
                Exist = false
            };
        }

        var doc = XDocument.Load(instance.Path, LoadOptions.PreserveWhitespace);
        var element = FindElement(doc, instance.XPath, instance.Namespaces);

        if (element == null)
        {
            return new Schema()
            {
                Path = instance.Path,
                XPath = instance.XPath,
                Exist = false
            };
        }

        var attributes = element.Attributes()
            .Where(a => !a.IsNamespaceDeclaration)
            .ToDictionary(a => a.Name.LocalName, a => a.Value);

        return new Schema()
        {
            Path = instance.Path,
            XPath = instance.XPath,
            Value = element.Value,
            Attributes = attributes.Count > 0 ? attributes : null,
            Namespaces = instance.Namespaces
        };
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (!File.Exists(instance.Path))
        {
            throw new FileNotFoundException($"XML file not found: {instance.Path}");
        }

        var encoding = DetectEncoding(instance.Path);
        var doc = XDocument.Load(instance.Path, LoadOptions.PreserveWhitespace);
        var element = FindOrCreateElement(doc, instance.XPath, instance.Namespaces);

        if (instance.Value != null)
        {
            element.Value = instance.Value;
        }

        if (instance.Attributes != null)
        {
            var currentAttrs = new HashSet<string>(
                element.Attributes().Where(a => !a.IsNamespaceDeclaration).Select(a => a.Name.LocalName),
                StringComparer.OrdinalIgnoreCase);
            var desiredAttrs = new HashSet<string>(instance.Attributes.Keys, StringComparer.OrdinalIgnoreCase);

            if (instance.Purge == true)
            {
                foreach (var attr in currentAttrs.Except(desiredAttrs))
                {
                    element.Attribute(attr)?.Remove();
                }
            }

            foreach (var (key, value) in instance.Attributes)
            {
                element.SetAttributeValue(key, value);
            }
        }

        SaveDocument(doc, instance.Path, encoding);
        return null;
    }

    public void Delete(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (!File.Exists(instance.Path))
        {
            return;
        }

        var encoding = DetectEncoding(instance.Path);
        var doc = XDocument.Load(instance.Path, LoadOptions.PreserveWhitespace);
        var element = FindElement(doc, instance.XPath, instance.Namespaces);

        if (element != null)
        {
            element.Remove();
            SaveDocument(doc, instance.Path, encoding);
        }
    }

    private static XElement? FindElement(XDocument doc, string xpath, Dictionary<string, string>? namespaces)
    {
        var navigator = doc.CreateNavigator();
        var nsManager = new XmlNamespaceManager(navigator.NameTable);

        if (namespaces != null)
        {
            foreach (var (prefix, uri) in namespaces)
            {
                nsManager.AddNamespace(prefix, uri);
            }
        }

        var nav = navigator.SelectSingleNode(xpath, nsManager);
        return nav?.UnderlyingObject as XElement;
    }

    private static XElement FindOrCreateElement(XDocument doc, string xpath, Dictionary<string, string>? namespaces)
    {
        var element = FindElement(doc, xpath, namespaces);
        if (element != null)
        {
            return element;
        }

        var parts = xpath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        XElement current = doc.Root ?? throw new InvalidOperationException("XML document has no root element");

        foreach (var part in parts.Skip(1))
        {
            var cleanPart = part.Split('[')[0];

            var child = current.Element(cleanPart);
            if (child == null)
            {
                child = new XElement(cleanPart);
                current.Add(child);
            }
            current = child;
        }

        return current;
    }

    private static Encoding DetectEncoding(string filePath)
    {
        using var reader = new StreamReader(filePath, true);
        reader.Peek();
        return reader.CurrentEncoding;
    }

    private static void SaveDocument(XDocument doc, string filePath, Encoding encoding)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = encoding,
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false
        };

        using var writer = XmlWriter.Create(filePath, settings);
        doc.Save(writer);
    }
}
