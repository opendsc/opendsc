// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Schema.Tests;

public class DscEnumTests
{
    [Fact]
    public void DscOperation_Values_ShouldBeCorrect()
    {
        // Verify all enum values exist and have expected values
        var get = DscOperation.Get;
        var set = DscOperation.Set;
        var test = DscOperation.Test;
        var export = DscOperation.Export;

        get.Should().Be(DscOperation.Get);
        set.Should().Be(DscOperation.Set);
        test.Should().Be(DscOperation.Test);
        export.Should().Be(DscOperation.Export);
    }

    [Fact]
    public void DscOperation_JsonSerialization_ShouldWorkCorrectly()
    {
        var json = "\"Get\"";
        var deserialized = JsonSerializer.Deserialize<DscOperation>(json);

        deserialized.Should().Be(DscOperation.Get);
    }

    [Theory]
    [InlineData(DscOperation.Get, "Get")]
    [InlineData(DscOperation.Set, "Set")]
    [InlineData(DscOperation.Test, "Test")]
    [InlineData(DscOperation.Export, "Export")]
    public void DscOperation_JsonSerialization_AllValues_ShouldRoundTrip(DscOperation operation, string _)
    {
        var serialized = JsonSerializer.Serialize(operation);
        var deserialized = JsonSerializer.Deserialize<DscOperation>(serialized);

        deserialized.Should().Be(operation);
    }

    [Fact]
    public void DscExecutionKind_Values_ShouldBeCorrect()
    {
        var actual = DscExecutionKind.Actual;
        var whatIf = DscExecutionKind.WhatIf;

        actual.Should().Be(DscExecutionKind.Actual);
        whatIf.Should().Be(DscExecutionKind.WhatIf);
    }

    [Theory]
    [InlineData(DscExecutionKind.Actual, "Actual")]
    [InlineData(DscExecutionKind.WhatIf, "WhatIf")]
    public void DscExecutionKind_JsonSerialization_AllValues_ShouldRoundTrip(DscExecutionKind kind, string _)
    {
        var serialized = JsonSerializer.Serialize(kind);
        var deserialized = JsonSerializer.Deserialize<DscExecutionKind>(serialized);

        deserialized.Should().Be(kind);
    }

    [Fact]
    public void DscSecurityContext_Values_ShouldBeCorrect()
    {
        var current = DscSecurityContext.Current;
        var elevated = DscSecurityContext.Elevated;
        var restricted = DscSecurityContext.Restricted;

        current.Should().Be(DscSecurityContext.Current);
        elevated.Should().Be(DscSecurityContext.Elevated);
        restricted.Should().Be(DscSecurityContext.Restricted);
    }

    [Theory]
    [InlineData(DscSecurityContext.Current, "Current")]
    [InlineData(DscSecurityContext.Elevated, "Elevated")]
    [InlineData(DscSecurityContext.Restricted, "Restricted")]
    public void DscSecurityContext_JsonSerialization_AllValues_ShouldRoundTrip(DscSecurityContext context, string _)
    {
        var serialized = JsonSerializer.Serialize(context);
        var deserialized = JsonSerializer.Deserialize<DscSecurityContext>(serialized);

        deserialized.Should().Be(context);
    }

    [Fact]
    public void DscMessageLevel_Values_ShouldBeCorrect()
    {
        var error = DscMessageLevel.Error;
        var warning = DscMessageLevel.Warning;
        var information = DscMessageLevel.Information;

        error.Should().Be(DscMessageLevel.Error);
        warning.Should().Be(DscMessageLevel.Warning);
        information.Should().Be(DscMessageLevel.Information);
    }

    [Theory]
    [InlineData(DscMessageLevel.Error, "Error")]
    [InlineData(DscMessageLevel.Warning, "Warning")]
    [InlineData(DscMessageLevel.Information, "Information")]
    public void DscMessageLevel_JsonSerialization_AllValues_ShouldRoundTrip(DscMessageLevel level, string _)
    {
        var serialized = JsonSerializer.Serialize(level);
        var deserialized = JsonSerializer.Deserialize<DscMessageLevel>(serialized);

        deserialized.Should().Be(level);
    }

    [Fact]
    public void DscTraceLevel_Values_ShouldBeCorrect()
    {
        var error = DscTraceLevel.Error;
        var warn = DscTraceLevel.Warn;
        var info = DscTraceLevel.Info;
        var debug = DscTraceLevel.Debug;
        var trace = DscTraceLevel.Trace;

        error.Should().Be(DscTraceLevel.Error);
        warn.Should().Be(DscTraceLevel.Warn);
        info.Should().Be(DscTraceLevel.Info);
        debug.Should().Be(DscTraceLevel.Debug);
        trace.Should().Be(DscTraceLevel.Trace);
    }

    [Theory]
    [InlineData(DscTraceLevel.Error, "Error")]
    [InlineData(DscTraceLevel.Warn, "Warn")]
    [InlineData(DscTraceLevel.Info, "Info")]
    [InlineData(DscTraceLevel.Debug, "Debug")]
    [InlineData(DscTraceLevel.Trace, "Trace")]
    public void DscTraceLevel_JsonSerialization_AllValues_ShouldRoundTrip(DscTraceLevel level, string _)
    {
        var serialized = JsonSerializer.Serialize(level);
        var deserialized = JsonSerializer.Deserialize<DscTraceLevel>(serialized);

        deserialized.Should().Be(level);
    }

    [Fact]
    public void DscScope_Values_ShouldBeCorrect()
    {
        var user = DscScope.User;
        var machine = DscScope.Machine;

        user.Should().Be(DscScope.User);
        machine.Should().Be(DscScope.Machine);
    }

    [Fact]
    public void DscExitCode_Values_ShouldBeCorrect()
    {
        DscExitCode.Success.Should().Be((DscExitCode)0);
        DscExitCode.InvalidArgument.Should().Be((DscExitCode)1);
        DscExitCode.ResourceError.Should().Be((DscExitCode)2);
        DscExitCode.JsonSerializationError.Should().Be((DscExitCode)3);
        DscExitCode.InvalidInput.Should().Be((DscExitCode)4);
        DscExitCode.SchemaValidationError.Should().Be((DscExitCode)5);
        DscExitCode.Cancelled.Should().Be((DscExitCode)6);
    }

    [Theory]
    [InlineData(DscExitCode.Success, 0)]
    [InlineData(DscExitCode.InvalidArgument, 1)]
    [InlineData(DscExitCode.ResourceError, 2)]
    [InlineData(DscExitCode.JsonSerializationError, 3)]
    [InlineData(DscExitCode.InvalidInput, 4)]
    [InlineData(DscExitCode.SchemaValidationError, 5)]
    [InlineData(DscExitCode.Cancelled, 6)]
    public void DscExitCode_Values_ShouldMatchExpectedIntegers(DscExitCode code, int expectedValue)
    {
        ((int)code).Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(DscExitCode.Success, "Success")]
    [InlineData(DscExitCode.InvalidArgument, "InvalidArgument")]
    [InlineData(DscExitCode.ResourceError, "ResourceError")]
    [InlineData(DscExitCode.JsonSerializationError, "JsonSerializationError")]
    [InlineData(DscExitCode.InvalidInput, "InvalidInput")]
    [InlineData(DscExitCode.SchemaValidationError, "SchemaValidationError")]
    [InlineData(DscExitCode.Cancelled, "Cancelled")]
    public void DscExitCode_JsonSerialization_AllValues_ShouldRoundTrip(DscExitCode code, string _)
    {
        var serialized = JsonSerializer.Serialize(code);
        var deserialized = JsonSerializer.Deserialize<DscExitCode>(serialized);

        deserialized.Should().Be(code);
    }
}
