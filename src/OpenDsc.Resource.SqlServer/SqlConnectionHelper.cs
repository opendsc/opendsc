// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.SqlServer.Management.Smo;

namespace OpenDsc.Resource.SqlServer;

/// <summary>
/// Helper class for creating SQL Server connections with support for both
/// Windows Authentication and SQL Authentication.
/// </summary>
public static class SqlConnectionHelper
{
    /// <summary>
    /// Creates a new Server connection with the specified authentication.
    /// </summary>
    /// <param name="serverInstance">The SQL Server instance name.</param>
    /// <param name="authentication">The authentication configuration.</param>
    /// <param name="connectTimeout">Connection timeout in seconds. Default is 30.</param>
    /// <returns>A configured Server object.</returns>
    /// <exception cref="ArgumentException">Thrown when SQL authentication is specified but username is missing.</exception>
    public static Server CreateConnection(
        string serverInstance,
        SqlAuthentication? authentication = null,
        int connectTimeout = 30)
    {
        var server = new Server(serverInstance);
        server.ConnectionContext.ConnectTimeout = connectTimeout;

        if (authentication is null || authentication.AuthType == SqlAuthType.Windows)
        {
            server.ConnectionContext.LoginSecure = true;
        }
        else if (authentication.AuthType == SqlAuthType.Sql)
        {
            if (string.IsNullOrEmpty(authentication.Username))
            {
                throw new ArgumentException("Username is required for SQL Server authentication.");
            }

            server.ConnectionContext.LoginSecure = false;
            server.ConnectionContext.Login = authentication.Username;
            server.ConnectionContext.Password = authentication.Password ?? string.Empty;
        }

        return server;
    }

    /// <summary>
    /// Safely disconnects from the server if connected.
    /// </summary>
    /// <param name="server">The server to disconnect from.</param>
    public static void SafeDisconnect(Server? server)
    {
        if (server?.ConnectionContext?.IsOpen == true)
        {
            server.ConnectionContext.Disconnect();
        }
    }
}
