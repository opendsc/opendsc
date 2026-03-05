// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Lcm.Contracts;

/// <summary>
/// LCM service operating modes.
/// </summary>
public enum ConfigurationMode
{
    /// <summary>
    /// Monitor mode: run 'dsc config test' periodically and report drift.
    /// </summary>
    Monitor,

    /// <summary>
    /// Remediate mode: run 'dsc config test' and apply corrections as needed.
    /// </summary>
    Remediate
}
