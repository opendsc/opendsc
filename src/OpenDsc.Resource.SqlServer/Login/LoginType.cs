// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource.SqlServer.Login;

/// <summary>
/// Specifies the type of SQL Server login.
/// </summary>
public enum LoginType
{
    /// <summary>
    /// Login is based on Windows User.
    /// </summary>
    WindowsUser,

    /// <summary>
    /// Login is based on Windows Group.
    /// </summary>
    WindowsGroup,

    /// <summary>
    /// Login is based on SQL login.
    /// </summary>
    SqlLogin,

    /// <summary>
    /// Login is based on certificate.
    /// </summary>
    Certificate,

    /// <summary>
    /// Login is based on asymmetric key.
    /// </summary>
    AsymmetricKey,

    /// <summary>
    /// Login is based on External User.
    /// </summary>
    ExternalUser,

    /// <summary>
    /// Login is based on External Group.
    /// </summary>
    ExternalGroup
}
