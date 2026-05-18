// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

/// <summary>
/// Type of user account.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// A regular user account.
    /// </summary>
    User,

    /// <summary>
    /// A service account for programmatic access.
    /// </summary>
    ServiceAccount
}
