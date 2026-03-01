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
}
