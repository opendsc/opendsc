// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Resource.CommandLine.IntegrationTests;

/// <summary>
/// Test schema for integration tests
/// </summary>
public class TestSchema
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

/// <summary>
/// Test resource that implements all DSC capabilities
/// </summary>
[DscResource("TestResource/All", "1.0.0")]
[ExitCode(1, Description = "Not found")]
[ExitCode(2, Description = "Invalid input")]
public class TestResourceAll : DscResource<TestSchema>,
    IGettable<TestSchema>,
    ISettable<TestSchema>,
    ITestable<TestSchema>,
    IDeletable<TestSchema>,
    IExportable<TestSchema>
{
    public TestResourceAll() : base(new TestSourceGenerationContext())
    {
    }

    public override string GetSchema()
    {
        return """
        {
            "$schema": "https://json-schema.org/draft/2020-12/schema",
            "$id": "https://example.com/test-resource.json",
            "title": "TestResource",
            "type": "object",
            "properties": {
                "name": { "type": "string" },
                "value": { "type": "string" },
                "enabled": { "type": "boolean" }
            }
        }
        """;
    }

    public SetResult<TestSchema>? Set(TestSchema? desiredState)
    {
        return new SetResult<TestSchema>(desiredState ?? new TestSchema())
        {
            ChangedProperties = new HashSet<string> { "name" }
        };
    }

    public TestResult<TestSchema> Test(TestSchema? desiredState)
    {
        return new TestResult<TestSchema>(desiredState ?? new TestSchema())
        {
            DifferingProperties = new HashSet<string>()
        };
    }

    public TestSchema Get(TestSchema? filter)
    {
        return new TestSchema { Name = "test", Value = "value", Enabled = true };
    }

    public void Delete(TestSchema? instance)
    {
        // Intentionally left empty for testing
    }

    public IEnumerable<TestSchema> Export(TestSchema? filter)
    {
        yield return new TestSchema { Name = "test", Value = "value", Enabled = true };
    }
}

/// <summary>
/// Test resource that implements only Get and Set
/// </summary>
[DscResource("TestResource/GetSet", "1.0.0")]
public class TestResourceGetSet : DscResource<TestSchema>,
    IGettable<TestSchema>,
    ISettable<TestSchema>
{
    public TestResourceGetSet() : base(new TestSourceGenerationContext())
    {
    }

    public override string GetSchema()
    {
        return """
        {
            "$schema": "https://json-schema.org/draft/2020-12/schema",
            "$id": "https://example.com/test-resource-getset.json",
            "title": "TestResourceGetSet",
            "type": "object",
            "properties": {
                "name": { "type": "string" },
                "value": { "type": "string" }
            }
        }
        """;
    }

    public SetResult<TestSchema>? Set(TestSchema? desiredState)
    {
        return new SetResult<TestSchema>(desiredState ?? new TestSchema());
    }

    public TestSchema Get(TestSchema? filter)
    {
        return new TestSchema { Name = "test" };
    }
}

/// <summary>
/// Test resource that implements set and delete what-if capabilities.
/// Tracks whether the non-what-if operations were invoked so tests can
/// assert that what-if execution does not mutate anything.
/// </summary>
[DscResource("TestResource/WhatIf", "1.0.0", SetReturn = SetReturn.StateAndDiff, WhatIfReturn = SetReturn.StateAndDiff)]
public class TestResourceWithWhatIf : DscResource<TestSchema>,
    IGettable<TestSchema>,
    ISettableWhatIf<TestSchema>,
    IDeletableWhatIf<TestSchema>
{
    public bool SetInvoked { get; private set; }

    public bool DeleteInvoked { get; private set; }

    public TestResourceWithWhatIf() : base(new TestSourceGenerationContext())
    {
    }

    public override string GetSchema()
    {
        return """
        {
            "$schema": "https://json-schema.org/draft/2020-12/schema",
            "$id": "https://example.com/test-resource-whatif.json",
            "title": "TestResourceWithWhatIf",
            "type": "object",
            "properties": {
                "name": { "type": "string" },
                "value": { "type": "string" },
                "enabled": { "type": "boolean" }
            }
        }
        """;
    }

    public TestSchema Get(TestSchema? filter)
    {
        return new TestSchema { Name = "test", Value = "value", Enabled = true };
    }

    public SetResult<TestSchema>? Set(TestSchema? desiredState)
    {
        SetInvoked = true;
        return new SetResult<TestSchema>(desiredState ?? new TestSchema())
        {
            ChangedProperties = new HashSet<string> { "name" }
        };
    }

    public SetResult<TestSchema> SetWhatIf(TestSchema? desiredState)
    {
        return new SetResult<TestSchema>(desiredState ?? new TestSchema())
        {
            ChangedProperties = new HashSet<string> { "name" }
        };
    }

    public void Delete(TestSchema? instance)
    {
        DeleteInvoked = true;
    }

    public DeleteResult DeleteWhatIf(TestSchema? instance)
    {
        return new DeleteResult
        {
            Metadata = new DeleteWhatIfMetadata
            {
                WhatIf = [$"Would delete '{instance?.Name ?? "unknown"}'"]
            }
        };
    }
}

/// <summary>
/// Test resource with no capabilities (for error testing)
/// </summary>
[DscResource("TestResource/NoOps", "1.0.0")]
public class TestResourceNoOps : DscResource<TestSchema>
{
    public TestResourceNoOps() : base(new TestSourceGenerationContext())
    {
    }
}

/// <summary>
/// Test resource without DscResourceAttribute (for error testing)
/// </summary>
public class TestResourceMissingDscResourceAttribute : DscResource<TestSchema>,
    IGettable<TestSchema>
{
    public TestResourceMissingDscResourceAttribute() : base(new TestSourceGenerationContext())
    {
    }

    public TestSchema Get(TestSchema? filter) => new TestSchema();
}

/// <summary>
/// Source-generated JSON serializer context for TestSchema
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TestSchema))]
public partial class TestSourceGenerationContext : JsonSerializerContext
{
}
