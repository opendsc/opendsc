// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Schema.Tests;

public class DscOperationResultTests
{
    [Fact]
    public void DscGetOperationResult_DefaultValues_ShouldBeValid()
    {
        var result = new DscGetOperationResult();

        result.ActualState.ValueKind.Should().Be(JsonValueKind.Undefined);
    }

    [Fact]
    public void DscGetOperationResult_WithActualState_ShouldStoreState()
    {
        var jsonElement = JsonDocument.Parse("{\"prop\": \"value\"}").RootElement;
        var result = new DscGetOperationResult { ActualState = jsonElement };

        result.ActualState.GetProperty("prop").GetString().Should().Be("value");
    }

    [Fact]
    public void DscGetOperationResult_WithComplexState_ShouldStoreComplexObject()
    {
        var jsonElement = JsonDocument.Parse("{\"nested\": {\"deep\": \"value\"}, \"array\": [1, 2, 3]}").RootElement;
        var result = new DscGetOperationResult { ActualState = jsonElement };

        result.ActualState.GetProperty("nested").GetProperty("deep").GetString().Should().Be("value");
        result.ActualState.GetProperty("array")[0].GetInt32().Should().Be(1);
    }

    [Fact]
    public void DscTestOperationResult_DefaultValues_ShouldBeValid()
    {
        var result = new DscTestOperationResult();

        result.DesiredState.ValueKind.Should().Be(JsonValueKind.Undefined);
        result.ActualState.ValueKind.Should().Be(JsonValueKind.Undefined);
        result.InDesiredState.Should().BeFalse();
        result.DifferingProperties.Should().BeNull();
    }

    [Fact]
    public void DscTestOperationResult_WithAllProperties_ShouldStoreAll()
    {
        var desiredJson = JsonDocument.Parse("{\"prop\": \"desired\"}").RootElement;
        var actualJson = JsonDocument.Parse("{\"prop\": \"actual\"}").RootElement;
        var differing = new[] { "prop1", "prop2" };

        var result = new DscTestOperationResult
        {
            DesiredState = desiredJson,
            ActualState = actualJson,
            InDesiredState = false,
            DifferingProperties = differing
        };

        result.DesiredState.GetProperty("prop").GetString().Should().Be("desired");
        result.ActualState.GetProperty("prop").GetString().Should().Be("actual");
        result.InDesiredState.Should().BeFalse();
        result.DifferingProperties.Should().Equal("prop1", "prop2");
    }

    [Fact]
    public void DscTestOperationResult_InDesiredState_ShouldStoreTrue()
    {
        var desiredJson = JsonDocument.Parse("{}").RootElement;
        var actualJson = JsonDocument.Parse("{}").RootElement;

        var result = new DscTestOperationResult
        {
            DesiredState = desiredJson,
            ActualState = actualJson,
            InDesiredState = true
        };

        result.InDesiredState.Should().BeTrue();
        result.DifferingProperties.Should().BeNull();
    }

    [Fact]
    public void DscTestOperationResult_WithEmptyDifferingProperties_ShouldStoreEmptyArray()
    {
        var desiredJson = JsonDocument.Parse("{}").RootElement;
        var actualJson = JsonDocument.Parse("{}").RootElement;

        var result = new DscTestOperationResult
        {
            DesiredState = desiredJson,
            ActualState = actualJson,
            InDesiredState = true,
            DifferingProperties = []
        };

        result.DifferingProperties.Should().BeEmpty();
    }

    [Fact]
    public void DscSetOperationResult_DefaultValues_ShouldBeValid()
    {
        var result = new DscSetOperationResult();

        result.BeforeState.ValueKind.Should().Be(JsonValueKind.Undefined);
        result.AfterState.ValueKind.Should().Be(JsonValueKind.Undefined);
        result.ChangedProperties.Should().BeNull();
    }

    [Fact]
    public void DscSetOperationResult_WithAllProperties_ShouldStoreAll()
    {
        var beforeJson = JsonDocument.Parse("{\"prop\": \"before\"}").RootElement;
        var afterJson = JsonDocument.Parse("{\"prop\": \"after\"}").RootElement;
        var changed = new[] { "prop1", "prop2" };

        var result = new DscSetOperationResult
        {
            BeforeState = beforeJson,
            AfterState = afterJson,
            ChangedProperties = changed
        };

        result.BeforeState.GetProperty("prop").GetString().Should().Be("before");
        result.AfterState.GetProperty("prop").GetString().Should().Be("after");
        result.ChangedProperties.Should().Equal("prop1", "prop2");
    }

    [Fact]
    public void DscSetOperationResult_WithNoChanges_ShouldHaveNullChangedProperties()
    {
        var beforeJson = JsonDocument.Parse("{\"prop\": \"value\"}").RootElement;
        var afterJson = JsonDocument.Parse("{\"prop\": \"value\"}").RootElement;

        var result = new DscSetOperationResult
        {
            BeforeState = beforeJson,
            AfterState = afterJson
        };

        result.BeforeState.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        result.AfterState.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        result.ChangedProperties.Should().BeNull();
    }

    [Fact]
    public void DscSetOperationResult_WithEmptyChangedProperties_ShouldStoreEmptyArray()
    {
        var beforeJson = JsonDocument.Parse("{}").RootElement;
        var afterJson = JsonDocument.Parse("{}").RootElement;

        var result = new DscSetOperationResult
        {
            BeforeState = beforeJson,
            AfterState = afterJson,
            ChangedProperties = []
        };

        result.ChangedProperties.Should().BeEmpty();
    }

    [Fact]
    public void DscSetOperationResult_WithComplexStates_ShouldStoreComplexObjects()
    {
        var beforeJson = JsonDocument.Parse("{\"config\": {\"nested\": {\"deep\": \"value1\"}}, \"array\": [1, 2]}").RootElement;
        var afterJson = JsonDocument.Parse("{\"config\": {\"nested\": {\"deep\": \"value2\"}}, \"array\": [1, 2, 3]}").RootElement;

        var result = new DscSetOperationResult
        {
            BeforeState = beforeJson,
            AfterState = afterJson
        };

        result.BeforeState.GetProperty("config").GetProperty("nested").GetProperty("deep").GetString().Should().Be("value1");
        result.AfterState.GetProperty("config").GetProperty("nested").GetProperty("deep").GetString().Should().Be("value2");
        result.BeforeState.GetProperty("array")[0].GetInt32().Should().Be(1);
        result.AfterState.GetProperty("array")[2].GetInt32().Should().Be(3);
    }
}
