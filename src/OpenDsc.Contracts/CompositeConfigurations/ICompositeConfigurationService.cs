// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.CompositeConfigurations;

/// <summary>
/// Umbrella service interface for all composite configuration operations.
/// Implements all capability sub-interfaces; register via this umbrella in DI.
/// </summary>
public interface ICompositeConfigurationService
    : ICompositeConfigurationReader,
      ICompositeConfigurationManager,
      ICompositeConfigurationPermissions
{
}
