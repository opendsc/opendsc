// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO;
using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using ValueResource = OpenDsc.Resource.Json.Value.Resource;
using ValueSchema = OpenDsc.Resource.Json.Value.Schema;

namespace OpenDsc.Resource.Json.Tests.Value;

[Trait("Category", "Integration")]
public sealed class ValueTests
{
    private readonly ValueResource _resource = new(OpenDsc.Resource.Json.SourceGenerationContext.Default);

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
        var attr = typeof(ValueResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Json/Value");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentFile_ReturnsExistFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.json");

        var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.config.value" });

        result.Exist.Should().BeFalse();
        result.Path.Should().Be(tempFile);
        result.JsonPath.Should().Be("$.config.value");
    }

    [Fact]
    public void Get_NonExistentValue_ReturnsExistFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.config.missing" });

            result.Exist.Should().BeFalse();
            result.Path.Should().Be(tempFile);
            result.JsonPath.Should().Be("$.config.missing");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_ExistingValue_ReturnsValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": { \"name\": \"MyApp\" } }", System.Text.Encoding.UTF8);

        try
        {
            var result = _resource.Get(new ValueSchema { Path = tempFile, JsonPath = "$.config.name" });

            result.Exist.Should().BeNull();
            result.Value.Should().NotBeNull();
            result.Value?.GetString().Should().Be("MyApp");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_PrimitiveValue_CreatesValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.timeout", Value = JsonDocument.Parse("30").RootElement });

            var content = File.ReadAllText(tempFile);
            var parsed = JsonDocument.Parse(content);
            parsed.RootElement.GetProperty("config").GetProperty("timeout").GetInt32().Should().Be(30);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_NonExistentFile_ThrowsFileNotFoundException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.json");

        var act = () => _resource.Set(new ValueSchema { Path = tempFile, JsonPath = "$.config.name", Value = JsonDocument.Parse("\"Test\"").RootElement });

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Delete_ExistingValue_RemovesValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": { \"timeout\": 30, \"name\": \"MyApp\" } }", System.Text.Encoding.UTF8);

        try
        {
            _resource.Delete(new ValueSchema { Path = tempFile, JsonPath = "$.config.timeout", Exist = false });

            var parsed = JsonDocument.Parse(File.ReadAllText(tempFile));
            parsed.RootElement.GetProperty("config").TryGetProperty("timeout", out _).Should().BeFalse();
            parsed.RootElement.GetProperty("config").GetProperty("name").GetString().Should().Be("MyApp");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Delete_NonExistentValue_DoesNotThrow()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"jsonvalue_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{ \"config\": {} }", System.Text.Encoding.UTF8);

        try
        {
            var act = () => _resource.Delete(new ValueSchema { Path = tempFile, JsonPath = "$.config.missing", Exist = false });
            act.Should().NotThrow();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
