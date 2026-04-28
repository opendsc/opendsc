// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

#pragma warning disable xUnit1051

using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

public class ParameterMergerTests
{
    private readonly ParameterMerger _merger = new();

    #region Merge Method Tests

    [Fact]
    public void Merge_WithEmptyList_ReturnsEmptyYaml()
    {
        var result = _merger.Merge([]);

        result.Trim().Should().Be("{}");
    }

    [Fact]
    public void Merge_WithSingleFile_ReturnsFileContent()
    {
        var yaml = "key: value\nnumber: 42";

        var result = _merger.Merge([yaml]);

        result.Should().Contain("key");
        result.Should().Contain("value");
    }

    [Fact]
    public void Merge_WithMultipleFiles_MergesInOrder()
    {
        var file1 = "env: dev\ndb: localhost";
        var file2 = "env: prod\nport: 5432";

        var result = _merger.Merge([file1, file2]);

        // file2 should override file1's env
        result.Should().Contain("env: prod");
        result.Should().Contain("port: 5432");
        result.Should().Contain("db: localhost");
    }

    [Fact]
    public void Merge_WithNestedObjects_PerformsDeepMerge()
    {
        var file1 = @"database:
  host: localhost
  port: 5432
  ssl: false";
        var file2 = @"database:
  port: 3306
  user: admin";

        var result = _merger.Merge([file1, file2]);

        result.Should().Contain("host: localhost");
        result.Should().Contain("port: 3306");
        result.Should().Contain("user: admin");
        result.Should().Contain("ssl: false");
    }

    [Fact]
    public void Merge_WithDeeplyNestedObjects_PreservesPaths()
    {
        var file1 = @"server:
  database:
    primary:
      host: db1.local
      port: 5432
      pool: 10";
        var file2 = @"server:
  database:
    primary:
      port: 3306
      pool: 20
      timeout: 30";

        var result = _merger.Merge([file1, file2]);

        result.Should().Contain("host: db1.local");
        result.Should().Contain("port: 3306");
        result.Should().Contain("pool: 20");
        result.Should().Contain("timeout: 30");
    }

    [Fact]
    public void Merge_WithArrays_ReplacesCompleteArray()
    {
        var file1 = @"servers:
  - srv1
  - srv2";
        var file2 = @"servers:
  - srv3
  - srv4
  - srv5";

        var result = _merger.Merge([file1, file2]);

        // Arrays should be replaced, not merged
        result.Should().Contain("srv3");
        result.Should().Contain("srv4");
        result.Should().Contain("srv5");
        result.Should().NotContain("srv1");
        result.Should().NotContain("srv2");
    }

    [Fact]
    public void Merge_WithJsonInput_ParsesCorrectly()
    {
        var json1 = @"{ ""key"": ""value"", ""number"": 42 }";
        var json2 = @"{ ""key"": ""newvalue"", ""extra"": true }";

        var result = _merger.Merge([json1, json2]);

        result.Should().Contain("key");
        result.Should().Contain("newvalue");
        result.Should().Contain("extra");
    }

    [Fact]
    public void Merge_WithMixedYamlAndJson_ParsesBoth()
    {
        var yaml = "env: prod\ndb: postgres";
        var json = @"{ ""cache"": ""redis"", ""env"": ""staging"" }";

        var result = _merger.Merge([yaml, json]);

        result.Should().Contain("env");
        result.Should().Contain("db");
        result.Should().Contain("cache");
    }

    [Fact]
    public void Merge_WithNullValues_PreservesNulls()
    {
        var file1 = "key1: value1\nkey2: null";
        var file2 = "key3: value3";

        var result = _merger.Merge([file1, file2]);

        result.Should().Contain("key1");
        result.Should().Contain("key2");
        result.Should().Contain("key3");
    }

    [Fact]
    public void Merge_WithEmptyObject_PreservesOtherContent()
    {
        var file1 = "key: value\nempty: {}";
        var file2 = "{}";
        var file3 = "key: updated";

        var result = _merger.Merge([file1, file2, file3]);

        result.Should().Contain("key: updated");
        result.Should().Contain("empty");
    }

    [Fact]
    public void Merge_WithOutputFormatJson_SerializesAsJson()
    {
        var yaml = "key: value\nnumber: 42";
        var options = new MergeOptions { OutputFormat = ParameterFormat.Json };

        var result = _merger.Merge([yaml], options);

        result.Should().Contain("\"key\"");
        result.Should().Contain("\"value\"");
        result.Should().StartWith("{");
        result.Should().EndWith("}");
    }

    [Fact]
    public void Merge_WithSpecialCharactersInKeys_HandlesCorrectly()
    {
        var yaml = @"env.name: production
app-version: '2.1.0'
'$special': value
nested@key: nested";

        var result = _merger.Merge([yaml]);

        result.Should().Contain("production");
        result.Should().Contain("2.1.0");
        result.Should().Contain("value");
        result.Should().Contain("nested");
    }

    [Fact]
    public void Merge_WithNumericVariations_PreservesTypes()
    {
        var yaml = @"int: 42
float: 3.14159
scientific: 1.5e-10
negative: -100
zero: 0";

        var result = _merger.Merge([yaml]);

        result.Should().Contain("42");
        result.Should().Contain("3.14159");
        result.Should().Contain("-100");
        result.Should().Contain("0");
    }

    [Fact]
    public void Merge_WithBooleanValues_PreservesBooleans()
    {
        var yaml = @"enabled: true
disabled: false
maybe: null";

        var result = _merger.Merge([yaml]);

        result.Should().Contain("true");
        result.Should().Contain("false");
    }

    [Fact]
    public void Merge_WithFiveOrMoreFiles_ProcessesAllInOrder()
    {
        var files = new[]
        {
            "setting: 1",
            "setting: 2",
            "setting: 3",
            "setting: 4\nextra: yes",
            "setting: 5"
        };

        var result = _merger.Merge(files);

        result.Should().Contain("setting: 5");
        result.Should().Contain("extra");
    }

    #endregion

    #region MergeWithProvenance Method Tests

    [Fact]
    public void MergeWithProvenance_WithSingleSource_TracksOrigin()
    {
        var sources = new[]
        {
            new ParameterSource { ScopeTypeName = "Default", ScopeValue = null, Precedence = 1, Content = "key: value" }
        };

        var result = _merger.MergeWithProvenance(sources);

        result.MergedContent.Should().Contain("key");
        result.Provenance.Should().ContainKey("key");
        result.Provenance["key"].ScopeTypeName.Should().Be("Default");
        result.Provenance["key"].Value.Should().Be("value");
    }

    [Fact]
    public void MergeWithProvenance_WithMultipleSources_TracksOverrides()
    {
        var sources = new[]
        {
            new ParameterSource { ScopeTypeName = "Default", ScopeValue = null, Precedence = 1, Content = "key: original" },
            new ParameterSource { ScopeTypeName = "Environment", ScopeValue = "Prod", Precedence = 2, Content = "key: overridden" }
        };

        var result = _merger.MergeWithProvenance(sources);

        result.Provenance["key"].ScopeTypeName.Should().Be("Environment");
        result.Provenance["key"].ScopeValue.Should().Be("Prod");
        result.Provenance["key"].Value.Should().Be("overridden");
        result.Provenance["key"].OverriddenValues.Should().NotBeNull();
        result.Provenance["key"].OverriddenValues.Should().HaveCount(1);
    }

    [Fact]
    public void MergeWithProvenance_WithNestedObjectOverride_TracksDottedPaths()
    {
        var sources = new[]
        {
            new ParameterSource { ScopeTypeName = "Default", ScopeValue = null, Precedence = 1, Content = @"db:
  host: localhost
  port: 5432" },
            new ParameterSource { ScopeTypeName = "Environment", ScopeValue = "Prod", Precedence = 2, Content = @"db:
  host: prod.db.local
  user: admin" }
        };

        var result = _merger.MergeWithProvenance(sources);

        result.Provenance.Should().ContainKey("db.host");
        result.Provenance.Should().ContainKey("db.port");
        result.Provenance.Should().ContainKey("db.user");

        result.Provenance["db.host"].ScopeTypeName.Should().Be("Environment");
        result.Provenance["db.port"].ScopeTypeName.Should().Be("Default");
        result.Provenance["db.user"].ScopeTypeName.Should().Be("Environment");
    }

    [Fact]
    public void MergeWithProvenance_WithThreeSources_TracksChainOfOverrides()
    {
        var sources = new[]
        {
            new ParameterSource { ScopeTypeName = "Default", ScopeValue = null, Precedence = 1, Content = "key: value1" },
            new ParameterSource { ScopeTypeName = "Environment", ScopeValue = "Dev", Precedence = 2, Content = "key: value2" },
            new ParameterSource { ScopeTypeName = "Node", ScopeValue = "Node1", Precedence = 3, Content = "key: value3" }
        };

        var result = _merger.MergeWithProvenance(sources);

        result.Provenance["key"].Value.Should().Be("value3");
        result.Provenance["key"].OverriddenValues.Should().HaveCount(2);
        result.Provenance["key"].OverriddenValues![0].Value.Should().Be("value2");
        result.Provenance["key"].OverriddenValues![1].Value.Should().Be("value1");
    }

    [Fact]
    public void MergeWithProvenance_WithDeeplyNestedPath_PreservesDottedNotation()
    {
        var sources = new[]
        {
            new ParameterSource { ScopeTypeName = "Default", ScopeValue = null, Precedence = 1, Content = @"app:
  server:
    config:
      timeout: 30
      retries: 3" }
        };

        var result = _merger.MergeWithProvenance(sources);

        result.Provenance.Should().ContainKey("app.server.config.timeout");
        result.Provenance.Should().ContainKey("app.server.config.retries");
        result.Provenance["app.server.config.timeout"].Value.Should().Be("30");
    }

    [Fact]
    public void MergeWithProvenance_WithArrayReplacement_TracksArrayOrigin()
    {
        var sources = new[]
        {
            new ParameterSource { ScopeTypeName = "Default", ScopeValue = null, Precedence = 1, Content = @"servers:
  - srv1
  - srv2" },
            new ParameterSource { ScopeTypeName = "Environment", ScopeValue = "Prod", Precedence = 2, Content = @"servers:
  - prod1
  - prod2
  - prod3" }
        };

        var result = _merger.MergeWithProvenance(sources);

        result.Provenance["servers"].ScopeTypeName.Should().Be("Environment");
    }

    [Fact]
    public void MergeWithProvenance_WithPartialObjectMerge_TracksMixedOrigins()
    {
        var sources = new[]
        {
            new ParameterSource { ScopeTypeName = "Default", ScopeValue = null, Precedence = 1, Content = @"config:
  key1: value1
  key2: value2
  key3: value3" },
            new ParameterSource { ScopeTypeName = "Environment", ScopeValue = "Prod", Precedence = 2, Content = @"config:
  key2: updated
  key4: value4" }
        };

        var result = _merger.MergeWithProvenance(sources);

        result.Provenance["config.key1"].ScopeTypeName.Should().Be("Default");
        result.Provenance["config.key2"].ScopeTypeName.Should().Be("Environment");
        result.Provenance["config.key3"].ScopeTypeName.Should().Be("Default");
        result.Provenance["config.key4"].ScopeTypeName.Should().Be("Environment");
    }

    [Fact]
    public void MergeWithProvenance_WithNullValues_TracksNullOrigin()
    {
        var sources = new[]
        {
            new ParameterSource { ScopeTypeName = "Default", ScopeValue = null, Precedence = 1, Content = "key: value" },
            new ParameterSource { ScopeTypeName = "Environment", ScopeValue = "Prod", Precedence = 2, Content = "key: null" }
        };

        var result = _merger.MergeWithProvenance(sources);

        result.Provenance["key"].Value.Should().BeNull();
        result.Provenance["key"].ScopeTypeName.Should().Be("Environment");
    }

    [Fact]
    public void MergeWithProvenance_WithJsonOutput_SerializesAsJson()
    {
        var sources = new[]
        {
            new ParameterSource { ScopeTypeName = "Default", ScopeValue = null, Precedence = 1, Content = "key: value" }
        };
        var options = new MergeOptions { OutputFormat = ParameterFormat.Json };

        var result = _merger.MergeWithProvenance(sources, options);

        result.MergedContent.Should().Contain("\"key\"");
        result.MergedContent.Should().StartWith("{");
    }

    #endregion

    #region Format Conversion Tests

    [Fact]
    public void Merge_YamlToJson_ConversionPreservesData()
    {
        var yaml = @"app: myapp
version: '1.0.0'
config:
  debug: 'true'
  port: '8080'";

        var result = _merger.Merge([yaml], new MergeOptions { OutputFormat = ParameterFormat.Json });

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.GetProperty("app").GetString().Should().Be("myapp");
        root.GetProperty("version").GetString().Should().Be("1.0.0");
        root.GetProperty("config").GetProperty("debug").GetString().Should().Be("true");
    }

    [Fact]
    public void Merge_JsonToYaml_ConversionPreservesData()
    {
        var json = @"{ ""app"": ""myapp"", ""version"": ""1.0.0"", ""config"": { ""debug"": true } }";

        var result = _merger.Merge([json]);

        result.Should().Contain("app");
        result.Should().Contain("myapp");
        result.Should().Contain("version");
    }

    [Fact]
    public void Merge_ComplexNestedJsonArray_ConversionWorks()
    {
        var json = @"{ ""items"": [ ""a"", ""b"", ""c"" ], ""nested"": { ""key"": ""value"" } }";

        var result = _merger.Merge([json], new MergeOptions { OutputFormat = ParameterFormat.Yaml });

        result.Should().Contain("items");
        result.Should().Contain("nested");
        result.Should().Contain("key");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Merge_WithEmptyStrings_HandlesGracefully()
    {
        var result = _merger.Merge(["", "", ""]);

        result.Trim().Should().Be("{}");
    }

    [Fact]
    public void Merge_WithWhitespaceOnlyStrings_HandlesGracefully()
    {
        var result = _merger.Merge(["   \n\t  ", "  \n  "]);

        result.Trim().Should().Be("{}");
    }

    [Fact]
    public void Merge_WithLargeNestedStructure_HandlesCorrectly()
    {
        var buildLargeYaml = () =>
        {
            var parts = new System.Text.StringBuilder();
            parts.AppendLine("level1:");
            parts.AppendLine("  level2:");
            parts.AppendLine("    level3:");
            parts.AppendLine("      level4:");
            for (int i = 0; i < 50; i++)
            {
                parts.AppendLine($"        key{i}: value{i}");
            }
            return parts.ToString();
        };

        var yaml = buildLargeYaml();
        var result = _merger.Merge([yaml]);

        result.Should().Contain("level1");
        result.Should().Contain("level4");
        result.Should().Contain("key0");
        result.Should().Contain("key49");
    }

    [Fact]
    public void MergeWithProvenance_WithManyPrecedenceLevels_HandlesAllCorrectly()
    {
        var sources = new[]
        {
            new ParameterSource { ScopeTypeName = "Default", ScopeValue = null, Precedence = 1, Content = "value: 1" },
            new ParameterSource { ScopeTypeName = "Level1", ScopeValue = "A", Precedence = 2, Content = "value: 2" },
            new ParameterSource { ScopeTypeName = "Level2", ScopeValue = "B", Precedence = 3, Content = "value: 3" },
            new ParameterSource { ScopeTypeName = "Level3", ScopeValue = "C", Precedence = 4, Content = "value: 4" }
        };

        var result = _merger.MergeWithProvenance(sources);

        result.Provenance["value"].Value.Should().Be("4");
        result.Provenance["value"].Precedence.Should().Be(4);
        result.Provenance["value"].OverriddenValues.Should().HaveCount(3);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_ComplexRealWorldScenario_WorksEnd()
    {
        var defaultConfig = @"database:
  host: localhost
  port: 5432
  pool: 10
app:
  timeout: 30
  retries: 3
  cache: memory
features:
  - auth
  - logging";

        var envConfig = @"database:
  host: db.prod.local
  pool: 20
app:
  cache: redis
  timeout: 60
logging:
  level: info";

        var nodeConfig = @"database:
  pool: 50
app:
  timeout: 90
  retries: 5
features:
  - auth
  - logging
  - monitoring";

        var result = _merger.Merge([defaultConfig, envConfig, nodeConfig]);

        result.Should().Contain("host: db.prod.local");
        result.Should().Contain("pool: 50");
        result.Should().Contain("timeout: 90");
        result.Should().Contain("cache: redis");
        result.Should().Contain("level: info");
        result.Should().Contain("monitoring");
    }

    [Fact]
    public void Integration_ProvenanceWithComplexScenario_TracksAllOrigins()
    {
        var sources = new[]
        {
            new ParameterSource { ScopeTypeName = "Default", ScopeValue = null, Precedence = 1, Content = @"db:
  host: local
  user: default
app:
  name: app1" },
            new ParameterSource { ScopeTypeName = "Environment", ScopeValue = "Prod", Precedence = 2, Content = @"db:
  host: prod.db
  pass: secret
app:
  debug: false" },
            new ParameterSource { ScopeTypeName = "Node", ScopeValue = "Web1", Precedence = 3, Content = @"app:
  port: 8080" }
        };

        var result = _merger.MergeWithProvenance(sources);

        result.Provenance["db.host"].ScopeTypeName.Should().Be("Environment");
        result.Provenance["db.user"].ScopeTypeName.Should().Be("Default");
        result.Provenance["db.pass"].ScopeTypeName.Should().Be("Environment");
        result.Provenance["app.name"].ScopeTypeName.Should().Be("Default");
        result.Provenance["app.debug"].ScopeTypeName.Should().Be("Environment");
        result.Provenance["app.port"].ScopeTypeName.Should().Be("Node");
        result.Provenance["app.port"].Value.Should().Be("8080");
    }

    #endregion
}
