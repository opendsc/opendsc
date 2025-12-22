// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Runtime.InteropServices;

namespace OpenDsc.Resource.Windows.OptionalFeature;

internal static class DismHelper
{
    private const int ERROR_NOT_FOUND = unchecked((int)0x80070490);
    private const int CBS_E_NOT_FOUND = unchecked((int)0x800F080C);

    public static (Schema schema, DismRestartType restartType) GetFeature(string featureName, bool? includeAllSubFeatures = null, string[]? source = null)
    {
        IntPtr session = IntPtr.Zero;
        IntPtr featureInfoPtr = IntPtr.Zero;

        try
        {
            DismApi.Initialize();
            session = DismApi.OpenOnlineSession();

            var hr = DismApi.DismGetFeatureInfo(
                session,
                featureName,
                null,
                DismPackageIdentifier.None,
                out featureInfoPtr);

            if (hr == ERROR_NOT_FOUND || hr == CBS_E_NOT_FOUND)
            {
                return (new Schema
                {
                    Name = featureName,
                    Exist = false
                }, DismRestartType.No);
            }

            if (hr != 0)
            {
                var errorMessage = DismApi.GetLastErrorMessage();
                throw new InvalidOperationException(
                    $"Failed to get feature info for '{featureName}': 0x{hr:X8}" +
                    (errorMessage != null ? $" - {errorMessage}" : string.Empty));
            }

            var featureInfo = Marshal.PtrToStructure<DismFeatureInfo>(featureInfoPtr);

            var isInstalled = featureInfo.State == DismPackageFeatureState.Installed ||
                            featureInfo.State == DismPackageFeatureState.InstallPending;

            return (new Schema
            {
                Name = featureInfo.FeatureName ?? string.Empty,
                Exist = isInstalled ? null : false,
                DisplayName = featureInfo.DisplayName,
                Description = featureInfo.Description,
                State = featureInfo.State,
                IncludeAllSubFeatures = includeAllSubFeatures,
                Source = source
            }, featureInfo.RestartRequired);
        }
        finally
        {
            if (featureInfoPtr != IntPtr.Zero)
            {
                _ = DismApi.DismDelete(featureInfoPtr);
            }
            DismApi.CloseSession(session);
            DismApi.Shutdown();
        }
    }

    public static DismRestartType EnableFeature(string featureName, bool includeAllSubFeatures = false, string[]? sources = null)
    {
        IntPtr session = IntPtr.Zero;

        try
        {
            DismApi.Initialize();
            session = DismApi.OpenOnlineSession();

            var sourceCount = sources?.Length ?? 0;

            var hr = DismApi.DismEnableFeature(
                session,
                featureName,
                null,
                DismPackageIdentifier.None,
                limitAccess: false,
                sources,
                (uint)sourceCount,
                includeAllSubFeatures,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (hr != 0)
            {
                var errorMessage = DismApi.GetLastErrorMessage();
                throw new InvalidOperationException(
                    $"Failed to enable feature '{featureName}': 0x{hr:X8}" +
                    (errorMessage != null ? $" - {errorMessage}" : string.Empty));
            }

            var (_, restartType) = GetFeature(featureName, includeAllSubFeatures, sources);
            return restartType;
        }
        finally
        {
            DismApi.CloseSession(session);
            DismApi.Shutdown();
        }
    }

    public static DismRestartType DisableFeature(string featureName, bool removePayload = false)
    {
        IntPtr session = IntPtr.Zero;

        try
        {
            DismApi.Initialize();
            session = DismApi.OpenOnlineSession();

            var hr = DismApi.DismDisableFeature(
                session,
                featureName,
                null,
                removePayload,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (hr != 0 && hr != ERROR_NOT_FOUND)
            {
                var errorMessage = DismApi.GetLastErrorMessage();
                throw new InvalidOperationException(
                    $"Failed to disable feature '{featureName}': 0x{hr:X8}" +
                    (errorMessage != null ? $" - {errorMessage}" : string.Empty));
            }

            var (_, restartType) = GetFeature(featureName);
            return restartType;
        }
        finally
        {
            DismApi.CloseSession(session);
            DismApi.Shutdown();
        }
    }

    public static IEnumerable<Schema> EnumerateFeatures()
    {
        IntPtr session = IntPtr.Zero;
        IntPtr featuresPtr = IntPtr.Zero;

        try
        {
            DismApi.Initialize();
            session = DismApi.OpenOnlineSession();

            var hr = DismApi.DismGetFeatures(
                session,
                null,
                DismPackageIdentifier.None,
                out featuresPtr,
                out var count);

            if (hr != 0)
            {
                var errorMessage = DismApi.GetLastErrorMessage();
                throw new InvalidOperationException(
                    $"Failed to enumerate features: 0x{hr:X8}" +
                    (errorMessage != null ? $" - {errorMessage}" : string.Empty));
            }

            var structSize = Marshal.SizeOf<DismFeature>();

            for (var i = 0; i < count; i++)
            {
                var currentFeaturePtr = IntPtr.Add(featuresPtr, i * structSize);
                var feature = Marshal.PtrToStructure<DismFeature>(currentFeaturePtr);

                if (!string.IsNullOrEmpty(feature.FeatureName))
                {
                    var isInstalled = feature.State == DismPackageFeatureState.Installed ||
                                    feature.State == DismPackageFeatureState.InstallPending;

                    yield return new Schema
                    {
                        Name = feature.FeatureName,
                        Exist = isInstalled ? null : false
                    };
                }
            }
        }
        finally
        {
            if (featuresPtr != IntPtr.Zero)
            {
                _ = DismApi.DismDelete(featuresPtr);
            }
            DismApi.CloseSession(session);
            DismApi.Shutdown();
        }
    }
}
