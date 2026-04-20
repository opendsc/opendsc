// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource.Tests;

[Trait("Category", "Unit")]

public class ExitCodeAttributeTests
{
    [Fact]
    public void Constructor_WithExitCode_SetsExitCodeProperty()
    {
        var attr = new ExitCodeAttribute(42);

        attr.ExitCode.Should().Be(42);
    }

    [Fact]
    public void Description_DefaultsToEmpty()
    {
        var attr = new ExitCodeAttribute(1);

        attr.Description.Should().BeEmpty();
    }

    [Fact]
    public void Description_CanBeSet()
    {
        var attr = new ExitCodeAttribute(1)
        {
            Description = "Test description"
        };

        attr.Description.Should().Be("Test description");
    }

    [Fact]
    public void Exception_CanBeSetToExceptionType()
    {
        var attr = new ExitCodeAttribute(1)
        {
            Exception = typeof(InvalidOperationException)
        };

        attr.Exception.Should().Be(typeof(InvalidOperationException));
    }

    [Fact]
    public void Exception_CanBeSetToArgumentException()
    {
        var attr = new ExitCodeAttribute(2)
        {
            Exception = typeof(ArgumentException)
        };

        attr.Exception.Should().Be(typeof(ArgumentException));
    }

    [Fact]
    public void Exception_CanBeSetToCustomExceptionType()
    {
        var attr = new ExitCodeAttribute(3)
        {
            Exception = typeof(CustomTestException)
        };

        attr.Exception.Should().Be(typeof(CustomTestException));
    }

    [Fact]
    public void Exception_ThrowsWhenSetToNonExceptionType()
    {
        var attr = new ExitCodeAttribute(1);

        var action = () => attr.Exception = typeof(string);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Exception_StartsAsNull()
    {
        var attr = new ExitCodeAttribute(1);

        attr.Exception.Should().BeNull();
    }

    [Fact]
    public void CanBeAppliedMultipleTimesToAClass()
    {
        var attrs = typeof(MultiExitCodeTestResource).GetCustomAttributes(typeof(ExitCodeAttribute), false);

        attrs.Length.Should().Be(3);
    }
}

public class CustomTestException : Exception
{
}

[ExitCode(1, Description = "First exit code")]
[ExitCode(2, Description = "Second exit code")]
[ExitCode(3, Description = "Third exit code")]
public class MultiExitCodeTestResource
{
}
