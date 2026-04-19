// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using NuGet.Versioning;

using Xunit;

namespace OpenDsc.Schema.Tests;

public class DscMetadataTests
{
    [Fact]
    public void DscMetadata_DefaultValues_ShouldBeCorrect()
    {
        var metadata = new DscMetadata();

        metadata.MicrosoftDsc.Should().BeNull();
    }

    [Fact]
    public void DscMetadata_DefaultValues_ShouldBeNull()
    {
        var metadata = new DscMetadata();

        metadata.MicrosoftDsc.Should().BeNull();
        metadata.AdditionalProperties.Should().BeNull();
    }

    [Fact]
    public void DscMetadata_WithMicrosoftDsc_ShouldStoreValue()
    {
        var metadata = new DscMetadata();
        var msDsc = new MicrosoftDscMetadata { Operation = DscOperation.Get };

        metadata.MicrosoftDsc = msDsc;

        metadata.MicrosoftDsc.Should().BeSameAs(msDsc);
        metadata.MicrosoftDsc!.Operation.Should().Be(DscOperation.Get);
    }

    [Fact]
    public void DscMetadata_WithAdditionalProperties_ShouldStoreValues()
    {
        var metadata = new DscMetadata
        {
            AdditionalProperties = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 }
            }
        };

        metadata.AdditionalProperties.Should().HaveCount(2);
        metadata.AdditionalProperties!["key1"].Should().Be("value1");
        metadata.AdditionalProperties!["key2"].Should().Be(42);
    }

    [Fact]
    public void DscMetadata_WithAllProperties_ShouldStoreAll()
    {
        var msDsc = new MicrosoftDscMetadata { Operation = DscOperation.Test };
        var additionalProps = new Dictionary<string, object> { { "custom", "data" } };

        var metadata = new DscMetadata
        {
            MicrosoftDsc = msDsc,
            AdditionalProperties = additionalProps
        };

        metadata.MicrosoftDsc.Should().BeSameAs(msDsc);
        metadata.AdditionalProperties.Should().BeSameAs(additionalProps);
    }
}

public class MicrosoftDscMetadataTests
{
    [Fact]
    public void MicrosoftDscMetadata_DefaultValues_ShouldBeNull()
    {
        var metadata = new MicrosoftDscMetadata();

        metadata.Version.Should().BeNull();
        metadata.Operation.Should().BeNull();
        metadata.ExecutionType.Should().BeNull();
        metadata.StartDatetime.Should().BeNull();
        metadata.EndDatetime.Should().BeNull();
        metadata.Duration.Should().BeNull();
        metadata.SecurityContext.Should().BeNull();
        metadata.RestartRequired.Should().BeNull();
    }

    [Fact]
    public void MicrosoftDscMetadata_WithVersion_ShouldStoreVersion()
    {
        var metadata = new MicrosoftDscMetadata();
        var version = SemanticVersion.Parse("3.0.0");

        metadata.Version = version;

        metadata.Version.Should().Be(version);
    }

    [Fact]
    public void MicrosoftDscMetadata_WithOperation_ShouldStoreOperation()
    {
        var metadata = new MicrosoftDscMetadata();

        metadata.Operation = DscOperation.Set;

        metadata.Operation.Should().Be(DscOperation.Set);
    }

    [Theory]
    [InlineData(DscOperation.Get)]
    [InlineData(DscOperation.Set)]
    [InlineData(DscOperation.Test)]
    [InlineData(DscOperation.Export)]
    public void MicrosoftDscMetadata_WithAllOperations_ShouldStoreCorrectly(DscOperation operation)
    {
        var metadata = new MicrosoftDscMetadata { Operation = operation };

        metadata.Operation.Should().Be(operation);
    }

    [Fact]
    public void MicrosoftDscMetadata_WithExecutionType_ShouldStoreExecutionType()
    {
        var metadata = new MicrosoftDscMetadata();

        metadata.ExecutionType = DscExecutionKind.WhatIf;

        metadata.ExecutionType.Should().Be(DscExecutionKind.WhatIf);
    }

    [Fact]
    public void MicrosoftDscMetadata_WithStartDatetime_ShouldStoreDatetime()
    {
        var metadata = new MicrosoftDscMetadata();
        var startTime = DateTimeOffset.UtcNow;

        metadata.StartDatetime = startTime;

        metadata.StartDatetime.Should().Be(startTime);
    }

    [Fact]
    public void MicrosoftDscMetadata_WithEndDatetime_ShouldStoreDatetime()
    {
        var metadata = new MicrosoftDscMetadata();
        var endTime = DateTimeOffset.UtcNow;

        metadata.EndDatetime = endTime;

        metadata.EndDatetime.Should().Be(endTime);
    }

    [Fact]
    public void MicrosoftDscMetadata_WithDuration_ShouldStoreDuration()
    {
        var metadata = new MicrosoftDscMetadata();
        var duration = TimeSpan.FromSeconds(30);

        metadata.Duration = duration;

        metadata.Duration.Should().Be(duration);
    }

    [Theory]
    [InlineData(DscSecurityContext.Current)]
    [InlineData(DscSecurityContext.Elevated)]
    [InlineData(DscSecurityContext.Restricted)]
    public void MicrosoftDscMetadata_WithSecurityContext_ShouldStoreCorrectly(DscSecurityContext context)
    {
        var metadata = new MicrosoftDscMetadata { SecurityContext = context };

        metadata.SecurityContext.Should().Be(context);
    }

    [Fact]
    public void MicrosoftDscMetadata_WithRestartRequired_ShouldStoreRequirements()
    {
        var restartReq = new DscRestartRequirement { System = "SERVER01" };
        var version = SemanticVersion.Parse("3.0.0");
        var metadata = new MicrosoftDscMetadata
        {
            Version = version,
            Operation = DscOperation.Set,
            ExecutionType = DscExecutionKind.Actual,
            RestartRequired = [restartReq]
        };

        metadata.Version.Should().Be(version);
        metadata.Operation.Should().Be(DscOperation.Set);
        metadata.ExecutionType.Should().Be(DscExecutionKind.Actual);
        metadata.RestartRequired.Should().HaveCount(1);
        metadata.RestartRequired![0].System.Should().Be("SERVER01");
    }

    [Fact]
    public void MicrosoftDscMetadata_WithEmptyRestartRequired_ShouldStoreEmptyList()
    {
        var metadata = new MicrosoftDscMetadata { RestartRequired = new List<DscRestartRequirement>() };

        metadata.RestartRequired.Should().BeEmpty();
    }

    [Fact]
    public void MicrosoftDscMetadata_WithAllProperties_ShouldStoreAll()
    {
        var version = SemanticVersion.Parse("3.0.0");
        var startTime = DateTimeOffset.UtcNow;
        var endTime = startTime.AddSeconds(30);
        var restartList = new List<DscRestartRequirement> { new DscRestartRequirement { System = "TEST" } };

        var metadata = new MicrosoftDscMetadata
        {
            Version = version,
            Operation = DscOperation.Get,
            ExecutionType = DscExecutionKind.Actual,
            StartDatetime = startTime,
            EndDatetime = endTime,
            Duration = TimeSpan.FromSeconds(30),
            SecurityContext = DscSecurityContext.Elevated,
            RestartRequired = restartList
        };

        metadata.Version.Should().Be(version);
        metadata.Operation.Should().Be(DscOperation.Get);
        metadata.ExecutionType.Should().Be(DscExecutionKind.Actual);
        metadata.StartDatetime.Should().Be(startTime);
        metadata.EndDatetime.Should().Be(endTime);
        metadata.Duration.Should().Be(TimeSpan.FromSeconds(30));
        metadata.SecurityContext.Should().Be(DscSecurityContext.Elevated);
        metadata.RestartRequired.Should().BeSameAs(restartList);
    }
}

public class DscProcessRestartInfoTests
{
    [Fact]
    public void DscProcessRestartInfo_DefaultValues_ShouldBeNull()
    {
        var info = new DscProcessRestartInfo();

        info.Name.Should().BeNull();
        info.Id.Should().BeNull();
    }

    [Fact]
    public void DscProcessRestartInfo_WithName_ShouldStoreName()
    {
        var info = new DscProcessRestartInfo { Name = "explorer.exe" };

        info.Name.Should().Be("explorer.exe");
    }

    [Fact]
    public void DscProcessRestartInfo_WithId_ShouldStoreId()
    {
        var info = new DscProcessRestartInfo { Id = 1234 };

        info.Id.Should().Be(1234u);
    }

    [Fact]
    public void DscProcessRestartInfo_WithAllProperties_ShouldStoreAll()
    {
        var info = new DscProcessRestartInfo { Name = "cmd.exe", Id = 5678 };

        info.Name.Should().Be("cmd.exe");
        info.Id.Should().Be(5678u);
    }
}
