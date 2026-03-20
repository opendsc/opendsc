// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using NuGet.Versioning;

namespace OpenDsc.Server.Services;

public static class VersionResolver
{
    /// <summary>
    /// Selects the semver-highest version from a collection that satisfies the major version
    /// and prerelease channel constraints.
    /// </summary>
    /// <typeparam name="T">The version entity type.</typeparam>
    /// <param name="versions">Collection of candidate versions.</param>
    /// <param name="getVersion">Extracts the semver string from an entity.</param>
    /// <param name="majorVersion">
    /// When set, only versions with this major version number are considered.
    /// When null, all major versions are eligible.
    /// </param>
    /// <param name="prereleaseChannel">
    /// The minimum prerelease threshold (free-text, semver-compared).
    /// When null, only stable (non-prerelease) versions are eligible.
    /// Any version whose prerelease identifier compares >= this value (or is stable) is eligible.
    /// </param>
    public static T? ResolveVersion<T>(
        IEnumerable<T> versions,
        Func<T, string> getVersion,
        int? majorVersion,
        string? prereleaseChannel) where T : class
    {
        return versions
            .Select(v => (entity: v, semver: SemanticVersion.TryParse(getVersion(v), out var sv) ? sv : null))
            .Where(x => x.semver is not null && IsEligible(x.semver, majorVersion, prereleaseChannel))
            .OrderByDescending(x => x.semver)
            .Select(x => x.entity)
            .FirstOrDefault();
    }

    /// <summary>
    /// Returns the semver-highest version string from a collection, ignoring any constraints.
    /// Used for display purposes (e.g., LatestVersion in list endpoints).
    /// </summary>
    public static string? LatestSemver(IEnumerable<string?> versions)
    {
        return versions
            .Where(v => v is not null && SemanticVersion.TryParse(v, out _))
            .Select(v => SemanticVersion.Parse(v!))
            .OrderByDescending(v => v)
            .Select(v => v.ToString())
            .FirstOrDefault();
    }

    private static bool IsEligible(SemanticVersion version, int? majorVersion, string? prereleaseChannel)
    {
        if (majorVersion.HasValue && version.Major != majorVersion.Value)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(version.Release))
        {
            if (prereleaseChannel is null)
            {
                return false;
            }

            var versionPre = new SemanticVersion(0, 0, 0, version.Release);
            var channelPre = new SemanticVersion(0, 0, 0, prereleaseChannel);
            return versionPre.CompareTo(channelPre) >= 0;
        }

        return true;
    }
}
