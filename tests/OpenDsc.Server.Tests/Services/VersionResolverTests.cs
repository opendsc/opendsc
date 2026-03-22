// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class VersionResolverTests
{
    private static List<string> Versions(params string[] versions) => [.. versions];

    [Fact]
    public void ResolveVersion_WithNoConstraints_ReturnsHighestSemver()
    {
        var versions = Versions("1.0.0", "2.0.0", "1.5.0");

        var result = VersionResolver.ResolveVersion(versions, v => v, null, null);

        result.Should().Be("2.0.0");
    }

    [Fact]
    public void ResolveVersion_WithTimestampMismatch_UsesSemverNotInsertionOrder()
    {
        // Simulates publishing 1.0.1 after 2.0.0 (the timestamp bug)
        var versions = Versions("2.0.0", "1.0.1");

        var result = VersionResolver.ResolveVersion(versions, v => v, null, null);

        result.Should().Be("2.0.0");
    }

    [Fact]
    public void ResolveVersion_WithMajorVersionConstraint_ReturnsHighestInThatMajor()
    {
        var versions = Versions("1.0.0", "1.2.0", "2.0.0", "1.1.5");

        var result = VersionResolver.ResolveVersion(versions, v => v, majorVersion: 1, null);

        result.Should().Be("1.2.0");
    }

    [Fact]
    public void ResolveVersion_WithMajorVersionConstraint_DoesNotCrossToNextMajor()
    {
        var versions = Versions("2.0.0", "2.1.0");

        var result = VersionResolver.ResolveVersion(versions, v => v, majorVersion: 1, null);

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveVersion_WithNullChannel_ExcludesPrerelease()
    {
        var versions = Versions("1.0.0", "1.1.0-beta.1", "1.1.0-rc.1");

        var result = VersionResolver.ResolveVersion(versions, v => v, null, prereleaseChannel: null);

        result.Should().Be("1.0.0");
    }

    [Fact]
    public void ResolveVersion_WithRcChannel_IncludesRcButNotBeta()
    {
        var versions = Versions("1.0.0", "1.1.0-beta.1", "1.1.0-rc.1");

        var result = VersionResolver.ResolveVersion(versions, v => v, null, prereleaseChannel: "rc");

        result.Should().Be("1.1.0-rc.1");
    }

    [Fact]
    public void ResolveVersion_WithBetaChannel_IncludesBetaAndRc()
    {
        var versions = Versions("1.0.0", "1.1.0-alpha.1", "1.1.0-beta.1", "1.1.0-rc.1");

        var result = VersionResolver.ResolveVersion(versions, v => v, null, prereleaseChannel: "beta");

        result.Should().Be("1.1.0-rc.1");
    }

    [Fact]
    public void ResolveVersion_WithAlphaChannel_IncludesAllPrerelease()
    {
        var versions = Versions("1.0.0", "1.1.0-alpha.1", "1.1.0-beta.1", "1.1.0-rc.1");

        var result = VersionResolver.ResolveVersion(versions, v => v, null, prereleaseChannel: "alpha");

        result.Should().Be("1.1.0-rc.1");
    }

    [Fact]
    public void ResolveVersion_StableVersionAlwaysEligibleRegardlessOfChannel()
    {
        var versions = Versions("1.0.0", "1.1.0-rc.1");

        var stableResult = VersionResolver.ResolveVersion(versions, v => v, null, prereleaseChannel: null);
        var rcResult = VersionResolver.ResolveVersion(versions, v => v, null, prereleaseChannel: "rc");

        stableResult.Should().Be("1.0.0");
        rcResult.Should().Be("1.1.0-rc.1");
    }

    [Fact]
    public void ResolveVersion_WithVersionPrereleaseLowerThanChannel_ExcludesVersion()
    {
        var versions = Versions("1.1.0-rc.1");

        // "rc" < "zz" alphabetically, so rc.1 does not meet the >= "zz" threshold
        var result = VersionResolver.ResolveVersion(versions, v => v, null, prereleaseChannel: "zz");

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveVersion_WithMajorAndChannel_ComposesCorrectly()
    {
        var versions = Versions("1.0.0", "1.1.0-rc.1", "2.0.0", "2.1.0-rc.1");

        var result = VersionResolver.ResolveVersion(versions, v => v, majorVersion: 1, prereleaseChannel: "rc");

        result.Should().Be("1.1.0-rc.1");
    }

    [Fact]
    public void ResolveVersion_WithEmptyCollection_ReturnsNull()
    {
        var result = VersionResolver.ResolveVersion(Versions(), v => v, null, null);

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveVersion_WithInvalidSemverEntries_SkipsThem()
    {
        var versions = Versions("not-a-version", "1.0.0", "also-bad");

        var result = VersionResolver.ResolveVersion(versions, v => v, null, null);

        result.Should().Be("1.0.0");
    }

    [Fact]
    public void ResolveVersion_ProjectsViaSelector()
    {
        var versions = new[] { new { Ver = "1.0.0" }, new { Ver = "2.0.0" } }.ToList();

        var result = VersionResolver.ResolveVersion(versions, v => v.Ver, null, null);

        result!.Ver.Should().Be("2.0.0");
    }

    [Fact]
    public void LatestSemver_ReturnsHighestSemver()
    {
        var versions = new List<string?> { "1.0.0", "2.0.0", "1.5.0", null };

        var result = VersionResolver.LatestSemver(versions);

        result.Should().Be("2.0.0");
    }

    [Fact]
    public void LatestSemver_IncludesPrereleaseInOrdering()
    {
        var versions = new List<string?> { "1.0.0", "2.0.0-rc.1" };

        var result = VersionResolver.LatestSemver(versions);

        result.Should().Be("2.0.0-rc.1");
    }

    [Fact]
    public void LatestSemver_WithAllInvalidEntries_ReturnsNull()
    {
        var versions = new List<string?> { null, "invalid", "" };

        var result = VersionResolver.LatestSemver(versions);

        result.Should().BeNull();
    }
}
