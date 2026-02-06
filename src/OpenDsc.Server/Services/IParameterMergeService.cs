// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Services;

public interface IParameterMergeService
{
    Task<string?> MergeParametersAsync(Guid nodeId, Guid configurationId, CancellationToken cancellationToken = default);
}
