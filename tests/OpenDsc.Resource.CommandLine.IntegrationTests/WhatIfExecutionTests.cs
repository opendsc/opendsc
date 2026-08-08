// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Xunit;

namespace OpenDsc.Resource.CommandLine.IntegrationTests;

/// <summary>
/// End-to-end tests for what-if execution through the System.CommandLine pipeline.
/// Only success paths are invoked in-process; failure paths call Environment.Exit
/// and are covered by manifest/structure tests instead.
/// </summary>
[Trait("Category", "Integration")]
public class WhatIfExecutionTests
{
    private static string CaptureConsoleOut(Action action)
    {
        var original = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(original);
        }

        return writer.ToString();
    }

    [Fact]
    public void SetWhatIf_PrintsProjectedStateAndDiff()
    {
        // Arrange
        var resource = new TestResourceWithWhatIf();
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(resource);
        var root = builder.Build();

        // Act
        var output = CaptureConsoleOut(() =>
            root.Parse(["set", "--input", """{"name":"demo"}""", "--what-if"]).Invoke());

        // Assert
        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("\"name\":\"demo\"", lines[0]);
        Assert.Contains("name", lines[1]);
    }

    [Fact]
    public void SetWhatIf_DoesNotInvokeSet()
    {
        // Arrange
        var resource = new TestResourceWithWhatIf();
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(resource);
        var root = builder.Build();

        // Act
        CaptureConsoleOut(() =>
            root.Parse(["set", "--input", """{"name":"demo"}""", "--what-if"]).Invoke());

        // Assert
        Assert.False(resource.SetInvoked);
    }

    [Fact]
    public void Set_WithoutWhatIf_InvokesSet()
    {
        // Arrange
        var resource = new TestResourceWithWhatIf();
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(resource);
        var root = builder.Build();

        // Act
        CaptureConsoleOut(() =>
            root.Parse(["set", "--input", """{"name":"demo"}"""]).Invoke());

        // Assert
        Assert.True(resource.SetInvoked);
    }

    [Fact]
    public void DeleteWhatIf_PrintsDeleteResult_AndDoesNotInvokeDelete()
    {
        // Arrange
        var resource = new TestResourceWithWhatIf();
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(resource);
        var root = builder.Build();

        // Act
        var output = CaptureConsoleOut(() =>
            root.Parse(["delete", "--input", """{"name":"demo"}""", "--what-if"]).Invoke());

        // Assert
        Assert.Contains("\"_metadata\"", output);
        Assert.Contains("\"whatIf\"", output);
        Assert.Contains("Would delete \\u0027demo\\u0027", output);
        Assert.False(resource.DeleteInvoked);
    }

    [Fact]
    public void Delete_WithoutWhatIf_InvokesDelete()
    {
        // Arrange
        var resource = new TestResourceWithWhatIf();
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(resource);
        var root = builder.Build();

        // Act
        CaptureConsoleOut(() =>
            root.Parse(["delete", "--input", """{"name":"demo"}"""]).Invoke());

        // Assert
        Assert.True(resource.DeleteInvoked);
    }

    [Fact]
    public void SetWhatIf_MultiResourceExe_ExecutesCorrectResource()
    {
        // Arrange
        var whatIfResource = new TestResourceWithWhatIf();
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(whatIfResource);
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        var root = builder.Build();

        // Act
        var output = CaptureConsoleOut(() =>
            root.Parse(["set", "--resource", "TestResource/WhatIf", "--input", """{"name":"demo"}""", "--what-if"]).Invoke());

        // Assert
        Assert.Contains("\"name\":\"demo\"", output);
        Assert.False(whatIfResource.SetInvoked);
    }

    [Fact]
    public void Manifest_WhatIfResource_EmitsWhatIfArgAndReturns()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(new TestResourceWithWhatIf());
        var root = builder.Build();

        // Act
        var output = CaptureConsoleOut(() => root.Parse(["manifest"]).Invoke());

        // Assert
        Assert.Contains("\"whatIfArg\":\"--what-if\"", output);
        Assert.Contains("\"whatIfReturns\":\"stateAndDiff\"", output);
    }

    [Fact]
    public void Manifest_NonWhatIfResource_OmitsWhatIfArtifacts()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        var root = builder.Build();

        // Act
        var output = CaptureConsoleOut(() => root.Parse(["manifest"]).Invoke());

        // Assert
        Assert.DoesNotContain("whatIfArg", output);
        Assert.DoesNotContain("whatIfReturns", output);
    }
}
