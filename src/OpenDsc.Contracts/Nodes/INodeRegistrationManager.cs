// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Node registration and registration-setting operations.
/// </summary>
public interface INodeRegistrationManager
{
    Task<Lcm.RegisterNodeResponse> RegisterNodeAsync(
        Lcm.RegisterNodeRequest request,
        string? certificateThumbprint,
        string? certificateSubject,
        DateTimeOffset? certificateNotAfter,
        CancellationToken cancellationToken = default);

    Task<RegistrationSettingsSummary> GetRegistrationSettingsAsync(
        CancellationToken cancellationToken = default);
}
