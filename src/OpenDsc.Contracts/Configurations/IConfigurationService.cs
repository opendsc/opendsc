// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Umbrella service interface for all configuration operations.
/// Implements all capability sub-interfaces; register via this umbrella in DI.
/// </summary>
public interface IConfigurationService
    : IConfigurationReader,
      IConfigurationManager,
      IConfigurationFileManager,
      IConfigurationPermissions,
      IConfigurationSettings
{
}
