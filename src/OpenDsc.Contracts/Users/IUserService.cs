// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

/// <summary>
/// Combined service interface for reading and managing user accounts, passwords, and role assignments.
/// </summary>
public interface IUserService : IUserReader, IUserManager
{
}
