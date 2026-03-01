// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using NuGet.Versioning;

using Xunit;

namespace OpenDsc.Schema.Tests;

public class DscResultTests
{
    [Fact]
    public void DscResult_DefaultValues_ShouldBeCorrect()
    {
        var result = new DscResult();

        result.Results.Should().BeNull();
        result.Messages.Should().BeNull();
        result.HadErrors.Should().BeFalse();
        result.Metadata.Should().BeNull();
    }

    [Fact]
    public void DscResult_WithResults_ShouldStoreResults()
    {
        var resourceResults = new List<DscResourceResult>
        {
            new() { Type = "Test/Resource", Name = "Instance1" }
        };

        var result = new DscResult { Results = resourceResults };

        result.Results.Should().HaveCount(1);
        result.Results![0].Type.Should().Be("Test/Resource");
        result.Results![0].Name.Should().Be("Instance1");
    }

    [Fact]
    public void DscResult_WithMessages_ShouldStoreMessages()
    {
        var messages = new List<DscMessage>
        {
            new() { Level = DscMessageLevel.Warning, Message = "Test warning" }
        };

        var result = new DscResult { Messages = messages };

        result.Messages.Should().HaveCount(1);
        result.Messages![0].Level.Should().Be(DscMessageLevel.Warning);
        result.Messages![0].Message.Should().Be("Test warning");
    }

    [Fact]
    public void DscResult_WithErrors_ShouldSetHadErrorsTrue()
    {
        var result = new DscResult { HadErrors = true };

        result.HadErrors.Should().BeTrue();
    }

    [Fact]
    public void DscResult_WithMetadata_ShouldStoreMetadata()
    {
        var version = SemanticVersion.Parse("3.0.0");
        var metadata = new DscMetadata
        {
            MicrosoftDsc = new MicrosoftDscMetadata
            {
                Version = version
            }
        };

        var result = new DscResult { Metadata = metadata };

        result.Metadata.Should().NotBeNull();
        result.Metadata!.MicrosoftDsc.Should().NotBeNull();
        result.Metadata.MicrosoftDsc!.Version.Should().Be(version);
    }
}
