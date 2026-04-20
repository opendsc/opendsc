// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Resource.Tests;

[Trait("Category", "Unit")]
public class DscJsonSchemaExporterOptionsTests
{
    [Fact]
    public void Default_ReturnsValidJsonSchemaExporterOptions()
    {
        var options = DscJsonSchemaExporterOptions.Default;

        options.Should().NotBeNull();
    }

    [Fact]
    public void Default_HasTreatNullObliviousAsNonNullableTrue()
    {
        var options = DscJsonSchemaExporterOptions.Default;

        options.TreatNullObliviousAsNonNullable.Should().BeTrue();
    }

    [Fact]
    public void Default_CanBeUsedMultipleTimes()
    {
        var options1 = DscJsonSchemaExporterOptions.Default;
        var options2 = DscJsonSchemaExporterOptions.Default;

        options1.Should().NotBeNull();
        options2.Should().NotBeNull();
    }
}
