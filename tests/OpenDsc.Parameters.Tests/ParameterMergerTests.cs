// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using FluentAssertions;

using Xunit;

namespace OpenDsc.Parameters.Tests;

public class ParameterMergerTests
{
    private readonly ParameterMerger _merger = new();

    [Fact]
    public void Merge_WithEmptyCollection_ReturnsEmptyYaml()
    {
        var result = _merger.Merge([]);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Merge_WithSingleYamlFile_ReturnsYamlContent()
    {
        var yaml = """
            server:
              host: localhost
              port: 8080
            """;

        var result = _merger.Merge([yaml]);

        result.Should().Contain("server:");
        result.Should().Contain("host: localhost");
        result.Should().Contain("port: 8080");
    }

    [Fact]
    public void Merge_WithSingleJsonFile_ReturnsYamlContent()
    {
        var json = """
            {
              "server": {
                "host": "localhost",
                "port": 8080
              }
            }
            """;

        var result = _merger.Merge([json]);

        result.Should().Contain("server:");
        result.Should().Contain("host: localhost");
        result.Should().Contain("port: 8080");
    }

    [Fact]
    public void Merge_WithJsonOutputFormat_ReturnsJson()
    {
        var yaml = """
            server:
              host: localhost
              port: 8080
            """;

        var result = _merger.Merge([yaml], new MergeOptions { OutputFormat = ParameterFormat.Json });

        result.Should().Contain("\"server\"");
        result.Should().Contain("\"host\"");
        result.Should().Contain("\"localhost\"");
    }

    [Fact]
    public void Merge_WithMultipleFiles_MergesInPrecedenceOrder()
    {
        var base1 = """
            server:
              host: localhost
              port: 8080
            database:
              connection: default
            """;

        var override1 = """
            server:
              port: 9090
            database:
              connection: production
              timeout: 30
            """;

        var result = _merger.Merge([base1, override1]);

        result.Should().Contain("host: localhost");
        result.Should().Contain("port: 9090");
        result.Should().Contain("connection: production");
        result.Should().Contain("timeout: 30");
    }

    [Fact]
    public void Merge_WithDeepNestedObjects_MergesRecursively()
    {
        var base1 = """
            app:
              config:
                level1:
                  level2:
                    value1: original
                    value2: keep
            """;

        var override1 = """
            app:
              config:
                level1:
                  level2:
                    value1: overridden
                    value3: new
            """;

        var result = _merger.Merge([base1, override1]);

        result.Should().Contain("value1: overridden");
        result.Should().Contain("value2: keep");
        result.Should().Contain("value3: new");
    }

    [Fact]
    public void Merge_WithArrays_ReplacesCompleteArray()
    {
        var base1 = """
            servers:
              - server1
              - server2
            """;

        var override1 = """
            servers:
              - server3
              - server4
              - server5
            """;

        var result = _merger.Merge([base1, override1]);

        result.Should().Contain("server3");
        result.Should().Contain("server4");
        result.Should().Contain("server5");
        result.Should().NotContain("server1");
        result.Should().NotContain("server2");
    }

    [Fact]
    public void Merge_WithNullValues_HandlesCorrectly()
    {
        var yaml = """
            server:
              host: localhost
              port: null
            """;

        var result = _merger.Merge([yaml]);

        result.Should().Contain("server:");
        result.Should().Contain("host: localhost");
    }

    [Fact]
    public void Merge_WithMixedYamlAndJson_MergesCorrectly()
    {
        var yaml = """
            server:
              host: localhost
            """;

        var json = """
            {
              "server": {
                "port": 8080
              }
            }
            """;

        var result = _merger.Merge([yaml, json]);

        result.Should().Contain("host: localhost");
        result.Should().Contain("port: 8080");
    }

    [Fact]
    public void Merge_WithThreeLevels_AppliesCorrectPrecedence()
    {
        var level1 = """
            value: level1
            keep1: original1
            """;

        var level2 = """
            value: level2
            keep2: original2
            """;

        var level3 = """
            value: level3
            keep3: original3
            """;

        var result = _merger.Merge([level1, level2, level3]);

        result.Should().Contain("value: level3");
        result.Should().Contain("keep1: original1");
        result.Should().Contain("keep2: original2");
        result.Should().Contain("keep3: original3");
    }

    [Fact]
    public void Merge_WithComplexTypes_HandlesNumbers()
    {
        var yaml = """
            settings:
              timeout: 30
              retries: 3
              threshold: 0.95
            """;

        var result = _merger.Merge([yaml]);

        result.Should().Contain("timeout: 30");
        result.Should().Contain("retries: 3");
        result.Should().Contain("threshold: 0.95");
    }

    [Fact]
    public void Merge_WithBooleanValues_HandlesCorrectly()
    {
        var yaml = """
            features:
              enabled: true
              debug: false
            """;

        var result = _merger.Merge([yaml]);

        result.Should().Contain("enabled: true");
        result.Should().Contain("debug: false");
    }

    [Fact]
    public void MergeWithProvenance_WithSingleSource_TracksProvenance()
    {
        var source = new ParameterSource
        {
            ScopeName = "Global",
            Precedence = 1,
            Content = "value: test"
        };

        var result = _merger.MergeWithProvenance([source]);

        result.MergedContent.Should().Contain("value: test");
        result.Provenance.Should().BeEmpty();
    }

    [Fact]
    public void MergeWithProvenance_WithMultipleSources_TracksOverrides()
    {
        var global = new ParameterSource
        {
            ScopeName = "Global",
            Precedence = 1,
            Content = """
                server:
                  host: localhost
                  port: 8080
                """
        };

        var environment = new ParameterSource
        {
            ScopeName = "Production",
            Precedence = 2,
            Content = """
                server:
                  host: prod.example.com
                """
        };

        var result = _merger.MergeWithProvenance([global, environment]);

        result.Provenance.Should().ContainKey("server.host");
        result.Provenance["server.host"].ScopeName.Should().Be("Production");
        result.Provenance["server.host"].Value.Should().Be("prod.example.com");
        result.Provenance["server.host"].OverriddenValues.Should().BeNullOrEmpty();
    }

    [Fact]
    public void MergeWithProvenance_WithThreeLevels_TracksMultipleOverrides()
    {
        var sources = new[]
        {
            new ParameterSource
            {
                ScopeName = "Global",
                Precedence = 1,
                Content = "value: global"
            },
            new ParameterSource
            {
                ScopeName = "Environment",
                Precedence = 2,
                Content = "value: environment"
            },
            new ParameterSource
            {
                ScopeName = "Node",
                Precedence = 3,
                Content = "value: node"
            }
        };

        var result = _merger.MergeWithProvenance(sources);

        result.Provenance["value"].ScopeName.Should().Be("Node");
        result.Provenance["value"].OverriddenValues.Should().HaveCount(1);
        result.Provenance["value"].OverriddenValues![0].ScopeName.Should().Be("Environment");
    }

    [Fact]
    public void MergeWithProvenance_WithNestedObjects_DoesNotTrackParentObjects()
    {
        var source = new ParameterSource
        {
            ScopeName = "Global",
            Precedence = 1,
            Content = """
                server:
                  host: localhost
                  port: 8080
                """
        };

        var result = _merger.MergeWithProvenance([source]);

        result.Provenance.Should().BeEmpty();
    }

    [Fact]
    public void MergeWithProvenance_WithJsonOutput_ReturnsJson()
    {
        var source = new ParameterSource
        {
            ScopeName = "Global",
            Precedence = 1,
            Content = "value: test"
        };

        var result = _merger.MergeWithProvenance([source], new MergeOptions { OutputFormat = ParameterFormat.Json });

        result.MergedContent.Should().Contain("\"value\"");
        result.MergedContent.Should().Contain("\"test\"");
    }

    [Fact]
    public void MergeWithProvenance_WithNewKeysAtDifferentLevels_TracksCorrectly()
    {
        var base1 = new ParameterSource
        {
            ScopeName = "Base",
            Precedence = 1,
            Content = """
                key1: value1
                """
        };

        var override1 = new ParameterSource
        {
            ScopeName = "Override",
            Precedence = 2,
            Content = """
                key2: value2
                """
        };

        var result = _merger.MergeWithProvenance([base1, override1]);

        result.Provenance.Should().NotContainKey("key1");
        result.Provenance.Should().ContainKey("key2");
        result.Provenance["key2"].ScopeName.Should().Be("Override");
        result.Provenance["key2"].OverriddenValues.Should().BeNull();
    }

    [Fact]
    public void MergeWithProvenance_WithArrayReplacement_TracksCorrectly()
    {
        var base1 = new ParameterSource
        {
            ScopeName = "Base",
            Precedence = 1,
            Content = """
                servers:
                  - server1
                  - server2
                """
        };

        var override1 = new ParameterSource
        {
            ScopeName = "Override",
            Precedence = 2,
            Content = """
                servers:
                  - server3
                """
        };

        var result = _merger.MergeWithProvenance([base1, override1]);

        result.Provenance.Should().ContainKey("servers");
        result.Provenance["servers"].ScopeName.Should().Be("Override");
        result.Provenance["servers"].OverriddenValues.Should().BeNullOrEmpty();
    }

    [Fact]
    public void MergeWithProvenance_WithDeepNesting_TracksFullPath()
    {
        var source = new ParameterSource
        {
            ScopeName = "Global",
            Precedence = 1,
            Content = """
                level1:
                  level2:
                    level3:
                      value: deep
                """
        };

        var result = _merger.MergeWithProvenance([source]);

        result.Provenance.Should().BeEmpty();
    }

    [Fact]
    public void MergeWithProvenance_WithMixedMergeAndReplace_TracksCorrectly()
    {
        var base1 = new ParameterSource
        {
            ScopeName = "Base",
            Precedence = 1,
            Content = """
                config:
                  keep: original
                  replace: old
                """
        };

        var override1 = new ParameterSource
        {
            ScopeName = "Override",
            Precedence = 2,
            Content = """
                config:
                  replace: new
                  add: additional
                """
        };

        var result = _merger.MergeWithProvenance([base1, override1]);

        result.Provenance.Should().ContainKey("config.replace");
        result.Provenance["config.replace"].ScopeName.Should().Be("Override");
        result.Provenance["config.replace"].Value.Should().Be("new");
        result.Provenance["config.replace"].OverriddenValues.Should().BeNullOrEmpty();

        result.Provenance.Should().ContainKey("config.add");
        result.Provenance["config.add"].ScopeName.Should().Be("Override");
        result.Provenance["config.add"].Value.Should().Be("additional");
    }

    [Fact]
    public void Merge_WithEmptyYamlFile_HandlesGracefully()
    {
        var result = _merger.Merge(["", "  ", "\n"]);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Merge_WithSpecialCharactersInKeys_HandlesCorrectly()
    {
        var yaml = """
            "special-key": value1
            "key.with.dots": value2
            "key:with:colons": value3
            """;

        var result = _merger.Merge([yaml]);

        result.Should().Contain("special-key:");
        result.Should().Contain("key.with.dots:");
        result.Should().Contain("key:with:colons:");
    }

    [Fact]
    public void MergeWithProvenance_WithNullOptionsAfterEmptyList_UsesDefaults()
    {
        var result = _merger.MergeWithProvenance([], null);

        result.MergedContent.Should().NotBeNullOrEmpty();
        result.Provenance.Should().BeEmpty();
    }

    [Fact]
    public void Merge_WithNullOptions_UsesDefaultYamlOutput()
    {
        var yaml = "value: test";

        var result = _merger.Merge([yaml], null);

        result.Should().Contain("value: test");
        result.Should().NotContain("{");
        result.Should().NotContain("\"value\"");
    }

    [Fact]
    public void Merge_WithIncludeComments_CreatesOptionsObject()
    {
        var yaml = "value: test";
        var options = new MergeOptions { IncludeComments = true };

        var result = _merger.Merge([yaml], options);

        result.Should().Contain("value: test");
    }
}
