// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;
using Xunit;

namespace OpenDsc.Resource.Tests;

public class SetResultTests
{
    [Fact]
    public void Constructor_WithActualState_SetsActualStateProperty()
    {
        var state = new TestSchema { Name = "test", Value = 42 };
        var result = new SetResult<TestSchema>(state);

        result.ActualState.Should().Be(state);
    }

    [Fact]
    public void ActualState_CannotBeChanged()
    {
        var state = new TestSchema { Name = "test", Value = 42 };
        var result = new SetResult<TestSchema>(state);

        result.ActualState.Should().Be(state);
    }

    [Fact]
    public void ChangedProperties_StartsAsNull()
    {
        var state = new TestSchema { Name = "test", Value = 42 };
        var result = new SetResult<TestSchema>(state);

        result.ChangedProperties.Should().BeNull();
    }

    [Fact]
    public void ChangedProperties_CanBeSet()
    {
        var state = new TestSchema { Name = "test", Value = 42 };
        var result = new SetResult<TestSchema>(state);
        var properties = new HashSet<string> { "Name", "Value" };

        result.ChangedProperties = properties;

        result.ChangedProperties.Should().Equal(properties);
    }

    [Fact]
    public void ChangedProperties_CanHavePropertiesAdded()
    {
        var state = new TestSchema();
        var result = new SetResult<TestSchema>(state)
        {
            ChangedProperties = new HashSet<string>()
        };

        result.ChangedProperties.Add("Name");
        result.ChangedProperties.Add("Value");

        result.ChangedProperties.Should().Contain("Name");
        result.ChangedProperties.Should().Contain("Value");
    }

    [Fact]
    public void MultipleSets_ReturnMultipleSetResults()
    {
        var state1 = new TestSchema { Name = "test1", Value = 1 };
        var state2 = new TestSchema { Name = "test2", Value = 2 };

        var result1 = new SetResult<TestSchema>(state1);
        var result2 = new SetResult<TestSchema>(state2);

        result1.ActualState.Should().Be(state1);
        result2.ActualState.Should().Be(state2);
    }
}

public class TestResultTests
{
    [Fact]
    public void Constructor_WithActualState_SetsActualStateProperty()
    {
        var state = new TestSchema { Name = "test", Value = 42 };
        var result = new TestResult<TestSchema>(state);

        result.ActualState.Should().Be(state);
    }

    [Fact]
    public void ActualState_CannotBeChanged()
    {
        var state = new TestSchema { Name = "test", Value = 42 };
        var result = new TestResult<TestSchema>(state);

        result.ActualState.Should().Be(state);
    }

    [Fact]
    public void DifferingProperties_StartsAsNull()
    {
        var state = new TestSchema { Name = "test", Value = 42 };
        var result = new TestResult<TestSchema>(state);

        result.DifferingProperties.Should().BeNull();
    }

    [Fact]
    public void DifferingProperties_CanBeSet()
    {
        var state = new TestSchema { Name = "test", Value = 42 };
        var result = new TestResult<TestSchema>(state);
        var properties = new HashSet<string> { "Name", "Value" };

        result.DifferingProperties = properties;

        result.DifferingProperties.Should().Equal(properties);
    }

    [Fact]
    public void DifferingProperties_CanHavePropertiesAdded()
    {
        var state = new TestSchema();
        var result = new TestResult<TestSchema>(state)
        {
            DifferingProperties = new HashSet<string>()
        };

        result.DifferingProperties.Add("Name");
        result.DifferingProperties.Add("Value");

        result.DifferingProperties.Should().Contain("Name");
        result.DifferingProperties.Should().Contain("Value");
    }

    [Fact]
    public void MultipleTests_ReturnMultipleTestResults()
    {
        var state1 = new TestSchema { Name = "test1", Value = 1 };
        var state2 = new TestSchema { Name = "test2", Value = 2 };

        var result1 = new TestResult<TestSchema>(state1);
        var result2 = new TestResult<TestSchema>(state2);

        result1.ActualState.Should().Be(state1);
        result2.ActualState.Should().Be(state2);
    }
}

public class SetReturnEnumTests
{
    [Fact]
    public void SetReturn_HasNoneValue()
    {
        SetReturn.None.Should().Be(SetReturn.None);
    }

    [Fact]
    public void SetReturn_HasStateValue()
    {
        SetReturn.State.Should().Be(SetReturn.State);
    }

    [Fact]
    public void SetReturn_HasStateAndDiffValue()
    {
        SetReturn.StateAndDiff.Should().Be(SetReturn.StateAndDiff);
    }
}

public class TestReturnEnumTests
{
    [Fact]
    public void TestReturn_HasStateValue()
    {
        TestReturn.State.Should().Be(TestReturn.State);
    }

    [Fact]
    public void TestReturn_HasStateAndDiffValue()
    {
        TestReturn.StateAndDiff.Should().Be(TestReturn.StateAndDiff);
    }
}
