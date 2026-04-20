// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Resource.Tests;

[Trait("Category", "Unit")]
public class ExitCodeResolverTests
{
    private readonly TestResource _resource = new(SourceGenerationContext.Default);

    [Fact]
    public void GetExitCode_WithExactExceptionMatch_ReturnsMatchingCode()
    {
        var exceptionType = typeof(InvalidOperationException);

        var exitCode = ExitCodeResolver.GetExitCode(_resource, exceptionType);

        exitCode.Should().Be(3);
    }

    [Fact]
    public void GetExitCode_WithArgumentException_ReturnsMatchingCode()
    {
        var exceptionType = typeof(ArgumentException);

        var exitCode = ExitCodeResolver.GetExitCode(_resource, exceptionType);

        exitCode.Should().Be(2);
    }

    [Fact]
    public void GetExitCode_WithUnmappedException_ThrowsInvalidOperationException()
    {
        var exceptionType = typeof(DivideByZeroException);

        var action = () => ExitCodeResolver.GetExitCode(_resource, exceptionType);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetExitCode_WithDerivedExceptionType_ReturnsClosestBaseMatch()
    {
        var exceptionType = typeof(CustomException);

        var exitCode = ExitCodeResolver.GetExitCode(_resource, exceptionType);

        exitCode.Should().Be(3);
    }
}

/// <summary>
/// Custom exception for testing derived exception handling.
/// </summary>
public class CustomException : InvalidOperationException
{
}
